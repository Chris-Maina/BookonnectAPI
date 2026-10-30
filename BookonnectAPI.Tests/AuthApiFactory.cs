using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using BookonnectAPI.Configuration;
using BookonnectAPI.Data;
using BookonnectAPI.Lib;
using BookonnectCore.DTO;
using BookonnectCore.Models;
using BookonnectCore.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace BookonnectAPI.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-only-signing-key-with-at-least-32-bytes";
    public const string Issuer = "https://issuer.example.test";
    public const string Audience = "https://client.example.test";
    private readonly SqliteConnection _database = new("Data Source=:memory:");

    public FakeUserAccounts Accounts { get; } = new();
    public FakeGoogleTokenValidator Google { get; } = new();
    public FakePreferences Preferences { get; } = new();
    public FakeMailLibrary Mail { get; } = new();

    public AuthApiFactory()
    {
        _database.Open();
        using var command = _database.CreateCommand();
        command.CommandText = """
            CREATE TABLE Users (
                ID INTEGER PRIMARY KEY, Name TEXT NOT NULL, Email TEXT NOT NULL,
                Image TEXT, Phone TEXT, Location TEXT);
            INSERT INTO Users (ID, Name, Email) VALUES (1, 'Legacy reader', 'legacy@example.test');
            """;
        command.ExecuteNonQuery();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JWTOptions.SectionName}:SecretKey"] = SigningKey,
                [$"{JWTOptions.SectionName}:Issuer"] = Issuer,
                [$"{JWTOptions.SectionName}:Audience"] = Audience,
                [$"{GoogleOptions.SectionName}:ClientId"] = "test-client.apps.googleusercontent.com",
                [$"{V2AuthorizationOptions.SectionName}:AdminUserIds:0"] = "2",
                ["VectorDB:Uri"] = "http://127.0.0.1:1",
                ["ApiKey:Gemini"] = "test-only-placeholder",
                ["RelationalDB:Uri"] = "Host=127.0.0.1;Database=unused;Username=unused;Password=unused"
            });
        });
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.RemoveAll<IUserAccountService>();
            services.RemoveAll<IPreferenceService>();
            services.RemoveAll<IMailLibrary>();
            services.RemoveAll<DbContextOptions<BookonnectContext>>();
            services.RemoveAll<BookonnectContext>();
            services.AddSingleton<IGoogleTokenValidator>(Google);
            services.AddSingleton<IUserAccountService>(Accounts);
            services.AddSingleton<IPreferenceService>(Preferences);
            services.AddSingleton<IMailLibrary>(Mail);
            services.AddDbContext<BookonnectContext>(options => options.UseSqlite(_database));
            services.AddSingleton(CompletedService.Create<IDatabaseSyncService>());
            services.AddSingleton(CompletedService.Create<IRelationalOperationsService>());
            services.AddSingleton(CompletedService.Create<IVectorOperationsService>());
        });
    }

    public string Token(int userId = 1, bool isV2User = true)
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITokenLibrary>().GetToken(userId, isV2User)!;
    }

    public static string CustomToken(
        string subject = "1", string? store = V2Authentication.IdentityStore,
        string issuer = Issuer, string audience = Audience, string key = SigningKey,
        bool expired = false, bool future = false, bool duplicateSubject = false, bool duplicateStore = false,
        bool missingExpiration = false)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, subject) };
        if (store != null) claims.Add(new(V2Authentication.IdentityStoreClaim, store));
        if (duplicateSubject) claims.Add(new(JwtRegisteredClaimNames.Sub, "2"));
        if (duplicateStore) claims.Add(new(V2Authentication.IdentityStoreClaim, V2Authentication.IdentityStore));
        var token = new JwtSecurityToken(issuer, audience, claims,
            notBefore: future ? DateTime.UtcNow.AddMinutes(5) : DateTime.UtcNow.AddHours(-2),
            expires: missingExpiration ? null :
                expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _database.Dispose();
    }
}

public sealed class FakeMailLibrary : IMailLibrary
{
    public List<BookonnectAPI.Models.Email> Emails { get; } = new();
    public Exception? Error { get; set; }

    public Task SendMail(BookonnectAPI.Models.Email email)
    {
        Emails.Add(email);
        return Error == null ? Task.CompletedTask : Task.FromException(Error);
    }
}

public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public GoogleJsonWebSignature.Payload Payload { get; set; } = new()
    {
        Subject = "google-subject",
        Email = "reader@example.test",
        EmailVerified = true,
        Name = "Reader",
        Picture = "https://example.test/avatar.png"
    };
    public Exception? Error { get; set; }
    public int Calls { get; private set; }

    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        Calls++;
        return Error == null
            ? Task.FromResult(Payload)
            : Task.FromException<GoogleJsonWebSignature.Payload>(Error);
    }
}

public sealed class FakeUserAccounts : IUserAccountService
{
    public Dictionary<int, User> Users { get; } = new()
    {
        [1] = new User { ID = 1, Name = "Reader", Email = "reader@example.test" },
        [2] = new User { ID = 2, Name = "Administrator", Email = "admin@example.test" }
    };
    public ExternalIdentity? LastIdentity { get; private set; }
    public bool IsNewUser { get; set; }
    public Exception? SignInError { get; set; }

    public Task<ExternalSignInResult> FindOrCreateExternalUserAsync(
        ExternalIdentity identity, CancellationToken cancellationToken)
    {
        LastIdentity = identity;
        return SignInError == null
            ? Task.FromResult(new ExternalSignInResult(Users[1], IsNewUser))
            : Task.FromException<ExternalSignInResult>(SignInError);
    }

    public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken) =>
        Task.FromResult(Users.GetValueOrDefault(userId));
}

public sealed class FakePreferences : IPreferenceService
{
    public Preference? Added { get; private set; }
    public int? QueriedUserId { get; private set; }
    public Preference Existing { get; set; } = new() { Id = 1, UserId = 1, BookId = "9781234567890" };

    public Task AddPreferenceAsync(Preference preference)
    {
        preference.Id = 3;
        Added = preference;
        return Task.CompletedTask;
    }

    public Task<Preference?> GetPreferenceByIdAsync(int preferenceId) => Task.FromResult<Preference?>(Existing);

    public Task<IEnumerable<Preference>?> GetPreferencesByUserIdAsync(int userId)
    {
        QueriedUserId = userId;
        return Task.FromResult<IEnumerable<Preference>?>(Array.Empty<Preference>());
    }

    public Task<IEnumerable<Preference>?> GetAllPreferencesAsync() => throw new NotSupportedException();
    public Task ProcessUpdatedPreferencesAsync(DateTime since) => throw new NotSupportedException();
    public Task UpdatePreferenceAsync(Preference preference) => throw new NotSupportedException();
    public Task DeletePreferenceAsync(int preferenceId) => throw new NotSupportedException();
}

public class CompletedService : DispatchProxy
{
    public static T Create<T>() where T : class => DispatchProxy.Create<T, CompletedService>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.ReturnType == typeof(Task)) return Task.CompletedTask;
        var type = targetMethod.ReturnType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = type.GenericTypeArguments[0];
            var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType)
                .Invoke(null, new[] { result });
        }
        throw new NotSupportedException(targetMethod.Name);
    }
}

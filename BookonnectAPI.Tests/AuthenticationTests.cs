using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookonnectAPI.Configuration;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using BookonnectCore.Exceptions;
using Google.Apis.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BookonnectAPI.Tests;

public sealed class AuthenticationTests
{
    [Theory]
    [InlineData(false, HttpStatusCode.OK)]
    [InlineData(true, HttpStatusCode.Created)]
    public async Task SignInUsesVerifiedIdentityAndIssuesAnIsolatedToken(bool isNew, HttpStatusCode status)
    {
        using var factory = new AuthApiFactory();
        factory.Accounts.IsNewUser = isNew;
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth",
            new { Provider = "Google", IdToken = "valid-google-token", UserId = 2, Role = "Admin" });

        Assert.Equal(status, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        if (isNew) Assert.EndsWith("/api/v2/Auth", response.Headers.Location?.ToString());
        var body = (await response.Content.ReadFromJsonAsync<UserToken>())!;
        Assert.Equal(1, body.ID);
        Assert.Equal("Reader", body.Name);
        Assert.NotNull(body.Token);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Token);
        Assert.Equal("1", jwt.Subject);
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == V2Authentication.IdentityStoreClaim && claim.Value == V2Authentication.IdentityStore);
        Assert.InRange(Math.Abs((jwt.ValidTo - body.Expires!.Value).TotalSeconds), 0, 1);
        Assert.Equal("google", factory.Accounts.LastIdentity?.Provider);
        Assert.Equal("google-subject", factory.Accounts.LastIdentity?.Subject);
        Assert.Equal("reader@example.test", factory.Accounts.LastIdentity?.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        using var profile = await client.GetAsync("/api/v2/Auth");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal("Reader", (await profile.Content.ReadFromJsonAsync<UserDTO>())?.Name);
        using var sync = await client.PostAsync("/api/v2/Sync/sync/full", null);
        Assert.Equal(HttpStatusCode.Forbidden, sync.StatusCode);
    }

    [Theory]
    [InlineData("apple", "token")]
    [InlineData("", "token")]
    [InlineData(null, "token")]
    [InlineData("google", "")]
    [InlineData("google", "   ")]
    [InlineData("google", null)]
    public async Task InvalidSignInInputNeverReachesGoogleOrTheDatabase(string? provider, string? idToken)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth", new { Provider = provider, IdToken = idToken });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Google.Calls);
        Assert.Null(factory.Accounts.LastIdentity);
    }

    [Fact]
    public async Task OversizedIdTokensAreRejectedBeforeValidation()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth", new { IdToken = new string('a', 16385) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Google.Calls);
        Assert.Null(factory.Accounts.LastIdentity);
    }

    [Fact]
    public async Task VerifiedIdentitiesWithoutAProfileNameCanStillSignIn()
    {
        using var factory = new AuthApiFactory();
        factory.Google.Payload.Name = "";
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth", new { IdToken = "token" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("reader@example.test", factory.Accounts.LastIdentity?.Name);
    }

    [Theory]
    [InlineData("unverified")]
    [InlineData("missing-subject")]
    [InlineData("missing-email")]
    [InlineData("invalid-token")]
    public async Task InvalidGoogleIdentitiesCannotSignIn(string failure)
    {
        using var factory = new AuthApiFactory();
        switch (failure)
        {
            case "unverified": factory.Google.Payload.EmailVerified = false; break;
            case "missing-subject": factory.Google.Payload.Subject = ""; break;
            case "missing-email": factory.Google.Payload.Email = ""; break;
            case "invalid-token": factory.Google.Error = new InvalidJwtException("invalid test token"); break;
        }
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth", new { IdToken = "token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(factory.Accounts.LastIdentity);
        Assert.DoesNotContain("\"token\":", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExistingEmailRequiresExplicitLinkingInsteadOfIssuingAToken()
    {
        using var factory = new AuthApiFactory();
        factory.Accounts.SignInError = new AccountLinkRequiredException();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth", new { IdToken = "token" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("verified account linking", body);
        Assert.DoesNotContain("\"token\":", body);
    }

    [Fact]
    public async Task SignInDoesNotDiscloseDatabaseFailures()
    {
        using var factory = new AuthApiFactory();
        factory.Accounts.SignInError = new InvalidOperationException("private database connection details");
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v2/Auth", new { IdToken = "token" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("missing-expiration")]
    [InlineData("future")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("legacy")]
    [InlineData("unknown-store")]
    [InlineData("subject")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("duplicate-subject")]
    [InlineData("duplicate-store")]
    [InlineData("malformed")]
    public async Task InvalidAccessTokensAreRejected(string failure)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var token = failure switch
        {
            "expired" => AuthApiFactory.CustomToken(expired: true),
            "missing-expiration" => AuthApiFactory.CustomToken(missingExpiration: true),
            "future" => AuthApiFactory.CustomToken(future: true),
            "issuer" => AuthApiFactory.CustomToken(issuer: "https://wrong.example.test"),
            "audience" => AuthApiFactory.CustomToken(audience: "https://wrong.example.test"),
            "signature" => AuthApiFactory.CustomToken(key: "different-test-signing-key-at-least-32-bytes"),
            "legacy" => AuthApiFactory.CustomToken(store: null),
            "unknown-store" => AuthApiFactory.CustomToken(store: "unknown"),
            "subject" => AuthApiFactory.CustomToken(subject: "not-an-id"),
            "zero" => AuthApiFactory.CustomToken(subject: "0"),
            "negative" => AuthApiFactory.CustomToken(subject: "-1"),
            "duplicate-subject" => AuthApiFactory.CustomToken(duplicateSubject: true),
            "duplicate-store" => AuthApiFactory.CustomToken(duplicateStore: true),
            _ => "not-a-token"
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync("/api/v2/Auth");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LegacyAndV2UsersWithTheSameIdCannotUseEachOthersTokens()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(isV2User: false));
        using var legacy = await client.GetAsync("/api/v1/Auth");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        Assert.Equal("Legacy reader", (await legacy.Content.ReadFromJsonAsync<UserDTO>())?.Name);
        using var v2WithLegacyToken = await client.GetAsync("/api/v2/Auth");
        Assert.Equal(HttpStatusCode.Unauthorized, v2WithLegacyToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var legacyWithV2Token = await client.GetAsync("/api/v1/Auth");
        Assert.Equal(HttpStatusCode.Unauthorized, legacyWithV2Token.StatusCode);
        using var v2 = await client.GetAsync("/api/v2/Auth");
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        Assert.Equal("Reader", (await v2.Content.ReadFromJsonAsync<UserDTO>())?.Name);
    }

    [Theory]
    [InlineData("/api/v2/Auth")]
    [InlineData("/api/v2/Books")]
    [InlineData("/api/v2/Preference")]
    [InlineData("/api/v2/Recommendations")]
    [InlineData("/api/v2/Sync/sync/status")]
    public async Task EveryV2ControllerRequiresV2Authentication(string path)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var anonymous = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(isV2User: false));
        using var legacy = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, legacy.StatusCode);
    }

    [Fact]
    public async Task DeletedUsersCannotKeepUsingTheirAccessToken()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        factory.Accounts.Users.Remove(1);
        using var response = await client.GetAsync("/api/v2/Auth");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v2/Books/search")]
    [InlineData("/api/v2/Recommendations")]
    public async Task AuthenticatedCoreUsersCanAccessV2CatalogEndpoints(string path)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PreferencesEnforceOwnershipOnReadsAndWrites()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var otherUserWrite = await client.PostAsJsonAsync("/api/v2/Preference",
            new { UserId = 2, BookId = "9781234567890", IsLiked = true });
        Assert.Equal(HttpStatusCode.Forbidden, otherUserWrite.StatusCode);
        Assert.Null(factory.Preferences.Added);
        factory.Preferences.Existing.UserId = 2;
        using var otherUserRead = await client.GetAsync("/api/v2/Preference/1");
        Assert.Equal(HttpStatusCode.NotFound, otherUserRead.StatusCode);
        factory.Preferences.Existing.UserId = 1;
        using var ownRead = await client.GetAsync("/api/v2/Preference/1");
        Assert.Equal(HttpStatusCode.OK, ownRead.StatusCode);
        using var list = await client.GetAsync("/api/v2/Preference?userId=2");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, factory.Preferences.QueriedUserId);
        using var ownWrite = await client.PostAsJsonAsync("/api/v2/Preference",
            new { UserId = 1, BookId = "9781234567890", IsLiked = true, Book = new { Title = "Untrusted book" } });
        Assert.Equal(HttpStatusCode.Created, ownWrite.StatusCode);
        Assert.Equal(1, factory.Preferences.Added?.UserId);
        Assert.Null(factory.Preferences.Added?.Book);
    }

    [Theory]
    [InlineData(1, HttpStatusCode.Forbidden)]
    [InlineData(2, HttpStatusCode.OK)]
    public async Task OnlyConfiguredAdministratorsCanSync(int userId, HttpStatusCode status)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(userId));
        using var response = await client.PostAsync("/api/v2/Sync/sync/full", null);
        Assert.Equal(status, response.StatusCode);
    }

    [Fact]
    public async Task AnEmptyAdministratorAllowlistDeniesSync()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        factory.Services.GetRequiredService<IOptions<V2AuthorizationOptions>>().Value.AdminUserIds = Array.Empty<int>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(2));
        using var response = await client.PostAsync("/api/v2/Sync/sync/full", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BrowserClientsCanReadAuthenticationFailures()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/Auth");
        request.Headers.Add("Origin", AuthApiFactory.Audience);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(AuthApiFactory.Audience, response.Headers.GetValues("Access-Control-Allow-Origin"));
    }
}


using BookonnectAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BookonnectAPI.Lib;
using BookonnectAPI.Services;
using BookonnectAPI.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using BookonnectCore.Extensions;
using BookonnectAPI.Filters;
using BookonnectCore.Services.Interfaces;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Override logging defaults
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register options.
builder.Services.AddOptions<JWTOptions>()
    .Bind(builder.Configuration.GetSection(JWTOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) &&
        !string.IsNullOrWhiteSpace(options.Audience), "JWT issuer and audience are required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey) &&
        Encoding.UTF8.GetByteCount(options.SecretKey) >= 32, "JWT signing key must be at least 32 bytes.")
    .ValidateOnStart();
builder.Services.Configure<MailSettingsOptions>(builder.Configuration.GetSection(MailSettingsOptions.SectionName));
builder.Services.AddOptions<GoogleOptions>()
    .Bind(builder.Configuration.GetSection(GoogleOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Google client ID is required.")
    .ValidateOnStart();
builder.Services.AddOptions<V2AuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(V2AuthorizationOptions.SectionName))
    .Validate(options => options.AdminUserIds != null && options.AdminUserIds.All(id => id > 0),
        "Administrator user IDs must be positive.")
    .ValidateOnStart();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

// Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0); // Default version if not specified
    opt.AssumeDefaultVersionWhenUnspecified = true; // Use default if no version provided
    opt.ReportApiVersions = true; // Include version info in response headers
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddControllers(options =>
{
    options.Filters.Add<CoreExceptionFilter>();
    options.InputFormatters.Insert(0, BookonnectJPIF.GetJsonPatchInputFormatter());
});

// Configure core library
builder.Services.AddBookonnectCore(builder.Configuration);

builder.Services.AddScoped<IBookApiService, BookApiService>();
builder.Services.AddScoped<IRecommendationApiService, RecommendationApiService>();
builder.Services.AddScoped<ITokenLibrary, TokenLibrary>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IMailLibrary, MailLibrary>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<BookonnectContext>(options =>
        options.UseSqlite("Data Source=./Data/Bookonnect.db"));
}
else
{
    var connectionString = builder.Configuration
        .GetConnectionString("AZURE_MYSQL_CONNECTIONSTRING")
        ?? throw new InvalidOperationException(
            "API MySQL connection string is not configured.");

    builder.Services.AddDbContext<BookonnectContext>(options =>
        options.UseMySQL(connectionString));
}

var jWTOptions = builder.Configuration.GetSection(JWTOptions.SectionName).Get<JWTOptions>()
    ?? throw new InvalidOperationException("JWT configuration is required.");
// CORS policy
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(jWTOptions.Audience)
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserClaimPolicy", policy =>
    {
        policy.Requirements.Add(new ClaimRequirement(ClaimTypes.NameIdentifier));
    });
    options.AddPolicy(V2Authentication.UserPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(V2Authentication.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(V2Authentication.IdentityStoreClaim, V2Authentication.IdentityStore);
    });
    options.AddPolicy(V2Authentication.AdminPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(V2Authentication.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(V2Authentication.IdentityStoreClaim, V2Authentication.IdentityStore);
        policy.AddRequirements(new AdminRequirement());
    });
});
// register authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, ClaimValueCheckHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();

// Authentication
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jWTOptions.Issuer,
    ValidAudience = jWTOptions.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jWTOptions.SecretKey))
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal!.HasClaim(claim => claim.Type == V2Authentication.IdentityStoreClaim))
                {
                    context.Fail("Invalid legacy access token.");
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer(V2Authentication.Scheme, options =>
    {
        options.MapInboundClaims = true;
        options.TokenValidationParameters = tokenValidationParameters.Clone();
        options.TokenValidationParameters.ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var storeClaims = context.Principal!.FindAll(V2Authentication.IdentityStoreClaim).ToArray();
                var userClaims = context.Principal.FindAll(ClaimTypes.NameIdentifier).ToArray();
                if (storeClaims.Length != 1 || storeClaims[0].Value != V2Authentication.IdentityStore ||
                    userClaims.Length != 1 || !int.TryParse(userClaims[0].Value,
                        NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
                {
                    context.Fail("Invalid v2 access token.");
                    return;
                }

                var userAccountService = context.HttpContext.RequestServices.GetRequiredService<IUserAccountService>();
                var user = await userAccountService.GetUserByIdAsync(userId, context.HttpContext.RequestAborted);
                if (user == null)
                {
                    context.Fail("Invalid v2 access token.");
                }
            }
        };
    });

// Add a named/typed http client
builder.Services.AddHttpClient("Safaricom", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://sandbox.safaricom.co.ke");
});
builder.Services.AddHttpClient<IGoogleBooksApiService, GoogleBooksApiService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bookonnect API",
        Version = "v1"
    });
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Bookonnect API",
        Version = "v2"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme}
            },
            new string[] { }
        }
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookonnect API v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Bookonnect API v2");
    });
}

// Enable CORS
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

public partial class Program { }

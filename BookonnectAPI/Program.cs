
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

var builder = WebApplication.CreateBuilder(args);

// Override logging defaults
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register options.
builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection(JWTOptions.SectionName));
builder.Services.Configure<MailSettingsOptions>(builder.Configuration.GetSection(MailSettingsOptions.SectionName));
builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection(GoogleOptions.SectionName));
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
    options.InputFormatters.Insert(0, BookonnectJPIF.GetJsonPatchInputFormatter());
});

// Configure core library
builder.Services.AddBookonnectCore(builder.Configuration);

builder.Services.AddScoped<IBookApiService, BookApiService>();
builder.Services.AddScoped<ITokenLibrary, TokenLibrary>();
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

JWTOptions? jWTOptions = new JWTOptions();
jWTOptions = builder.Configuration.GetSection(JWTOptions.SectionName).Get<JWTOptions>();
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
});
// register authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, ClaimValueCheckHandler>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
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

app.UseAuthentication();
app.UseAuthorization();
// Enable CORS
app.UseCors();

app.MapControllers();
app.Run();

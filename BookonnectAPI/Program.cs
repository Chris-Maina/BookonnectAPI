
using BookonnectAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BookonnectAPI.Lib;
using BookonnectAPI.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Override logging defaults
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to DI container.
builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection(JWTOptions.SectionName));
builder.Services.Configure<MailSettingsOptions>(builder.Configuration.GetSection(MailSettingsOptions.SectionName));
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, BookonnectJPIF.GetJsonPatchInputFormatter());
});
builder.Services.AddScoped<ITokenLibrary, TokenLibrary>();
builder.Services.AddScoped<IMpesaLibrary, MpesaLibrary>();
builder.Services.AddScoped<IMailLibrary, MailLibrary>();

// Connect to DB
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEntityFrameworkSqlite().AddDbContext<BookonnectContext>(options =>
        options.UseSqlite("Data Source=./Data/Bookonnect.db")
    );
}
else {
    builder.Services.AddDbContext<BookonnectContext>(options =>
        options.UseMySQL(builder.Configuration.GetConnectionString("AZURE_MYSQL_CONNECTIONSTRING")!));
}

JWTOptions? jWTOptions = new JWTOptions();
jWTOptions = builder.Configuration.GetSection(JWTOptions.SectionName).Get<JWTOptions>();
// CORS policy
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(jWTOptions.Audience, "https://bookonnect.vercel.app/")
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

// Authentication
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jWTOptions.Issuer,
            ValidAudience = jWTOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jWTOptions.SecretKey))
        };
    });

// Add a named http client
builder.Services.AddHttpClient("Safaricom", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://sandbox.safaricom.co.ke");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

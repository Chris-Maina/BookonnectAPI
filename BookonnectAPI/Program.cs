using BookonnectAPI.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BookonnectAPI.Lib;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Override logging defaults
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, BookonnectJPIF.GetJsonPatchInputFormatter());
});
builder.Services.AddScoped<ITokenLibrary, TokenLibrary>();
builder.Services.AddScoped<IMpesaLibrary, MpesaLibrary>();

// Connect to DB
var connectionString = builder.Configuration.GetConnectionString("WebApiDatabase");
var connection = new SqliteConnection(connectionString);
connection.Open();
builder.Services.AddEntityFrameworkSqlite().AddDbContext<BookonnectContext>(options =>
    options.UseSqlite(connectionString)
);

// CORS policy
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
        });
});

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration.GetValue<string>("Authentication:JWT:Issuer"),
            ValidAudience = builder.Configuration.GetValue<string>("Authentication:JWT:Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("Authentication:JWT:SecretKey")!))
        };
    });

// Add a named http client
builder.Services.AddHttpClient("Safaricom", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://sandbox.safaricom.co.ke");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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

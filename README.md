# BookonnectAPI

[![Build and deploy](https://github.com/Chris-Maina/BookonnectAPI/actions/workflows/main_bookconnect.yml/badge.svg)](https://github.com/Chris-Maina/BookonnectAPI/actions/workflows/main_bookconnect.yml)

An ASP.NET Core 8 Web API for book discovery, personalized reading recommendations, and a book-sharing marketplace.

## Features

- **Book discovery:** browse and search the v2 catalog, and retrieve books by ISBN-13.
- **Recommendations:** find similar books and personalized suggestions using BookonnectCore's vector services.
- **Reading preferences:** record likes and retrieve preferences belonging to the authenticated user.
- **Google sign-in:** validate Google ID tokens and issue JWT access tokens with separate v1 and v2 identities.
- **Onboarding email:** welcome new v2 users with a message about recommendations based on their reading preferences.
- **Legacy marketplace:** v1 controllers cover books, carts, orders, payments, reviews, affiliates, and Cloudinary images.
- **Administration:** allowlisted Core users can import CSV data and synchronize relational and vector databases.

## Technology

The API uses C#, controller-based ASP.NET Core routing, Entity Framework Core, JWT bearer authentication, Google token validation, Swagger/OpenAPI, and MailKit.

The legacy API database uses SQLite in Development and MySQL outside Development. V2 delegates account, catalog, preference, and vector operations to the separate **BookonnectCore** project. Configuring the legacy SQLite database does not configure Core's databases.

## Prerequisites

- .NET 8 SDK.
- Git.
- A checkout of BookonnectCore beside this repository.
- A Google OAuth client ID for interactive sign-in.
- The database and vector services required by your BookonnectCore checkout for live v2 requests.
- SMTP settings for email delivery and Cloudinary settings when using legacy image endpoints.
- The .NET EF Core 8 CLI when managing migrations.

## Installation

Arrange the repositories as siblings; the API references `../BookonnectCore/BookonnectCore.csproj`:

```text
Projects/
├── BookonnectAPI/
│   ├── BookonnectAPI.sln
│   ├── BookonnectAPI/
│   └── BookonnectAPI.Tests/
└── BookonnectCore/
    └── BookonnectCore.csproj
```

Clone this API into that layout, then restore and build from its repository root:

```bash
git clone https://github.com/Chris-Maina/BookonnectAPI.git
cd BookonnectAPI
dotnet restore
dotnet build
```

### Configuration

Keep credentials in user-secrets or environment variables, not committed `appsettings*.json` files. Environment-variable names use `__` in place of `:` in configuration keys.

Set the authentication values for local development:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<google-oauth-client-id>" --project BookonnectAPI
dotnet user-secrets set "Authentication:JWT:Issuer" "http://localhost:5080" --project BookonnectAPI
dotnet user-secrets set "Authentication:JWT:Audience" "http://localhost:3000" --project BookonnectAPI
dotnet user-secrets set "Authentication:JWT:SecretKey" "<random-signing-key-at-least-32-bytes>" --project BookonnectAPI
```

Replace placeholders with your own values. The JWT audience must also match the browser application's allowed CORS origin.

| Environment variable | Purpose |
|---|---|
| `Authentication__Google__ClientId` | Google OAuth audience accepted for ID tokens. |
| `Authentication__JWT__Issuer` | Issuer expected in API access tokens. |
| `Authentication__JWT__Audience` | Access-token audience and allowed browser origin. |
| `Authentication__JWT__SecretKey` | Random JWT signing key of at least 32 UTF-8 bytes. |
| `Authentication__V2Authorization__AdminUserIds__0` | First approved Core administrator ID; use subsequent indices for more IDs. Omit for no administrators. |
| `ConnectionStrings__AZURE_MYSQL_CONNECTIONSTRING` | Legacy API MySQL connection string outside Development. |
| `MailSettings__Host`, `MailSettings__Port` | SMTP server and port. |
| `MailSettings__UserName`, `MailSettings__Password` | SMTP credentials. |
| `MailSettings__Name`, `MailSettings__EmailId` | Sender name and address; also used by legacy administrator data. |
| `Storage__CloudinaryURL` | Cloudinary connection URL for legacy image endpoints. |

Core configuration is loaded through `AddBookonnectCore`. The API test fixture supplies `RelationalDB:Uri`, `VectorDB:Uri`, and `ApiKey:Gemini`; consult your BookonnectCore checkout for their live connection formats, additional requirements, and database setup. These are separate from the legacy API connection string.

The API also exposes `Authentication:Google:BooksApiKey` and `Authentication:Google:GeminiApiKey` options for its Google integrations.

### Database setup

Prepare both data stores you intend to use:

- **Legacy API:** `BookonnectContext` connects to `./Data/Bookonnect.db` in Development, or the configured MySQL connection otherwise. The SQLite path is relative to the process working directory.
- **V2/Core:** apply BookonnectCore's migrations using that project's setup instructions before using v2 authentication.

For legacy EF operations, install the EF 8 CLI if needed and run from the API project directory so the relative SQLite path resolves consistently:

```bash
dotnet tool install --global dotnet-ef --version '8.*'
cd BookonnectAPI
dotnet ef migrations list --context BookonnectContext -- --environment Development
dotnet ef database update --context BookonnectContext -- --environment Development
cd ..
```

`database update` applies available migrations; it does not generate a missing schema. If your checkout has no legacy migrations, establish an appropriate initial migration for the target provider before updating an empty database. Do not treat these commands as a substitute for Core migrations.

### Run locally

From the repository root:

```bash
cd BookonnectAPI
dotnet run --no-launch-profile -- --environment Development --urls http://localhost:5080
```

Swagger UI is available at **http://localhost:5080/swagger**. OpenAPI documents are served at:

- `/swagger/v1/swagger.json`
- `/swagger/v2/swagger.json`

Swagger is enabled only in Development. Use HTTPS for deployed environments; the local HTTP URL above is for development only.

## Usage

### Sign in with Google

Obtain a Google ID token from your client application, then exchange it for an API access token:

```bash
curl -X POST http://localhost:5080/api/v2/Auth \
  -H 'Content-Type: application/json' \
  -d '{"provider":"google","idToken":"<google-id-token>"}'
```

The server validates the Google token's signature, issuer, audience, and expiry. A verified email and Google subject are required. The server uses the verified subject, not a client-supplied user ID, to find or create the Core user.

| Status | Meaning |
|---|---|
| `200` | Existing user signed in. |
| `201` | New user created and signed in. |
| `400` | Invalid request, unsupported provider, or missing token. |
| `401` | Invalid Google token or identity without a verified email/subject. |
| `409` | Existing email requires explicit account linking. |
| `500` | Unexpected server failure. |

Successful responses contain the Core user profile, `token`, and `expires`. Access tokens last one day. Automatic email-based account linking and a linking endpoint are not provided.

New users receive an onboarding email about book recommendations based on reading preferences. Returning users do not receive another welcome email. Delivery is awaited; failures are logged without preventing sign-in.

### Call authenticated endpoints

Use the returned API access token, not the Google ID token:

```bash
curl http://localhost:5080/api/v2/Auth \
  -H 'Authorization: Bearer <api-access-token>'

curl 'http://localhost:5080/api/v2/Books/search?SearchTerm=fiction' \
  -H 'Authorization: Bearer <api-access-token>'

curl http://localhost:5080/api/v2/Recommendations \
  -H 'Authorization: Bearer <api-access-token>'
```

### V2 endpoint reference

All paths below start with `/api/v2`.

| Method | Path | Purpose | Access |
|---|---|---|---|
| `POST` | `/Auth` | Exchange a Google ID token for an API token. | Anonymous |
| `GET` | `/Auth` | Retrieve the authenticated Core profile. | V2 user |
| `GET` | `/Books` | Browse the catalog with pagination. | V2 user |
| `GET` | `/Books/{ISBN13}` | Retrieve a book by ISBN-13. | V2 user |
| `GET` | `/Books/search?SearchTerm=...` | Search the catalog. | V2 user |
| `POST` | `/Preference` | Save a reading preference. | V2 user |
| `GET` | `/Preference` | List your preferences. | V2 user |
| `GET` | `/Preference/{id}` | Retrieve one of your preferences. | V2 user |
| `GET` | `/Recommendations` | Retrieve personalized recommendations. | V2 user |
| `GET` | `/Recommendations/{isbn13}` | Find similar books. | V2 user |
| `POST` | `/Sync/sync/csv` | Import a server-accessible CSV file. | V2 administrator |
| `POST` | `/Sync/sync/full` | Synchronize relational data to the vector database. | V2 administrator |
| `GET` | `/Sync/sync/status` | Retrieve synchronization status. | V2 administrator |

Preference writes accept `userId`, `bookId`, and `isLiked`. The user ID must match the authenticated Core user; other users' preferences cannot be read. Recommendations can be empty when the relevant user profile or book embeddings are unavailable.

V2 JWTs carry `identity_store=core` and cannot authenticate against legacy v1 endpoints. Legacy tokens cannot authenticate against v2 endpoints. Authenticated v2 requests also require a Core user that still exists. Sync access requires an explicitly allowlisted Core user ID; an empty allowlist denies everyone, and Google sign-in alone never grants administrator access.

Legacy routes use `/api/v1`. Consult the v1 Swagger document for marketplace request and response schemas; v1 and v2 user IDs are not interchangeable.

## Project structure

```text
BookonnectAPI/
├── Configuration/       Strongly typed settings
├── Controllers/         Legacy v1 endpoints
│   └── V2/              Core-backed v2 endpoints
├── Data/                Legacy EF context and entity configuration
├── DTO/                 API request and response contracts
├── Filters/             Core exception-to-HTTP mapping
├── Lib/                 Authentication, mail, payment, and external API services
├── Mappings/            Entity-to-DTO mapping
├── Models/              Legacy entities and shared models
├── Services/            V2 catalog and recommendation orchestration
└── Program.cs           Dependency injection and HTTP pipeline
BookonnectAPI.Tests/     Authentication and authorization integration tests
.github/workflows/       Azure build and deployment workflow
```

## Testing

Run from the repository root:

```bash
dotnet test
```

To run only authentication tests:

```bash
dotnet test BookonnectAPI.Tests/BookonnectAPI.Tests.csproj --filter FullyQualifiedName~AuthenticationTests
```

The xUnit tests use `WebApplicationFactory`, real API/JWT handlers, fake Google/Core/mail services, and an in-memory SQLite legacy database. They do not need external service credentials or send real email. BookonnectCore must still be checked out beside the API to compile.

Coverage includes sign-in, invalid identities and access tokens, v1/v2 token isolation, preference ownership, administrator authorization, and onboarding email behavior.

## Deployment

The [GitHub Actions workflow](.github/workflows/main_bookconnect.yml) runs on pushes to `main` and manual dispatch. It builds and tests in Release, publishes the API, packages a Linux EF migrations bundle, and deploys to the Azure Web App `bookconnect` using Azure OIDC authentication.

The workflow currently checks out only this repository. Its build also needs the sibling BookonnectCore project, so ensure that dependency is available on the runner before relying on deployment. The workflow packages the migration bundle but does not explicitly execute it or apply Core migrations.

Configure production credentials through Azure application settings or another secret store. Keep Swagger disabled outside Development, serve traffic over HTTPS, and apply the appropriate legacy and Core migrations as part of your deployment process.

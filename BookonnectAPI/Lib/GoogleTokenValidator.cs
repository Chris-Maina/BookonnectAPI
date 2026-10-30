using BookonnectAPI.Configuration;
using Microsoft.Extensions.Options;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace BookonnectAPI.Lib;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleOptions _options;

    public GoogleTokenValidator(IOptions<GoogleOptions> options)
    {
        _options = options.Value;
    }

    public Task<Payload> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idToken);
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Google client ID is not configured.");
        }

        return Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(
            idToken, new ValidationSettings { Audience = new[] { _options.ClientId } })
            .WaitAsync(cancellationToken);
    }
}

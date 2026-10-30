using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace BookonnectAPI.Lib;

public interface IGoogleTokenValidator
{
    Task<Payload> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

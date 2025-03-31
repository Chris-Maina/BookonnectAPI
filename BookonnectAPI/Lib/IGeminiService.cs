using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

public interface IGeminiService
{
	public Task<GeminiApiResponse?> GenerateContent(string prompt);
    public string GetRecommendationsPrompt(string email, IEnumerable<ReviewDTO> reviews);
}


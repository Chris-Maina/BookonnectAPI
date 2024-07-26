using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Lib;

// Adds support of JSON Patch to the System.Text.Json-based input formatter using Newtonsoft
public static class BookonnectJPIF
{
    public static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
	{
		var builder = new ServiceCollection()
			.AddLogging()
			.AddMvc()
            .AddNewtonsoftJson()
			.Services.BuildServiceProvider();

        /* Create an instance of NewtonsoftJsonPatchInputFormatter
         * insert it as a first entry of MvcOptions InputFormatters.
         * This order ensures that NewtonsoftJsonPatchInputFormatter processes JSON Patch requests and
         * The existing System.Text.Json-based input and formatters process all other JSON requests and responses.
         * */
        return builder
			.GetRequiredService<IOptions<MvcOptions>>()
			.Value
			.InputFormatters
			.OfType<NewtonsoftJsonPatchInputFormatter>()
			.First();

	}
}


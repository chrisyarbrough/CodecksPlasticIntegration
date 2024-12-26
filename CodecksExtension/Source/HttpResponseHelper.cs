namespace Xarbrough.CodecksPlasticIntegration;

using log4net;

static class HttpResponseHelper
{
	private static readonly ILog log = LogManager.GetLogger("Codecks");

	public static void Validate(HttpResponseMessage response)
	{
		try
		{
			response.EnsureSuccessStatusCode();
		}
		catch (HttpRequestException ex)
		{
			string remaining = GetHeaderValue(response, "x-ratelimit-Remaining");
			if (int.TryParse(remaining, out int remainingValue) && remainingValue == 0)
			{
				string limit = GetHeaderValue(response, "x-ratelimit-Limit");
				string reset = GetHeaderValue(response, "x-ratelimit-Reset");

				log.Error($"Rate limit exceeded: {remaining}/{limit}, Reset: {reset}");
			}

			// The body sometimes includes helpful information.
			string body = response.Content.ReadAsStringAsync().Result;
			throw new HttpRequestException(ex.Message + "\n" + body);
		}
	}

	private static string GetHeaderValue(HttpResponseMessage response, string headerName)
	{
		if (response.Headers.TryGetValues(headerName, out var values))
			return values.First();

		return string.Empty;
	}
}
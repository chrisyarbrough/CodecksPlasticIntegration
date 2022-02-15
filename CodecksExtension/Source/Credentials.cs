namespace Xarbrough.CodecksPlasticIntegration
{
	using Newtonsoft.Json;
	using System.Net;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Codecks uses the X-Account and X-Auth-Token headers
	/// to authenticate each sent web request. To retrieve
	/// the token, a login request must be sent first.
	/// </summary>
	public class CodecksCredentials
	{
		private string token;

		private readonly string account;
		private readonly string email;
		private readonly string password;

		public CodecksCredentials(
			string account,
			string email,
			string password)
		{
			this.account = account;
			this.email = email;
			this.password = password;
		}

		public void Login(WebClient client, string baseURL)
		{
			this.token = FetchRequestToken(client, baseURL);
		}

		public void Authenticate(WebClient client)
		{
			if (string.IsNullOrEmpty(token))
			{
				throw new WebException(
					"Connection failed. Check the issue tracker configuration.");
			}

			client.Headers["X-Account"] = account;
			client.Headers["X-Auth-Token"] = token;
		}

		private string FetchRequestToken(WebClient webClient, string baseURL)
		{
			string json = JsonConvert.SerializeObject(new
			{
				email,
				password
			});

			webClient.Headers["X-Account"] = account;
			webClient.Headers["Content-Type"] = "application/json";
			webClient.UploadString(baseURL + "dispatch/users/login", json);

			if (TryParseCookieToken(webClient.ResponseHeaders, out string token))
				return token;
			else
				throw new WebException("Failed to parse response set-cookie header.");
		}

		private static bool TryParseCookieToken(WebHeaderCollection headers, out string cookie)
		{
			string header = headers["set-cookie"];

			// Use manual parsing instead of bringing in an external dependency
			// such as System.Web.HttpCookie because it failed to load in the
			// beta version of PlasticX.
			Match match = Regex.Match(header, "at=(.*?);");

			if (match.Success)
			{
				cookie = match.Groups[1].Value;
				return true;
			}
			cookie = string.Empty;
			return false;
		}
	}
}

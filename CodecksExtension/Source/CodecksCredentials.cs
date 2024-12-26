namespace Xarbrough.CodecksPlasticIntegration;

using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

partial class CodecksCredentials
{
	public bool HasToken => !string.IsNullOrEmpty(token);

	private string token;

	private readonly string account;
	private readonly string email;
	private readonly string password;

	public CodecksCredentials(
		string account,
		string email,
		string password)
	{
		this.account = account ?? throw new ArgumentNullException(nameof(account));
		this.email = email ?? throw new ArgumentNullException(nameof(email));
		this.password = password ?? throw new ArgumentNullException(nameof(password));
	}

	public void Init(HttpClient client)
	{
		// All requests need the account.
		client.DefaultRequestHeaders.Add("X-Account", account);
	}

	public void Login(HttpClient client, string baseUrl)
	{
		string json = JsonConvert.SerializeObject(new
		{
			email,
			password
		});

		var content = new StringContent(json, Encoding.UTF8, "application/json");

		HttpResponseMessage response = client.PostAsync(baseUrl + "dispatch/users/login", content).Result;
		response.EnsureSuccessStatusCode();

		if (TryParseCookieToken(response.Headers, out string parsedToken))
			token = parsedToken;
		else
			throw new WebException("Failed to parse response set-cookie header.");
	}

	private static bool TryParseCookieToken(HttpHeaders headers, out string cookie)
	{
		cookie = string.Empty;

		// Use manual parsing instead of bringing in an external dependency
		// such as System.Web.HttpCookie because it failed to load in the
		// beta version of PlasticX.
		if (headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies))
		{
			foreach (string header in cookies)
			{
				// Use Regex to match the token pattern
				Match match = ExtractTokenRegex().Match(header);

				if (match.Success)
				{
					cookie = match.Groups[1].Value;
					return true;
				}
			}
		}

		return false;
	}

	public void Authenticate(HttpClient client)
	{
		if (!HasToken)
		{
			throw new WebException(
				"Connection failed. Check the issue tracker configuration.");
		}

		client.DefaultRequestHeaders.Add("X-Auth-Token", token);
	}

	[GeneratedRegex("at=(.*?);")]
	private static partial Regex ExtractTokenRegex();
}

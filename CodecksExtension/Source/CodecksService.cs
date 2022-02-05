namespace Xarbrough.CodecksPlasticIntegration
{
	using log4net;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using System;
	using System.IO;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Interfaces with the Codecks web API.
	/// </summary>
	public class CodecksService
	{
		private string token;
		private string url;
		private string accountName;

		private static readonly ILog log = LogManager.GetLogger("CodecksService");

		/// <summary>
		/// Before any other call to the API, an authorized user must log into the service.
		/// </summary>
		/// <param name="url">The codecks base urls. Most likely 'https://api.codecks.io/'.</param>
		/// <param name="accountName">The organization to which the user belongs.</param>
		public void Login(
			string url,
			string accountName,
			string email,
			string password)
		{
			// Because users configure the base-url in the preferences UI,
			// this input data should be sanitized for robustness.
			if (url.EndsWith("/") == false)
				url += "/";

			byte[] data = SerializeCredentials(email, password);
			this.token = FetchRequestToken(url, accountName, data);
			this.url = url;
			this.accountName = accountName;
		}

		private static byte[] SerializeCredentials(string email, string password)
		{
			string json = JsonConvert.SerializeObject(new
			{
				email,
				password
			});
			return Encoding.UTF8.GetBytes(json);
		}

		/// <summary>
		/// Codecks uses a simple cookie token for authentication.
		/// </summary>
		/// <exception cref="CookieException"></exception>
		private static string FetchRequestToken(
			string url, string accountName, byte[] credentialsData)
		{
			var request = (HttpWebRequest)WebRequest.Create(
				url + "dispatch/users/login");

			request.Headers["X-Account"] = accountName;
			request.ContentType = "application/json";
			request.Method = "POST";

			var stream = request.GetRequestStream();
			stream.Write(credentialsData, 0, credentialsData.Length);

			using (var response = (HttpWebResponse)request.GetResponse())
			{
				if (TryParseToken(response, out string token))
					return token;
			}

			// This point will most likely never be reached because the web request
			// will throw an exception earlier if authentication fails.
			throw new CookieException();
		}

		private static bool TryParseToken(HttpWebResponse response, out string cookie)
		{
			string header = response.Headers["set-cookie"];

			// To avoid bringing in more dependencies (e.g. System.Web.HttpCookie),
			// use manual parsing instead.
			Match match = Regex.Match(header, "at=(.*?);");

			if (match.Success)
			{
				cookie = match.Groups[1].Value;
				return true;
			}
			cookie = string.Empty;
			return false;
		}

		public dynamic PostQuery(string query)
		{
			ThrowIfNotLoggedIn();

			var request = (HttpWebRequest)WebRequest.Create(url);
			SetupValidRequest(request);
			WriteBody(request, query);

			using (var response = (HttpWebResponse)request.GetResponse())
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				string json = reader.ReadToEnd();
				return JObject.Parse(json);
			}
		}

		public void PostCardUpdate(string body)
		{
			ThrowIfNotLoggedIn();

			var request = (HttpWebRequest)WebRequest.Create(
				url + "dispatch/cards/update");

			SetupValidRequest(request);
			WriteBody(request, body);

			using (var response = (HttpWebResponse)request.GetResponse())
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				string json = reader.ReadToEnd();
				if (json.Length == 0)
					log.Error("Failed to update card status: " + response.StatusDescription);
			}
		}

		private void SetupValidRequest(WebRequest request)
		{
			request.Headers = new WebHeaderCollection
			{
				{ "X-Account", accountName },
				{ "X-Auth-Token", token },
			};
			request.ContentType = "application/json";
			request.Method = "POST";
		}

		private static void WriteBody(WebRequest request, string payload)
		{
			var stream = request.GetRequestStream();
			byte[] bytes = Encoding.UTF8.GetBytes(payload);
			stream.Write(bytes, 0, bytes.Length);
		}

		private void ThrowIfNotLoggedIn()
		{
			if (string.IsNullOrEmpty(token))
			{
				throw new InvalidOperationException(
					"Requests can only be sent after successful login.");
			}
		}

		public string LoadAccountID()
		{
			ThrowIfNotLoggedIn();
			const string query = "{\"query\":{\"_root\":[{\"account\":[\"name\",\"id\"]}]}}";
			return PostQuery(query)._root.account;
		}
	}
}

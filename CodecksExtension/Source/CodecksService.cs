namespace Xarbrough.CodecksPlasticIntegration;

using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

/// <summary>
/// Interfaces with the Codecks web API.
/// </summary>
public sealed class CodecksService : IDisposable
{
	private const string baseUrl = "https://api.codecks.io/";

	private readonly CodecksCredentials credentials;
	private readonly HttpClient client;

	public CodecksService(CodecksCredentials credentials)
	{
		client = new HttpClient();
		this.credentials = credentials;
		credentials.Init(client);
	}

	/// <summary>
	/// Before any other call to the API, an authorized user must log into the service.
	/// </summary>
	public void Login()
	{
		credentials.Login(client, baseUrl);
	}

	public IEnumerable<Card> GetPendingCards(Query query)
	{
		string queryText = query.Build();
		return LoadCardObjects(queryText);
	}

	public Card GetCard(int accountSeq)
	{
		string query = GetQuery("GetCard.json");
		query = query.Replace("<ACCOUNT_SEQ>", accountSeq.ToString());
		return LoadCardObjects(query).First();
	}

	public IEnumerable<Card> GetCards(IEnumerable<string> accountSeqs)
	{
		string query = GetQuery("GetCard.json");
		query = query.Replace("<ACCOUNT_SEQ>", string.Join(",", accountSeqs));
		return LoadCardObjects(query);
	}

	public string GetAccountId()
	{
		string query = GetQuery("GetAccountId.json");
		dynamic result = SendJsonRequest(query);
		return result._root.account;
	}

	public IEnumerable<User> GetAllUsers(string accountId)
	{
		string query = GetQuery("GetAllUsers.json");
		query = query.Replace("<ACCOUNT>", accountId);
		dynamic result = SendAuthenticatedJsonRequest(query);
		foreach (JProperty prop in result.userEmail)
		{
			yield return new User(
				(string)prop.Value["userId"],
				(string)prop.Value["email"]);
		}
	}

	public string GetUserEmail(string userId)
	{
		if (string.IsNullOrEmpty(userId))
			return string.Empty;

		string query = GetQuery("GetUserEmail.json");
		query = query.Replace("<USER>", userId);
		dynamic result = SendJsonRequest(query);
		string emailId = result.user[userId].primaryEmail;
		return result.userEmail[emailId].email;
	}

	public void SetCardStatusToStarted(string cardGuid)
	{
		// The card id is the full-length guid, not to confuse with the accountSeq.
		UploadString(
			baseUrl + "dispatch/cards/update",
			"{\"id\":\"" + cardGuid + "\",\"status\":\"started\"}");
	}

	private IEnumerable<Card> LoadCardObjects(string query)
	{
		dynamic result = SendAuthenticatedJsonRequest(query);

		if (result.card == null)
			yield break;

		foreach (JProperty card in result.card)
			yield return card.Value.ToObject<Card>();
	}

	private string UploadString(string url, string payload)
	{
		var content = new StringContent(payload, Encoding.UTF8, "application/json");
		HttpResponseMessage response = client.PostAsync(url, content).Result;
		RateLimitHelper.Validate(response);
		response.EnsureSuccessStatusCode();
		return response.Content.ReadAsStringAsync().Result;
	}

	private dynamic SendAuthenticatedJsonRequest(string jsonPayload)
	{
		// There seem to be special cases in which 'Connect' is not
		// called for the extension (e.g. when switching between task-on-branch
		// and task-on-changeset mode). In these cases, the user might
		// not be logged in, but service calls are being issued.
		if (credentials.HasToken == false)
			Login();

		credentials.Authenticate(client);
		return SendJsonRequest(jsonPayload);
	}

	private dynamic SendJsonRequest(string jsonPayload)
	{
		string response = UploadString(baseUrl, jsonPayload);
		return JObject.Parse(response);
	}

	private static string GetQuery(string fileName) => QueryProvider.GetQuery(fileName);

	public static string GetCardBrowserUrl(string account, string idLabel)
	{
		// There are several ways to display a card in the web app:
		// Within the deck:
		// https://mysubdomain.codecks.io/decks/105-preproduction/card/1w4-start-documentation

		// Or as a single card on the hand:
		// https://mysubdomain.codecks.io/card/1w4-start-documentation

		// Conveniently, a short URL is also supported:
		// https://mysubdomain.codecks.io/card/1w4

		return "https://" + account + ".codecks.io/card/" + idLabel;
	}

	public void Dispose() => client.Dispose();
}
namespace Xarbrough.CodecksPlasticIntegration;

using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

/// <summary>
/// Interfaces with the Codecks web API.
/// </summary>
sealed class CodecksService : IDisposable
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
		return GetCards(new[] { accountSeq.ToString() }).First();
	}

	public IEnumerable<Card> GetCards(IEnumerable<string> accountSeqs)
	{
		string query = Query.Load("GetCard.json");
		query = query.Replace("<ACCOUNT_SEQ>", string.Join(",", accountSeqs));
		return LoadCardObjects(query);
	}

	public string GetAccountId()
	{
		string query = Query.Load("GetAccountId.json");
		dynamic result = SendJsonRequest(query);
		return result._root.account;
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
		{
			var cardObject = card.Value.ToObject<Card>();

			// TODO: inconsistency: GetCard will not have the user, hence only an id returned,
			// but GetPendingCards will use the user name.
			if (cardObject.Assignee != null && result.user != null)
			{
				dynamic user = result.user[cardObject.Assignee];
				string name = user.name;
				string fullName = user.fullName;
				cardObject.Assignee = string.IsNullOrEmpty(fullName) ? name : fullName;
			}

			yield return cardObject;
		}
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
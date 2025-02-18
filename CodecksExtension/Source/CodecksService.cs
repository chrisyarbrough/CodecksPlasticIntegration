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
		credentials.Login(this);
	}

	public IEnumerable<Card> GetPendingCards(PendingCardsQuery query)
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

	public void SetCardStatusToStarted(string cardGuid)
	{
		// The card id is the full-length guid, not to confuse with the accountSeq.
		PostJson("dispatch/cards/update",
			$"{{\"id\":\"{cardGuid}\",\"status\":\"started\"}}");
	}

	private IEnumerable<Card> LoadCardObjects(string query)
	{
		dynamic result = SendAuthenticatedJsonRequest(query);

		if (result.card == null)
			yield break;

		foreach (JProperty cardProperty in result.card)
		{
			dynamic cardJson = cardProperty.Value;

			string userName = string.Empty;
			if (cardJson.assignee != null)
			{
				dynamic user = result.user[cardJson.assignee.ToString()];
				string name = user.name;
				string fullName = user.fullName;
				userName = string.IsNullOrEmpty(fullName) ? name : fullName;
			}

			yield return new Card
			{
				CardId = cardJson.cardId,
				AccountSeq = cardJson.accountSeq,
				Title = cardJson.title,
				Content = cardJson.content,
				Status = cardJson.status,
				Assignee = userName
			};
		}
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

		HttpResponseMessage response = PostJson(string.Empty, jsonPayload);
		string content = response.Content.ReadAsStringAsync().Result;
		return JObject.Parse(content);
	}

	public HttpResponseMessage PostJson(string url, string payload)
	{
		var content = new StringContent(payload, Encoding.UTF8, "application/json");
		HttpResponseMessage response = client.PostAsync(baseUrl + url, content).Result;
		HttpResponseHelper.Validate(response);
		return response;
	}

	public void Dispose() => client.Dispose();
}
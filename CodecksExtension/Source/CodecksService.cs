using System.Text;

namespace Xarbrough.CodecksPlasticIntegration;

using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;

/// <summary>
/// Interfaces with the Codecks web API.
/// </summary>
public class CodecksService
{
	private const string baseURL = "https://api.codecks.io/";

	private readonly CodecksCredentials credentials;
	private readonly HttpClient client;
	private readonly QueryProvider queryProvider = new QueryProvider();

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
		credentials.Login(client, baseURL);
	}

	public IEnumerable<Card> GetPendingCards()
	{
		string query = GetQueryFromFile("GetPendingCards.json");
		return LoadCardObjects(query);
	}

	/// <summary>
	/// Fetches cards that are assigned to the given user id (not email).
	/// </summary>
	public IEnumerable<Card> GetPendingCards(string userId)
	{
		string query = GetQueryFromFile("GetPendingCardsWithAssignee.json");
		query = query.Replace("<ASSIGNEE>", userId);
		return LoadCardObjects(query);
	}

	public Card GetCard(int accountSeq)
	{
		string query = GetQueryFromFile("GetCard.json");
		query = query.Replace("<ACCOUNT_SEQ>", accountSeq.ToString());
		return LoadCardObjects(query).First();
	}

	public IEnumerable<Card> GetCards(IEnumerable<string> accountSeqs)
	{
		string query = GetQueryFromFile("GetCard.json");
		query = query.Replace("<ACCOUNT_SEQ>", string.Join(",", accountSeqs));
		return LoadCardObjects(query);
	}

	public string GetAccountId()
	{
		string query = GetQueryFromFile("GetAccountId.json");
		dynamic result = SendJsonRequest(query);
		return result._root.account;
	}

	public IEnumerable<User> GetAllUsers(string accountId)
	{
		string query = GetQueryFromFile("GetAllUsers.json");
		query = query.Replace("<ACCOUNT>", accountId);
		dynamic result = SendAuthenticatedJsonRequest(query);
		foreach (JProperty prop in result.userEmail)
		{
			yield return new User(
				(string)prop.Value["userId"],
				(string)prop.Value["email"]);
		}
	}

	public string FindUserIdByMail(string accountID, string email)
	{
		// This could be improved by using a more powerful query
		// which directly finds the user by email on the server.
		string query = GetQueryFromFile("GetAllUsers.json");
		query = query.Replace("<ACCOUNT>", accountID);

		dynamic data = SendAuthenticatedJsonRequest(query);
		foreach (JProperty property in data.userEmail)
		{
			string mail = (string)property.Value["email"];
			if (mail == email)
			{
				return (string)property.Value["userId"];
			}
		}
		throw new WebException($"Failed to find user by mail: {email}");
	}

	public string GetUserEmail(string userId)
	{
		if (string.IsNullOrEmpty(userId))
			return string.Empty;

		string query = GetQueryFromFile("GetUserEmail.json");
		query = query.Replace("<USER>", userId);
		dynamic result = SendJsonRequest(query);
		string emailID = result.user[userId].primaryEmail;
		return result.userEmail[emailID].email;
	}

	public void SetCardStatusToStarted(string cardGuid)
	{
		// The card id is the full-length guid, not to confuse with the accountSeq.
		UploadString(
			baseURL + "dispatch/cards/update",
			"{\"id\":\"" + cardGuid + "\",\"status\":\"started\"}");
	}

	private IEnumerable<Card> LoadCardObjects(string query)
	{
		dynamic result = SendAuthenticatedJsonRequest(query);
		foreach (JProperty card in result.card)
			yield return card.Value.ToObject<Card>();
	}

	private string UploadString(string url, string payload)
	{
		var content = new StringContent(payload, Encoding.UTF8, "application/json");
		HttpResponseMessage response = client.PostAsync(url, content).Result;
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
		string response = UploadString(baseURL, jsonPayload);
		return JObject.Parse(response);
	}

	private string GetQueryFromFile(string fileName)
	{
		return queryProvider.GetQuery(fileName);
	}

	public static string GetCardBrowserURL(string account, string idLabel)
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
}

namespace Xarbrough.CodecksPlasticIntegration.Tests;

[Ignore("Run manually only.")]
public class CodecksServiceTests
{
	private CodecksService service;
	private const string email = "";

	[OneTimeSetUp]
	public void BeforeAll()
	{
		var credential = new CodecksCredentials(account: "", email, password: "");
		service = new CodecksService(credential);
	}

	[SetUp]
	public void BeforeEach()
	{
		// Avoid rate limiting.
		Thread.Sleep(3000);
	}

	[Test]
	public void Login()
	{
		service.Login();
	}

	[Test]
	public void TestConnection()
	{
		// service.Login();
		// Doesn't need the X-Auth-Token.
		Assert.Greater(service.GetAccountId().Length, 0);
	}

	[Test]
	public void GetPendingCards()
	{
		service.Login();
		var result = service.GetPendingCards();
		Assert.Greater(result.Count(), 0);
	}

	[Test]
	public void GetPendingCardsWithAssignee()
	{
		service.Login();
		Thread.Sleep(1000);

		// This is a special case: Plastic gives use the email of the user that is logged into Plastic (e.g. Unity account),
		// but we want to filter by the Codecks account email, which the user has configured in the extension settings.

		string accountId = service.GetAccountId();
		string userId = service.FindUserIdByMail(accountId, email);
		var cards = service.GetPendingCards(userId).ToArray();

		Assert.Greater(cards.Length, 0);
	}

	[Test]
	public void GetAllUsers()
	{
		service.Login();
		string accountId = service.GetAccountId();
		var users = service.GetAllUsers(accountId).ToArray();
		Assert.Greater(users.Length, 0);
	}
}

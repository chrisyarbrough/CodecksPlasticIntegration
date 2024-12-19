namespace Xarbrough.CodecksPlasticIntegration.Tests;

using Microsoft.Extensions.Configuration;

public class CodecksServiceTests
{
	private CodecksService service;
	private string email;

	[OneTimeSetUp]
	public void BeforeAll()
	{
		try
		{
			IConfigurationRoot configuration = new ConfigurationBuilder()
				.AddUserSecrets<CodecksServiceTests>(optional: false)
				.Build();

			email = configuration["Codecks:Email"];
			string account = configuration["Codecks:Account"];
			string password = configuration["Codecks:Password"];

			var credential = new CodecksCredentials(account, email, password);
			service = new CodecksService(credential);
		}
		catch (Exception)
		{
			Assert.Inconclusive("Configuration is not valid. Skipping all tests. Please setup dotnet user-secrets.");
		}
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
		// Doesn't need the X-Auth-Token.
		Assert.Greater(service.GetAccountId().Length, 0);
	}

	[Test]
	public void GetPendingCards()
	{
		service.Login();
		var result = service.GetPendingCards().ToArray();
		Assert.Greater(result.Length, 0);
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
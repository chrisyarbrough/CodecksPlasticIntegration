namespace Xarbrough.CodecksPlasticIntegration.Tests;

using FluentAssertions;
using Microsoft.Extensions.Configuration;

[Category("EndToEnd"), Explicit]
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

	[TearDown]
	public void AfterEach()
	{
		// Avoid rate limiting.
		Thread.Sleep(5000);
	}

	[Test, Order(1)]
	public void Login()
	{
		service.Login();
	}

	[Test]
	public void TestConnection()
	{
		// Doesn't need the X-Auth-Token.
		string accountId = service.GetAccountId();
		TestContext.WriteLine(accountId);
		accountId.Should().NotBeNullOrEmpty();
	}

	[Test]
	public void GetCard()
	{
		service.Login();
		int accountSeq = new CardIdConverter().SeqToInt("113");
		Card card = service.GetCard(accountSeq);
		TestContext.WriteLine(card.ToString());
		card.Should().NotBeNull();
	}

	[Test]
	[TestCase("CodecksPlasticIntegration", "Bugs", "***REMOVED***")]
	[TestCase("CodecksPlasticIntegration", "", "***REMOVED***")]
	[TestCase("CodecksPlasticIntegration", "", "")]
	[TestCase("", "Bugs", "***REMOVED***")]
	[TestCase("", "", "")]
	public void FullQueryWithVariables(string project, string deck, string assigneeEmail)
	{
		var query = new Query
		{
			ProjectName = project,
			DeckTitle = deck,
			AssigneeEmail = assigneeEmail
		};

		service.Login();
		Card[] cards = service.GetPendingCards(query).ToArray();
		cards.Should().NotBeEmpty();
	}
}
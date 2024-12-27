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

	[Test]
	public void TestConnection()
	{
		var result = service.GetPendingCards(new PendingCardsQuery()).ToArray();
		TestContext.WriteLine(result.Length);
		result.Should().NotBeEmpty();
	}

	[Test]
	public void GetCard()
	{
		int accountSeq = new CardIdConverter().SeqToInt("113");
		Card card = service.GetCard(accountSeq);
		TestContext.WriteLine(card.ToString());
		card.Should().NotBeNull();
	}

	[Test]
	[TestCase("CodecksPlasticIntegration", "Bugs", true)]
	[TestCase("CodecksPlasticIntegration", "", true)]
	[TestCase("CodecksPlasticIntegration", "", false)]
	[TestCase("", "Bugs", true)]
	[TestCase("", "", false)]
	public void FullQueryWithVariables(string project, string deck, bool useEmail)
	{
		var query = new PendingCardsQuery
		{
			ProjectName = project,
			DeckTitle = deck,
			AssigneeEmail = useEmail ? email : null
		};

		Card[] cards = service.GetPendingCards(query).ToArray();
		cards.Should().NotBeEmpty();
	}
}
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
	public void GetPendingCards()
	{
		service.Login();
		Card[] result = service.GetPendingCards().ToArray();
		TestContext.WriteLine(string.Join(',', result.Select(c => c.ToString())));
		result.Should().NotBeEmpty();
	}

	[Test]
	public void GetPendingCardsWithAssignee()
	{
		service.Login();

		// This is a special case: Plastic gives use the email of the user that is logged into Plastic (e.g. Unity account),
		// but we want to filter by the Codecks account email, which the user has configured in the extension settings.
		string accountId = service.GetAccountId();
		string userId = service.FindUserIdByMail(accountId, email);
		var cards = service.GetPendingCards(userId: userId).ToArray();
		cards.Should().NotBeEmpty();
	}

	[Test]
	public void GetPendingCardsByDeck()
	{
		service.Login();
		(string id, string project) deck = service.GetDeck("Art");
		IEnumerable<Card> cards = service.GetPendingCards(deckId: deck.id).ToArray();
		cards.Should().NotBeEmpty();
	}

	[Test]
	public void GetPendingCardsWithAssigneeAndDeck()
	{
		service.Login();

		// This is a special case: Plastic gives use the email of the user that is logged into Plastic (e.g. Unity account),
		// but we want to filter by the Codecks account email, which the user has configured in the extension settings.
		string accountId = service.GetAccountId();
		string userId = service.FindUserIdByMail(accountId, email);
		(string id, string project) deck = service.GetDeck("Bugs");
		var cards = service.GetPendingCards(userId: userId, deckId: deck.id).ToArray();
		cards.Should().NotBeEmpty();
	}

	[Test]
	public void GetAllUsers()
	{
		service.Login();
		string accountId = service.GetAccountId();
		var users = service.GetAllUsers(accountId).ToArray();
		users.Should().NotBeEmpty();
	}

	[Test]
	public void GetDecks()
	{
		service.Login();
		var decks = service.GetDecks().ToArray();
		decks.Should().NotBeEmpty();
	}

	[Test]
	public void GetDeck()
	{
		service.Login();
		(string id, string project) deck = service.GetDeck("Bugs");
		deck.Should().NotBeNull();
		deck.id.Should().NotBeEmpty();
		deck.project.Should().NotBeEmpty();
	}
}
namespace Xarbrough.CodecksPlasticIntegration;

using Newtonsoft.Json;

/// <summary>
/// A data transfer object which represents
/// the relevant properties of a Codecks card.
/// </summary>
public record Card
{
	/// <summary>
	/// A guid, e.g. '2e8ec154-521f-11ec-be97-07520a644149'
	/// </summary>
	[JsonProperty("cardId")]
	public readonly string CardId;

	/// <summary>
	/// A number, e.g. '123'. It seems this number
	/// is unique within the account and is incremented
	/// sequentially for every new card.
	/// Strongly related to the card display label
	/// (no terminology is known) which looks like e.g. '1w4'.
	/// </summary>
	[JsonProperty("accountSeq")]
	public readonly int AccountSeq;

	/// <summary>
	/// Plain text. Could also be called card header or name.
	/// </summary>
	[JsonProperty("title")]
	public readonly string Title;

	/// <summary>
	/// Plain text. Could also be called card body or description.
	/// </summary>
	[JsonProperty("content")]
	public readonly string Content;

	/// <summary>
	/// Examples:
	/// not_started (could be open, but also reviewed or blocked)
	/// done
	/// started
	/// </summary>
	[JsonProperty("status")]
	public readonly string Status;

	/// <summary>
	/// The user guid assigned to this card.
	/// </summary>
	[JsonProperty("assignee")]
	public readonly string Assignee;


	[JsonProperty("deck")]
	public readonly string Deck;

	/// <summary>
	/// Initializes a new instance of the <see cref="Card"/> record.
	/// </summary>
	[JsonConstructor]
	public Card(string cardId, int accountSeq, string title, string content, string status, string assignee, string deck)
	{
		CardId = cardId;
		AccountSeq = accountSeq;
		Title = title;
		Content = content;
		Status = status;
		Assignee = assignee;
		Deck = deck;
	}
}
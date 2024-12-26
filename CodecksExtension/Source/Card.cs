namespace Xarbrough.CodecksPlasticIntegration;

/// <summary>
/// A data transfer object which represents
/// the relevant properties of a Codecks card.
/// </summary>
record Card
{
	/// <summary>
	/// A guid, e.g. '2e8ec154-521f-11ec-be97-07520a644149'
	/// </summary>
	public string CardId { get; init; }

	/// <summary>
	/// A number, e.g. '123'. It seems this number
	/// is unique within the account and is incremented
	/// sequentially for every new card.
	/// Strongly related to the card display label
	/// (no terminology is known) which looks like e.g. '1w4'.
	/// </summary>
	public int AccountSeq { get; init; }

	/// <summary>
	/// Plain text. Could also be called card header or name.
	/// </summary>
	public string Title { get; init; }

	/// <summary>
	/// Plain text. Could also be called card body or description.
	/// </summary>
	public string Content { get; init; }

	/// <summary>
	/// Examples:
	/// not_started (could be open, but also reviewed or blocked)
	/// done
	/// started
	/// </summary>
	public string Status { get; init; }

	/// <summary>
	/// The user name of the assignee.
	/// </summary>
	/// <remarks>
	/// Can be null if not assigned. Set from a more complex structure after deserializing the card.
	/// </remarks>
	public string Assignee { get; init; }
}
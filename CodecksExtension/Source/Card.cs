namespace Xarbrough.CodecksPlasticIntegration
{
	/// <summary>
	/// A data transfer object which represents 
	/// the relevant properties of a Codecks card.
	/// </summary>
	public struct Card
	{
		/// <summary>
		/// A guid, e.g. '2e8ec154-521f-11ec-be97-07520a644149'
		/// </summary>
		public string cardId;

		/// <summary>
		/// A number, e.g. '123'. It seems this number
		/// is unique within the account and is incremented
		/// sequentially for every new card.
		/// Strongly related to the card display label
		/// (no terminology is known) which looks like e.g. '1w4'.
		/// </summary>
		public int accountSeq;

		/// <summary>
		/// Plain text. Could also be called card header or name.
		/// </summary>
		public string title;

		/// <summary>
		/// Plain text. Could also be called card body or description.
		/// </summary>
		public string content;

		/// <summary>
		/// Examples: 'started', 'not_started'.
		/// </summary>
		public string status;

		/// <summary>
		/// The user guid assigned to this card.
		/// </summary>
		public string assignee;
	}
}

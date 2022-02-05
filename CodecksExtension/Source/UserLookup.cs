namespace Xarbrough.CodecksPlasticIntegration
{
	using System.Collections.Generic;

	/// <summary>
	/// Maps bidirectionally between email and user guid.
	/// </summary>
	public class UserLookup
	{
		private readonly Dictionary<string, string> emailToID = new Dictionary<string, string>();
		private readonly Dictionary<string, string> idToEmail = new Dictionary<string, string>();

		public void Clear()
		{
			emailToID.Clear();
			idToEmail.Clear();
		}

		public void Add(string id, string mail)
		{
			emailToID[mail] = id;
			idToEmail[id] = mail;
		}

		public string EmailToID(string email) => emailToID[email];

		public string IDToEmail(string userID)
		{
			if (userID == null)
				return null;

			return idToEmail[userID];
		}
	}
}

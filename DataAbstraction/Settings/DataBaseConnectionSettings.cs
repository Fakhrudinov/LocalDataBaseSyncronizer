namespace DataAbstraction.Settings
{
	public class DataBaseConnectionSettings
	{
		public string Server { get; set; }
		public int Port { get; set; }
		public string Database { get; set; }
		public string UserId { get; set; }
		public string Password { get; set; }
		public bool IsSource { get; set; }
	}
}

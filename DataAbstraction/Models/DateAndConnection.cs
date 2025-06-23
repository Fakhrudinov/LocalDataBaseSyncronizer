namespace DataAbstraction.Models
{
	public class DateAndConnection
	{
		public DateTime ? EventDate { get; set; }
		public string Connection { get; set; } = string.Empty;
		public bool IsSuccess { get; set; } = true;
	}
}

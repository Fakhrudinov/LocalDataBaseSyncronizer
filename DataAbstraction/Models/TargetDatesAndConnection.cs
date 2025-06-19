namespace DataAbstraction.Models
{
	public class TargetDatesAndConnection
	{
		public DateTime ? TargetDate { get; set; }
		public string Connection { get; set; } = string.Empty;
		public bool IsSuccess { get; set; } = true;
	}
}

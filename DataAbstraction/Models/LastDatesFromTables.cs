namespace DataAbstraction.Models
{
	public class LastDatesFromTables
	{
		public DateAndConnection SourceData { get; set; } = new DateAndConnection();
		public List<DateAndConnection> TargetDatesAndConnections { get; set; } = new List<DateAndConnection>();
		public DateTime Pointer { get; set; } = DateTime.MaxValue;
	}
}

namespace DataAbstraction.Models
{
	public class LastDatesFromTables
	{
		public DateTime ? SourceDate { get; set; }
		public List<TargetDatesAndConnection> TargetDatesAndConnections { get; set; } = new List<TargetDatesAndConnection>();
		public bool IsSuccess { get; set; } = true;

		public DateTime Pointer { get; set; } = DateTime.MaxValue;
	}
}

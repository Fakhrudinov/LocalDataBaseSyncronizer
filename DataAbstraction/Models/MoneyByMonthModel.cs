namespace DataAbstraction.Models
{
	public class MoneyByMonthModel
	{
		//(`event_date`, `total_in`, `month_in`, `dividend`, `dosrochnoe`, `deals_sum`, `brok_comission`, `money_sum`)
		public DateTime EventDate { get; set; }
		public decimal? TotalIn { get; set; }
		public decimal? MonthIn { get; set; }
		public decimal? Divident { get; set; }
		public decimal? Dosrochnoe { get; set; }
		public decimal? DealsSum { get; set; }
		public decimal? BrokComission { get; set; }
		public decimal? MoneySum { get; set; }
	}
}

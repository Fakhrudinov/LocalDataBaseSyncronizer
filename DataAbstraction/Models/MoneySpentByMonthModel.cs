namespace DataAbstraction.Models
{
	public class MoneySpentByMonthModel
	{
		//`money_spent_by_month` 
		//(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`)
		public DateTime EventDate { get; set; }
		public decimal? Total { get; set; }
		public decimal? Appartment { get; set; }
		public decimal? Electricity { get; set; }
		public decimal? Internet { get; set; }
		public decimal? Phone { get; set; }
	}
}

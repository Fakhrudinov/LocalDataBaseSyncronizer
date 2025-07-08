namespace DataAbstraction.Models
{
	public class BankDepositModel
	{
		//`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`, `percent`, `summ`, `income_summ`
		public int Id { get; set; }
		public short IsOpen { get; set; }
		public DateTime EventDate { get; set; }
		public DateTime DateClose { get; set; }
		public string Name { get; set; }
		public int PlaceName { get; set; }
		public decimal Percent { get; set; }
		public decimal Summ { get; set; }
		public decimal ? IncomeSumm { get; set; }

	}
}

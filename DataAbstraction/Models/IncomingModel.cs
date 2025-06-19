namespace DataAbstraction.Models
{
	public class IncomingModel : SeccodeAndSecboard
	{
		//(`id`, `event_date`, `seccode`, `secboard`, `category`, `value`, `comission`)
		public DateTime EventDate { get; set; }
		public int Category { get; set; }
		public decimal Value { get; set; }
		public decimal ? Comission { get; set; }
	}
}

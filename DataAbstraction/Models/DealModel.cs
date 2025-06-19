namespace DataAbstraction.Models
{
	public class DealModel : SeccodeAndSecboard
	{
		//(`id`, `event_date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`)
		public DateTime EventDate { get; set; }
		public decimal AvPrice { get; set; }
		public int Pieces { get; set; }
		public decimal? Comission { get; set; }
		public decimal? NKD { get; set; }
	}
}

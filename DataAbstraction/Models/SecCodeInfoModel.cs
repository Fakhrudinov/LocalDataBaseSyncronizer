namespace DataAbstraction.Models
{
	public class SecCodeInfoModel : SeccodeAndSecboard
	{
		//(`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
		public string Name { get; set; }
		public string FullName { get; set; }
		public string ISIN {  get; set; }
		public DateTime ? ExpiredDate { get; set; }
	}
}

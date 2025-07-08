namespace DataAbstraction.Models
{
	public class WishListModel
	{
		//(`seccode`, `wish_level`, `description`)
		public string SecCode { get; set; }
		public int Level { get; set; }
		public string ? Description { get; set; }
	}
}

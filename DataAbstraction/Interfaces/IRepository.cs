using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
	public interface IRepository
	{
		Task<List<DealModel>?> GetDealsOlderThanDate(DateTime pointer, string connection);
		Task<List<IncomingModel>?> GetIncomingsOlderThanDate(DateTime pointer, string _sourceConnectStr);
		Task<DateTime?> GetLastDateFromTable(string connectionString, string tableName);
		Task<List<WishLevelModel>?> GetWishLevels(string connection);
		Task<int> PostDataToTableDeals(List<DealModel> dataForTarget, string connection);
		Task<int> PostDataToTableIncoming(List<IncomingModel> dataItems, string connection);
		Task<int> PutDataToTableWishLevels(List<WishLevelModel> wishTarget, string connection);
	}
}

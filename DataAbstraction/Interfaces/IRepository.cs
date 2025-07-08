using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
	public interface IRepository
	{
		Task<int> DeleteFromDateWithSkipList(string connection, string tableName, DateOnly yearAgo, string rowsToDelete);
		Task<int> DeleteFromBankDepositsByIdWithSkipList(
			string connection, 
			int minimumSourceId, 
			string idToSkip);
		Task<int> DeleteFromTableSecCodeInfo(string connection, string rowsToDelete);
		Task<int> DeleteFromTableWishList(string connection, string rowsToDelete);
		Task<List<BankDepositModel>?> GetBankDepositsLastRows(string connection, int recordsToPast);
		Task<List<DealModel>?> GetDealsOlderThanDate(DateTime pointer, string connection);
		Task<List<IncomingModel>?> GetIncomingsOlderThanDate(DateTime pointer, string _sourceConnectStr);
		Task<DateTime?> GetLastDateFromTable(string connectionString, string tableName);
		Task<List<MoneyByMonthModel>?> GetMoneyByMonthAllRowsOlderThanDate(string connection, DateOnly yearAgo);
		Task<List<MoneySpentByMonthModel>?> GetMoneySpentByMonthAllRowsOlderThanDate(string connection, DateOnly yearAgo);
		Task<List<SecCodeInfoModel>?> GetSecCodeInfo(string connection);
		Task<List<WishLevelModel>?> GetWishLevels(string connection);
		Task<List<WishListModel>?> GetWishList(string connection);
		Task<int> PostDataToTableDeals(List<DealModel> dataForTarget, string connection);
		Task<int> PostDataToTableIncoming(List<IncomingModel> dataItems, string connection);
		Task<int> PutDataToTableBankDeposits(string connection, List<BankDepositModel> sourceBankDeposits);
		Task<int> PutDataToTableMoneyByMonth(string connection, List<MoneyByMonthModel> sourceMoneyByMonth);
		Task<int> PutDataToTableMoneySpentByMonth(string connection, List<MoneySpentByMonthModel> sourceMoneySpent);
		Task<int> PutDataToTableSecCodeInfo(string connection, List<SecCodeInfoModel> listToAdd);
		Task<int> PutDataToTableWishLevels(List<WishLevelModel> wishTarget, string connection);
		Task<int> PutDataToTableWishList(string connection, List<WishListModel> listToAdd);
	}
}


using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
	public interface ICommonRepository
	{
		Task<int> DeleteFromDateWithSkipList(string connection, string tableName, DateOnly yearAgo, string rowsToSkipDelete);
		Task<DateTime?> GetLastDateFromTable(string connection, string tableName);
		string? GetQueryTextByFolderAndFilename(string folderName, string queryFileName);
		Task<List<SecCodeInfoModel>?> GetSecCodeInfo(string connection);
		Task<List<WishLevelModel>?> GetWishLevels(string connection);
		Task<List<WishListModel>?> GetWishList(string connection);
	}
}

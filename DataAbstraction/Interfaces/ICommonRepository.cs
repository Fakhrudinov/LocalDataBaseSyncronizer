
using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
	public interface ICommonRepository
	{
		Task<DateTime?> GetLastDateFromTable(string connection, string tableName);
		string? GetQueryTextByFolderAndFilename(string folderName, string queryFileName);
		Task<List<WishLevelModel>?> GetWishLevels(string connection);
	}
}

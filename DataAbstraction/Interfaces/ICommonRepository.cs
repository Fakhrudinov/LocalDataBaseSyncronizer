
namespace DataAbstraction.Interfaces
{
	public interface ICommonRepository
	{
		Task<DateTime?> GetLastDateBySqlQuery(string connectionString, string query);
		string? GetQueryTextByFolderAndFilename(string folderName, string queryFileName);
	}
}

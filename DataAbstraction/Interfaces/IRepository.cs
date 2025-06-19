using DataAbstraction.Models;

namespace DataAbstraction.Interfaces
{
	public interface IRepository
	{
		Task<List<IncomingModel>?> GetIncomingsOlderThanDate(DateTime pointer, string _sourceConnectStr);
		Task<DateTime?> GetLastDateFromTable(string connectionString, string tableName);
		Task<int> PostDataToTableIncoming(List<IncomingModel> dataItems, string connection);
	}
}


namespace DataAbstraction.Interfaces
{
	public interface ILogicCore
	{
		Task<bool> CheckDataBaseIsAccessible();
		Task<List<string>> FillAbsentDataAtAllDB();
	}
}

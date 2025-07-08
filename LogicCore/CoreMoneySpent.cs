using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using System.Text;


namespace LogicCore
{
	internal class CoreMoneySpent
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;
		private LastDatesFromTables _connections;

		private List<string>? _jobLog;

		internal CoreMoneySpent(IRepository repository, ILogger<Core> logger, LastDatesFromTables connections)
		{
			_repository = repository;
			_logger = logger;
			_connections = connections;
		}

		internal async Task<List<string>> CheckAndFixMoneySpentByMonth()
		{
			_jobLog = new List<string>();
			DateTime today = DateTime.Now.AddYears(-1);
			DateOnly yearAgo = new DateOnly(today.Year, today.Month, 1);
			/// GET rows from source money_spent_by_month
			///		where event_date < (yearAgo)
			/// if fail break <---------
			/// 
			/// prepare list of dates from source to NOT IN delete list
			/// 
			/// foreach target:
			/// remove possible double records:
			///		DELETE all rows	older than (yearAgo) and NOT in list of dates, received from source
			///		if fail - CONTINUE
			///	PUT data from source


			if (!_connections.SourceData.IsSuccess && _connections.SourceData.Connection == string.Empty)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'money_spent_by_month' is terminated - " +
					$"no connections string on start");
				_jobLog.Add($"Error! Update tables 'money_spent_by_month' is terminated - no connections string on start");
				return _jobLog;
			}

			/// GET rows from source money_spent_by_month
			///		where event_date < today to 12 month in past
			/// if fail break <---------
			List<MoneySpentByMonthModel>? sourceMoneySpent = await _repository
				.GetMoneySpentByMonthAllRowsOlderThanDate(_connections.SourceData.Connection, yearAgo);
			if (sourceMoneySpent is null)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'money_spent_by_month' is terminated - no data from source BD");
				_jobLog.Add($"Error! Update tables 'money_spent_by_month' is terminated - no data from source BD");
				return _jobLog;
			}

			/// prepare list of dates from source to NOT IN delete list
			StringBuilder sb = new StringBuilder();
			foreach (MoneySpentByMonthModel item in sourceMoneySpent)
			{
				sb.Append(",'" + item.EventDate.ToString("yyyy-MM-01")+ "'");
			}
			sb.Remove(0, 1);
			string rowsToDelete = sb.ToString();

			/// foreach target:
			foreach (DateAndConnection target in _connections.TargetDatesAndConnections)
			{
				/// remove possible double records:
				///		DELETE all rows	older than (yearAgo) and NOT in list of dates, received from source
				///		if fail - CONTINUE

				string delete = string.Empty;
				int deletefromDBCount = await _repository.DeleteFromDateWithSkipList(
					target.Connection,
					"money_spent_by_month",
					yearAgo,
					rowsToDelete);
				if (deletefromDBCount == -1)
				{
					_logger.LogInformation($"Error! Delete items from table 'money_spent_by_month' " +
						$"failed for " + target.Connection);
					delete = $"Delete exceeded items from table 'money_spent_by_month' failed!";
				}


				///	PUT data from source
				string update = string.Empty;
				int addToDBCount = await _repository.PutDataToTableMoneySpentByMonth(target.Connection, sourceMoneySpent);
				if (addToDBCount == 0 || addToDBCount == -1)
				{
					_logger.LogInformation($"Error! Update table 'money_spent_by_month' failed for " + target.Connection);
					update = $"Update table 'money_spent_by_month' failed! ";
				}


				if (update.Length > 0 || delete.Length > 0)
				{
					_jobLog.Add($"Error! {update}{delete} Connection = '{target.Connection}'");
				}
				else
				{
					_jobLog.Add($"Update table 'money_spent_by_month' succeed for '{target.Connection}'");
				}
			}


			return _jobLog;
		}
	}
}

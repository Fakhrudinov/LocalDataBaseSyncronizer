using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LogicCore
{
	internal class CoreBankDeposits
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;
		private LastDatesFromTables _connections;

		private List<string>? _jobLog;
		private readonly int recordsToPast = 12;

		internal CoreBankDeposits(IRepository repository, ILogger<Core> logger, LastDatesFromTables connections)
		{
			_repository = repository;
			_logger = logger;
			_connections = connections;
		}

		internal async Task<List<string>> CheckAndFixBankDeposits()
		{
			_jobLog = new List<string>();
			/// GET last 'recordsToPast' rows from source bank_deposits
			///		if fail break <---------
			///	
			/// get minimal id
			/// prepare list of id from source
			/// 
			/// foreach target:
			/// remove rows where id > minimal id in sourceBankDeposits AND id not in list of ids in sourceBankDeposits
			///		if fail - CONTINUE
			///	PUT data from source


			if (!_connections.SourceData.IsSuccess && _connections.SourceData.Connection == string.Empty)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'bank_deposits' is terminated - " +
					$"no connections string on start");
				_jobLog.Add($"Error! Update tables 'bank_deposits' is terminated - no connections string on start");
				return _jobLog;
			}

			/// GET last 'recordsToPast' rows from source bank_deposits
			///		if fail break <---------
			List<BankDepositModel>? sourceBankDeposits = await _repository
				.GetBankDepositsLastRows(_connections.SourceData.Connection, recordsToPast);
			if (sourceBankDeposits is null)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'bank_deposits' is terminated - no data from source BD");
				_jobLog.Add($"Error! Update tables 'bank_deposits' is terminated - no data from source BD");
				return _jobLog;
			}


			/// get minimal id
			/// prepare list of id from source
			int minimumSourceId = int.MaxValue;
			StringBuilder sb = new StringBuilder();
			foreach (BankDepositModel item in sourceBankDeposits)
			{
				sb.Append("," + item.Id);

				if (item.Id < minimumSourceId)
				{
					minimumSourceId = item.Id;
				}
			}
			sb.Remove(0, 1);
			string idToSkip = sb.ToString();


			/// foreach target:
			foreach (DateAndConnection target in _connections.TargetDatesAndConnections)
			{
				/// remove rows where id > minimal id in sourceBankDeposits AND id not in list of ids in sourceBankDeposits
				///		if fail - CONTINUE
				string delete = string.Empty;
				int deletefromDBCount = await _repository.DeleteFromBankDepositsByIdWithSkipList(
					target.Connection,
					minimumSourceId,
					idToSkip);
				if (deletefromDBCount == -1)
				{
					_logger.LogInformation($"Error! Delete items from table 'bank_deposits' " +
						$"failed for " + target.Connection);
					delete = $"Delete exceeded items from table 'bank_deposits' failed!";
				}



				///	PUT data from source
				string update = string.Empty;
				int addToDBCount = await _repository.PutDataToTableBankDeposits(target.Connection, sourceBankDeposits);
				if (addToDBCount == 0 || addToDBCount == -1)
				{
					_logger.LogInformation($"Error! Update table 'bank_deposits' failed for " + target.Connection);
					update = $"Update table 'bank_deposits' failed! ";
				}


				if (update.Length > 0 || delete.Length > 0)
				{
					_jobLog.Add($"Error! {update}{delete} Connection = '{target.Connection}'");
				}
				else
				{
					_jobLog.Add($"Update table 'bank_deposits' succeed for '{target.Connection}'");
				}
			}

			return _jobLog;
		}
	}
}

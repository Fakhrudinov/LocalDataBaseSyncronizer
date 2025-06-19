using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LogicCore
{
	public class Core : ILogicCore
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;
		private readonly string _sourceConnectStr;
		private readonly List<string> _targetConnectStrList;
		private List<string> _jobLog;

		public Core (
			IRepository repository, 
			ILogger<Core> logger,
			IConfiguration configuration)
		{
			_repository = repository;
			_logger = logger;

			// get connections
			IConfiguration _config = configuration;
			List<DataBaseConnectionSettings>? dbSettingsList = _config
				.GetSection("DataBaseList")
				.Get<List<DataBaseConnectionSettings>>();

			if (dbSettingsList is not null && dbSettingsList.Count > 0)
			{
				_targetConnectStrList = new List<string>();

				foreach (DataBaseConnectionSettings dbSettings in dbSettingsList)
				{
					string connectSettings = $"" +
						$"Server={dbSettings.Server};" +
						$"User ID={dbSettings.UserId};" +
						$"Password={dbSettings.Password};" +
						$"Port={dbSettings.Port};" +
						$"Database={dbSettings.Database}";

					if (dbSettings.IsSource)
					{
						_sourceConnectStr = connectSettings;
					}
					else
					{
						_targetConnectStrList.Add(connectSettings);
					}
				}
			}
			else
			{
				_logger.LogError("Connections setting to DB is not avaliable! " +
					"Check appsettings.json (prod or dev) DataBaseList ");

				throw new Exception("Connections setting to DB is not avaliable! " +
					"Check appsettings.json (prod or dev) section - DataBaseList ");
			}
		}

		public async Task<bool> CheckDataBaseIsAccessible()
		{
			DateTime ? dateTime = await _repository.GetLastDateFromTable(_sourceConnectStr, "incoming");

			if (dateTime is null || dateTime.Equals(DateTime.MinValue))
			{
				return false;
			}

			return true;
		}

		public async Task<List<string>> FillAbsentDataAtAllDB()
		{
			_jobLog = new List<string>();
			await FillAbsentDataAtIncoming();

			return _jobLog;
		}

		private async Task FillAbsentDataAtIncoming()
		{
			/// work with incoming table ------------------------------------------------------------
			/// get DateTime source
			///		if error - scip work <---------
			/// 
			/// DateTime pointer - how many data will be requested to fix any tables
			///		foreach targets:
			///			if target DateTime is less then source (and pointer)
			/// if pointer equal source
			///		- scip work <---------
			///  
			/// GET from source: data older then pointer
			/// foreach target 
			///		if DateTime.MinValue
			///			- scip work at this target<---------
			///		prepare data - remove data older then its pointer
			///		
			///	POST data



			/// work with incoming table
			/// get DateTime source
			///		if error - scip work <---------

			LastDatesFromTables lastDatesFromTables = await GetLastDatesFromTable("incoming");
			if (!lastDatesFromTables.IsSuccess)
			{
				// break operation
				_logger.LogWarning("Error! Fill tables 'incoming' is terminated - no date from source BD");
				return;
			}


			/// if pointer equal source
			///		- scip work <---------
			if (lastDatesFromTables.SourceDate <= lastDatesFromTables.Pointer)
			{
				// break operation
				_logger.LogInformation("Fill tables 'incoming' is terminated - all tables has same data");
				_jobLog.Add("Fill tables 'incoming' is terminated - all tables has same data");
				return;
			}


			/// GET from source: data older then pointer
			/// foreach target 
			///		if DateTime.MinValue
			///			- scip work at this target<---------
			///		prepare data - remove data older then its pointer
			List<IncomingModel>? incomingsSource = await _repository.GetIncomingsOlderThanDate(
				lastDatesFromTables.Pointer, 
				_sourceConnectStr);
			if (incomingsSource is null)
			{
				// break operation
				_logger.LogInformation("Error! Fill tables 'incoming' is terminated - GetIncomingsOlderThanDate failed");
				_jobLog.Add("Error! Fill tables 'incoming' is terminated - GetIncomingsOlderThanDate failed");
				return;
			}

			foreach (TargetDatesAndConnection table in lastDatesFromTables.TargetDatesAndConnections)
			{
				if (!table.IsSuccess)
				{
					_logger.LogInformation("Error! Fill table 'incoming' not executed - " +
						"target date is not filled from " + table.Connection);
					_jobLog.Add("Error! Fill table 'incoming' not executed - " +
						"target date is not filled from " + table.Connection);
					continue;
				}

				if (table.TargetDate == lastDatesFromTables.SourceDate)
				{
					_jobLog.Add($"Fill table 'incoming' from '{table.Connection}' is not needed " +
						$"- table date({table.TargetDate}) is equal than source date({lastDatesFromTables.SourceDate})");
					continue;
				}

				if (table.TargetDate > lastDatesFromTables.SourceDate)
				{
					_jobLog.Add($"Error! Fill table 'incoming' from '{table.Connection}' is not needed " +
						$"- table date({table.TargetDate}) greater than source date({lastDatesFromTables.SourceDate})");
					continue;
				}

				// prepare data - remove data older then its pointer
				List<IncomingModel> dataForTarget = new List<IncomingModel>();
				foreach (IncomingModel incoming in incomingsSource)
				{
					if (incoming.EventDate > table.TargetDate)
					{
						dataForTarget.Add(incoming);
					}
				}

				if (dataForTarget.Count == 0)
				{
					_logger.LogInformation("Error! Fill table 'incoming' not executed - target date is not filled - " +
						"not any of data items is not older than table already has for " + table.Connection);
					_jobLog.Add("Error! Fill table 'incoming' not executed - target date is not filled - " +
						"not any of data items is not older than table already has for " + table.Connection);
					continue;
				}

				///	POST data
				int addToDBCount = await _repository.PostDataToTableIncoming(dataForTarget, table.Connection);
				if (addToDBCount == 0 || addToDBCount == -1)
				{
					_logger.LogInformation("Error! Fill table 'incoming' failed for " + table.Connection);
					_jobLog.Add("Error! Fill table 'incoming' failed for " + table.Connection);
				}
				else
				{
					_jobLog.Add($"Fill table 'incoming' succed for '{table.Connection}', filled {addToDBCount} rows");
				}
			}


		}

		private async Task<LastDatesFromTables> GetLastDatesFromTable(string tableName)
		{
			LastDatesFromTables result = new LastDatesFromTables();

			result.SourceDate = await _repository.GetLastDateFromTable(_sourceConnectStr, tableName);
			if (result.SourceDate is null || result.SourceDate.Equals(DateTime.MinValue))
			{
				_jobLog.Add(tableName + " GetLastDatesFromTable source error for connection to " + _sourceConnectStr);
				_logger.LogError(tableName + " GetLastDatesFromTable source error for connection to " + _sourceConnectStr);
				result.IsSuccess = false;

				return result;
			}
			else
			{
				_logger.LogDebug(tableName + " source GetLastDatesFromTable dt=" + result.SourceDate);
			}

			foreach (string connection in _targetConnectStrList)
			{
				TargetDatesAndConnection targetDT = new TargetDatesAndConnection 
				{ 
					Connection = connection
				};

				targetDT.TargetDate = await _repository.GetLastDateFromTable(connection, tableName);
				if (targetDT.TargetDate is null || targetDT.TargetDate.Equals(DateTime.MinValue))
				{
					_jobLog.Add(tableName + " GetLastDateFromTable target error for connection to " + connection);
					_logger.LogError(tableName + " GetLastDateFromTable target error for connection to " + connection);
					targetDT.IsSuccess = false;
				}
				else
				{
					_logger.LogDebug(tableName + " target GetLastDateFromTable dt=" + targetDT.TargetDate);
					if(targetDT.TargetDate < result.Pointer)
					{
						result.Pointer = (DateTime)targetDT.TargetDate;
					}
				}

				result.TargetDatesAndConnections.Add(targetDT);
			}

			return result;
		}
	}
}

using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

			CoreIncoming coreIncoming = new CoreIncoming(
				_repository,
				_logger,
				await GetLastDatesFromTable("incoming")
				);
			List<string> jobLogIncom = await coreIncoming.FillAbsentDataAtIncoming();
			_jobLog.AddRange(jobLogIncom);

			LastDatesFromTables latestDates = await GetLastDatesFromTable("deals");//will be reused at next requests
			CoreDeals coreDeals = new CoreDeals(
				_repository,
				_logger,
				latestDates
				);
			List<string> jobLogDeals = await coreDeals.FillAbsentDataAtDeals();
			_jobLog.AddRange(jobLogDeals);

			CoreWish coreWish = new CoreWish(
				_repository,
				_logger,
				latestDates
				);
			List<string> jobLogWishLevels = await coreWish.CheckAndFixDataAtWishLevels();
			_jobLog.AddRange(jobLogWishLevels);

			return _jobLog;
		}

		private async Task<LastDatesFromTables> GetLastDatesFromTable(string tableName)
		{
			LastDatesFromTables result = new LastDatesFromTables();

			result.SourceData.EventDate = await _repository.GetLastDateFromTable(_sourceConnectStr, tableName);
			if (result.SourceData.EventDate is null || result.SourceData.EventDate.Equals(DateTime.MinValue))
			{
				_jobLog.Add(tableName + " GetLastDatesFromTable source error for connection to " + _sourceConnectStr);
				_logger.LogError(tableName + " GetLastDatesFromTable source error for connection to " + _sourceConnectStr);
				result.SourceData.IsSuccess = false;

				return result;
			}
			else
			{
				result.SourceData.Connection = _sourceConnectStr;
				_logger.LogDebug(tableName + " source GetLastDatesFromTable dt=" + result.SourceData.EventDate);
			}

			foreach (string connection in _targetConnectStrList)
			{
				DateAndConnection targetDT = new DateAndConnection
				{
					Connection = connection
				};

				targetDT.EventDate = await _repository.GetLastDateFromTable(connection, tableName);
				if (targetDT.EventDate is null || targetDT.EventDate.Equals(DateTime.MinValue))
				{
					_jobLog.Add(tableName + " GetLastDateFromTable target error for connection to " + connection);
					_logger.LogError(tableName + " GetLastDateFromTable target error for connection to " + connection);
					targetDT.IsSuccess = false;
				}
				else
				{
					_logger.LogDebug(tableName + " target GetLastDateFromTable dt=" + targetDT.EventDate);
					if (targetDT.EventDate < result.Pointer)
					{
						result.Pointer = (DateTime)targetDT.EventDate;
					}
				}

				result.TargetDatesAndConnections.Add(targetDT);
			}

			return result;
		}
	}
}

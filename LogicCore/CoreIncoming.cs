using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;

namespace LogicCore
{
	internal class CoreIncoming
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;		
		private LastDatesFromTables _lastDatesFromTables;

		private List<string> _jobLog = new List<string>();
		private readonly string _name = "incoming";

		internal CoreIncoming(
			IRepository repository,
			ILogger<Core> logger,
			LastDatesFromTables lastDatesFromTables
			)
		{
			_repository = repository;
			_logger = logger;
			_lastDatesFromTables = lastDatesFromTables;
		}

		internal async Task<List<string>> FillAbsentDataAtIncoming()
		{
			/// work with {_name} table ------------------------------------------------------------
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



			/// work with {_name} table
			/// get DateTime source
			///		if error - scip work <---------

			if (!_lastDatesFromTables.SourceData.IsSuccess)
			{
				// break operation
				_logger.LogWarning($"Error! Fill tables '{_name}' is terminated - no date from source BD");
				_jobLog.Add($"Error! Fill tables '{_name}' is terminated - no date from source BD");
				return _jobLog;
			}


			/// if pointer equal source
			///		- scip work <---------
			if (_lastDatesFromTables.SourceData.EventDate <= _lastDatesFromTables.Pointer)
			{
				// break operation
				_logger.LogInformation($"Fill tables '{_name}' is terminated - all tables has same data");
				_jobLog.Add($"Fill tables '{_name}' is terminated - all tables has same data");
				return _jobLog;
			}


			/// GET from source: data older then pointer
			/// foreach target 
			///		if DateTime.MinValue
			///			- scip work at this target<---------
			///		prepare data - remove data older then its pointer
			List<IncomingModel>? eventsFromSource = await _repository.GetIncomingsOlderThanDate(
				_lastDatesFromTables.Pointer,
				_lastDatesFromTables.SourceData.Connection);
			if (eventsFromSource is null)
			{
				// break operation
				_logger.LogInformation($"Error! Fill tables '{_name}' is terminated - GetIncomingsOlderThanDate failed");
				_jobLog.Add($"Error! Fill tables '{_name}' is terminated - GetIncomingsOlderThanDate failed");
				return _jobLog;
			}

			foreach (DateAndConnection table in _lastDatesFromTables.TargetDatesAndConnections)
			{
				if (!table.IsSuccess)
				{
					_logger.LogInformation($"Error! Fill table '{_name}' not executed - " +
						"target date is not filled from " + table.Connection);
					_jobLog.Add($"Error! Fill table '{_name}' not executed - " +
						"target date is not filled from " + table.Connection);
					continue;
				}

				if (table.EventDate == _lastDatesFromTables.SourceData.EventDate)
				{
					_jobLog.Add($"Fill table '{_name}' from '{table.Connection}' is not needed " +
						$"- table date({table.EventDate}) is equal source date({_lastDatesFromTables.SourceData.EventDate})");
					continue;
				}

				if (table.EventDate > _lastDatesFromTables.SourceData.EventDate)
				{
					_jobLog.Add($"Error! Fill table '{_name}' from '{table.Connection}' is not needed " +
						$"- table date({table.EventDate}) greater then source date({_lastDatesFromTables.SourceData.EventDate})");
					continue;
				}

				// prepare data - remove data older then its pointer
				List<IncomingModel> dataForTarget = new List<IncomingModel>();
				foreach (IncomingModel singleEvent in eventsFromSource)
				{
					if (singleEvent.EventDate > table.EventDate)
					{
						dataForTarget.Add(singleEvent);
					}
				}

				if (dataForTarget.Count == 0)
				{
					_logger.LogInformation($"Error! Fill table '{_name}' not executed - target date is not filled - " +
						"not any of data items is not older than table already has for " + table.Connection);
					_jobLog.Add($"Error! Fill table '{_name}' not executed - target date is not filled - " +
						"not any of data items is not older than table already has for " + table.Connection);
					continue;
				}

				///	POST data
				int addToDBCount = await _repository.PostDataToTableIncoming(dataForTarget, table.Connection);
				if (addToDBCount == 0 || addToDBCount == -1)
				{
					_logger.LogInformation($"Error! Fill table '{_name}' failed for " + table.Connection);
					_jobLog.Add($"Error! Fill table '{_name}' failed for " + table.Connection);
				}
				else
				{
					_jobLog.Add($"Fill table '{_name}' succeed for '{table.Connection}', filled {addToDBCount} rows");
				}
			}

			return _jobLog;
		}
	}
}

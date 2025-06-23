using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;


namespace LogicCore
{
	internal class CoreWish
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;
		private LastDatesFromTables _connections;

		private List<string> _jobLog;

		internal CoreWish(IRepository repository, ILogger<Core> logger, LastDatesFromTables connections)
		{
			_repository = repository;
			_logger = logger;
			_connections = connections;
		}

		internal async Task<List<string>> CheckAndFixDataAtWishLevels()
		{
			/// GET ALL rows from source wish_levels
			/// if fail break <---------
			/// 
			/// foreach target:
			///		GET ALL rows from target wish_levels
			///			if fail 
			///				scip work at this target<---------
			///			COMPARE levels
			///				if no differ - remove from target wish_levels
			///				
			///				if target wish_levels empty
			///					- scip work at this target<---------
			///				else
			///					PUT changes
			///	

			_jobLog = new List<string>();

			/// GET ALL rows from source wish_levels
			/// if fail break <---------
			List<WishLevelModel>? wishLevels = await _repository.GetWishLevels(_connections.SourceData.Connection);
			if (wishLevels is null)
			{
				// break operation
				_logger.LogWarning($"Error! Fill tables 'wish_levels' is terminated - no date from source BD");
				_jobLog.Add($"Error! Fill tables 'wish_levels' is terminated - no date from source BD");
				return _jobLog;
			}


			/// foreach target:
			foreach (DateAndConnection wish in _connections.TargetDatesAndConnections)
			{
				///		GET ALL rows from target wish_levels
				///			if fail 
				///				scip work at this target<---------
				List<WishLevelModel>? wishTarget = await _repository.GetWishLevels(wish.Connection);
				if (wishTarget is null)
				{
					// break operation
					_logger.LogWarning($"Error! Update tables 'wish_levels' is terminated for target {wish.Connection} - " +
						$"no data from GetWishLevels");
					_jobLog.Add($"Error! Update tables 'wish_levels' is terminated for target {wish.Connection} - " +
						$"no data from GetWishLevels");
					continue;
				}

				///			COMPARE levels
				///				if no differ - remove from target wish_levels
				for (int i = wishTarget.Count - 1; i >= 0; i--)
				{
					if (wishTarget[i].Level.Equals(wishLevels[i].Level) && wishTarget[i].Weight.Equals(wishLevels[i].Weight))
					{
						_logger.LogDebug($"Remove from table 'wish_levels' equal row with " +
							$"level={wishTarget[i].Level} Weight={wishTarget[i].Weight}");
						wishTarget.RemoveAt(i);
					}
					else
					{
						wishTarget[i].Weight = wishLevels[i].Weight;
					}
				}

				///				if target wish_levels empty
				///					- scip work at this target<---------
				if (wishTarget.Count == 0)
				{
					_jobLog.Add($"Update table 'wish_levels' from '{wish.Connection}' is not needed " +
						$"- table is equal to source");
					continue;
				}

				///				else
				///					PUT changes
				int addToDBCount = await _repository.PutDataToTableWishLevels(wishTarget, wish.Connection);
				if (addToDBCount == 0 || addToDBCount == -1)
				{
					_logger.LogInformation($"Error! Update table 'wish_levels' failed for " + wish.Connection);
					_jobLog.Add($"Error! Update table 'wish_levels' failed for " + wish.Connection);
				}
				else
				{
					_jobLog.Add($"Update table 'wish_levels' succeed for '{wish.Connection}', updated {addToDBCount} rows");
				}
			}


			return _jobLog;
		}
	}
}

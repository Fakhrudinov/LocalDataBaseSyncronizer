using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using System.Text;


namespace LogicCore
{
	internal class CoreWish
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;
		private LastDatesFromTables _connections;

		private List<string> ? _jobLog;

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

			if (!_connections.SourceData.IsSuccess && _connections.SourceData.Connection == string.Empty)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'wish_levels' is terminated - no connections string on start");
				_jobLog.Add($"Error! Update tables 'wish_levels' is terminated - no connections string on start");
				return _jobLog;
			}

			/// GET ALL rows from source wish_levels
			/// if fail break <---------
			List<WishLevelModel>? wishLevels = await _repository.GetWishLevels(_connections.SourceData.Connection);
			if (wishLevels is null)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'wish_levels' is terminated - no data from source BD");
				_jobLog.Add($"Error! Update tables 'wish_levels' is terminated - no data from source BD");
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

		internal async Task<List<string>> CheckAndFixDataAtWishList()
		{
			/// GET ALL rows from source wish_list
			/// if fail break <---------
			/// 
			/// foreach target:
			///		GET ALL rows from target wish_list
			///			if fail 
			///				scip work at this target<---------
			///			COMPARE list:
			///				create lists: 
			///					listToAdd = copy all items from source wish_list
			///					ListToUpdate
			///				for (listToAdd)
			///					if listToAdd item seccode row == any target wish_list row
			///						delete from listToAdd
			///						delete from target wish_list
			///					if listToAdd item seccode row != any target wish_list row but founded
			///						add to ListToUpdate as record from listToAdd
			///						delete from listToAdd
			///						delete from target wish_list

			///				if al lists empty - scip work at this target<---------
			///				else
			///					PUT and ADD changes to DB from ListToUpdate -- union lists
			///					Delete from DB from target wish_list

			///	

			_jobLog = new List<string>();

			if (!_connections.SourceData.IsSuccess && _connections.SourceData.Connection == string.Empty)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'wish_list' is terminated - no connections string on start");
				_jobLog.Add($"Error! Update tables 'wish_list' is terminated - no connections string on start");
				return _jobLog;
			}

			/// GET ALL rows from source wish_list
			/// if fail break <---------
			List<WishListModel>? wishListSource = await _repository.GetWishList(_connections.SourceData.Connection);
			if (wishListSource is null)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'wish_list' is terminated - no data from source BD");
				_jobLog.Add($"Error! Update tables 'wish_list' is terminated - no data from source BD");
				return _jobLog;
			}

			/// foreach target:
			///		GET ALL rows from target wish_list
			///			if fail 
			///				scip work at this target<---------
			foreach (DateAndConnection target in _connections.TargetDatesAndConnections)
			{
				List<WishListModel>? wishTarget = await _repository.GetWishList(target.Connection);
				if (wishTarget is null)
				{
					// break operation
					_logger.LogWarning($"Error! Update tables 'wish_list' is terminated for target {target.Connection} - " +
						$"no data from GetWishList");
					_jobLog.Add($"Error! Update tables 'wish_list' is terminated for target {target.Connection} - " +
						$"no data from GetWishList");
					continue;
				}


				///			COMPARE list:
				///				create lists: 
				///					listToAdd = copy all items from source wish_list
				///					ListToUpdate
				List<WishListModel> listToAdd = wishListSource.ToList();
				List<WishListModel> listToUpdate = new List<WishListModel>();

				///				for (listToAdd)
				///					if listToAdd item seccode row == any target wish_list row
				///						delete from listToAdd
				///						delete from target wish_list
				for(int i = listToAdd.Count - 1; i >= 0; i--)
				{
					for (int t = wishTarget.Count - 1; t >= 0; t--)
					{
						if (listToAdd[i].SecCode.Equals(wishTarget[t].SecCode))
						{
							///					if listToAdd item seccode row != any target wish_list row but founded
							///						add to ListToUpdate as record from listToAdd
							///						delete from listToAdd
							///						delete from target wish_list
							if (!listToAdd[i].Level.Equals(wishTarget[t].Level) ||
								!listToAdd[i].Description.Equals(wishTarget[t].Description))
							{
								listToUpdate.Add(listToAdd[i]);
							}

							listToAdd.RemoveAt(i);
							wishTarget.RemoveAt(t);

							break;
						}
					}
				}


				///				if all lists empty - scip work at this target<---------
				if (wishTarget.Count == 0 && listToAdd.Count == 0 && listToUpdate.Count == 0)
				{
					_jobLog.Add($"Update table 'wish_list' '{target.Connection}' is not needed " +
						$"- table is equal to source");
					continue;
				}
				///				else
				///					PUT and ADD changes to DB from ListToUpdate --union lists
				string update = string.Empty;
				if (listToAdd.Count > 0 || listToUpdate.Count > 0)
				{
					listToAdd.AddRange(listToUpdate);
					int addToDBCount = await _repository.PutDataToTableWishList(
						target.Connection,
						listToAdd);
					if (addToDBCount == 0 || addToDBCount == -1)
					{
						_logger.LogInformation($"Error! Update table 'wish_list' failed for " + target.Connection);
						update = $"Update table 'wish_list' failed! ";
					}
				}
				///					Delete from DB from target wish_list
				string delete = string.Empty;
				if (wishTarget.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					foreach (WishListModel item in wishTarget)
					{
						sb.Append(",'" + item.SecCode + "'");
					}
					sb.Remove(0,1);
					string rowsToDelete = sb.ToString();
					int deletefromDBCount = await _repository.DeleteFromTableWishList(
						target.Connection, 
						rowsToDelete);
					if (deletefromDBCount == 0 || deletefromDBCount == -1)
					{
						_logger.LogInformation($"Error! Delete items {rowsToDelete} from table 'wish_list' " +
							$"failed for " + target.Connection);
						delete = $"Delete exceeded items from table 'wish_list' failed!";
					}
				}

				if (update.Length > 0 || delete.Length > 0)
				{
					_jobLog.Add($"Error! {update}{delete} Connection = '{target.Connection}'");
				}
				else
				{
					_jobLog.Add($"Update table 'wish_list' succeed for '{target.Connection}'");
				}
			}

			return _jobLog;
		}
	}
}

using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LogicCore
{
	internal class CoreSecCodeInfo
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;
		private LastDatesFromTables _connections;

		private List<string> ? _jobLog;

		internal CoreSecCodeInfo (IRepository repository, ILogger<Core> logger, LastDatesFromTables connections)
		{
			_repository = repository;
			_logger = logger;
			_connections = connections;
		}

		internal async Task<List<string>> CheckAndFixDataAtSecCodeInfo()
		{
			/// GET ALL rows from source seccode_info
			/// if fail break <---------
			/// 
			/// foreach target:
			///		GET ALL rows from target seccode_info
			///			if fail 
			///				scip work at this target<---------
			///			COMPARE list:
			///				create lists: 
			///					listToAdd = copy all items from source seccode_info
			///					ListToUpdate
			///				for (listToAdd)
			///					if listToAdd item seccode row == any target seccode_info row
			///						delete from listToAdd
			///						delete from target seccode_info
			///					if listToAdd item seccode row != any target seccode_info row but founded
			///						add to ListToUpdate as record from listToAdd
			///						delete from listToAdd
			///						delete from target seccode_info

			///				if al lists empty - scip work at this target<---------
			///				else
			///					PUT and ADD changes to DB from ListToUpdate -- union lists
			///					Delete from DB from target seccode_info

			_jobLog = new List<string>();

			if (!_connections.SourceData.IsSuccess && _connections.SourceData.Connection == string.Empty)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'seccode_info' is terminated - no connections string on start");
				_jobLog.Add($"Error! Update tables 'seccode_info' is terminated - no connections string on start");
				return _jobLog;
			}

			/// GET ALL rows from source seccode_info
			/// if fail break <---------
			List<SecCodeInfoModel>? secCodeSource = await _repository.GetSecCodeInfo(_connections.SourceData.Connection);
			if (secCodeSource is null)
			{
				// break operation
				_logger.LogWarning($"Error! Update tables 'seccode_info' is terminated - no data from source BD");
				_jobLog.Add($"Error! Update tables 'seccode_info' is terminated - no data from source BD");
				return _jobLog;
			}


			/// foreach target:
			///		GET ALL rows from target seccode_info
			///			if fail 
			///				scip work at this target<---------
			foreach (DateAndConnection target in _connections.TargetDatesAndConnections)
			{
				List<SecCodeInfoModel>? seCodeInfoTarget = await _repository.GetSecCodeInfo(target.Connection);
				if (seCodeInfoTarget is null)
				{
					// break operation
					_logger.LogWarning($"Error! Update tables 'seccode_info' is terminated for target {target.Connection} - " +
						$"no data from GetSecCodeInfo");
					_jobLog.Add($"Error! Update tables 'seccode_info' is terminated for target {target.Connection} - " +
						$"no data from GetSecCodeInfo");
					continue;
				}


				///			COMPARE list:
				///				create lists: 
				///					listToAdd = copy all items from source seccode_info
				///					ListToUpdate
				List<SecCodeInfoModel> listToAdd = secCodeSource.ToList();
				List<SecCodeInfoModel> listToUpdate = new List<SecCodeInfoModel>();

				///				for (listToAdd)
				///					if listToAdd item seccode row == any seCodeInfoTarget row
				///						delete from listToAdd
				///						delete from target seCodeInfoTarget
				for (int i = listToAdd.Count - 1; i >= 0; i--)
				{
					for (int t = seCodeInfoTarget.Count - 1; t >= 0; t--)
					{
						if (listToAdd[i].SecCode.Equals(seCodeInfoTarget[t].SecCode))
						{
							///					if listToAdd item seccode row != any target seCodeInfoTarget row but founded
							///						add to ListToUpdate as record from listToAdd
							///						delete from listToAdd
							///						delete from target seCodeInfoTarget

							DateOnly? sourceDate = null;
							DateOnly? targetDate = null;
							if (listToAdd[i].ExpiredDate is not null)
							{
								sourceDate = DateOnly.FromDateTime((DateTime)listToAdd[i].ExpiredDate);
							}
							if (seCodeInfoTarget[t].ExpiredDate is not null)
							{
								targetDate = DateOnly.FromDateTime((DateTime)seCodeInfoTarget[t].ExpiredDate);
							}

							if (!listToAdd[i].SecBoard.Equals(seCodeInfoTarget[t].SecBoard) ||
								!listToAdd[i].FullName.Equals(seCodeInfoTarget[t].FullName) ||
								!listToAdd[i].Name.Equals(seCodeInfoTarget[t].Name) ||
								!listToAdd[i].ISIN.Equals(seCodeInfoTarget[t].ISIN) ||
								!sourceDate.Equals(targetDate)
								)
							{
								listToUpdate.Add(listToAdd[i]);
							}

							listToAdd.RemoveAt(i);
							seCodeInfoTarget.RemoveAt(t);

							break;
						}
					}
				}


				///				if all lists empty - scip work at this target<---------
				if (seCodeInfoTarget.Count == 0 && listToAdd.Count == 0 && listToUpdate.Count == 0)
				{
					_jobLog.Add($"Update table 'seccode_info' '{target.Connection}' is not needed " +
						$"- table is equal to source");
					continue;
				}
				///				else
				///					PUT and ADD changes to DB from ListToUpdate --union lists
				string update = string.Empty;
				if (listToAdd.Count > 0 || listToUpdate.Count > 0)
				{
					listToAdd.AddRange(listToUpdate);
					int addToDBCount = await _repository.PutDataToTableSecCodeInfo(
						target.Connection,
						listToAdd);
					if (addToDBCount == 0 || addToDBCount == -1)
					{
						_logger.LogInformation($"Error! Update table 'seccode_info' failed for " + target.Connection);
						update = $"Update table 'seccode_info' failed! ";
					}
				}
				///					Delete from DB from target seccode_info
				string delete = string.Empty;
				if (seCodeInfoTarget.Count > 0)
				{
					StringBuilder sb = new StringBuilder();
					foreach (SecCodeInfoModel item in seCodeInfoTarget)
					{
						sb.Append(",'" + item.SecCode + "'");
					}
					sb.Remove(0, 1);
					string rowsToDelete = sb.ToString();
					int deletefromDBCount = await _repository.DeleteFromTableSecCodeInfo(
						target.Connection,
						rowsToDelete);
					if (deletefromDBCount == 0 || deletefromDBCount == -1)
					{
						_logger.LogInformation($"Error! Delete items {rowsToDelete} from table 'seccode_info' " +
							$"failed for " + target.Connection);
						delete = $"Delete exceeded items from table 'seccode_info' failed!";
					}
				}

				if (update.Length > 0 || delete.Length > 0)
				{
					_jobLog.Add($"Error! {update}{delete} Connection = '{target.Connection}'");
				}
				else
				{
					_jobLog.Add($"Update table 'seccode_info' succeed for '{target.Connection}'");
				}
			}

			return _jobLog;
		}
	}
}

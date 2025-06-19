using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Text;

namespace DataBaseRepository
{
	public class Repository : IRepository
	{
		private readonly ILogger<Repository> _logger;
		private ICommonRepository _commonRepo;

		public Repository(ILogger<Repository> logger, ICommonRepository commonRepo)
		{
			_logger=logger;
			_commonRepo=commonRepo;
		}

		public async Task<List<IncomingModel>?> GetIncomingsOlderThanDate(DateTime pointer, string connectionString)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Incomings", "GetIncomingsOlderThanDate.sql");
			if (query is null)
			{
				return null;
			}


			List<IncomingModel> result = new List<IncomingModel>();


			using (MySqlConnection con = new MySqlConnection(connectionString))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@event_date", pointer);

					try
					{
						await con.OpenAsync();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								IncomingModel newIncoming = new IncomingModel();

								newIncoming.EventDate = sdr.GetDateTime("event_date");
								newIncoming.SecCode = sdr.GetString("seccode");
								newIncoming.SecBoard = sdr.GetInt32("secboard");
								newIncoming.Category= sdr.GetInt32("category");
								newIncoming.Value = sdr.GetDecimal("value");

								int checkForNull = sdr.GetOrdinal("comission");
								if (!sdr.IsDBNull(checkForNull))
								{
									newIncoming.Comission = sdr.GetDecimal("comission");
								}

								result.Add(newIncoming);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository GetIncomingsOlderThanDate Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<DateTime?> GetLastDateFromTable(string connectionString, string tableName)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("CommonScripts", "GetLastDateFromTable.sql");
			if (query is null)
			{
				return null;
			}

			//set tableName to script
			query = query.Replace("@tableName", tableName);

			return await _commonRepo.GetLastDateBySqlQuery(connectionString, query);
		}

		public async Task<int> PostDataToTableIncoming(List<IncomingModel> dataItems, string connection)
		{
			string? query = GetSqlRequestForNewIncomingsFromList(dataItems.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					///@date_time{i}, @seccode{i}, @secboard{i}, @category{i}, @value{i}, @comission{i}
					for (int i = 0; i < dataItems.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@date_time" + i, dataItems[i].EventDate);
						cmd.Parameters.AddWithValue("@seccode" + i, dataItems[i].SecCode);
						cmd.Parameters.AddWithValue("@secboard" + i, dataItems[i].SecBoard);
						cmd.Parameters.AddWithValue("@category" + i, dataItems[i].Category);
						cmd.Parameters.AddWithValue("@value" + i, dataItems[i].Value);//_helper.CleanPossibleNumber(model[i].Comission));

						if (dataItems[i].Comission is not null && !dataItems[i].Comission.Equals(""))
						{
							cmd.Parameters.AddWithValue("@comission" + i, dataItems[i].Comission);//_helper.CleanPossibleNumber(model[i].Comission));
						}
						else
						{
							cmd.Parameters.AddWithValue("@comission" + i, DBNull.Value);
						}
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PostDataToTableIncoming execution affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PostDataToTableIncoming Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string ? GetSqlRequestForNewIncomingsFromList(int count)
		{
			/// INSERT INTO `incoming` 
			///     (`date`, `seccode`, `secboard`, `category`, `value`, `comission`) 
			/// VALUES 
			/// (
			///     `date`, `seccode`, `secboard`, `category`, `value`, `comission`
			///     ) , (
			///     `date`, `seccode`, `secboard`, `category`, `value`, `comissionNULL`
			/// );
			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("Incomings", "CreateNewIncomingsFromList.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForNewIncomingsFromList Error! " +
					$"File CreateNewIncomingsFromList.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@date_time{i}, @seccode{i}, @secboard{i}, @category{i}, @value{i}, @comission{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForNewIncomingsFromList execute " +
				$"query CreateNewIncomingsFromList.sql\r\n{query}");

			return query.ToString();
		}
	}
}


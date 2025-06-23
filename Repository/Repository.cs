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

		public async Task<List<DealModel>?> GetDealsOlderThanDate(DateTime pointer, string connection)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "GetDealsOlderThanDate.sql");
			if (query is null)
			{
				return null;
			}


			List<DealModel> result = new List<DealModel>();


			using (MySqlConnection con = new MySqlConnection(connection))
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
								DealModel newDeal = new DealModel();

								//(`id`, `event_date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`)
;
								newDeal.EventDate = sdr.GetDateTime("event_date");
								newDeal.SecCode = sdr.GetString("seccode");
								newDeal.SecBoard = sdr.GetInt32("secboard");

								newDeal.AvPrice = sdr.GetDecimal("av_price");
								newDeal.Pieces = sdr.GetInt32("pieces");

								int checkForNull = sdr.GetOrdinal("comission");
								if (!sdr.IsDBNull(checkForNull))
								{
									newDeal.Comission = sdr.GetDecimal("comission");
								}

								int checkForNullNkd = sdr.GetOrdinal("nkd");
								if (!sdr.IsDBNull(checkForNullNkd))
								{
									newDeal.NKD = sdr.GetDecimal("nkd");
								}

								result.Add(newDeal);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository GetDealsOlderThanDate Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
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

		public async Task<DateTime?> GetLastDateFromTable(string connection, string tableName)
		{
			return await _commonRepo.GetLastDateFromTable(connection, tableName);
		}


		public async Task<int> PostDataToTableDeals(List<DealModel> dataItems, string connection)
		{
			string? query = GetSqlRequestForNewDealsFromList(dataItems.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					////(@date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd);
					for (int i = 0; i < dataItems.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@date_time" + i, dataItems[i].EventDate);
						cmd.Parameters.AddWithValue("@seccode" + i, dataItems[i].SecCode);
						cmd.Parameters.AddWithValue("@secboard" + i, dataItems[i].SecBoard);
						cmd.Parameters.AddWithValue("@av_price" + i, dataItems[i].AvPrice);
						cmd.Parameters.AddWithValue("@pieces" + i, dataItems[i].Pieces);

						if (dataItems[i].Comission is not null && !dataItems[i].Comission.Equals(0))
						{
							cmd.Parameters.AddWithValue("@comission" + i, dataItems[i].Comission);
						}
						else
						{
							cmd.Parameters.AddWithValue("@comission" + i, DBNull.Value);
						}

						if (dataItems[i].NKD is not null && !dataItems[i].NKD.Equals(0))
						{
							cmd.Parameters.AddWithValue("@nkd" + i, dataItems[i].NKD);
						}
						else
						{
							cmd.Parameters.AddWithValue("@nkd" + i, DBNull.Value);
						}
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PostDataToTableDeals execution affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PostDataToTableDeals Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForNewDealsFromList(int count)
		{
			/// INSERT INTO `deals` 
			///     (`date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`) 
			/// VALUES
			///     (
			///          @date_time, @seccode, @secboard, @av_price, @pieces, @comission, @nkd
			///                 ),(
			///          '2024-10-07 12:00:36', 'BSPB', '1', '365.0000', '20', null, '1.55'
			///     );
			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("Deals", "CreateNewDealsFromList.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForNewDealsFromList Error! " +
					$"File CreateNewDealsFromList.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@date_time{i}, @seccode{i}, @secboard{i}, @av_price{i}, @pieces{i}, @comission{i}, @nkd{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForNewDealsFromList execute query " +
				$"CreateNewDealsFromList.sql\r\n{query}");

			return query.ToString();
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

		public async Task<List<WishLevelModel>?> GetWishLevels(string connection)
		{
			return await _commonRepo.GetWishLevels(connection);
		}

		public async Task<int> PutDataToTableWishLevels(List<WishLevelModel> wishLevels, string connection)
		{
			string? query = GetSqlRequestForWishLevelsFromList(wishLevels.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					///@level{i}, @weight{i}
					for (int i = 0; i < wishLevels.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@level" + i, wishLevels[i].Level);
						cmd.Parameters.AddWithValue("@weight" + i, wishLevels[i].Weight);
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PutDataToTableWishLevels execution affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PutDataToTableWishLevels Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForWishLevelsFromList(int count)
		{

			/// INSERT INTO wish_levels
			///		(level, weight)
			///			VALUES
			///				(1, 2300),			
			///				(2, 3300),
			///				(@level{i}, @weight{i})
			///		AS aliased
			/// ON DUPLICATE KEY UPDATE weight=aliased.weight;

			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("Wish", "PutDataToTableWishLevels.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForWishLevelsFromList Error! " +
					$"File PutDataToTableWishLevels.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@level{i}, @weight{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForWishLevelsFromList execute query " +
				$"PutDataToTableWishLevels.sql\r\n{query}");

			return query.ToString();
		}
	}
}


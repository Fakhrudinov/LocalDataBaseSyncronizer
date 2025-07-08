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
			string? query = GetSqlRequestForWishLevels(wishLevels.Count);
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

		public async Task<List<WishListModel>?> GetWishList(string connection)
		{
			return await _commonRepo.GetWishList(connection);
		}

		public async Task<int> DeleteFromTableWishList(string connection, string rowsToDelete)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("Wish", "DeleteFromTableWishList.sql");
			if (query is null)
			{
				return -1;
			}
			query = query.Replace("@values", rowsToDelete);

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int delResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository DeleteFromTableWishList execution affected {delResult} lines");

						return delResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository DeleteFromTableWishList Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<int> PutDataToTableWishList(string connection, List<WishListModel> list)
		{
			string? query = GetSqlRequestForWishList(list.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					///(@seccode{i}, @wish_level{i}, @description{i})
					for (int i = 0; i < list.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@seccode" + i, list[i].SecCode);
						cmd.Parameters.AddWithValue("@wish_level" + i, list[i].Level);
						cmd.Parameters.AddWithValue("@description" + i, list[i].Description);
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PutDataToTableWishList execution affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PutDataToTableWishList Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForWishLevels(int count)
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
				_logger.LogError($"Repository GetSqlRequestForWishLevels Error! " +
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

			_logger.LogInformation($"Repository GetSqlRequestForWishLevels execute query " +
				$"PutDataToTableWishLevels.sql\r\n{query}");

			return query.ToString();
		}
		private string? GetSqlRequestForWishList(int count)
		{
			/// INSERT INTO money_test.wish_list
			///		(`seccode`, `wish_level`, `description`)
			///	VALUES
			///		('FAKE', 1, 'someNew222'),
			///		(@seccode{i}, @wish_level{i}, @description{i})
			///	AS aliased
			///	ON DUPLICATE KEY UPDATE 
			///		`wish_level` = aliased.`wish_level`,  
			///		`description` = aliased.`description`;
			
			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("Wish", "PutDataToTableWishList.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForWishList Error! " +
					$"File PutDataToTableWishList.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@seccode{i}, @wish_level{i}, @description{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForWishList execute query " +
				$"PutDataToTableWishList.sql\r\n{query}");

			return query.ToString();
		}

		public async Task<List<SecCodeInfoModel>?> GetSecCodeInfo(string connection)
		{
			return await _commonRepo.GetSecCodeInfo(connection);
		}

		public async Task<int> DeleteFromTableSecCodeInfo(string connection, string rowsToDelete)
		{
			string? query = _commonRepo.GetQueryTextByFolderAndFilename("SecCodeInfo", "DeleteFromTableSecCodeInfo.sql");
			if (query is null)
			{
				return -1;
			}
			query = query.Replace("@values", rowsToDelete);

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int delResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository DeleteFromTableSecCodeInfo execution affected {delResult} lines");

						return delResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository DeleteFromTableSecCodeInfo Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<int> PutDataToTableSecCodeInfo(string connection, List<SecCodeInfoModel> list)
		{
			string? query = GetSqlRequestForSecCodeInfo(list.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					//@seccode{i}, @secboard{i}, @name{i}, @full_name{i}, @isin{i}, @expired_date{i}
					for (int i = 0; i < list.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@seccode" + i, list[i].SecCode);
						cmd.Parameters.AddWithValue("@secboard" + i, list[i].SecBoard);
						cmd.Parameters.AddWithValue("@name" + i, list[i].Name);
						cmd.Parameters.AddWithValue("@full_name" + i, list[i].FullName);
						cmd.Parameters.AddWithValue("@isin" + i, list[i].ISIN);

						if (list[i].ExpiredDate is not null && !list[i].ExpiredDate.Equals(""))
						{
							cmd.Parameters.AddWithValue("@expired_date" + i, list[i].ExpiredDate);
						}
						else
						{
							cmd.Parameters.AddWithValue("@expired_date" + i, DBNull.Value);
						}
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PutDataToTableSecCodeInfo execution affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PutDataToTableSecCodeInfo Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForSecCodeInfo(int count)
		{
			/// INSERT INTO money_test.seccode_info
			/// (`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
			/// VALUES
			/// ('AKRN', 1, 'facronnnn', 'fullacron', 'RUUUUUUUUUU', NULL),
			/// ('FAKE2', 2, 'someNwerwer', '3f34f35fg', 'RUUUUUUUU01', '2024-01-01'),
			/// (@seccode{i}, @secboard{i}, @name{i}, @full_name{i}, @isin{i}, @expired_date{i})
			/// AS aliased
			/// ON DUPLICATE KEY UPDATE 
			/// `secboard` = aliased.`secboard`,  
			/// `name` = aliased.`name`, 
			/// `full_name` = aliased.`full_name`, 
			/// `isin` = aliased.`isin`, 
			/// `expired_date` = aliased.`expired_date`;

			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("SecCodeInfo", "PutDataToTableSecCodeInfo.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForSecCodeInfo Error! " +
					$"File PutDataToTableSecCodeInfo.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			//(`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@seccode{i}, @secboard{i}, @name{i}, @full_name{i}, @isin{i}, @expired_date{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForSecCodeInfo execute query " +
				$"PutDataToTableSecCodeInfo.sql\r\n{query}");

			return query.ToString();
		}

		public async Task<List<MoneyByMonthModel>?> GetMoneyByMonthAllRowsOlderThanDate(
			string connection, 
			DateOnly yearAgo)
		{
			string? query = _commonRepo
				.GetQueryTextByFolderAndFilename("MoneyByMonth", "GetMoneyByMonthAllRowsOlderThanDate.sql");
			if (query is null)
			{
				return null;
			}

			List<MoneyByMonthModel> result = new List<MoneyByMonthModel>();

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@event_date", yearAgo);

					try
					{
						await con.OpenAsync();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								MoneyByMonthModel mbm = new MoneyByMonthModel();
								// (`event_date`, `total_in`, `month_in`, `dividend`,
								// `dosrochnoe`, `deals_sum`, `brok_comission`, `money_sum`)
								mbm.EventDate = sdr.GetDateTime("event_date");

								int checkForNull0 = sdr.GetOrdinal("total_in");
								if (!sdr.IsDBNull(checkForNull0))
								{
									mbm.TotalIn = sdr.GetDecimal("total_in");
								}

								int checkForNull1 = sdr.GetOrdinal("month_in");
								if (!sdr.IsDBNull(checkForNull1))
								{
									mbm.MonthIn = sdr.GetDecimal("month_in");
								}

								int checkForNull2 = sdr.GetOrdinal("dividend");
								if (!sdr.IsDBNull(checkForNull2))
								{
									mbm.Divident = sdr.GetDecimal("dividend");
								}

								int checkForNull3 = sdr.GetOrdinal("dosrochnoe");
								if (!sdr.IsDBNull(checkForNull3))
								{
									mbm.Dosrochnoe = sdr.GetDecimal("dosrochnoe");
								}

								int checkForNull4 = sdr.GetOrdinal("deals_sum");
								if (!sdr.IsDBNull(checkForNull4))
								{
									mbm.DealsSum = sdr.GetDecimal("deals_sum");
								}

								int checkForNull5 = sdr.GetOrdinal("brok_comission");
								if (!sdr.IsDBNull(checkForNull5))
								{
									mbm.BrokComission = sdr.GetDecimal("brok_comission");
								}

								int checkForNull6 = sdr.GetOrdinal("money_sum");
								if (!sdr.IsDBNull(checkForNull6))
								{
									mbm.MoneySum = sdr.GetDecimal("money_sum");
								}

								result.Add(mbm);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository GetMoneyByMonthAllRowsOlderThanDate Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<int> DeleteFromDateWithSkipList(
			string connection, 
			string tableName, 
			DateOnly yearAgo, 
			string rowsToSkipDelete)
		{
			return await _commonRepo.DeleteFromDateWithSkipList(connection, tableName, yearAgo, rowsToSkipDelete);
		}

		public async Task<int> PutDataToTableMoneyByMonth(string connection, List<MoneyByMonthModel> list)
		{
			string? query = GetSqlRequestForMoneyByMonth(list.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					//`event_date`, `total_in`, `month_in`, `dividend`, `dosrochnoe`,
					//`deals_sum`, `brok_comission`, `money_sum`
					for (int i = 0; i < list.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@event_date" + i, list[i].EventDate);

						if (list[i].TotalIn is not null)
						{
							cmd.Parameters.AddWithValue("@total_in" + i, list[i].TotalIn);
						}
						else
						{
							cmd.Parameters.AddWithValue("@total_in" + i, DBNull.Value);
						}

						if (list[i].MonthIn is not null)
						{
							cmd.Parameters.AddWithValue("@month_in" + i, list[i].MonthIn);
						}
						else
						{
							cmd.Parameters.AddWithValue("@month_in" + i, DBNull.Value);
						}

						if (list[i].Divident is not null)
						{
							cmd.Parameters.AddWithValue("@dividend" + i, list[i].Divident);
						}
						else
						{
							cmd.Parameters.AddWithValue("@dividend" + i, DBNull.Value);
						}

						if (list[i].Dosrochnoe is not null)
						{
							cmd.Parameters.AddWithValue("@dosrochnoe" + i, list[i].Dosrochnoe);
						}
						else
						{
							cmd.Parameters.AddWithValue("@dosrochnoe" + i, DBNull.Value);
						}

						if (list[i].DealsSum is not null)
						{
							cmd.Parameters.AddWithValue("@deals_sum" + i, list[i].DealsSum);
						}
						else
						{
							cmd.Parameters.AddWithValue("@deals_sum" + i, DBNull.Value);
						}

						if (list[i].BrokComission is not null)
						{
							cmd.Parameters.AddWithValue("@brok_comission" + i, list[i].BrokComission);
						}
						else
						{
							cmd.Parameters.AddWithValue("@brok_comission" + i, DBNull.Value);
						}

						if (list[i].MoneySum is not null)
						{
							cmd.Parameters.AddWithValue("@money_sum" + i, list[i].MoneySum);
						}
						else
						{
							cmd.Parameters.AddWithValue("@money_sum" + i, DBNull.Value);
						}
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PutDataToTableMoneyByMonth execution affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PutDataToTableMoneyByMonth Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForMoneyByMonth(int count)
		{
			/// INSERT INTO money_by_month
			/// (`event_date`, `total_in`, `month_in`, `dividend`, `dosrochnoe`, `deals_sum`, `brok_comission`, `money_sum`)
			/// VALUES
			/// ('2025-06-01', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
			/// ('2025-07-02', 9542.85, 9542.85, 9542.85, 9542.85, 9542.85, 9542.85, 324234),
			/// ('2025-08-02', 9542.85, 9542.85, 9542.85, NULL, 9542.85, NULL, 324234)
			/// AS aliased
			/// ON DUPLICATE KEY UPDATE 
			/// `total_in` = aliased.`total_in`,  
			/// `month_in` = aliased.`month_in`, 
			/// `dividend` = aliased.`dividend`, 
			/// `dosrochnoe` = aliased.`dosrochnoe`, 
			/// `deals_sum` = aliased.`deals_sum`,
			/// `brok_comission` = aliased.`brok_comission`,
			/// `money_sum` = aliased.`money_sum`;

			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename("MoneyByMonth", "PutDataToTableMoneyByMonth.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForMoneyByMonth Error! " +
					$"File PutDataToTableMoneyByMonth.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			//`event_date`, `total_in`, `month_in`, `dividend`, `dosrochnoe`, `deals_sum`, `brok_comission`, `money_sum`
			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@event_date{i}, @total_in{i}, @month_in{i}, @dividend{i}, @dosrochnoe{i}, " +
					$"@deals_sum{i}, @brok_comission{i}, @money_sum{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForMoneyByMonth execute query " +
				$"PutDataToTableMoneyByMonth.sql\r\n{query}");

			return query.ToString();
		}

		public async Task<List<MoneySpentByMonthModel>?> GetMoneySpentByMonthAllRowsOlderThanDate(
			string connection, 
			DateOnly yearAgo)
		{
			string? query = _commonRepo
				.GetQueryTextByFolderAndFilename("MoneySpent", "GetMoneySpentByMonthAllRowsOlderThanDate.sql");
			if (query is null)
			{
				return null;
			}

			List<MoneySpentByMonthModel> result = new List<MoneySpentByMonthModel>();

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@event_date", yearAgo);

					try
					{
						await con.OpenAsync();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								MoneySpentByMonthModel mbm = new MoneySpentByMonthModel();
								// `money_spent_by_month`
								//(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`)
								mbm.EventDate = sdr.GetDateTime("event_date");

								int checkForNull0 = sdr.GetOrdinal("total");
								if (!sdr.IsDBNull(checkForNull0))
								{
									mbm.Total = sdr.GetDecimal("total");
								}

								int checkForNull1 = sdr.GetOrdinal("appartment");
								if (!sdr.IsDBNull(checkForNull1))
								{
									mbm.Appartment = sdr.GetDecimal("appartment");
								}

								int checkForNull2 = sdr.GetOrdinal("electricity");
								if (!sdr.IsDBNull(checkForNull2))
								{
									mbm.Electricity = sdr.GetDecimal("electricity");
								}

								int checkForNull3 = sdr.GetOrdinal("internet");
								if (!sdr.IsDBNull(checkForNull3))
								{
									mbm.Internet = sdr.GetDecimal("internet");
								}

								int checkForNull4 = sdr.GetOrdinal("phone");
								if (!sdr.IsDBNull(checkForNull4))
								{
									mbm.Phone = sdr.GetDecimal("phone");
								}

								result.Add(mbm);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository GetMoneySpentByMonthAllRowsOlderThanDate Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<int> PutDataToTableMoneySpentByMonth(
			string connection, 
			List<MoneySpentByMonthModel> list)
		{
			string? query = GetSqlRequestForMoneySpent(list.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					//(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`)
					for (int i = 0; i < list.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@event_date" + i, list[i].EventDate);

						if (list[i].Total is not null)
						{
							cmd.Parameters.AddWithValue("@total" + i, list[i].Total);
						}
						else
						{
							cmd.Parameters.AddWithValue("@total" + i, DBNull.Value);
						}

						if (list[i].Appartment is not null)
						{
							cmd.Parameters.AddWithValue("@appartment" + i, list[i].Appartment);
						}
						else
						{
							cmd.Parameters.AddWithValue("@appartment" + i, DBNull.Value);
						}

						if (list[i].Electricity is not null)
						{
							cmd.Parameters.AddWithValue("@electricity" + i, list[i].Electricity);
						}
						else
						{
							cmd.Parameters.AddWithValue("@electricity" + i, DBNull.Value);
						}

						if (list[i].Internet is not null)
						{
							cmd.Parameters.AddWithValue("@internet" + i, list[i].Internet);
						}
						else
						{
							cmd.Parameters.AddWithValue("@internet" + i, DBNull.Value);
						}

						if (list[i].Phone is not null)
						{
							cmd.Parameters.AddWithValue("@phone" + i, list[i].Phone);
						}
						else
						{
							cmd.Parameters.AddWithValue("@phone" + i, DBNull.Value);
						}
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PutDataToTableMoneySpentByMonth execution " +
							$"affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PutDataToTableMoneySpentByMonth Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForMoneySpent(int count)
		{
			/// INSERT INTO 

			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename(
				"MoneySpent", 
				"PutDataToTableMoneySpentByMonth.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForMoneySpent Error! " +
					$"File PutDataToTableMoneySpentByMonth.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			//(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`)
			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@event_date{i}, @total{i}, @appartment{i}, @electricity{i}, @internet{i}, @phone{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForMoneySpent execute query " +
				$"PutDataToTableMoneySpentByMonth.sql\r\n{query}");

			return query.ToString();
		}

		public async Task<List<BankDepositModel>?> GetBankDepositsLastRows(string connection, int recordsToPast)
		{
			string? query = _commonRepo
				.GetQueryTextByFolderAndFilename("BankDeposits", "GetBankDepositsLastRows.sql");
			if (query is null)
			{
				return null;
			}

			List<BankDepositModel> result = new List<BankDepositModel>();

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@limit", recordsToPast);

					try
					{
						await con.OpenAsync();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								BankDepositModel model = new BankDepositModel();
								//`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`,
								//`percent`, `summ`, `income_summ`
								model.Id = sdr.GetInt32("id");
								model.IsOpen = sdr.GetInt16("isopen");
								model.EventDate = sdr.GetDateTime("event_date");
								model.DateClose = sdr.GetDateTime("date_close");
								model.Name = sdr.GetString("name");
								model.PlaceName = sdr.GetInt32("placed_name");
								model.Percent = sdr.GetDecimal("percent");
								model.Summ = sdr.GetDecimal("summ");

								int checkForNull0 = sdr.GetOrdinal("income_summ");
								if (!sdr.IsDBNull(checkForNull0))
								{
									model.IncomeSumm = sdr.GetDecimal("income_summ");
								}

								result.Add(model);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository GetBankDepositsLastRows Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return result;
		}

		public async Task<int> DeleteFromBankDepositsByIdWithSkipList(
			string connection, 
			int minimumSourceId, 
			string idToSkip)
		{
			string? query = _commonRepo
				.GetQueryTextByFolderAndFilename("BankDeposits", "DeleteFromBankDepositsByIdWithSkipList.sql");
			if (query is null)
			{
				return -1;
			}

			query = query.Replace("@values", idToSkip);

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					cmd.Parameters.AddWithValue("@id", minimumSourceId);

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int delResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository DeleteFromBankDepositsByIdWithSkipList " +
							$"execution affected {delResult} lines");

						return delResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository DeleteFromBankDepositsByIdWithSkipList Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<int> PutDataToTableBankDeposits(string connection, List<BankDepositModel> list)
		{
			string? query = GetSqlRequestForBankDeposits(list.Count);
			if (query is null)
			{
				return -1;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					//(`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`, `percent`, `summ`, `income_summ`)
					for (int i = 0; i < list.Count(); i++)
					{
						cmd.Parameters.AddWithValue("@id" + i, list[i].Id);
						cmd.Parameters.AddWithValue("@isopen" + i, list[i].IsOpen);
						cmd.Parameters.AddWithValue("@event_date" + i, list[i].EventDate);
						cmd.Parameters.AddWithValue("@date_close" + i, list[i].DateClose);
						cmd.Parameters.AddWithValue("@name" + i, list[i].Name);
						cmd.Parameters.AddWithValue("@placed_name" + i, list[i].PlaceName);
						cmd.Parameters.AddWithValue("@percent" + i, list[i].Percent);
						cmd.Parameters.AddWithValue("@summ" + i, list[i].Summ);

						if (list[i].IncomeSumm is not null)
						{
							cmd.Parameters.AddWithValue("@income_summ" + i, list[i].IncomeSumm);
						}
						else
						{
							cmd.Parameters.AddWithValue("@income_summ" + i, DBNull.Value);
						}
					}

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int insertResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"Repository PutDataToTableBankDeposits execution " +
							$"affected {insertResult} lines");

						return insertResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Repository PutDataToTableBankDeposits Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		private string? GetSqlRequestForBankDeposits(int count)
		{
			/// INSERT INTO money_test.bank_deposits
			/// (`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`, `percent`, `summ`, `income_summ`)
			/// VALUES
			/// (22, 1, '2025-06-01', '2025-07-01', 'name22', 1, 12, 123123.1, NULL),
			/// (21, 0, '2025-06-01', '2025-09-01', 'name21', 0, 11, 6123.1, NULL),
			/// (23, 1, '2025-06-01', '2025-08-01', 'name23', 1, 34, 123.1, 123.12)
			/// AS aliased
			/// ON DUPLICATE KEY UPDATE 
			/// `isopen` = aliased.`isopen`, 
			/// `event_date` = aliased.`event_date`, 
			/// `date_close` = aliased.`date_close`, 
			/// `name` = aliased.`name`, 
			/// `placed_name` = aliased.`placed_name`,
			/// `percent` = aliased.`percent`,
			/// `summ` = aliased.`summ`,
			/// `income_summ` = aliased.`income_summ`;

			string? queryStr = _commonRepo.GetQueryTextByFolderAndFilename(
				"BankDeposits",
				"PutDataToTableBankDeposits.sql");
			if (queryStr is null)
			{
				_logger.LogError($"Repository GetSqlRequestForBankDeposits Error! " +
					$"File PutDataToTableBankDeposits.sql not found");
				return null;
			}

			StringBuilder query = new StringBuilder(queryStr);
			StringBuilder parameters = new StringBuilder();

			//(`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`, `percent`, `summ`, `income_summ`)
			for (int i = 0; i < count; i++)
			{
				parameters.Append($"),\r\n(" +
					$"@id{i}, @isopen{i}, @event_date{i}, @date_close{i}, @name{i}, " +
					$"@placed_name{i}, @percent{i}, @summ{i}, @income_summ{i}");
			}
			parameters.Remove(0, 5);
			string parametersStr = parameters.ToString();

			query.Replace("@values", parametersStr);

			_logger.LogInformation($"Repository GetSqlRequestForBankDeposits execute query " +
				$"PutDataToTableBankDeposits.sql\r\n{query}");

			return query.ToString();
		}
	}
}


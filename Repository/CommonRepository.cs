using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;


namespace DataBaseRepository
{
	public class CommonRepository : ICommonRepository
	{
		private ILogger<CommonRepository> _logger;

		public CommonRepository(ILogger<CommonRepository> logger)
		{
			_logger = logger;
		}

		public async Task<int> DeleteFromDateWithSkipList(
			string connection, 
			string tableName, 
			DateOnly yearAgo, 
			string rowsToSkipDelete)
		{
			string? query = GetQueryTextByFolderAndFilename("CommonScripts", "DeleteFromDateWithSkipList.sql");
			if (query is null)
			{
				return -1;
			}
			query = query.Replace("@tableName", tableName);
			query = query.Replace("@values", rowsToSkipDelete);

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;
					
					cmd.Parameters.AddWithValue("@event_date", yearAgo);

					try
					{
						await con.OpenAsync();

						//Return Int32 Number of rows affected
						int delResult = await cmd.ExecuteNonQueryAsync();
						_logger.LogInformation($"CommonRepository DeleteFromDateWithSkipList " +
							$"execution affected {delResult} lines");

						return delResult;

					}
					catch (Exception ex)
					{
						_logger.LogWarning($"CommonRepository DeleteFromDateWithSkipList Exception!\r\n{ex.Message}");
						return -1;
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}
		}

		public async Task<DateTime?> GetLastDateFromTable(string connection, string tableName)
		{
			string? query = GetQueryTextByFolderAndFilename("CommonScripts", "GetLastDateFromTable.sql");
			if (query is null)
			{
				return null;
			}

			//set tableName to script
			query = query.Replace("@tableName", tableName);


			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								DateTime dt = sdr.GetDateTime(0);
								_logger.LogDebug($"CommonRepository GetLastDateFromTable Date=" + dt.ToString());
								return dt;
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"CommonRepository GetLastDateFromTable Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return null;
		}


		public string? GetQueryTextByFolderAndFilename(string folderName, string queryFileName)
		{
			string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SqlQueries", folderName, queryFileName);
			if (!File.Exists(filePath))
			{
				_logger.LogWarning($"CommonRepository Error! File with SQL script not found at " + filePath);
				return null;
			}

			string query = File.ReadAllText(filePath);
			_logger.LogInformation($"CommonRepository GetQueryTextByFolderAndFilename query {queryFileName} text is:" +
				$"\r\n{query}");

			return query;
		}

		public async Task<List<SecCodeInfoModel>?> GetSecCodeInfo(string connection)
		{
			string? query = GetQueryTextByFolderAndFilename("SecCodeInfo", "GetSecCodeInfo.sql");
			if (query is null)
			{
				return null;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync();

						List<SecCodeInfoModel> secCodeInfoList = new List<SecCodeInfoModel>();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								// `seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`
								SecCodeInfoModel secCodeInfo = new SecCodeInfoModel();
								secCodeInfo.SecCode = sdr.GetString("seccode");
								secCodeInfo.SecBoard = sdr.GetInt32("secboard");
								secCodeInfo.Name = sdr.GetString("name");
								secCodeInfo.FullName = sdr.GetString("full_name");
								secCodeInfo.ISIN = sdr.GetString("isin");
								
								int checkForNull = sdr.GetOrdinal("expired_date");
								if (!sdr.IsDBNull(checkForNull))
								{
									secCodeInfo.ExpiredDate= sdr.GetDateTime("expired_date");
								}

								secCodeInfoList.Add(secCodeInfo);
							}
						}

						return secCodeInfoList;
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"CommonRepository GetSecCodeInfo Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return null;
		}

		public async Task<List<WishLevelModel>?> GetWishLevels(string connection)
		{
			string? query = GetQueryTextByFolderAndFilename("Wish", "GetWishLevelsWeight.sql");
			if (query is null)
			{
				return null;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync();

						List<WishLevelModel> wishLevels = new List<WishLevelModel>();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								WishLevelModel wishLevel = new WishLevelModel();
								wishLevel.Level = sdr.GetInt32("level");
								wishLevel.Weight = sdr.GetInt32("weight");
								wishLevels.Add(wishLevel);
							}
						}

						return wishLevels;
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"CommonRepository GetWishLevels Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return null;
		}

		public async Task<List<WishListModel>?> GetWishList(string connection)
		{
			string? query = GetQueryTextByFolderAndFilename("Wish", "GetWishList.sql");
			if (query is null)
			{
				return null;
			}

			using (MySqlConnection con = new MySqlConnection(connection))
			{
				using (MySqlCommand cmd = new MySqlCommand(query))
				{
					cmd.Connection = con;

					try
					{
						await con.OpenAsync();

						List<WishListModel> wishLevels = new List<WishListModel>();

						using (MySqlDataReader sdr = await cmd.ExecuteReaderAsync())
						{
							while (await sdr.ReadAsync())
							{
								WishListModel wishList = new WishListModel();
								//(`seccode`, `wish_level`, `description`)
								wishList.SecCode = sdr.GetString("seccode");
								wishList.Level = sdr.GetInt32("wish_level");

								int checkForNull = sdr.GetOrdinal("description");
								if (!sdr.IsDBNull(checkForNull))
								{
									wishList.Description = sdr.GetString("description");
								}

								wishLevels.Add(wishList);
							}
						}

						return wishLevels;
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"CommonRepository GetWishList Exception!\r\n{ex.Message}");
					}
					finally
					{
						await con.CloseAsync();
					}
				}
			}

			return null;
		}
	}
}

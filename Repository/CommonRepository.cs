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
	}
}

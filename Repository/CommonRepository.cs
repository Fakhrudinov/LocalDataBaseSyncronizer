using DataAbstraction.Interfaces;
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

		public async Task<DateTime?> GetLastDateBySqlQuery(string connectionString, string query)
		{
			using (MySqlConnection con = new MySqlConnection(connectionString))
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
								_logger.LogDebug($"CommonRepository GetLastDateBySqlQuery Date=" + dt.ToString());
								return dt;
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"CommonRepository GetLastDateBySqlQuery Exception!\r\n{ex.Message}");
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
	}
}

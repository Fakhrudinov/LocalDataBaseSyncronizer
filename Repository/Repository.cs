using DataAbstraction.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataBaseRepository
{
	public class Repository : IRepository
	{
		private readonly ILogger<Repository> _logger;

		public Repository(ILogger<Repository> logger)
		{
			_logger=logger;
		}
	}
}

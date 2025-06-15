using DataAbstraction.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogicCore
{
	public class Core : ILogicCore
	{
		private IRepository _repository;
		private readonly ILogger<Core> _logger;

		public Core (IRepository repository, ILogger<Core> logger)
		{
			_repository = repository;
			_logger = logger;
		}


	}
}

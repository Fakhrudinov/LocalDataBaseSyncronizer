using DataAbstraction.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalDataBaseSyncrinizer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DataBaseController : ControllerBase
	{
		private ILogicCore _core;
		private readonly ILogger<DataBaseController> _logger;

		public DataBaseController(ILogicCore core, ILogger<DataBaseController> logger)
		{
			_core=core;
			_logger=logger;
		}

		[HttpPost]
		public IActionResult FixAddAbsentDataAtAllDB()
		{
			return Ok();
		}
	}
}

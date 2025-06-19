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

		[HttpGet("Check/DataBase/Is/Accessible")]
		public async Task<IActionResult> CheckDataBaseIsAccessible()
		{
			bool result = await _core.CheckDataBaseIsAccessible();
			return Ok(result);
		}

		[HttpPost]
		public async Task<IActionResult> FillAbsentDataAtAllDB()
		{
			List<string> list = await _core.FillAbsentDataAtAllDB();
			return Ok(list);
		}
	}
}

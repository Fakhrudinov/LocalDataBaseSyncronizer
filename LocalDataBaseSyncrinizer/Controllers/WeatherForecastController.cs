using DataAbstraction.Settings;
using Microsoft.AspNetCore.Mvc;


namespace LocalDataBaseSyncrinizer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly ILogger<WeatherForecastController> _logger;
		private IConfiguration Configuration;

		public WeatherForecastController(ILogger<WeatherForecastController> logger

            , IConfiguration configuration
			)
        {
			Configuration = configuration;
			_logger = logger;
			var some = Configuration.GetSection("DataBaseList").Get<List<DataBaseConnectionSettings>>();
		}

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}

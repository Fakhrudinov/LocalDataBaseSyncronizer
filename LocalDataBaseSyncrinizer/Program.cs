using DataAbstraction.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace LocalDataBaseSyncrinizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();

			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();


            // Configure the HTTP request pipeline.

            app.UseAuthorization();

			//https://dotnettutorials.net/lesson/swagger-api-in-asp-net-core-web-api/
			app.UseSwagger();
			app.UseSwaggerUI();

			app.MapControllers();

            app.Run();
        }
    }
}

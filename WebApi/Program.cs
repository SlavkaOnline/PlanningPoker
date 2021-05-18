using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grains;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans;

namespace WebApi
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
				.UseOrleans(siloBuilder =>
				{
					siloBuilder
					.AddMemoryGrainStorage("InMemory")
					.AddMemoryGrainStorage("PubSubStore")
					.AddLogStorageBasedLogConsistencyProvider()
					.AddSimpleMessageStreamProvider("SMS", configureStream => configureStream.FireAndForgetDelivery = true)
					.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SessionGrain).Assembly).WithReferences())
					.UseLocalhostClustering();
				})
				.ConfigureLogging(logging =>
				  {

				  })
				.ConfigureServices(services =>
				  {

				  })
				.UseConsoleLifetime();

	}
}
using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FeedbackBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectDirectory = Path.Combine(currentDirectory, "src", "FeedbackBot");
            var contentRoot = File.Exists(Path.Combine(projectDirectory, "appsettings.json"))
                ? projectDirectory
                : currentDirectory;
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(contentRoot)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

            if (environment == "Development")
            {
                configurationBuilder.AddUserSecrets<Program>(optional: true);
            }

            var configuration = configurationBuilder
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseConfiguration(configuration)
                .UseStartup<Startup>();
        }
    }
}

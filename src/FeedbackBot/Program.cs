using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FeedbackBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var contentRoot = GetContentRoot();

            return Host.CreateDefaultBuilder(args)
                .UseContentRoot(contentRoot)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear();

                    config.SetBasePath(contentRoot)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddUserSecrets<Program>(optional: true);
                    }

                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }

        private static string GetContentRoot()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectDirectory = Path.Combine(currentDirectory, "src", "FeedbackBot");

            return File.Exists(Path.Combine(projectDirectory, "appsettings.json"))
                ? projectDirectory
                : currentDirectory;
        }
    }
}

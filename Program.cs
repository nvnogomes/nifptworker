using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

namespace NIFPTWorker;

public class Program {

    public static void Main(string[] args) {
        // Read Configuration from appSettings
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Initialize Logger
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        try {
            var host = CreateHostBuilder(args).Build();
            Log.Information("Starting Application.");
            host.Run();
        } catch (Exception ex) {
            Log.Fatal(ex, "Application terminated unexpectedly.");
        } finally {
            Log.CloseAndFlush();
        }
    }


    public static IHostBuilder CreateHostBuilder(string[] args) {
        var host = Host.CreateDefaultBuilder(args);

        // IConfiguration configuration in your constructors
        host.ConfigureAppConfiguration(
              (hostContext, config) => {
                  config.SetBasePath(Directory.GetCurrentDirectory());
                  config.AddJsonFile("appsettings.json", false, true);
                  config.AddCommandLine(args);
              }
        );

        // logging
        host.ConfigureLogging(
            loggingBuilder => {
                loggingBuilder.AddSerilog();
            });

        // services
        host.ConfigureServices((hostContext, services) => {
            services.Configure<ServiceConfiguration>(hostContext.Configuration.GetSection(ServiceConfiguration.Section));
            services.AddSingleton<NIFptService>();

            services.AddSingleton<DbContext>();

            services.AddHostedService<Worker>();
            services.AddLogging();
        });

        return host;
    }
}

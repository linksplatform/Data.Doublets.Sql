using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sql;
using Serilog;

namespace Platform.Data.Doublets.Sql.Server;

public class Program
{
    private static readonly string DefaultDatabaseFileName = "db.links";
    public static string DatabaseFileName { get; set; } = DefaultDatabaseFileName;

    public static int Main(params string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting SQL Server for Doublets");

            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                DatabaseFileName = args[0];
                Log.Information("Using database file: {DatabaseFile}", DatabaseFileName);
            }

            var serverPort = 1433;
            if (args.Length > 1 && int.TryParse(args[1], out var port))
            {
                serverPort = port;
            }

            CreateHostBuilder(args, serverPort).Build().Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args, int port) => 
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseUrls($"http://localhost:{port}")
                    .UseStartup<Startup>();
            });
}
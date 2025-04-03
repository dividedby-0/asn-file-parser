using AsnFileParser.Services;
using Microsoft.Extensions.Configuration;

class Program
{
    private static IConfiguration Configuration;
    private static string folderPath;
    private static int pollingInterval;
    private static string connectionString;
    private static string dbPassword;

    static async Task Main()
    {
        LoadConfiguration();

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var dbService = new DatabaseService(connectionString);
        var parser = new AsnParser();

        Console.WriteLine($"Monitoring folder: {folderPath} (Polling every {pollingInterval}ms)");

        CancellationTokenSource cts = new();
        Task monitorTask = MonitorFolderAsync(parser, dbService, cts.Token);

        Console.WriteLine("Press any key to stop monitoring...");
        Console.ReadKey();
        cts.Cancel();

        await monitorTask;
    }

    private static void LoadConfiguration()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        connectionString = Configuration["Database:ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found in configuration.");
        }

        dbPassword = Environment.GetEnvironmentVariable("MSSQL_DB_PASSWORD");

        if (string.IsNullOrEmpty(dbPassword))
        {
            throw new InvalidOperationException("Environment variable MSSQL_DB_PASSWORD not found.");
        }

        connectionString = connectionString.Replace("{dbPassword}", dbPassword);


        folderPath = Path.Combine(Directory.GetCurrentDirectory(), Configuration["FileWatcher:DirectoryPath"]);
        pollingInterval = int.Parse(Configuration["FileWatcher:PollIntervalMs"]);
    }

    private static async Task MonitorFolderAsync(AsnParser parser, DatabaseService dbService, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var files = Directory.GetFiles(folderPath, "*.txt")
                .OrderBy(f => new FileInfo(f).CreationTimeUtc) // in case multiple files are dropped at the same time
                .ToList();

            foreach (var file in files)
            {
                Console.WriteLine($"Detected new file: {Path.GetFileName(file)}");

                // avoids conflicting filenames in processed folder
                string uniqueFilename = Path.Combine(
                    folderPath,
                    $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(file)}"
                );

                File.Move(file, uniqueFilename);

                var boxes = await parser.ParseFileAsync(uniqueFilename);

                foreach (var box in boxes)
                {
                    await dbService.InsertBoxAsync(box);
                }

                await dbService.LogFileProcessedAsync(Path.GetFileName(uniqueFilename));

                try
                {
                    string processedFolder = Path.Combine(folderPath, "processed");
                    Directory.CreateDirectory(processedFolder);
                    string destination = Path.Combine(processedFolder, Path.GetFileName(uniqueFilename));
                    File.Move(uniqueFilename, destination);

                    Console.WriteLine($"File processed and moved to: {destination}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing or moving file '{uniqueFilename}': {ex.Message}");
                }
            }

            await Task.Delay(pollingInterval, token); // polling in case of network issues and to prevent excessive CPU usage
        }
    }
}
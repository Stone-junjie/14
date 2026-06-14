using ISO11820.App.Forms;
using ISO11820.Data;
using ISO11820.Services;
using Microsoft.Extensions.Configuration;

namespace ISO11820.App;

static class Program
{
    public static IConfiguration Configuration { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(baseDir, "appsettings.json");
        if (!File.Exists(configPath))
        {
            // Try parent directory (when running from project dir during development)
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        Configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configPath)!)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        var dbPath = Configuration["Database:SqlitePath"]!;
        var fullDbPath = Path.Combine(Path.GetDirectoryName(configPath)!, dbPath);
        var dbDir = Path.GetDirectoryName(fullDbPath)!;
        if (!Directory.Exists(dbDir))
            Directory.CreateDirectory(dbDir);

        var dbHelper = new DbHelper(fullDbPath);
        DatabaseInitializer.Initialize(dbHelper);

        var configService = new ConfigurationService(Configuration);
        var appContext = new AppContext(dbHelper, configService);

        Application.Run(new LoginForm(appContext));
    }
}

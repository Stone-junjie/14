using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Services;
using ISO11820.Services.Simulation;

namespace ISO11820.App;

public class AppContext
{
    public DbHelper Db { get; }
    public ConfigurationService Config { get; }
    public TestController TestController { get; }
    public SensorSimulator Simulator { get; }
    public DaqWorker DaqWorker { get; }
    public ExportService ExportService { get; }

    public AppContext(DbHelper db, ConfigurationService config)
    {
        Db = db;
        Config = config;
        TestController = new TestController(Db);
        Simulator = new SensorSimulator(Config);
        DaqWorker = new DaqWorker(TestController, Simulator);
        ExportService = new ExportService(Config);
    }
}

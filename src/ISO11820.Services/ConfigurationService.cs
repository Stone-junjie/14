using Microsoft.Extensions.Configuration;

namespace ISO11820.Services;

public class ConfigurationService
{
    private readonly IConfiguration _config;

    public ConfigurationService(IConfiguration config)
    {
        _config = config;
    }

    public string SqlitePath => _config["Database:SqlitePath"] ?? "Data\\ISO11820.db";
    public int ConstPower => int.Parse(_config["Hardware:ConstPower"] ?? "2048");
    public double PidTemperature => double.Parse(_config["Hardware:PidTemperature"] ?? "750");
    public bool EnableSimulation => bool.Parse(_config["Simulation:EnableSimulation"] ?? "true");
    public double InitialFurnaceTemp => double.Parse(_config["Simulation:InitialFurnaceTemp"] ?? "720");
    public double TargetFurnaceTemp => double.Parse(_config["Simulation:TargetFurnaceTemp"] ?? "750");
    public double HeatingRatePerSecond => double.Parse(_config["Simulation:HeatingRatePerSecond"] ?? "40");
    public double TempFluctuation => double.Parse(_config["Simulation:TempFluctuation"] ?? "0.5");
    public double StableThreshold => double.Parse(_config["Simulation:StableThreshold"] ?? "3.0");
    public string BaseDirectory => _config["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
    public string TestDataDirectory => _config["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";
    public string ReportOutputDirectory => _config["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";
    public bool EnablePdfExport => bool.Parse(_config["Report:EnablePdfExport"] ?? "true");
}

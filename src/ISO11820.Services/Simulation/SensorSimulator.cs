namespace ISO11820.Services.Simulation;

public class SensorSimulator
{
    private readonly ConfigurationService _config;
    private readonly Random _rng = new();
    private bool _isRecording;

    public SensorSimulator(ConfigurationService config)
    {
        _config = config;
    }

    public void SetRecording(bool recording) => _isRecording = recording;

    public Dictionary<string, double> Update(Dictionary<string, double> currentValues, bool isRecording)
    {
        _isRecording = isRecording;
        double tf1 = currentValues.GetValueOrDefault("TF1", 25.0);
        double tf2 = currentValues.GetValueOrDefault("TF2", 25.0);
        double ts = currentValues.GetValueOrDefault("TS", 25.0);
        double tc = currentValues.GetValueOrDefault("TC", 25.0);

        double fluctuation = _config.TempFluctuation;
        double targetTemp = _config.TargetFurnaceTemp;
        double heatRate = _config.HeatingRatePerSecond * 0.8;

        double Noise(double amp = 1.0) => (_rng.NextDouble() * 2 - 1) * fluctuation * amp;

        if (tf1 < targetTemp - _config.StableThreshold)
        {
            // 升温阶段
            tf1 += heatRate + Noise();
            tf2 += heatRate + Noise();
            ts = tf1 * 0.3 + Noise(0.5);
            tc = tf1 * 0.25 + Noise(0.5);
        }
        else
        {
            // 稳定阶段
            tf1 = targetTemp + Noise();
            tf2 = targetTemp + Noise();

            if (_isRecording)
            {
                double surfaceTarget = Math.Min(tf1 * 0.95, 800);
                ts += (surfaceTarget - ts) * 0.02 + Noise(0.5);
                double centerTarget = Math.Min(tf1 * 0.85, 750);
                tc += (centerTarget - tc) * 0.01 + Noise(0.5);
            }
            else
            {
                ts = tf1 * 0.3 + Noise(0.5);
                tc = tf1 * 0.25 + Noise(0.5);
            }
        }

        double tCal = tf1 + Noise(2.0);

        return new Dictionary<string, double>
        {
            ["TF1"] = Math.Round(tf1, 1),
            ["TF2"] = Math.Round(tf2, 1),
            ["TS"] = Math.Round(ts, 1),
            ["TC"] = Math.Round(tc, 1),
            ["TCal"] = Math.Round(tCal, 1)
        };
    }
}

using ISO11820.Core;
using ISO11820.Core.Enums;
using ISO11820.Services.Simulation;
using System.Timers;

namespace ISO11820.Services;

public class DaqWorker : IDisposable
{
    private readonly TestController _controller;
    private readonly SensorSimulator _simulator;
    private readonly System.Timers.Timer _timer;
    private bool _disposed;

    public DaqWorker(TestController controller, SensorSimulator simulator)
    {
        _controller = controller;
        _simulator = simulator;
        _timer = new System.Timers.Timer(800);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = true;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        try
        {
            bool isRecording = _controller.CurrentState == TestState.Recording;
            var temps = _simulator.Update(_controller.SensorValues, isRecording);
            _controller.OnTemperatureUpdated(temps);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DaqWorker error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _timer.Dispose();
            _disposed = true;
        }
    }
}

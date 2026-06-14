namespace ISO11820.Core.Events;

public class MasterMessage
{
    public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Message { get; set; } = "";

    public MasterMessage() { }

    public MasterMessage(string message)
    {
        Time = DateTime.Now.ToString("HH:mm:ss");
        Message = message;
    }
}

public class DataBroadcastEventArgs : EventArgs
{
    public Dictionary<string, double> SensorValues { get; set; } = new();
    public Core.Enums.TestState CurrentState { get; set; }
    public int ElapsedSeconds { get; set; }
    public double TemperatureDrift { get; set; }
    public List<MasterMessage> Messages { get; set; } = new();
    public string ProductId { get; set; } = "";
    public string TestId { get; set; } = "";
}

public class SensorDataPoint
{
    public int Time { get; set; }
    public double Temp1 { get; set; }
    public double Temp2 { get; set; }
    public double TempSurface { get; set; }
    public double TempCenter { get; set; }
    public double TempCalibration { get; set; }
}

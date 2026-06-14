using ISO11820.Core.Enums;
using ISO11820.Core.Events;
using ISO11820.Data;

namespace ISO11820.Core;

public class TestController
{
    private readonly DbHelper _db;
    private TestState _state = TestState.Idle;

    public TestState CurrentState => _state;
    public string CurrentProductId { get; private set; } = "";
    public string CurrentTestId { get; private set; } = "";
    public string OperatorName { get; private set; } = "";
    public string OperatorType { get; private set; } = "";

    public Dictionary<string, double> SensorValues { get; } = new()
    {
        ["TF1"] = 25.0,
        ["TF2"] = 25.0,
        ["TS"] = 25.0,
        ["TC"] = 25.0,
        ["TCal"] = 25.0
    };

    public double AmbientTemp { get; set; } = 25.0;
    public double PreWeight { get; set; }
    public int TargetDurationSeconds { get; set; } = 3600;

    public int ElapsedSeconds { get; set; }
    public int StableCounter { get; set; }

    public List<double> PidOutputQueue { get; } = new();
    public int ConstPower { get; set; } = 2048;

    public List<SensorDataPoint> SensorHistory { get; } = new();

    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;
    public event EventHandler<string>? StateChanged;

    public TestController(DbHelper db)
    {
        _db = db;
    }

    public void SetOperator(string name, string type)
    {
        OperatorName = name;
        OperatorType = type;
    }

    public bool HasUnfinishedTest() => _db.HasUnfinishedTest();

    public TestMaster? GetUnfinishedTest() => _db.GetUnfinishedTest();

    public void CreateTest(string productId, string productName, string specific,
        double diameter, double height, double preWeight, double ambTemp, double ambHumi,
        string testId)
    {
        _db.InsertProduct(productId, productName, specific, diameter, height);

        var apparatus = _db.GetApparatus(0)
            ?? throw new Exception("设备信息未找到");

        _db.InsertTest(productId, testId, OperatorName, preWeight, ambTemp, ambHumi,
            productId, apparatus.ApparatusId.ToString(), apparatus.ApparatusName,
            apparatus.CheckDateF.ToString("yyyy-MM-dd"));

        CurrentProductId = productId;
        CurrentTestId = testId;
        PreWeight = preWeight;
        AmbientTemp = ambTemp;
        ElapsedSeconds = 0;
        SensorHistory.Clear();
    }

    public void StartHeating(double initialTemp)
    {
        if (_state != TestState.Idle)
            throw new InvalidOperationException($"当前状态 {_state} 不允许开始升温");

        SensorValues["TF1"] = initialTemp;
        SensorValues["TF2"] = initialTemp;
        _state = TestState.Preparing;
        StableCounter = 0;
        StateChanged?.Invoke(this, $"状态变更: {_state}");
    }

    public void StopHeating()
    {
        if (_state != TestState.Preparing && _state != TestState.Ready)
            throw new InvalidOperationException($"当前状态 {_state} 不允许停止升温");

        _state = TestState.Idle;
        StateChanged?.Invoke(this, $"状态变更: {_state}");
    }

    public void StartRecording()
    {
        if (_state != TestState.Ready)
            throw new InvalidOperationException($"当前状态 {_state} 不允许开始记录");

        if (PidOutputQueue.Count > 0)
            ConstPower = (int)PidOutputQueue.Average();
        else
            ConstPower = 2048;

        _state = TestState.Recording;
        ElapsedSeconds = 0;
        SensorHistory.Clear();
        StateChanged?.Invoke(this, $"状态变更: {_state}");
    }

    public void StopRecording()
    {
        if (_state != TestState.Recording)
            throw new InvalidOperationException($"当前状态 {_state} 不允许停止记录");

        _state = TestState.Complete;
        StateChanged?.Invoke(this, $"状态变更: {_state}");
    }

    public void ResetToPreparing()
    {
        _state = TestState.Preparing;
        StateChanged?.Invoke(this, $"状态变更: {_state}");
    }

    public void OnTemperatureUpdated(Dictionary<string, double> temps)
    {
        SensorValues["TF1"] = temps["TF1"];
        SensorValues["TF2"] = temps["TF2"];
        SensorValues["TS"] = temps["TS"];
        SensorValues["TC"] = temps["TC"];
        SensorValues["TCal"] = temps["TCal"];

        var messages = new List<MasterMessage>();
        double tf1 = temps["TF1"];

        switch (_state)
        {
            case TestState.Preparing:
                if (tf1 >= 745.0 && tf1 <= 755.0)
                {
                    StableCounter++;
                    if (StableCounter > 3)
                    {
                        _state = TestState.Ready;
                        messages.Add(new MasterMessage("温度已稳定，可以开始记录"));
                        StateChanged?.Invoke(this, $"状态变更: {_state}");
                    }
                }
                else
                {
                    StableCounter = 0;
                }
                break;

            case TestState.Ready:
                if (tf1 < 745.0 || tf1 > 755.0)
                {
                    _state = TestState.Preparing;
                    StableCounter = 0;
                    StateChanged?.Invoke(this, $"状态变更: {_state}");
                }
                break;

            case TestState.Recording:
                ElapsedSeconds++;
                SensorHistory.Add(new SensorDataPoint
                {
                    Time = ElapsedSeconds,
                    Temp1 = temps["TF1"],
                    Temp2 = temps["TF2"],
                    TempSurface = temps["TS"],
                    TempCenter = temps["TC"],
                    TempCalibration = temps["TCal"]
                });

                if (TargetDurationSeconds > 0 && ElapsedSeconds >= TargetDurationSeconds)
                {
                    _state = TestState.Complete;
                    messages.Add(new MasterMessage($"记录时间到达 {ElapsedSeconds} 秒，试验自动结束"));
                    StateChanged?.Invoke(this, $"状态变更: {_state}");
                }
                break;
        }

        DataBroadcast?.Invoke(this, new DataBroadcastEventArgs
        {
            SensorValues = new Dictionary<string, double>(SensorValues),
            CurrentState = _state,
            ElapsedSeconds = ElapsedSeconds,
            ProductId = CurrentProductId,
            TestId = CurrentTestId,
            Messages = messages
        });
    }

    public double CalculateTemperatureDrift()
    {
        var recentData = SensorHistory
            .Where(d => d.Time >= ElapsedSeconds - 600)
            .ToList();
        if (recentData.Count < 10) return 0;

        var first = recentData.First();
        var last = recentData.Last();
        double timeSpan = last.Time - first.Time;
        if (timeSpan == 0) return 0;
        return (last.Temp1 - first.Temp1) / timeSpan * 600;
    }
}

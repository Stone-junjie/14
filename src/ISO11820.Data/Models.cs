namespace ISO11820.Data;

public class Operator
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Pwd { get; set; } = "";
    public string UserType { get; set; } = "";
}

public class Apparatus
{
    public int ApparatusId { get; set; }
    public string InnerNumber { get; set; } = "";
    public string ApparatusName { get; set; } = "";
    public DateTime CheckDateF { get; set; }
    public DateTime CheckDateT { get; set; }
    public string PidPort { get; set; } = "";
    public string PowerPort { get; set; } = "";
    public int? ConstPower { get; set; }
}

public class ProductMaster
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Specific { get; set; } = "";
    public double Diameter { get; set; }
    public double Height { get; set; }
    public string? Flag { get; set; }
}

public class TestMaster
{
    public string ProductId { get; set; } = "";
    public string TestId { get; set; } = "";
    public DateTime TestDate { get; set; }
    public double AmbTemp { get; set; }
    public double AmbHumi { get; set; }
    public string According { get; set; } = "ISO 11820:2022";
    public string Operator { get; set; } = "";
    public string ApparatusId { get; set; } = "";
    public string ApparatusName { get; set; } = "";
    public DateTime ApparatusChkDate { get; set; }
    public string RptNo { get; set; } = "";
    public double PreWeight { get; set; }
    public double PostWeight { get; set; }
    public double LostWeight { get; set; }
    public double LostWeightPer { get; set; }
    public int TotalTestTime { get; set; }
    public int ConstPower { get; set; }
    public string PhenoCode { get; set; } = "";
    public int FlameTime { get; set; }
    public int FlameDuration { get; set; }
    public double MaxTf1 { get; set; }
    public double MaxTf2 { get; set; }
    public double MaxTs { get; set; }
    public double MaxTc { get; set; }
    public int MaxTf1Time { get; set; }
    public int MaxTf2Time { get; set; }
    public int MaxTsTime { get; set; }
    public int MaxTcTime { get; set; }
    public double FinalTf1 { get; set; }
    public double FinalTf2 { get; set; }
    public double FinalTs { get; set; }
    public double FinalTc { get; set; }
    public int FinalTf1Time { get; set; }
    public int FinalTf2Time { get; set; }
    public int FinalTsTime { get; set; }
    public int FinalTcTime { get; set; }
    public double DeltaTf1 { get; set; }
    public double DeltaTf2 { get; set; }
    public double DeltaTf { get; set; }
    public double DeltaTs { get; set; }
    public double DeltaTc { get; set; }
    public string? Memo { get; set; }
    public string? Flag { get; set; }
}

public class Sensor
{
    public int SensorId { get; set; }
    public string SensorName { get; set; } = "";
    public string DispName { get; set; } = "";
    public string SensorGroup { get; set; } = "";
    public string Unit { get; set; } = "℃";
    public string Discription { get; set; } = "";
    public string Flag { get; set; } = "";
    public double SignalZero { get; set; }
    public double SignalSpan { get; set; }
    public double OutputZero { get; set; }
    public double OutputSpan { get; set; }
    public double OutputValue { get; set; }
    public double InputValue { get; set; }
    public int SignalType { get; set; }
}

public class CalibrationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CalibrationDate { get; set; } = DateTime.Now.ToString("O");
    public string CalibrationType { get; set; } = "";
    public int ApparatusId { get; set; }
    public string Operator { get; set; } = "";
    public string TemperatureData { get; set; } = "[]";
    public double? UniformityResult { get; set; }
    public double? MaxDeviation { get; set; }
    public double? AverageTemperature { get; set; }
    public int PassedCriteria { get; set; }
    public string Remarks { get; set; } = "";
    public string CreatedAt { get; set; } = DateTime.Now.ToString("O");
    public double? TempA1 { get; set; } public double? TempA2 { get; set; } public double? TempA3 { get; set; }
    public double? TempB1 { get; set; } public double? TempB2 { get; set; } public double? TempB3 { get; set; }
    public double? TempC1 { get; set; } public double? TempC2 { get; set; } public double? TempC3 { get; set; }
    public double? TAvg { get; set; }
    public double? TAvgAxis1 { get; set; } public double? TAvgAxis2 { get; set; } public double? TAvgAxis3 { get; set; }
    public double? TAvgLevela { get; set; } public double? TAvgLevelb { get; set; } public double? TAvgLevelc { get; set; }
    public double? TDevAxis1 { get; set; } public double? TDevAxis2 { get; set; } public double? TDevAxis3 { get; set; }
    public double? TDevLevela { get; set; } public double? TDevLevelb { get; set; } public double? TDevLevelc { get; set; }
    public double? TAvgDevAxis { get; set; } public double? TAvgDevLevel { get; set; }
    public string? CenterTempData { get; set; }
    public string? Memo { get; set; }
}

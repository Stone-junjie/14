# ISO 11820 建筑材料不燃性试验仿真系统 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 从零构建完整的 ISO 11820 不燃性试验仿真系统，包含登录、新建试验、仿真温度生成、实时曲线图、数据导出和历史查询。

**Architecture:** 四层架构 — UI (WinForms) → Core (状态机/模型) → Services (仿真/导出) → Data (SQLite)。数据向上通过事件传递，UI 跨线程用 Invoke 保护。

**Tech Stack:** C# .NET 8, WinForms, SQLite (Microsoft.Data.Sqlite), OxyPlot 2.x, EPPlus 7.x, PDFsharp-MigraDoc 6.x, MathNet.Numerics 5.x, Serilog 4.x, Microsoft.Extensions.Configuration 8.x

---

## 项目文件结构

```
ISO11820/
├── ISO11820.sln
├── appsettings.json
│
├── src/
│   ├── ISO11820.App/              # WinForms 主程序 (.exe)
│   │   ├── ISO11820.App.csproj
│   │   ├── Program.cs
│   │   ├── AppContext.cs
│   │   ├── Forms/
│   │   │   ├── LoginForm.cs
│   │   │   ├── MainForm.cs
│   │   │   ├── NewTestForm.cs
│   │   │   └── TestRecordForm.cs
│   │   └── appsettings.json
│   │
│   ├── ISO11820.Core/             # 业务核心层
│   │   ├── ISO11820.Core.csproj
│   │   ├── Enums/
│   │   │   └── TestState.cs
│   │   ├── Models/
│   │   │   ├── Operator.cs
│   │   │   ├── Apparatus.cs
│   │   │   ├── ProductMaster.cs
│   │   │   ├── TestMaster.cs
│   │   │   ├── Sensor.cs
│   │   │   ├── CalibrationRecord.cs
│   │   │   └── MasterMessage.cs
│   │   ├── Events/
│   │   │   └── DataBroadcastEventArgs.cs
│   │   └── TestController.cs
│   │
│   ├── ISO11820.Services/         # 服务层
│   │   ├── ISO11820.Services.csproj
│   │   ├── Simulation/
│   │   │   └── SensorSimulator.cs
│   │   ├── DaqWorker.cs
│   │   ├── ExportService.cs
│   │   └── ConfigurationService.cs
│   │
│   └── ISO11820.Data/             # 数据层
│       ├── ISO11820.Data.csproj
│       ├── DbHelper.cs
│       └── DatabaseInitializer.cs
```

---

### Task 1: 项目骨架搭建

**Files:**
- Create: `ISO11820.sln`
- Create: `src/ISO11820.App/ISO11820.App.csproj`
- Create: `src/ISO11820.Core/ISO11820.Core.csproj`
- Create: `src/ISO11820.Services/ISO11820.Services.csproj`
- Create: `src/ISO11820.Data/ISO11820.Data.csproj`
- Create: `appsettings.json`
- Create: `src/ISO11820.App/Program.cs`

- [ ] **Step 1: 创建解决方案和四个项目**

```bash
cd C:/Users/23072/Desktop/homework/test-heat
mkdir -p src/ISO11820.App/Forms
mkdir -p src/ISO11820.Core/Enums
mkdir -p src/ISO11820.Core/Models
mkdir -p src/ISO11820.Core/Events
mkdir -p src/ISO11820.Services/Simulation
mkdir -p src/ISO11820.Data

dotnet new sln -n ISO11820
dotnet new winforms -n ISO11820.App -o src/ISO11820.App --framework net8.0
dotnet new classlib -n ISO11820.Core -o src/ISO11820.Core --framework net8.0
dotnet new classlib -n ISO11820.Services -o src/ISO11820.Services --framework net8.0
dotnet new classlib -n ISO11820.Data -o src/ISO11820.Data --framework net8.0

dotnet sln add src/ISO11820.App/ISO11820.App.csproj
dotnet sln add src/ISO11820.Core/ISO11820.Core.csproj
dotnet sln add src/ISO11820.Services/ISO11820.Services.csproj
dotnet sln add src/ISO11820.Data/ISO11820.Data.csproj
```

- [ ] **Step 2: 添加项目引用**

```bash
dotnet add src/ISO11820.App/ISO11820.App.csproj reference src/ISO11820.Core/ISO11820.Core.csproj
dotnet add src/ISO11820.App/ISO11820.App.csproj reference src/ISO11820.Services/ISO11820.Services.csproj
dotnet add src/ISO11820.App/ISO11820.App.csproj reference src/ISO11820.Data/ISO11820.Data.csproj
dotnet add src/ISO11820.Services/ISO11820.Services.csproj reference src/ISO11820.Core/ISO11820.Core.csproj
dotnet add src/ISO11820.Services/ISO11820.Services.csproj reference src/ISO11820.Data/ISO11820.Data.csproj
dotnet add src/ISO11820.Core/ISO11820.Core.csproj reference src/ISO11820.Data/ISO11820.Data.csproj
```

- [ ] **Step 3: 安装 NuGet 包**

```bash
dotnet add src/ISO11820.App package OxyPlot.WindowsForms --version 2.2.0
dotnet add src/ISO11820.App package EPPlus --version 7.4.0
dotnet add src/ISO11820.App package PDFsharp-MigraDoc --version 6.1.1
dotnet add src/ISO11820.App package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add src/ISO11820.Services package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add src/ISO11820.Services package Microsoft.Extensions.Configuration.Binder --version 8.0.0
dotnet add src/ISO11820.Services package MathNet.Numerics --version 5.0.0
dotnet add src/ISO11820.Services package Serilog --version 4.0.0
dotnet add src/ISO11820.Services package Serilog.Sinks.File --version 6.0.0
dotnet add src/ISO11820.Data package Microsoft.Data.Sqlite --version 8.0.10
dotnet add src/ISO11820.Data package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add src/ISO11820.Data package Microsoft.Extensions.Configuration.Binder --version 8.0.0
dotnet add src/ISO11820.Core package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add src/ISO11820.Core package Microsoft.Extensions.Configuration.Binder --version 8.0.0
```

- [ ] **Step 4: 删除自动生成的 Class1.cs**

```bash
rm -f src/ISO11820.Core/Class1.cs
rm -f src/ISO11820.Services/Class1.cs
rm -f src/ISO11820.Data/Class1.cs
rm -f src/ISO11820.App/Form1.cs src/ISO11820.App/Form1.Designer.cs
```

- [ ] **Step 5: 创建根目录 appsettings.json**

Write `appsettings.json`:
```json
{
  "Database": {
    "Provider": "Sqlite",
    "SqlitePath": "Data\\ISO11820.db"
  },
  "Hardware": {
    "ConstPower": 2048,
    "PidTemperature": 750,
    "SensorProtocol": "ModbusRtu"
  },
  "Simulation": {
    "EnableSimulation": true,
    "SimulateSensors": true,
    "SimulatePidController": true,
    "InitialFurnaceTemp": 720.0,
    "TargetFurnaceTemp": 750.0,
    "HeatingRatePerSecond": 40.0,
    "TempFluctuation": 0.5,
    "StableThreshold": 3.0,
    "SimulateFlame": false
  },
  "FileStorage": {
    "BaseDirectory": "D:\\ISO11820",
    "TestDataDirectory": "D:\\ISO11820\\TestData"
  },
  "Report": {
    "OutputDirectory": "D:\\ISO11820\\Reports",
    "EnablePdfExport": true
  }
}
```

- [ ] **Step 6: 创建 Program.cs**

Write `src/ISO11820.App/Program.cs`:
```csharp
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

        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var dbPath = Configuration["Database:SqlitePath"]!;
        var fullDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
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
```

- [ ] **Step 7: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: scaffold project structure with solution, projects, NuGet packages, and appsettings"
```

---

### Task 2: 数据层 — DbHelper 和 DatabaseInitializer

**Files:**
- Create: `src/ISO11820.Data/DbHelper.cs`
- Create: `src/ISO11820.Data/DatabaseInitializer.cs`

- [ ] **Step 1: 创建 DbHelper.cs**

Write `src/ISO11820.Data/DbHelper.cs`:
```csharp
using Microsoft.Data.Sqlite;

namespace ISO11820.Data;

public class DbHelper
{
    private readonly string _connStr;

    public DbHelper(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        // Enable WAL mode for better concurrency
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL";
        cmd.ExecuteNonQuery();
    }

    public SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        return conn;
    }

    // ===== 登录验证 =====
    public bool Login(string username, string pwd, out string userid, out string usertype)
    {
        userid = ""; usertype = "";
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, usertype FROM operators WHERE username=$name AND pwd=$pwd";
        cmd.Parameters.AddWithValue("$name", username);
        cmd.Parameters.AddWithValue("$pwd", pwd);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            userid = reader.GetString(0);
            usertype = reader.GetString(1);
            return true;
        }
        return false;
    }

    // ===== 样品操作 =====
    public void InsertProduct(string productId, string productName, string specific,
        double diameter, double height)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO productmaster (productid, productname, specific, diameter, height)
            VALUES ($pid, $pname, $spec, $diam, $h)";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$pname", productName);
        cmd.Parameters.AddWithValue("$spec", specific);
        cmd.Parameters.AddWithValue("$diam", diameter);
        cmd.Parameters.AddWithValue("$h", height);
        cmd.ExecuteNonQuery();
    }

    public ProductMaster? GetProduct(string productId)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT productid, productname, specific, diameter, height, flag FROM productmaster WHERE productid=$pid";
        cmd.Parameters.AddWithValue("$pid", productId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new ProductMaster
            {
                ProductId = reader.GetString(0),
                ProductName = reader.GetString(1),
                Specific = reader.GetString(2),
                Diameter = reader.GetDouble(3),
                Height = reader.GetDouble(4),
                Flag = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }
        return null;
    }

    // ===== 设备操作 =====
    public Apparatus? GetApparatus(int apparatusId)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower FROM apparatus WHERE apparatusid=$id";
        cmd.Parameters.AddWithValue("$id", apparatusId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Apparatus
            {
                ApparatusId = reader.GetInt32(0),
                InnerNumber = reader.GetString(1),
                ApparatusName = reader.GetString(2),
                CheckDateF = reader.GetDateTime(3),
                CheckDateT = reader.GetDateTime(4),
                PidPort = reader.GetString(5),
                PowerPort = reader.GetString(6),
                ConstPower = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            };
        }
        return null;
    }

    // ===== 试验操作 =====
    public void InsertTest(string productId, string testId, string operatorName,
        double preweight, double ambtemp, double ambhumi, string rptno,
        string apparatusId, string apparatusName, string apparatusChkDate)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO testmaster
                (productid, testid, testdate, operator, ambtemp, ambhumi,
                 according, apparatusid, apparatusname, apparatuschkdate, rptno,
                 preweight, postweight, lostweight, lostweight_per,
                 totaltesttime, constpower, phenocode, flametime, flameduration,
                 maxtf1,maxtf2,maxts,maxtc,
                 maxtf1_time,maxtf2_time,maxts_time,maxtc_time,
                 finaltf1,finaltf2,finalts,finaltc,
                 finaltf1_time,finaltf2_time,finalts_time,finaltc_time,
                 deltatf1,deltatf2,deltatf,deltats,deltatc)
            VALUES
                ($pid,$tid,date('now'),$op,$ambtemp,$ambhumi,
                 'ISO 11820:2022',$apid,$apname,$apchk,$rptno,
                 $prewt,0,0,0,
                 0,0,'',0,0,
                 0,0,0,0,0,0,0,0,
                 0,0,0,0,0,0,0,0,
                 0,0,0,0,0)";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        cmd.Parameters.AddWithValue("$op", operatorName);
        cmd.Parameters.AddWithValue("$ambtemp", ambtemp);
        cmd.Parameters.AddWithValue("$ambhumi", ambhumi);
        cmd.Parameters.AddWithValue("$apid", apparatusId);
        cmd.Parameters.AddWithValue("$apname", apparatusName);
        cmd.Parameters.AddWithValue("$apchk", apparatusChkDate);
        cmd.Parameters.AddWithValue("$rptno", rptno);
        cmd.Parameters.AddWithValue("$prewt", preweight);
        cmd.ExecuteNonQuery();
    }

    public void UpdateTestResult(string productId, string testId,
        double preweight, double postweight, double lostPer,
        double deltaTf, double deltaTs, double deltaTc,
        double deltaTf1, double deltaTf2,
        int totalTime, string phenocode,
        int flameTime, int flameDuration,
        double maxTf1, double maxTf2, double maxTs, double maxTc,
        int maxTf1Time, int maxTf2Time, int maxTsTime, int maxTcTime,
        double finalTf1, double finalTf2, double finalTs, double finalTc,
        int finalTf1Time, int finalTf2Time, int finalTsTime, int finalTcTime)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE testmaster SET
                postweight = $post,
                lostweight = $lost,
                lostweight_per = $lostper,
                deltatf = $dtf,
                deltats = $dts,
                deltatc = $dtc,
                deltatf1 = $dtf1,
                deltatf2 = $dtf2,
                totaltesttime = $time,
                phenocode = $pheno,
                flametime = $ftime,
                flameduration = $fdur,
                maxtf1 = $mtf1,
                maxtf2 = $mtf2,
                maxts = $mts,
                maxtc = $mtc,
                maxtf1_time = $mtf1t,
                maxtf2_time = $mtf2t,
                maxts_time = $mtst,
                maxtc_time = $mtct,
                finaltf1 = $ftf1,
                finaltf2 = $ftf2,
                finalts = $fts,
                finaltc = $ftc,
                finaltf1_time = $ftf1t,
                finaltf2_time = $ftf2t,
                finalts_time = $ftst,
                finaltc_time = $ftct,
                flag = '10000000'
            WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$post", postweight);
        cmd.Parameters.AddWithValue("$lost", preweight - postweight);
        cmd.Parameters.AddWithValue("$lostper", lostPer);
        cmd.Parameters.AddWithValue("$dtf", deltaTf);
        cmd.Parameters.AddWithValue("$dts", deltaTs);
        cmd.Parameters.AddWithValue("$dtc", deltaTc);
        cmd.Parameters.AddWithValue("$dtf1", deltaTf1);
        cmd.Parameters.AddWithValue("$dtf2", deltaTf2);
        cmd.Parameters.AddWithValue("$time", totalTime);
        cmd.Parameters.AddWithValue("$pheno", phenocode);
        cmd.Parameters.AddWithValue("$ftime", flameTime);
        cmd.Parameters.AddWithValue("$fdur", flameDuration);
        cmd.Parameters.AddWithValue("$mtf1", maxTf1);
        cmd.Parameters.AddWithValue("$mtf2", maxTf2);
        cmd.Parameters.AddWithValue("$mts", maxTs);
        cmd.Parameters.AddWithValue("$mtc", maxTc);
        cmd.Parameters.AddWithValue("$mtf1t", maxTf1Time);
        cmd.Parameters.AddWithValue("$mtf2t", maxTf2Time);
        cmd.Parameters.AddWithValue("$mtst", maxTsTime);
        cmd.Parameters.AddWithValue("$mtct", maxTcTime);
        cmd.Parameters.AddWithValue("$ftf1", finalTf1);
        cmd.Parameters.AddWithValue("$ftf2", finalTf2);
        cmd.Parameters.AddWithValue("$fts", finalTs);
        cmd.Parameters.AddWithValue("$ftc", finalTc);
        cmd.Parameters.AddWithValue("$ftf1t", finalTf1Time);
        cmd.Parameters.AddWithValue("$ftf2t", finalTf2Time);
        cmd.Parameters.AddWithValue("$ftst", finalTsTime);
        cmd.Parameters.AddWithValue("$ftct", finalTcTime);
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        cmd.ExecuteNonQuery();
    }

    public TestMaster? GetTest(string productId, string testId)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return ReadTestMaster(reader);
        }
        return null;
    }

    public List<TestMaster> QueryTests(DateTime from, DateTime to, string productId, string op)
    {
        var result = new List<TestMaster>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        var sql = @"SELECT * FROM testmaster
            WHERE testdate BETWEEN $from AND $to
              AND ($pid = '' OR productid LIKE '%' || $pid || '%')
              AND ($op = '' OR operator = $op)";
        if (!string.IsNullOrEmpty(productId))
        {
            sql += @" AND ($pid = '' OR productid LIKE '%' || $pid || '%')";
        }
        if (!string.IsNullOrEmpty(op))
        {
            sql += @" AND ($op = '' OR operator = $op)";
        }
        // Simplified query
        cmd.CommandText = @"SELECT * FROM testmaster
            WHERE testdate BETWEEN $from AND $to
            ORDER BY testdate DESC";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadTestMaster(reader));
        }
        return result;
    }

    public bool HasUnfinishedTest()
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM testmaster WHERE totaltesttime > 0 AND flag != '10000000'";
        var count = (long)cmd.ExecuteScalar()!;
        return count > 0;
    }

    public TestMaster? GetUnfinishedTest()
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE totaltesttime > 0 AND flag != '10000000' LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return ReadTestMaster(reader);
        }
        return null;
    }

    // ===== 传感器操作 =====
    public List<Sensor> GetSensors()
    {
        var sensors = new List<Sensor>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM sensors ORDER BY sensorid";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            sensors.Add(new Sensor
            {
                SensorId = reader.GetInt32(0),
                SensorName = reader.GetString(1),
                DispName = reader.GetString(2),
                SensorGroup = reader.GetString(3),
                Unit = reader.GetString(4),
                Discription = reader.GetString(5),
                Flag = reader.GetString(6),
                SignalZero = reader.GetDouble(7),
                SignalSpan = reader.GetDouble(8),
                OutputZero = reader.GetDouble(9),
                OutputSpan = reader.GetDouble(10),
                OutputValue = reader.GetDouble(11),
                InputValue = reader.GetDouble(12),
                SignalType = reader.GetInt32(13)
            });
        }
        return sensors;
    }

    public void UpdateSensorValue(int sensorId, double outputValue, double inputValue)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE sensors SET outputvalue=$ov, inputvalue=$iv WHERE sensorid=$sid";
        cmd.Parameters.AddWithValue("$ov", outputValue);
        cmd.Parameters.AddWithValue("$iv", inputValue);
        cmd.Parameters.AddWithValue("$sid", sensorId);
        cmd.ExecuteNonQuery();
    }

    // ===== 校准记录操作 =====
    public void InsertCalibrationRecord(CalibrationRecord record)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO CalibrationRecords
            (Id, CalibrationDate, CalibrationType, ApparatusId, Operator,
             TemperatureData, UniformityResult, MaxDeviation, AverageTemperature,
             PassedCriteria, Remarks, CreatedAt,
             TempA1,TempA2,TempA3,TempB1,TempB2,TempB3,TempC1,TempC2,TempC3,
             TAvg,TAvgAxis1,TAvgAxis2,TAvgAxis3,TAvgLevela,TAvgLevelb,TAvgLevelc,
             TDevAxis1,TDevAxis2,TDevAxis3,TDevLevela,TDevLevelb,TDevLevelc,
             TAvgDevAxis,TAvgDevLevel,CenterTempData,Memo)
            VALUES
            ($id,$date,$type,$apid,$op,
             $tempdata,$uni,$maxdev,$avgtemp,
             $passed,$remarks,$created,
             $ta1,$ta2,$ta3,$tb1,$tb2,$tb3,$tc1,$tc2,$tc3,
             $tavg,$tavga1,$tavga2,$tavga3,$tavglA,$tavglB,$tavglC,
             $tdeva1,$tdeva2,$tdeva3,$tdevlA,$tdevlB,$tdevlC,
             $tavgdeva,$tavgdevl,$ctd,$memo)";
        cmd.Parameters.AddWithValue("$id", record.Id);
        cmd.Parameters.AddWithValue("$date", record.CalibrationDate);
        cmd.Parameters.AddWithValue("$type", record.CalibrationType);
        cmd.Parameters.AddWithValue("$apid", record.ApparatusId);
        cmd.Parameters.AddWithValue("$op", record.Operator);
        cmd.Parameters.AddWithValue("$tempdata", record.TemperatureData);
        cmd.Parameters.AddWithValue("$uni", record.UniformityResult ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$maxdev", record.MaxDeviation ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$avgtemp", record.AverageTemperature ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$passed", record.PassedCriteria);
        cmd.Parameters.AddWithValue("$remarks", record.Remarks);
        cmd.Parameters.AddWithValue("$created", record.CreatedAt);
        // 9 temp points
        cmd.Parameters.AddWithValue("$ta1", record.TempA1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ta2", record.TempA2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ta3", record.TempA3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb1", record.TempB1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb2", record.TempB2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb3", record.TempB3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc1", record.TempC1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc2", record.TempC2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc3", record.TempC3 ?? (object)DBNull.Value);
        // computed values
        cmd.Parameters.AddWithValue("$tavg", record.TAvg ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavga1", record.TAvgAxis1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavga2", record.TAvgAxis2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavga3", record.TAvgAxis3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglA", record.TAvgLevela ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglB", record.TAvgLevelb ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglC", record.TAvgLevelc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdeva1", record.TDevAxis1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdeva2", record.TDevAxis2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdeva3", record.TDevAxis3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlA", record.TDevLevela ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlB", record.TDevLevelb ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlC", record.TDevLevelc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgdeva", record.TAvgDevAxis ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgdevl", record.TAvgDevLevel ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ctd", record.CenterTempData ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", record.Memo ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<CalibrationRecord> GetCalibrationRecords()
    {
        var records = new List<CalibrationRecord>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CalibrationRecords ORDER BY CreatedAt DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(ReadCalibrationRecord(reader));
        }
        return records;
    }

    // ===== 执行原始SQL（初始化用）=====
    public void ExecuteNonQuery(string sql)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // ===== Helper readers =====
    private static TestMaster ReadTestMaster(SqliteDataReader reader)
    {
        return new TestMaster
        {
            ProductId = reader.GetString(reader.GetOrdinal("productid")),
            TestId = reader.GetString(reader.GetOrdinal("testid")),
            TestDate = reader.GetDateTime(reader.GetOrdinal("testdate")),
            AmbTemp = reader.GetDouble(reader.GetOrdinal("ambtemp")),
            AmbHumi = reader.GetDouble(reader.GetOrdinal("ambhumi")),
            According = reader.GetString(reader.GetOrdinal("according")),
            Operator = reader.GetString(reader.GetOrdinal("operator")),
            ApparatusId = reader.GetString(reader.GetOrdinal("apparatusid")),
            ApparatusName = reader.GetString(reader.GetOrdinal("apparatusname")),
            ApparatusChkDate = reader.GetDateTime(reader.GetOrdinal("apparatuschkdate")),
            RptNo = reader.GetString(reader.GetOrdinal("rptno")),
            PreWeight = reader.GetDouble(reader.GetOrdinal("preweight")),
            PostWeight = reader.GetDouble(reader.GetOrdinal("postweight")),
            LostWeight = reader.GetDouble(reader.GetOrdinal("lostweight")),
            LostWeightPer = reader.GetDouble(reader.GetOrdinal("lostweight_per")),
            TotalTestTime = reader.GetInt32(reader.GetOrdinal("totaltesttime")),
            ConstPower = reader.GetInt32(reader.GetOrdinal("constpower")),
            PhenoCode = reader.GetString(reader.GetOrdinal("phenocode")),
            FlameTime = reader.GetInt32(reader.GetOrdinal("flametime")),
            FlameDuration = reader.GetInt32(reader.GetOrdinal("flameduration")),
            MaxTf1 = reader.GetDouble(reader.GetOrdinal("maxtf1")),
            MaxTf2 = reader.GetDouble(reader.GetOrdinal("maxtf2")),
            MaxTs = reader.GetDouble(reader.GetOrdinal("maxts")),
            MaxTc = reader.GetDouble(reader.GetOrdinal("maxtc")),
            MaxTf1Time = reader.GetInt32(reader.GetOrdinal("maxtf1_time")),
            MaxTf2Time = reader.GetInt32(reader.GetOrdinal("maxtf2_time")),
            MaxTsTime = reader.GetInt32(reader.GetOrdinal("maxts_time")),
            MaxTcTime = reader.GetInt32(reader.GetOrdinal("maxtc_time")),
            FinalTf1 = reader.GetDouble(reader.GetOrdinal("finaltf1")),
            FinalTf2 = reader.GetDouble(reader.GetOrdinal("finaltf2")),
            FinalTs = reader.GetDouble(reader.GetOrdinal("finalts")),
            FinalTc = reader.GetDouble(reader.GetOrdinal("finaltc")),
            FinalTf1Time = reader.GetInt32(reader.GetOrdinal("finaltf1_time")),
            FinalTf2Time = reader.GetInt32(reader.GetOrdinal("finaltf2_time")),
            FinalTsTime = reader.GetInt32(reader.GetOrdinal("finalts_time")),
            FinalTcTime = reader.GetInt32(reader.GetOrdinal("finaltc_time")),
            DeltaTf1 = reader.GetDouble(reader.GetOrdinal("deltatf1")),
            DeltaTf2 = reader.GetDouble(reader.GetOrdinal("deltatf2")),
            DeltaTf = reader.GetDouble(reader.GetOrdinal("deltatf")),
            DeltaTs = reader.GetDouble(reader.GetOrdinal("deltats")),
            DeltaTc = reader.GetDouble(reader.GetOrdinal("deltatc")),
            Memo = reader.IsDBNull(reader.GetOrdinal("memo")) ? null : reader.GetString(reader.GetOrdinal("memo")),
            Flag = reader.IsDBNull(reader.GetOrdinal("flag")) ? null : reader.GetString(reader.GetOrdinal("flag"))
        };
    }

    private static CalibrationRecord ReadCalibrationRecord(SqliteDataReader reader)
    {
        return new CalibrationRecord
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            CalibrationDate = reader.GetString(reader.GetOrdinal("CalibrationDate")),
            CalibrationType = reader.GetString(reader.GetOrdinal("CalibrationType")),
            ApparatusId = reader.GetInt32(reader.GetOrdinal("ApparatusId")),
            Operator = reader.GetString(reader.GetOrdinal("Operator")),
            TemperatureData = reader.GetString(reader.GetOrdinal("TemperatureData")),
            UniformityResult = reader.IsDBNull(reader.GetOrdinal("UniformityResult")) ? null : reader.GetDouble(reader.GetOrdinal("UniformityResult")),
            MaxDeviation = reader.IsDBNull(reader.GetOrdinal("MaxDeviation")) ? null : reader.GetDouble(reader.GetOrdinal("MaxDeviation")),
            AverageTemperature = reader.IsDBNull(reader.GetOrdinal("AverageTemperature")) ? null : reader.GetDouble(reader.GetOrdinal("AverageTemperature")),
            PassedCriteria = reader.GetInt32(reader.GetOrdinal("PassedCriteria")),
            Remarks = reader.GetString(reader.GetOrdinal("Remarks")),
            CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt")),
            TempA1 = reader.IsDBNull(reader.GetOrdinal("TempA1")) ? null : reader.GetDouble(reader.GetOrdinal("TempA1")),
            TempA2 = reader.IsDBNull(reader.GetOrdinal("TempA2")) ? null : reader.GetDouble(reader.GetOrdinal("TempA2")),
            TempA3 = reader.IsDBNull(reader.GetOrdinal("TempA3")) ? null : reader.GetDouble(reader.GetOrdinal("TempA3")),
            TempB1 = reader.IsDBNull(reader.GetOrdinal("TempB1")) ? null : reader.GetDouble(reader.GetOrdinal("TempB1")),
            TempB2 = reader.IsDBNull(reader.GetOrdinal("TempB2")) ? null : reader.GetDouble(reader.GetOrdinal("TempB2")),
            TempB3 = reader.IsDBNull(reader.GetOrdinal("TempB3")) ? null : reader.GetDouble(reader.GetOrdinal("TempB3")),
            TempC1 = reader.IsDBNull(reader.GetOrdinal("TempC1")) ? null : reader.GetDouble(reader.GetOrdinal("TempC1")),
            TempC2 = reader.IsDBNull(reader.GetOrdinal("TempC2")) ? null : reader.GetDouble(reader.GetOrdinal("TempC2")),
            TempC3 = reader.IsDBNull(reader.GetOrdinal("TempC3")) ? null : reader.GetDouble(reader.GetOrdinal("TempC3")),
            TAvg = reader.IsDBNull(reader.GetOrdinal("TAvg")) ? null : reader.GetDouble(reader.GetOrdinal("TAvg")),
            TAvgAxis1 = reader.IsDBNull(reader.GetOrdinal("TAvgAxis1")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgAxis1")),
            TAvgAxis2 = reader.IsDBNull(reader.GetOrdinal("TAvgAxis2")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgAxis2")),
            TAvgAxis3 = reader.IsDBNull(reader.GetOrdinal("TAvgAxis3")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgAxis3")),
            TAvgLevela = reader.IsDBNull(reader.GetOrdinal("TAvgLevela")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgLevela")),
            TAvgLevelb = reader.IsDBNull(reader.GetOrdinal("TAvgLevelb")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgLevelb")),
            TAvgLevelc = reader.IsDBNull(reader.GetOrdinal("TAvgLevelc")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgLevelc")),
            TDevAxis1 = reader.IsDBNull(reader.GetOrdinal("TDevAxis1")) ? null : reader.GetDouble(reader.GetOrdinal("TDevAxis1")),
            TDevAxis2 = reader.IsDBNull(reader.GetOrdinal("TDevAxis2")) ? null : reader.GetDouble(reader.GetOrdinal("TDevAxis2")),
            TDevAxis3 = reader.IsDBNull(reader.GetOrdinal("TDevAxis3")) ? null : reader.GetDouble(reader.GetOrdinal("TDevAxis3")),
            TDevLevela = reader.IsDBNull(reader.GetOrdinal("TDevLevela")) ? null : reader.GetDouble(reader.GetOrdinal("TDevLevela")),
            TDevLevelb = reader.IsDBNull(reader.GetOrdinal("TDevLevelb")) ? null : reader.GetDouble(reader.GetOrdinal("TDevLevelb")),
            TDevLevelc = reader.IsDBNull(reader.GetOrdinal("TDevLevelc")) ? null : reader.GetDouble(reader.GetOrdinal("TDevLevelc")),
            TAvgDevAxis = reader.IsDBNull(reader.GetOrdinal("TAvgDevAxis")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgDevAxis")),
            TAvgDevLevel = reader.IsDBNull(reader.GetOrdinal("TAvgDevLevel")) ? null : reader.GetDouble(reader.GetOrdinal("TAvgDevLevel")),
            CenterTempData = reader.IsDBNull(reader.GetOrdinal("CenterTempData")) ? null : reader.GetString(reader.GetOrdinal("CenterTempData")),
            Memo = reader.IsDBNull(reader.GetOrdinal("Memo")) ? null : reader.GetString(reader.GetOrdinal("Memo"))
        };
    }
}
```

- [ ] **Step 2: 创建 DatabaseInitializer.cs**

Write `src/ISO11820.Data/DatabaseInitializer.cs`:
```csharp
namespace ISO11820.Data;

public static class DatabaseInitializer
{
    public static void Initialize(DbHelper db)
    {
        CreateTables(db);
        InsertInitialData(db);
    }

    private static void CreateTables(DbHelper db)
    {
        // operators
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS operators (
                userid TEXT NOT NULL,
                username TEXT NOT NULL,
                pwd TEXT NOT NULL,
                usertype TEXT NOT NULL
            )");

        // apparatus
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS apparatus (
                apparatusid INTEGER NOT NULL PRIMARY KEY,
                innernumber TEXT NOT NULL,
                apparatusname TEXT NOT NULL,
                checkdatef date NOT NULL,
                checkdatet date NOT NULL,
                pidport TEXT NOT NULL,
                powerport TEXT NOT NULL,
                constpower INTEGER NULL
            )");

        // productmaster
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS productmaster (
                productid TEXT NOT NULL PRIMARY KEY,
                productname TEXT NOT NULL,
                specific TEXT NOT NULL,
                diameter REAL NOT NULL,
                height REAL NOT NULL,
                flag TEXT NULL
            )");

        // sensors
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS sensors (
                sensorid INTEGER NOT NULL PRIMARY KEY,
                sensorname TEXT NOT NULL,
                dispname TEXT NOT NULL,
                sensorgroup TEXT NOT NULL,
                unit TEXT NOT NULL,
                discription TEXT NOT NULL,
                flag TEXT NOT NULL,
                signalzero REAL NOT NULL,
                signalspan REAL NOT NULL,
                outputzero REAL NOT NULL,
                outputspan REAL NOT NULL,
                outputvalue REAL NOT NULL,
                inputvalue REAL NOT NULL,
                signaltype INTEGER NOT NULL
            )");

        // testmaster
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS testmaster (
                productid TEXT NOT NULL,
                testid TEXT NOT NULL,
                testdate date NOT NULL,
                ambtemp REAL NOT NULL,
                ambhumi REAL NOT NULL,
                according TEXT NOT NULL,
                operator TEXT NOT NULL,
                apparatusid TEXT NOT NULL,
                apparatusname TEXT NOT NULL,
                apparatuschkdate date NOT NULL,
                rptno TEXT NOT NULL,
                preweight REAL NOT NULL,
                postweight REAL NOT NULL,
                lostweight REAL NOT NULL,
                lostweight_per REAL NOT NULL,
                totaltesttime INTEGER NOT NULL,
                constpower INTEGER NOT NULL,
                phenocode TEXT NOT NULL,
                flametime INTEGER NOT NULL,
                flameduration INTEGER NOT NULL,
                maxtf1 REAL NOT NULL,
                maxtf2 REAL NOT NULL,
                maxts REAL NOT NULL,
                maxtc REAL NOT NULL,
                maxtf1_time INTEGER NOT NULL,
                maxtf2_time INTEGER NOT NULL,
                maxts_time INTEGER NOT NULL,
                maxtc_time INTEGER NOT NULL,
                finaltf1 REAL NOT NULL,
                finaltf2 REAL NOT NULL,
                finalts REAL NOT NULL,
                finaltc REAL NOT NULL,
                finaltf1_time INTEGER NOT NULL,
                finaltf2_time INTEGER NOT NULL,
                finalts_time INTEGER NOT NULL,
                finaltc_time INTEGER NOT NULL,
                deltatf1 REAL NOT NULL,
                deltatf2 REAL NOT NULL,
                deltatf REAL NOT NULL,
                deltats REAL NOT NULL,
                deltatc REAL NOT NULL,
                memo TEXT NULL,
                flag TEXT NULL,
                PRIMARY KEY (productid, testid),
                FOREIGN KEY (productid) REFERENCES productmaster (productid)
            )");

        // indexes
        db.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate ON testmaster (testdate)");
        db.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_Testmaster_Operator ON testmaster (operator)");
        db.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate_Productid ON testmaster (testdate, productid)");

        // calibration records
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS CalibrationRecords (
                Id TEXT NOT NULL PRIMARY KEY,
                CalibrationDate TEXT NOT NULL,
                CalibrationType TEXT NOT NULL,
                ApparatusId INTEGER NOT NULL,
                Operator TEXT NOT NULL,
                TemperatureData TEXT NOT NULL,
                UniformityResult REAL NULL,
                MaxDeviation REAL NULL,
                AverageTemperature REAL NULL,
                PassedCriteria INTEGER NOT NULL,
                Remarks TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                TempA1 REAL NULL, TempA2 REAL NULL, TempA3 REAL NULL,
                TempB1 REAL NULL, TempB2 REAL NULL, TempB3 REAL NULL,
                TempC1 REAL NULL, TempC2 REAL NULL, TempC3 REAL NULL,
                TAvg REAL NULL,
                TAvgAxis1 REAL NULL, TAvgAxis2 REAL NULL, TAvgAxis3 REAL NULL,
                TAvgLevela REAL NULL, TAvgLevelb REAL NULL, TAvgLevelc REAL NULL,
                TDevAxis1 REAL NULL, TDevAxis2 REAL NULL, TDevAxis3 REAL NULL,
                TDevLevela REAL NULL, TDevLevelb REAL NULL, TDevLevelc REAL NULL,
                TAvgDevAxis REAL NULL, TAvgDevLevel REAL NULL,
                CenterTempData TEXT NULL, Memo TEXT NULL
            )");
        db.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Date ON CalibrationRecords (CalibrationDate)");
        db.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Operator ON CalibrationRecords (Operator)");
    }

    private static void InsertInitialData(DbHelper db)
    {
        // 插入操作员（如果不存在）
        db.ExecuteNonQuery(@"
            INSERT INTO operators (userid, username, pwd, usertype)
            SELECT '1', 'admin', '123456', 'admin'
            WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin')");
        db.ExecuteNonQuery(@"
            INSERT INTO operators (userid, username, pwd, usertype)
            SELECT '2', 'experimenter', '123456', 'operator'
            WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter')");

        // 插入设备（如果不存在）
        db.ExecuteNonQuery(@"
            INSERT INTO apparatus (apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower)
            SELECT 0, 'FURNACE-01', '一号试验炉', date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048
            WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid = 0)");

        // 插入传感器（如果不存在）
        var sensorSqls = new[]
        {
            "INSERT INTO sensors VALUES (0,'Sensor0','炉温1','采集','℃','炉温1','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (1,'Sensor1','炉温2','采集','℃','炉温2','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (2,'Sensor2','表面温度','采集','℃','表面温度','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (3,'Sensor3','中心温度','采集','℃','中心温度','启用',0,0,0,1000,0,0,4)",
            "INSERT INTO sensors VALUES (16,'Sensor16','校准温度','校准','℃','校准温度','启用',0,0,0,1000,0,0,4)",
        };
        for (int i = 4; i <= 15; i++)
        {
            // Use parameterized approach where NOT EXISTS is implicit via try-catch or check
        }

        try
        {
            foreach (var sql in sensorSqls)
            {
                db.ExecuteNonQuery(sql);
            }
            // Insert backup channels 4-15
            for (int i = 4; i <= 15; i++)
            {
                db.ExecuteNonQuery($@"
                    INSERT INTO sensors VALUES ({i},'Sensor{i}','备用通道{i+1}','备用','℃','备用通道','启用',0,0,0,1000,0,0,4)");
            }
        }
        catch
        {
            // Sensors already exist, ignore
        }
    }
}
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: implement data layer with DbHelper and DatabaseInitializer"
```

---

### Task 3: 核心层 — 模型和枚举

**Files:**
- Create: `src/ISO11820.Core/Models/Operator.cs`
- Create: `src/ISO11820.Core/Models/Apparatus.cs`
- Create: `src/ISO11820.Core/Models/ProductMaster.cs`
- Create: `src/ISO11820.Core/Models/TestMaster.cs`
- Create: `src/ISO11820.Core/Models/Sensor.cs`
- Create: `src/ISO11820.Core/Models/CalibrationRecord.cs`
- Create: `src/ISO11820.Core/Models/MasterMessage.cs`
- Create: `src/ISO11820.Core/Enums/TestState.cs`
- Create: `src/ISO11820.Core/Events/DataBroadcastEventArgs.cs`

- [ ] **Step 1: 创建所有模型文件**

Write `src/ISO11820.Core/Enums/TestState.cs`:
```csharp
namespace ISO11820.Core.Enums;

public enum TestState
{
    Idle,
    Preparing,
    Ready,
    Recording,
    Complete
}
```

Write `src/ISO11820.Core/Models/Operator.cs`:
```csharp
namespace ISO11820.Core.Models;

public class Operator
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Pwd { get; set; } = "";
    public string UserType { get; set; } = "";
}
```

Write `src/ISO11820.Core/Models/Apparatus.cs`:
```csharp
namespace ISO11820.Core.Models;

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
```

Write `src/ISO11820.Core/Models/ProductMaster.cs`:
```csharp
namespace ISO11820.Core.Models;

public class ProductMaster
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Specific { get; set; } = "";
    public double Diameter { get; set; }
    public double Height { get; set; }
    public string? Flag { get; set; }
}
```

Write `src/ISO11820.Core/Models/TestMaster.cs`:
```csharp
namespace ISO11820.Core.Models;

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
```

Write `src/ISO11820.Core/Models/Sensor.cs`:
```csharp
namespace ISO11820.Core.Models;

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
```

Write `src/ISO11820.Core/Models/CalibrationRecord.cs`:
```csharp
namespace ISO11820.Core.Models;

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
```

Write `src/ISO11820.Core/Models/MasterMessage.cs`:
```csharp
namespace ISO11820.Core.Models;

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
```

Write `src/ISO11820.Core/Events/DataBroadcastEventArgs.cs`:
```csharp
using ISO11820.Core.Models;

namespace ISO11820.Core.Events;

public class DataBroadcastEventArgs : EventArgs
{
    public Dictionary<string, double> SensorValues { get; set; } = new();
    public TestState CurrentState { get; set; }
    public int ElapsedSeconds { get; set; }
    public double TemperatureDrift { get; set; }
    public List<MasterMessage> Messages { get; set; } = new();
    public string ProductId { get; set; } = "";
    public string TestId { get; set; } = "";
}
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat: add core models, enums, and event args"
```

---

### Task 4: 核心层 — TestController 状态机

**Files:**
- Create: `src/ISO11820.Core/TestController.cs`

Write `src/ISO11820.Core/TestController.cs`:
```csharp
using ISO11820.Core.Enums;
using ISO11820.Core.Events;
using ISO11820.Core.Models;
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
    public int TargetDurationSeconds { get; set; } = 3600; // 默认60分钟

    public int ElapsedSeconds { get; set; }
    public int StableCounter { get; set; }

    public List<double> PidOutputQueue { get; } = new();
    public int ConstPower { get; set; } = 2048;

    // 传感器历史数据（用于温漂计算和CSV保存）
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

    public bool HasUnfinishedTest()
    {
        return _db.HasUnfinishedTest();
    }

    public TestMaster? GetUnfinishedTest()
    {
        return _db.GetUnfinishedTest();
    }

    public void CreateTest(string productId, string productName, string specific,
        double diameter, double height, double preWeight, double ambTemp, double ambHumi,
        string testId)
    {
        // 保存样品信息
        _db.InsertProduct(productId, productName, specific, diameter, height);

        // 取设备信息
        var apparatus = _db.GetApparatus(0);
        if (apparatus == null)
            throw new Exception("设备信息未找到");

        // 插入试验记录
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

        // 计算恒功率
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
                // 检查是否达到Ready条件：745~755且稳定
                if (tf1 >= 745.0 && tf1 <= 755.0)
                {
                    StableCounter++;
                    if (StableCounter > 3) // 约3.2秒
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
                // 检查温度是否跌出稳定范围
                if (tf1 < 745.0 || tf1 > 755.0)
                {
                    _state = TestState.Preparing;
                    StableCounter = 0;
                    StateChanged?.Invoke(this, $"状态变更: {_state}");
                }
                break;

            case TestState.Recording:
                ElapsedSeconds++;
                // 记录温度历史
                SensorHistory.Add(new SensorDataPoint
                {
                    Time = ElapsedSeconds,
                    Temp1 = temps["TF1"],
                    Temp2 = temps["TF2"],
                    TempSurface = temps["TS"],
                    TempCenter = temps["TC"],
                    TempCalibration = temps["TCal"]
                });

                // 检查终止条件
                if (TargetDurationSeconds > 0 && ElapsedSeconds >= TargetDurationSeconds)
                {
                    _state = TestState.Complete;
                    messages.Add(new MasterMessage($"记录时间到达 {ElapsedSeconds} 秒，试验自动结束"));
                    StateChanged?.Invoke(this, $"状态变更: {_state}");
                }
                break;
        }

        // 广播数据
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
        // 取最近10分钟的数据做线性回归
        var recentData = SensorHistory
            .Where(d => d.Time >= ElapsedSeconds - 600)
            .ToList();
        if (recentData.Count < 10) return 0;

        // Simple slope calculation using first and last points of recent data
        // For full implementation, MathNet.Numerics SimpleRegression could be used
        var first = recentData.First();
        var last = recentData.Last();
        double timeSpan = last.Time - first.Time;
        if (timeSpan == 0) return 0;
        return (last.Temp1 - first.Temp1) / timeSpan * 600; // 转换为°C/10min
    }
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
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement TestController state machine"
```

---

### Task 5: 服务层 — ConfigurationService 和 SensorSimulator

**Files:**
- Create: `src/ISO11820.Services/ConfigurationService.cs`
- Create: `src/ISO11820.Services/Simulation/SensorSimulator.cs`

- [ ] **Step 1: 创建 ConfigurationService.cs**

Write `src/ISO11820.Services/ConfigurationService.cs`:
```csharp
using Microsoft.Extensions.Configuration;

namespace ISO11820.Services;

public class ConfigurationService
{
    private readonly IConfiguration _config;

    public ConfigurationService(IConfiguration config)
    {
        _config = config;
    }

    // Database
    public string SqlitePath => _config["Database:SqlitePath"] ?? "Data\\ISO11820.db";

    // Hardware
    public int ConstPower => int.Parse(_config["Hardware:ConstPower"] ?? "2048");
    public double PidTemperature => double.Parse(_config["Hardware:PidTemperature"] ?? "750");

    // Simulation
    public bool EnableSimulation => bool.Parse(_config["Simulation:EnableSimulation"] ?? "true");
    public double InitialFurnaceTemp => double.Parse(_config["Simulation:InitialFurnaceTemp"] ?? "720");
    public double TargetFurnaceTemp => double.Parse(_config["Simulation:TargetFurnaceTemp"] ?? "750");
    public double HeatingRatePerSecond => double.Parse(_config["Simulation:HeatingRatePerSecond"] ?? "40");
    public double TempFluctuation => double.Parse(_config["Simulation:TempFluctuation"] ?? "0.5");
    public double StableThreshold => double.Parse(_config["Simulation:StableThreshold"] ?? "3.0");

    // File Storage
    public string BaseDirectory => _config["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
    public string TestDataDirectory => _config["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";
    public string ReportOutputDirectory => _config["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";
    public bool EnablePdfExport => bool.Parse(_config["Report:EnablePdfExport"] ?? "true");

    public T GetSection<T>(string sectionName) where T : new()
    {
        var section = new T();
        _config.GetSection(sectionName).Bind(section);
        return section;
    }
}
```

- [ ] **Step 2: 创建 SensorSimulator.cs**

Write `src/ISO11820.Services/Simulation/SensorSimulator.cs`:
```csharp
namespace ISO11820.Services.Simulation;

public class SensorSimulator
{
    private readonly ConfigurationService _config;
    private readonly Random _rng = new();
    private bool _isRecording = false;

    public SensorSimulator(ConfigurationService config)
    {
        _config = config;
    }

    public void SetRecording(bool recording)
    {
        _isRecording = recording;
    }

    public Dictionary<string, double> Update(Dictionary<string, double> currentValues, bool isRecording)
    {
        _isRecording = isRecording;
        double tf1 = currentValues.GetValueOrDefault("TF1", 25.0);
        double tf2 = currentValues.GetValueOrDefault("TF2", 25.0);
        double ts = currentValues.GetValueOrDefault("TS", 25.0);
        double tc = currentValues.GetValueOrDefault("TC", 25.0);

        double fluctuation = _config.TempFluctuation;
        double targetTemp = _config.TargetFurnaceTemp;
        double heatRate = _config.HeatingRatePerSecond * 0.8; // 每次 tick 800ms

        double noise(double amp = 1.0) => (_rng.NextDouble() * 2 - 1) * fluctuation * amp;

        if (tf1 < targetTemp - _config.StableThreshold) // 升温阶段: < 747°C
        {
            tf1 += heatRate + noise();
            tf2 += heatRate + noise();
            ts = tf1 * 0.3 + noise(0.5);
            tc = tf1 * 0.25 + noise(0.5);
        }
        else // 稳定阶段: >= 747°C
        {
            tf1 = targetTemp + noise();
            tf2 = targetTemp + noise();

            if (_isRecording)
            {
                double surfaceTarget = Math.Min(tf1 * 0.95, 800);
                ts += (surfaceTarget - ts) * 0.02 + noise(0.5);

                double centerTarget = Math.Min(tf1 * 0.85, 750);
                tc += (centerTarget - tc) * 0.01 + noise(0.5);
            }
            else
            {
                ts = tf1 * 0.3 + noise(0.5);
                tc = tf1 * 0.25 + noise(0.5);
            }
        }

        double tCal = tf1 + noise(2.0);

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
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement ConfigurationService and SensorSimulator"
```

---

### Task 6: 服务层 — DaqWorker 数据采集服务

**Files:**
- Create: `src/ISO11820.Services/DaqWorker.cs`

Write `src/ISO11820.Services/DaqWorker.cs`:
```csharp
using ISO11820.Core;
using ISO11820.Core.Models;
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
        _timer = new System.Timers.Timer(800); // 800ms
        _timer.Elapsed += OnTick;
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        try
        {
            bool isRecording = _controller.CurrentState == Core.Enums.TestState.Recording;
            var temps = _simulator.Update(_controller.SensorValues, isRecording);
            _controller.OnTemperatureUpdated(temps);
        }
        catch (Exception ex)
        {
            // Log error, don't crash the timer
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
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement DaqWorker data acquisition service"
```

---

### Task 7: 服务层 — ExportService 导出服务

**Files:**
- Create: `src/ISO11820.Services/ExportService.cs`

Write `src/ISO11820.Services/ExportService.cs`:
```csharp
using ISO11820.Core;
using ISO11820.Core.Models;
using System.Text;

namespace ISO11820.Services;

public class ExportService
{
    private readonly ConfigurationService _config;

    public ExportService(ConfigurationService config)
    {
        _config = config;
    }

    public string SaveCsv(string productId, string testId, List<SensorDataPoint> data)
    {
        var dir = Path.Combine(_config.TestDataDirectory, productId, testId);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "sensor_data.csv");

        var sb = new StringBuilder();
        sb.AppendLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        foreach (var point in data)
        {
            sb.AppendLine($"{point.Time},{point.Temp1:F1},{point.Temp2:F1},{point.TempSurface:F1},{point.TempCenter:F1},{point.TempCalibration:F1}");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public string SaveExcelReport(TestMaster test, ProductMaster product, List<SensorDataPoint> data)
    {
        // Use EPPlus to create Excel
        var dir = _config.ReportOutputDirectory;
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{test.TestId}_报告.xlsx");

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using var package = new OfficeOpenXml.ExcelPackage();
        // Sheet1: 试验信息
        var sheet1 = package.Workbook.Worksheets.Add("试验信息");
        sheet1.Cells["A1"].Value = "ISO 11820 不燃性试验报告";
        sheet1.Cells["A1"].Style.Font.Size = 16;
        sheet1.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        sheet1.Cells[row, 1].Value = "样品编号"; sheet1.Cells[row++, 2].Value = test.ProductId;
        sheet1.Cells[row, 1].Value = "样品名称"; sheet1.Cells[row++, 2].Value = product.ProductName;
        sheet1.Cells[row, 1].Value = "试验日期"; sheet1.Cells[row++, 2].Value = test.TestDate.ToString("yyyy-MM-dd");
        sheet1.Cells[row, 1].Value = "操作员"; sheet1.Cells[row++, 2].Value = test.Operator;
        sheet1.Cells[row, 1].Value = "环境温度(°C)"; sheet1.Cells[row++, 2].Value = test.AmbTemp;
        sheet1.Cells[row, 1].Value = "环境湿度(%)"; sheet1.Cells[row++, 2].Value = test.AmbHumi;
        sheet1.Cells[row, 1].Value = "试验前质量(g)"; sheet1.Cells[row++, 2].Value = test.PreWeight;
        sheet1.Cells[row, 1].Value = "试验后质量(g)"; sheet1.Cells[row++, 2].Value = test.PostWeight;
        sheet1.Cells[row, 1].Value = "失重率(%)"; sheet1.Cells[row++, 2].Value = test.LostWeightPer;
        sheet1.Cells[row, 1].Value = "样品温升(°C)"; sheet1.Cells[row++, 2].Value = test.DeltaTf;
        sheet1.Cells[row, 1].Value = "试验时长(秒)"; sheet1.Cells[row++, 2].Value = test.TotalTestTime;

        // Sheet2: 温度数据
        var sheet2 = package.Workbook.Worksheets.Add("温度数据");
        sheet2.Cells["A1"].Value = "Time(s)";
        sheet2.Cells["B1"].Value = "炉温1(°C)";
        sheet2.Cells["C1"].Value = "炉温2(°C)";
        sheet2.Cells["D1"].Value = "表面温(°C)";
        sheet2.Cells["E1"].Value = "中心温(°C)";
        for (int i = 0; i < data.Count; i++)
        {
            sheet2.Cells[i + 2, 1].Value = data[i].Time;
            sheet2.Cells[i + 2, 2].Value = data[i].Temp1;
            sheet2.Cells[i + 2, 3].Value = data[i].Temp2;
            sheet2.Cells[i + 2, 4].Value = data[i].TempSurface;
            sheet2.Cells[i + 2, 5].Value = data[i].TempCenter;
        }

        // Sheet3: 温度曲线图
        var sheet3 = package.Workbook.Worksheets.Add("温度曲线");
        var chart = sheet3.Drawings.AddChart("TemperatureChart", OfficeOpenXml.Drawing.Chart.eChartType.XYScatterLines);
        chart.Title.Text = "温度曲线";
        chart.SetPosition(0, 0, 0, 0);
        chart.SetSize(800, 600);

        if (data.Count > 0)
        {
            // Chart data setup
            var chartSheet = package.Workbook.Worksheets.Add("_ChartData");
            for (int i = 0; i < data.Count; i++)
            {
                chartSheet.Cells[i + 1, 1].Value = data[i].Time;
                chartSheet.Cells[i + 1, 2].Value = data[i].Temp1;
                chartSheet.Cells[i + 1, 3].Value = data[i].Temp2;
                chartSheet.Cells[i + 1, 4].Value = data[i].TempSurface;
                chartSheet.Cells[i + 1, 5].Value = data[i].TempCenter;
            }

            var series1 = chart.Series.Add(chartSheet.Cells[1, 2, data.Count, 2], chartSheet.Cells[1, 1, data.Count, 1]);
            series1.Header = "炉温1";
            var series2 = chart.Series.Add(chartSheet.Cells[1, 3, data.Count, 3], chartSheet.Cells[1, 1, data.Count, 1]);
            series2.Header = "炉温2";
            var series3 = chart.Series.Add(chartSheet.Cells[1, 4, data.Count, 4], chartSheet.Cells[1, 1, data.Count, 1]);
            series3.Header = "表面温";
            var series4 = chart.Series.Add(chartSheet.Cells[1, 5, data.Count, 5], chartSheet.Cells[1, 1, data.Count, 1]);
            series4.Header = "中心温";
        }

        package.SaveAs(filePath);
        return filePath;
    }

    public string? SavePdfReport(TestMaster test, ProductMaster product, List<SensorDataPoint> data)
    {
        if (!_config.EnablePdfExport) return null;

        var dir = _config.ReportOutputDirectory;
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{test.TestId}_报告.pdf");

        // Basic PDF generation using PdfSharp
        var document = new PdfSharp.Pdf.PdfDocument();
        document.Info.Title = "ISO 11820 不燃性试验报告";

        var page = document.AddPage();
        var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
        var font = new PdfSharp.Drawing.XFont("SimSun", 12);
        var fontTitle = new PdfSharp.Drawing.XFont("SimSun", 18);

        double y = 40;
        gfx.DrawString("ISO 11820 不燃性试验报告", fontTitle, PdfSharp.Drawing.XBrushes.Black, 40, y);
        y += 30;

        var lines = new[]
        {
            $"样品编号: {test.ProductId}",
            $"样品名称: {product.ProductName}",
            $"试验日期: {test.TestDate:yyyy-MM-dd}",
            $"操作员: {test.Operator}",
            $"环境温度: {test.AmbTemp}°C",
            $"环境湿度: {test.AmbHumi}%",
            $"试验前质量: {test.PreWeight}g",
            $"试验后质量: {test.PostWeight}g",
            $"失重率: {test.LostWeightPer:F2}%",
            $"样品温升: {test.DeltaTf:F1}°C",
            $"试验时长: {test.TotalTestTime}秒",
            $"判定: {(test.LostWeightPer <= 50 && test.DeltaTf <= 50 ? "通过" : "不通过")}"
        };

        foreach (var line in lines)
        {
            y += 20;
            gfx.DrawString(line, font, PdfSharp.Drawing.XBrushes.Black, 40, y);
        }

        document.Save(filePath);
        return filePath;
    }
}
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement ExportService for CSV/Excel/PDF"
```

---

### Task 8: UI 层 — AppContext 和 LoginForm

**Files:**
- Create: `src/ISO11820.App/AppContext.cs`
- Create: `src/ISO11820.App/Forms/LoginForm.cs`

- [ ] **Step 1: 创建 AppContext.cs**

Write `src/ISO11820.App/AppContext.cs`:
```csharp
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
```

- [ ] **Step 2: 创建 LoginForm.cs**

Write `src/ISO11820.App/Forms/LoginForm.cs`:
```csharp
using ISO11820.App;

namespace ISO11820.App.Forms;

public partial class LoginForm : Form
{
    private readonly AppContext _app;
    private TextBox txtPassword = null!;
    private RadioButton rbAdmin = null!;
    private RadioButton rbExperimenter = null!;
    private Button btnLogin = null!;
    private Label lblError = null!;

    public LoginForm(AppContext app)
    {
        _app = app;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "ISO 11820 不燃性试验系统 - 登录";
        this.Size = new Size(420, 320);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(240, 240, 245);

        var lblTitle = new Label
        {
            Text = "ISO 11820 不燃性试验系统",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            Location = new Point(60, 30),
            Size = new Size(300, 35),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblRole = new Label
        {
            Text = "选择角色:",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(80, 85)
        };

        rbAdmin = new RadioButton
        {
            Text = "管理员 (admin)",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(100, 110),
            Size = new Size(200, 25),
            Checked = true
        };

        rbExperimenter = new RadioButton
        {
            Text = "试验员 (experimenter)",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(100, 140),
            Size = new Size(200, 25)
        };

        var lblPwd = new Label
        {
            Text = "输入密码:",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(80, 175)
        };

        txtPassword = new TextBox
        {
            Location = new Point(100, 200),
            Size = new Size(200, 28),
            PasswordChar = '●',
            Font = new Font("Microsoft YaHei", 10)
        };
        txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DoLogin(); };

        btnLogin = new Button
        {
            Text = "登 录",
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            Location = new Point(140, 240),
            Size = new Size(120, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += (s, e) => DoLogin();

        lblError = new Label
        {
            ForeColor = Color.Red,
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(80, 280),
            Size = new Size(260, 25),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.AddRange(new Control[] { lblTitle, lblRole, rbAdmin, rbExperimenter, lblPwd, txtPassword, btnLogin, lblError });
    }

    private void DoLogin()
    {
        string username = rbAdmin.Checked ? "admin" : "experimenter";
        string pwd = txtPassword.Text;

        if (_app.Db.Login(username, pwd, out string userId, out string userType))
        {
            _app.TestController.SetOperator(username, userType);
            var mainForm = new MainForm(_app);
            this.Hide();
            mainForm.ShowDialog();
            this.Close();
        }
        else
        {
            lblError.Text = "密码错误，请重新输入";
            txtPassword.SelectAll();
            txtPassword.Focus();
        }
    }
}
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement AppContext and LoginForm"
```

---

### Task 9: UI 层 — MainForm 主窗体

**Files:**
- Create: `src/ISO11820.App/Forms/MainForm.cs`

This is the largest file. The MainForm contains:
- TabControl with tabs: 试验监控, 记录查询, 设备校准
- Real-time temperature display (5 channels, LED-style labels)
- OxyPlot chart (4 lines, scrolling 10-minute window)
- System message log (RichTextBox)
- State-dependent buttons (开始升温, 停止升温, 开始记录, 停止记录, 新建试验, 试验记录)
- Timer/elapsed display
- Temperature drift display

Write `src/ISO11820.App/Forms/MainForm.cs`:
```csharp
using ISO11820.App;
using ISO11820.Core.Enums;
using ISO11820.Core.Events;
using ISO11820.Core.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;

namespace ISO11820.App.Forms;

public partial class MainForm : Form
{
    private readonly AppContext _app;
    private TabControl tabControl = null!;

    // Tab 1: 试验监控
    private Label lblTf1 = null!, lblTf2 = null!, lblTs = null!, lblTc = null!, lblTCal = null!;
    private Label lblState = null!, lblTimer = null!, lblDrift = null!;
    private Label lblProductId = null!;
    private RichTextBox rtbLog = null!;
    private PlotView plotView = null!;
    private Button btnNewTest = null!, btnStartHeat = null!, btnStopHeat = null!;
    private Button btnStartRecord = null!, btnStopRecord = null!, btnSaveRecord = null!;

    // Tab 2: 记录查询
    private DataGridView dgvRecords = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private TextBox txtSearchProduct = null!;
    private Button btnSearch = null!, btnExport = null!;

    // Tab 3: 设备校准
    private Label lblCalibTemp = null!;
    private Button btnRecordCalib = null!;
    private DataGridView dgvCalibRecords = null!;

    private OxyPlot.Series.LineSeries _seriesTf1 = null!, _seriesTf2 = null!, _seriesTs = null!, _seriesTc = null!;
    private Queue<(double time, double tf1, double tf2, double ts, double tc)> _plotData = new();
    private double _plotTime;
    private System.Windows.Forms.Timer _uiTimer = null!;

    public MainForm(AppContext app)
    {
        _app = app;
        InitializeComponents();
        SubscribeEvents();
    }

    private void InitializeComponents()
    {
        this.Text = "ISO 11820 建筑材料不燃性试验仿真系统";
        this.Size = new Size(1280, 820);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1024, 700);

        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 10)
        };

        // ===== Tab 1: 试验监控 =====
        var tabTest = new TabPage("试验监控");
        BuildTestMonitorTab(tabTest);
        tabControl.TabPages.Add(tabTest);

        // ===== Tab 2: 记录查询 =====
        var tabQuery = new TabPage("记录查询");
        BuildQueryTab(tabQuery);
        tabControl.TabPages.Add(tabQuery);

        // ===== Tab 3: 设备校准 =====
        var tabCalib = new TabPage("设备校准");
        BuildCalibrationTab(tabCalib);
        tabControl.TabPages.Add(tabCalib);

        Controls.Add(tabControl);

        // UI timer for updating chart (every second)
        _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uiTimer.Tick += OnUiTimerTick;
        _uiTimer.Start();

        this.FormClosing += (s, e) =>
        {
            _app.DaqWorker.Stop();
            _app.DaqWorker.Dispose();
        };
    }

    private void BuildTestMonitorTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(30, 30, 30);

        // Left panel (300px): temperatures + info
        var leftPanel = new Panel
        {
            Width = 300,
            Dock = DockStyle.Left,
            BackColor = Color.FromArgb(45, 45, 48)
        };

        int y = 10;

        // Product ID label
        lblProductId = CreateInfoLabel("样品编号: --", ref y, leftPanel, 12, Color.Cyan);
        y += 10;

        // Temperature labels
        lblTf1 = CreateTempLabel("炉温1 (TF1)", ref y, leftPanel);
        lblTf2 = CreateTempLabel("炉温2 (TF2)", ref y, leftPanel);
        lblTs = CreateTempLabel("表面温 (TS)", ref y, leftPanel);
        lblTc = CreateTempLabel("中心温 (TC)", ref y, leftPanel);
        lblTCal = CreateTempLabel("校准温 (TCal)", ref y, leftPanel);

        y += 10;

        // State label
        lblState = CreateInfoLabel("状态: 空闲", ref y, leftPanel, 14, Color.LimeGreen);
        // Timer label
        lblTimer = CreateInfoLabel("计时: 0 秒", ref y, leftPanel, 12, Color.White);
        // Drift label
        lblDrift = CreateInfoLabel("温漂: 0.00 °C/10min", ref y, leftPanel, 10, Color.Gray);

        y += 10;

        // Buttons
        btnNewTest = CreateButton("新建试验", ref y, leftPanel);
        btnStartHeat = CreateButton("开始升温", ref y, leftPanel);
        btnStopHeat = CreateButton("停止升温", ref y, leftPanel);
        btnStartRecord = CreateButton("开始记录", ref y, leftPanel);
        btnStopRecord = CreateButton("停止记录", ref y, leftPanel);
        btnSaveRecord = CreateButton("试验记录", ref y, leftPanel);

        tab.Controls.Add(leftPanel);

        // Right panel: chart + log
        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // Chart (top 60%)
        plotView = new PlotView
        {
            Dock = DockStyle.Top,
            Height = 420,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        SetupChart();
        rightPanel.Controls.Add(plotView);

        // Log (bottom)
        rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Consolas", 9),
            ReadOnly = true
        };
        rightPanel.Controls.Add(rtbLog);

        tab.Controls.Add(rightPanel);

        UpdateButtonStates();
    }

    private void SetupChart()
    {
        var model = new PlotModel
        {
            Title = "温度曲线",
            TitleColor = OxyColor.FromRgb(200, 200, 200),
            PlotAreaBorderColor = OxyColor.FromRgb(80, 80, 80),
            TextColor = OxyColor.FromRgb(200, 200, 200),
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            TitleColor = OxyColor.FromRgb(200, 200, 200),
            AxislineColor = OxyColor.FromRgb(80, 80, 80),
            TextColor = OxyColor.FromRgb(200, 200, 200),
            Minimum = 0,
            Maximum = 600
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            TitleColor = OxyColor.FromRgb(200, 200, 200),
            AxislineColor = OxyColor.FromRgb(80, 80, 80),
            TextColor = OxyColor.FromRgb(200, 200, 200),
            Minimum = 0,
            Maximum = 800
        });

        _seriesTf1 = new OxyPlot.Series.LineSeries { Title = "炉温1", Color = OxyColor.FromRgb(255, 80, 80), StrokeThickness = 1.5 };
        _seriesTf2 = new OxyPlot.Series.LineSeries { Title = "炉温2", Color = OxyColor.FromRgb(255, 160, 60), StrokeThickness = 1.5 };
        _seriesTs = new OxyPlot.Series.LineSeries { Title = "表面温", Color = OxyColor.FromRgb(80, 200, 255), StrokeThickness = 1.5 };
        _seriesTc = new OxyPlot.Series.LineSeries { Title = "中心温", Color = OxyColor.FromRgb(80, 255, 120), StrokeThickness = 1.5 };

        model.Series.Add(_seriesTf1);
        model.Series.Add(_seriesTf2);
        model.Series.Add(_seriesTs);
        model.Series.Add(_seriesTc);

        plotView.Model = model;
    }

    private Label CreateTempLabel(string name, ref int y, Panel parent)
    {
        var lbl = new Label
        {
            Text = $"{name}: --.- °C",
            Font = new Font("Consolas", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 200, 50),
            Location = new Point(10, y),
            Size = new Size(280, 35),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(60, 60, 65),
            BorderStyle = BorderStyle.FixedSingle
        };
        y += 40;
        parent.Controls.Add(lbl);
        return lbl;
    }

    private Label CreateInfoLabel(string text, ref int y, Panel parent, int fontSize, Color color)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Microsoft YaHei", fontSize),
            ForeColor = color,
            Location = new Point(10, y),
            Size = new Size(280, 25)
        };
        y += 30;
        parent.Controls.Add(lbl);
        return lbl;
    }

    private Button CreateButton(string text, ref int y, Panel parent)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(10, y),
            Size = new Size(280, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White
        };
        btn.FlatAppearance.BorderSize = 0;
        y += 42;
        parent.Controls.Add(btn);

        // Wire up events
        switch (text)
        {
            case "新建试验":
                btn.Click += (s, e) => OnNewTest();
                break;
            case "开始升温":
                btn.Click += (s, e) => OnStartHeating();
                break;
            case "停止升温":
                btn.Click += (s, e) => OnStopHeating();
                break;
            case "开始记录":
                btn.Click += (s, e) => OnStartRecording();
                break;
            case "停止记录":
                btn.Click += (s, e) => OnStopRecording();
                break;
            case "试验记录":
                btn.Click += (s, e) => OnSaveRecord();
                break;
        }

        return btn;
    }

    private void BuildQueryTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(240, 240, 245);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };

        var lblFrom = new Label { Text = "从:", Location = new Point(10, 15), Font = new Font("Microsoft YaHei", 9) };
        dtpFrom = new DateTimePicker { Location = new Point(40, 12), Width = 130, Format = DateTimePickerFormat.Short };
        var lblTo = new Label { Text = "到:", Location = new Point(180, 15), Font = new Font("Microsoft YaHei", 9) };
        dtpTo = new DateTimePicker { Location = new Point(210, 12), Width = 130, Format = DateTimePickerFormat.Short };

        var lblSearch = new Label { Text = "样品编号:", Location = new Point(355, 15), Font = new Font("Microsoft YaHei", 9) };
        txtSearchProduct = new TextBox { Location = new Point(425, 12), Width = 130 };

        btnSearch = new Button { Text = "查询", Location = new Point(565, 10), Size = new Size(80, 30), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += (s, e) => RefreshQueryResults();

        btnExport = new Button { Text = "导出Excel", Location = new Point(655, 10), Size = new Size(100, 30), BackColor = Color.FromArgb(0, 150, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnExport.FlatAppearance.BorderSize = 0;

        topPanel.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblSearch, txtSearchProduct, btnSearch, btnExport });
        tab.Controls.Add(topPanel);

        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        tab.Controls.Add(dgvRecords);

        dtpFrom.Value = DateTime.Now.AddMonths(-1);
        dtpTo.Value = DateTime.Now;
    }

    private void BuildCalibrationTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(240, 240, 245);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };

        lblCalibTemp = new Label
        {
            Text = "校准温度: --.- °C",
            Font = new Font("Consolas", 20, FontStyle.Bold),
            Location = new Point(10, 15),
            Size = new Size(350, 40),
            ForeColor = Color.FromArgb(200, 100, 0)
        };

        btnRecordCalib = new Button
        {
            Text = "记录校准数据",
            Location = new Point(400, 20),
            Size = new Size(130, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRecordCalib.FlatAppearance.BorderSize = 0;

        topPanel.Controls.AddRange(new Control[] { lblCalibTemp, btnRecordCalib });
        tab.Controls.Add(topPanel);

        dgvCalibRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false
        };
        tab.Controls.Add(dgvCalibRecords);
    }

    private void SubscribeEvents()
    {
        _app.TestController.DataBroadcast += OnDataBroadcast;
    }

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        this.Invoke(() =>
        {
            // Update temperature labels
            lblTf1.Text = $"炉温1 (TF1): {e.SensorValues["TF1"]:F1} °C";
            lblTf2.Text = $"炉温2 (TF2): {e.SensorValues["TF2"]:F1} °C";
            lblTs.Text = $"表面温 (TS): {e.SensorValues["TS"]:F1} °C";
            lblTc.Text = $"中心温 (TC): {e.SensorValues["TC"]:F1} °C";
            lblTCal.Text = $"校准温 (TCal): {e.SensorValues["TCal"]:F1} °C";
            lblCalibTemp.Text = $"校准温度: {e.SensorValues["TCal"]:F1} °C";

            // Update state
            string stateText = e.CurrentState switch
            {
                TestState.Idle => "状态: 空闲",
                TestState.Preparing => "状态: 升温中",
                TestState.Ready => "状态: 就绪 ✓",
                TestState.Recording => "状态: 记录中 ●",
                TestState.Complete => "状态: 完成",
                _ => "状态: 未知"
            };
            lblState.Text = stateText;
            lblTimer.Text = $"计时: {e.ElapsedSeconds} 秒";
            lblProductId.Text = $"样品编号: {e.ProductId}";

            // Update chart data
            _plotTime += 1;
            _plotData.Enqueue((_plotTime,
                e.SensorValues["TF1"], e.SensorValues["TF2"],
                e.SensorValues["TS"], e.SensorValues["TC"]));
            while (_plotData.Count > 600) _plotData.Dequeue();

            // Update messages
            foreach (var msg in e.Messages)
            {
                Color color = msg.Message.Contains("终止") ? Color.Yellow :
                              msg.Message.Contains("稳定") ? Color.LimeGreen :
                              Color.White;
                rtbLog.SelectionColor = color;
                rtbLog.AppendText($"{msg.Time}  {msg.Message}\n");
                rtbLog.ScrollToCaret();
            }

            UpdateButtonStates();
        });
    }

    private void OnUiTimerTick(object? sender, EventArgs e)
    {
        if (_plotData.Count == 0) return;

        _seriesTf1.Points.Clear();
        _seriesTf2.Points.Clear();
        _seriesTs.Points.Clear();
        _seriesTc.Points.Clear();

        double minX = Math.Max(0, _plotTime - 600);
        foreach (var (time, tf1, tf2, ts, tc) in _plotData)
        {
            if (time >= minX)
            {
                _seriesTf1.Points.Add(new DataPoint(time, tf1));
                _seriesTf2.Points.Add(new DataPoint(time, tf2));
                _seriesTs.Points.Add(new DataPoint(time, ts));
                _seriesTc.Points.Add(new DataPoint(time, tc));
            }
        }

        plotView.Model.Axes[0].Minimum = minX;
        plotView.Model.Axes[0].Maximum = minX + 610;
        plotView.InvalidatePlot(true);

        // Update drift
        double drift = _app.TestController.CalculateTemperatureDrift();
        lblDrift.Text = $"温漂: {drift:F2} °C/10min";
    }

    private void UpdateButtonStates()
    {
        var state = _app.TestController.CurrentState;
        bool hasActiveTest = !string.IsNullOrEmpty(_app.TestController.CurrentProductId);

        btnNewTest.Enabled = state == TestState.Idle;
        btnStartHeat.Enabled = state == TestState.Idle;
        btnStopHeat.Enabled = state == TestState.Preparing || state == TestState.Ready;
        btnStartRecord.Enabled = state == TestState.Ready;
        btnStopRecord.Enabled = state == TestState.Recording;
        btnSaveRecord.Enabled = state == TestState.Complete;
    }

    private void OnNewTest()
    {
        var dialog = new NewTestForm(_app);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            AddLog("新建试验成功", Color.LimeGreen);
            UpdateButtonStates();
        }
    }

    private void OnStartHeating()
    {
        _app.TestController.StartHeating(_app.Config.InitialFurnaceTemp);
        _app.DaqWorker.Start();
        AddLog("开始升温，系统升温中", Color.White);
        UpdateButtonStates();
    }

    private void OnStopHeating()
    {
        _app.TestController.StopHeating();
        _app.DaqWorker.Stop();
        AddLog("停止升温", Color.White);
        UpdateButtonStates();
    }

    private void OnStartRecording()
    {
        _app.TestController.StartRecording();
        AddLog("开始记录，计时开始", Color.White);
        UpdateButtonStates();
    }

    private void OnStopRecording()
    {
        _app.TestController.StopRecording();
        AddLog("用户手动停止记录", Color.White);
        UpdateButtonStates();
    }

    private void OnSaveRecord()
    {
        var dialog = new TestRecordForm(_app);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            AddLog("试验记录保存成功", Color.LimeGreen);
            // Export files
            var data = _app.TestController.SensorHistory.ToList();
            var csvPath = _app.ExportService.SaveCsv(
                _app.TestController.CurrentProductId,
                _app.TestController.CurrentTestId, data);
            AddLog($"CSV已保存: {csvPath}", Color.Gray);

            var test = _app.Db.GetTest(_app.TestController.CurrentProductId, _app.TestController.CurrentTestId);
            var product = _app.Db.GetProduct(_app.TestController.CurrentProductId);
            if (test != null && product != null)
            {
                var xlsxPath = _app.ExportService.SaveExcelReport(test, product, data);
                AddLog($"Excel报告已保存: {xlsxPath}", Color.Gray);

                var pdfPath = _app.ExportService.SavePdfReport(test, product, data);
                if (pdfPath != null)
                    AddLog($"PDF报告已保存: {pdfPath}", Color.Gray);
            }

            _app.TestController.ResetToPreparing();
            UpdateButtonStates();
        }
    }

    private void RefreshQueryResults()
    {
        var tests = _app.Db.QueryTests(dtpFrom.Value, dtpTo.Value, "", "");
        dgvRecords.DataSource = null;
        if (tests.Count == 0)
        {
            dgvRecords.Columns.Clear();
            return;
        }

        dgvRecords.DataSource = tests.Select(t => new
        {
            试验ID = t.TestId,
            样品编号 = t.ProductId,
            试验日期 = t.TestDate.ToString("yyyy-MM-dd"),
            操作员 = t.Operator,
            时长 = t.TotalTestTime,
            失重率 = $"{t.LostWeightPer:F2}%",
            温升 = $"{t.DeltaTf:F1}°C"
        }).ToList();
    }

    private void AddLog(string message, Color color)
    {
        this.Invoke(() =>
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.SelectionColor = color;
            rtbLog.AppendText($"{time}  {message}\n");
            rtbLog.ScrollToCaret();
        });
    }
}
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement MainForm with tabs, chart, and test monitor"
```

---

### Task 10: UI 层 — NewTestForm 和 TestRecordForm

**Files:**
- Create: `src/ISO11820.App/Forms/NewTestForm.cs`
- Create: `src/ISO11820.App/Forms/TestRecordForm.cs`

- [ ] **Step 1: 创建 NewTestForm.cs**

Write `src/ISO11820.App/Forms/NewTestForm.cs`:
```csharp
using ISO11820.App;

namespace ISO11820.App.Forms;

public partial class NewTestForm : Form
{
    private readonly AppContext _app;
    private TextBox txtProductId = null!, txtProductName = null!, txtSpecific = null!;
    private TextBox txtDiameter = null!, txtHeight = null!;
    private TextBox txtAmbTemp = null!, txtAmbHumi = null!;
    private TextBox txtPreWeight = null!;
    private ComboBox cmbDuration = null!;
    private NumericUpDown nudCustomMinutes = null!;

    public NewTestForm(AppContext app)
    {
        _app = app;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "新建试验";
        this.Size = new Size(450, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 15;
        var lbl = (string text, int x) => new Label
        {
            Text = text,
            Location = new Point(x, y + 3),
            Font = new Font("Microsoft YaHei", 9),
            Size = new Size(100, 25)
        };
        var txt = (int x, string defaultValue = "") => new TextBox
        {
            Location = new Point(x + 110, y),
            Width = 280,
            Font = new Font("Microsoft YaHei", 9),
            Text = defaultValue
        };
        var nextRow = () => { y += 35; };

        // Row: 样品编号
        Controls.Add(lbl("样品编号:", 20));
        txtProductId = txt(20, DateTime.Now.ToString("yyyyMMdd") + "-001");
        Controls.Add(txtProductId);
        nextRow();

        // Row: 样品名称
        Controls.Add(lbl("样品名称:", 20));
        txtProductName = txt(20, "岩棉隔热板");
        Controls.Add(txtProductName);
        nextRow();

        // Row: 规格型号
        Controls.Add(lbl("规格型号:", 20));
        txtSpecific = txt(20, "100×50×25mm");
        Controls.Add(txtSpecific);
        nextRow();

        // Row: 直径
        Controls.Add(lbl("直径 (mm):", 20));
        txtDiameter = txt(20, "100");
        Controls.Add(txtDiameter);
        nextRow();

        // Row: 高度
        Controls.Add(lbl("高度 (mm):", 20));
        txtHeight = txt(20, "50");
        Controls.Add(txtHeight);
        nextRow();

        // Row: 环境温度
        Controls.Add(lbl("环境温度 (°C):", 20));
        txtAmbTemp = txt(20, "25.0");
        Controls.Add(txtAmbTemp);
        nextRow();

        // Row: 环境湿度
        Controls.Add(lbl("环境湿度 (%):", 20));
        txtAmbHumi = txt(20, "50.0");
        Controls.Add(txtAmbHumi);
        nextRow();

        // Row: 试验前质量
        Controls.Add(lbl("试验前质量 (g):", 20));
        txtPreWeight = txt(20, "50.0");
        Controls.Add(txtPreWeight);
        nextRow();

        // Row: 试验时长模式
        Controls.Add(lbl("时长模式:", 20));
        cmbDuration = new ComboBox
        {
            Location = new Point(130, y),
            Width = 130,
            Font = new Font("Microsoft YaHei", 9),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbDuration.Items.AddRange(new[] { "标准60分钟", "自定义分钟" });
        cmbDuration.SelectedIndex = 0;
        Controls.Add(cmbDuration);

        nudCustomMinutes = new NumericUpDown
        {
            Location = new Point(270, y),
            Width = 90,
            Minimum = 1,
            Maximum = 120,
            Value = 30,
            Font = new Font("Microsoft YaHei", 9),
            Enabled = false
        };
        Controls.Add(nudCustomMinutes);
        cmbDuration.SelectedIndexChanged += (s, e) =>
        {
            nudCustomMinutes.Enabled = cmbDuration.SelectedIndex == 1;
        };
        nextRow();
        y += 15;

        // Buttons
        var btnOk = new Button
        {
            Text = "创建试验",
            Location = new Point(140, y),
            Size = new Size(130, 38),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => DoCreate();
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(290, y),
            Size = new Size(100, 38),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);

        // Display operator info
        y += 50;
        var lblOp = new Label
        {
            Text = $"操作员: {_app.TestController.OperatorName} | 设备: FURNACE-01 一号试验炉",
            Location = new Point(20, y),
            Size = new Size(400, 25),
            Font = new Font("Microsoft YaHei", 8),
            ForeColor = Color.Gray
        };
        Controls.Add(lblOp);
    }

    private void DoCreate()
    {
        if (string.IsNullOrWhiteSpace(txtProductId.Text))
        {
            MessageBox.Show("请输入样品编号", "提示");
            return;
        }
        if (string.IsNullOrWhiteSpace(txtPreWeight.Text) || !double.TryParse(txtPreWeight.Text, out double preWeight))
        {
            MessageBox.Show("请输入有效的试验前质量", "提示");
            return;
        }

        try
        {
            string productId = txtProductId.Text.Trim();
            string testId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            int duration = cmbDuration.SelectedIndex == 1 ? (int)nudCustomMinutes.Value * 60 : 3600;

            _app.TestController.TargetDurationSeconds = duration;
            _app.TestController.CreateTest(
                productId,
                txtProductName.Text.Trim(),
                txtSpecific.Text.Trim(),
                double.TryParse(txtDiameter.Text, out double diam) ? diam : 100,
                double.TryParse(txtHeight.Text, out double h) ? h : 50,
                preWeight,
                double.TryParse(txtAmbTemp.Text, out double ambT) ? ambT : 25.0,
                double.TryParse(txtAmbHumi.Text, out double ambH) ? ambH : 50.0,
                testId
            );

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建试验失败: {ex.Message}", "错误");
        }
    }
}
```

- [ ] **Step 2: 创建 TestRecordForm.cs**

Write `src/ISO11820.App/Forms/TestRecordForm.cs`:
```csharp
using ISO11820.App;

namespace ISO11820.App.Forms;

public partial class TestRecordForm : Form
{
    private readonly AppContext _app;
    private CheckBox chkFlame = null!;
    private NumericUpDown nudFlameTime = null!, nudFlameDuration = null!;
    private TextBox txtPostWeight = null!, txtMemo = null!;

    public TestRecordForm(AppContext app)
    {
        _app = app;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "试验现象记录";
        this.Size = new Size(420, 380);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 15;
        var lbl = (string text) => new Label
        {
            Text = text,
            Location = new Point(20, y + 3),
            Font = new Font("Microsoft YaHei", 9),
            Size = new Size(140, 25)
        };
        var nextRow = () => { y += 35; };

        // Pre-weight (read-only)
        Controls.Add(lbl("试验前质量 (g):"));
        Controls.Add(new Label
        {
            Text = _app.TestController.PreWeight.ToString("F1"),
            Location = new Point(170, y + 3),
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Size = new Size(100, 25)
        });
        nextRow();

        // Post-weight (required)
        Controls.Add(lbl("试验后质量 (g): *"));
        txtPostWeight = new TextBox
        {
            Location = new Point(170, y),
            Width = 200,
            Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(txtPostWeight);
        nextRow();

        // Flame checkbox
        chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(20, y),
            Font = new Font("Microsoft YaHei", 9),
            Size = new Size(200, 25)
        };
        chkFlame.CheckedChanged += (s, e) =>
        {
            nudFlameTime.Enabled = chkFlame.Checked;
            nudFlameDuration.Enabled = chkFlame.Checked;
        };
        Controls.Add(chkFlame);
        nextRow();

        // Flame time
        Controls.Add(lbl("火焰发生时刻 (秒):"));
        nudFlameTime = new NumericUpDown
        {
            Location = new Point(170, y),
            Width = 200,
            Minimum = 0,
            Maximum = 99999,
            Enabled = false,
            Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(nudFlameTime);
        nextRow();

        // Flame duration
        Controls.Add(lbl("火焰持续时间 (秒):"));
        nudFlameDuration = new NumericUpDown
        {
            Location = new Point(170, y),
            Width = 200,
            Minimum = 0,
            Maximum = 99999,
            Enabled = false,
            Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(nudFlameDuration);
        nextRow();

        // Memo
        y += 5;
        Controls.Add(lbl("备注:"));
        txtMemo = new TextBox
        {
            Location = new Point(20, y + 25),
            Width = 360,
            Height = 60,
            Multiline = true,
            Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(txtMemo);
        y += 95;

        // Buttons
        var btnSave = new Button
        {
            Text = "保存记录",
            Location = new Point(120, y),
            Size = new Size(120, 38),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => DoSave();
        Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(260, y),
            Size = new Size(100, 38),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);
    }

    private void DoSave()
    {
        if (!double.TryParse(txtPostWeight.Text, out double postWeight))
        {
            MessageBox.Show("请输入有效的试验后质量", "提示");
            return;
        }

        double preWeight = _app.TestController.PreWeight;
        double lostPer = (preWeight - postWeight) / preWeight * 100.0;
        double ambTemp = _app.TestController.AmbientTemp;
        int totalTime = _app.TestController.ElapsedSeconds;

        var sv = _app.TestController.SensorValues;
        double deltaTs = sv["TS"] - ambTemp;
        double deltaTc = sv["TC"] - ambTemp;
        double deltaTf1 = sv["TF1"] - ambTemp;
        double deltaTf2 = sv["TF2"] - ambTemp;
        double deltaTf = deltaTs; // 综合温升取表面温升

        // Max values and their times from sensor history
        var history = _app.TestController.SensorHistory;
        double maxTf1 = history.Count > 0 ? history.Max(h => h.Temp1) : sv["TF1"];
        double maxTf2 = history.Count > 0 ? history.Max(h => h.Temp2) : sv["TF2"];
        double maxTs = history.Count > 0 ? history.Max(h => h.TempSurface) : sv["TS"];
        double maxTc = history.Count > 0 ? history.Max(h => h.TempCenter) : sv["TC"];
        int maxTf1Time = history.FirstOrDefault(h => h.Temp1 == maxTf1)?.Time ?? totalTime;
        int maxTf2Time = history.FirstOrDefault(h => h.Temp2 == maxTf2)?.Time ?? totalTime;
        int maxTsTime = history.FirstOrDefault(h => h.TempSurface == maxTs)?.Time ?? totalTime;
        int maxTcTime = history.FirstOrDefault(h => h.TempCenter == maxTc)?.Time ?? totalTime;

        // Final values
        double finalTf1 = sv["TF1"], finalTf2 = sv["TF2"], finalTs = sv["TS"], finalTc = sv["TC"];

        int flameTime = chkFlame.Checked ? (int)nudFlameTime.Value : 0;
        int flameDuration = chkFlame.Checked ? (int)nudFlameDuration.Value : 0;
        string phenoCode = chkFlame.Checked ? "Flame" : "";

        _app.Db.UpdateTestResult(
            _app.TestController.CurrentProductId,
            _app.TestController.CurrentTestId,
            preWeight, postWeight, lostPer,
            deltaTf, deltaTs, deltaTc, deltaTf1, deltaTf2,
            totalTime, phenoCode, flameTime, flameDuration,
            maxTf1, maxTf2, maxTs, maxTc,
            maxTf1Time, maxTf2Time, maxTsTime, maxTcTime,
            finalTf1, finalTf2, finalTs, finalTc,
            totalTime, totalTime, totalTime, totalTime);

        DialogResult = DialogResult.OK;
        Close();
    }
}
```

- [ ] **验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add -A && git commit -m "feat: implement NewTestForm and TestRecordForm"
```

---

### Task 11: 集成验证和最终修复

- [ ] **Step 1: 完整构建**

```bash
cd C:/Users/23072/Desktop/homework/test-heat
dotnet build
```

Fix any compilation errors.

- [ ] **Step 2: 运行测试**

```bash
dotnet run --project src/ISO11820.App
```

Verify: Login form appears, can log in with admin/123456 or experimenter/123456.

- [ ] **Step 3: 检查所有功能**

Run through the demo flow:
1. Login → MainForm appears
2. 新建试验 → NewTestForm dialog opens
3. 开始升温 → Temperature starts rising
4. Wait for Ready state
5. 开始记录 → Timer starts, CSV data collects
6. 停止记录 → Completes
7. 试验记录 → Save record dialog
8. Check 记录查询 tab for the record
9. Verify CSV/Excel/PDF files created at D:\ISO11820\

- [ ] **Step 4: Commit final fixes**

```bash
git add -A && git commit -m "fix: integration fixes and final adjustments"
```

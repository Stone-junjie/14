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
        cmd.CommandText = @"INSERT OR IGNORE INTO productmaster (productid, productname, specific, diameter, height)
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
            return ReadProduct(reader);
        }
        return null;
    }

    private static ProductMaster ReadProduct(SqliteDataReader reader)
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
            return ReadTestMaster(reader);
        return null;
    }

    public List<TestMaster> QueryTests(DateTime from, DateTime to, string productId, string op)
    {
        var result = new List<TestMaster>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT * FROM testmaster
            WHERE testdate BETWEEN $from AND $to
            ORDER BY testdate DESC";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadTestMaster(reader));
        return result;
    }

    public bool HasUnfinishedTest()
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM testmaster WHERE totaltesttime > 0 AND flag != '10000000'";
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public TestMaster? GetUnfinishedTest()
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE totaltesttime > 0 AND flag != '10000000' LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMaster(reader);
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
        cmd.Parameters.AddWithValue("$ta1", record.TempA1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ta2", record.TempA2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ta3", record.TempA3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb1", record.TempB1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb2", record.TempB2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb3", record.TempB3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc1", record.TempC1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc2", record.TempC2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc3", record.TempC3 ?? (object)DBNull.Value);
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
            records.Add(ReadCalibrationRecord(reader));
        return records;
    }

    // ===== 执行SQL（初始化用）=====
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

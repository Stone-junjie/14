using ISO11820.App;
using ISO11820.Core.Enums;
using ISO11820.Core.Events;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;

namespace ISO11820.App.Forms;

public partial class MainForm : Form
{
    private readonly AppContext _app;
    private TabControl tabControl = null!;

    // Tab 1 controls
    private Label lblTf1 = null!, lblTf2 = null!, lblTs = null!, lblTc = null!, lblTCal = null!;
    private Label lblState = null!, lblTimer = null!, lblDrift = null!, lblProductId = null!;
    private RichTextBox rtbLog = null!;
    private PlotView plotView = null!;
    private Button btnNewTest = null!, btnStartHeat = null!, btnStopHeat = null!;
    private Button btnStartRecord = null!, btnStopRecord = null!, btnSaveRecord = null!;

    // Tab 2 controls
    private DataGridView dgvRecords = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private Button btnSearch = null!;

    // Tab 3 controls
    private Label lblCalibTemp = null!;

    // Chart data
    private LineSeries _seriesTf1 = null!, _seriesTf2 = null!, _seriesTs = null!, _seriesTc = null!;
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

        tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei", 10) };

        var tabTest = new TabPage("试验监控");
        BuildTestMonitorTab(tabTest);
        tabControl.TabPages.Add(tabTest);

        var tabQuery = new TabPage("记录查询");
        BuildQueryTab(tabQuery);
        tabControl.TabPages.Add(tabQuery);

        var tabCalib = new TabPage("设备校准");
        BuildCalibrationTab(tabCalib);
        tabControl.TabPages.Add(tabCalib);

        Controls.Add(tabControl);

        _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uiTimer.Tick += OnUiTimerTick;
        _uiTimer.Start();

        this.FormClosing += (s, e) =>
        {
            _app.DaqWorker.Stop();
            _app.DaqWorker.Dispose();
        };
    }

    #region Tab: 试验监控

    private void BuildTestMonitorTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(30, 30, 30);

        var leftPanel = new Panel
        {
            Width = 300, Dock = DockStyle.Left,
            BackColor = Color.FromArgb(45, 45, 48)
        };

        int y = 10;

        lblProductId = MakeInfoLabel("样品编号: --", ref y, leftPanel, 12, Color.Cyan);
        y += 10;

        lblTf1 = MakeTempLabel("炉温1 (TF1)", ref y, leftPanel);
        lblTf2 = MakeTempLabel("炉温2 (TF2)", ref y, leftPanel);
        lblTs = MakeTempLabel("表面温 (TS)", ref y, leftPanel);
        lblTc = MakeTempLabel("中心温 (TC)", ref y, leftPanel);
        lblTCal = MakeTempLabel("校准温 (TCal)", ref y, leftPanel);

        y += 10;

        lblState = MakeInfoLabel("状态: 空闲", ref y, leftPanel, 14, Color.LimeGreen);
        lblTimer = MakeInfoLabel("计时: 0 秒", ref y, leftPanel, 12, Color.White);
        lblDrift = MakeInfoLabel("温漂: 0.00 °C/10min", ref y, leftPanel, 10, Color.Gray);

        y += 10;

        btnNewTest = MakeButton("新建试验", ref y, leftPanel, Color.FromArgb(0, 120, 215));
        btnStartHeat = MakeButton("开始升温", ref y, leftPanel, Color.FromArgb(220, 80, 0));
        btnStopHeat = MakeButton("停止升温", ref y, leftPanel, Color.FromArgb(180, 60, 60));
        btnStartRecord = MakeButton("开始记录", ref y, leftPanel, Color.FromArgb(0, 150, 80));
        btnStopRecord = MakeButton("停止记录", ref y, leftPanel, Color.FromArgb(200, 140, 0));
        btnSaveRecord = MakeButton("试验记录", ref y, leftPanel, Color.FromArgb(100, 60, 180));

        tab.Controls.Add(leftPanel);

        var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };

        plotView = new PlotView
        {
            Dock = DockStyle.Top, Height = 420,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        SetupChart();
        rightPanel.Controls.Add(plotView);

        rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White, Font = new Font("Consolas", 9), ReadOnly = true
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
            Position = AxisPosition.Bottom, Title = "时间 (秒)",
            TitleColor = OxyColor.FromRgb(200, 200, 200),
            AxislineColor = OxyColor.FromRgb(80, 80, 80),
            TextColor = OxyColor.FromRgb(200, 200, 200),
            Minimum = 0, Maximum = 600
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left, Title = "温度 (°C)",
            TitleColor = OxyColor.FromRgb(200, 200, 200),
            AxislineColor = OxyColor.FromRgb(80, 80, 80),
            TextColor = OxyColor.FromRgb(200, 200, 200),
            Minimum = 0, Maximum = 800
        });

        _seriesTf1 = new LineSeries { Title = "炉温1", Color = OxyColor.FromRgb(255, 80, 80), StrokeThickness = 1.5 };
        _seriesTf2 = new LineSeries { Title = "炉温2", Color = OxyColor.FromRgb(255, 160, 60), StrokeThickness = 1.5 };
        _seriesTs = new LineSeries { Title = "表面温", Color = OxyColor.FromRgb(80, 200, 255), StrokeThickness = 1.5 };
        _seriesTc = new LineSeries { Title = "中心温", Color = OxyColor.FromRgb(80, 255, 120), StrokeThickness = 1.5 };

        model.Series.Add(_seriesTf1);
        model.Series.Add(_seriesTf2);
        model.Series.Add(_seriesTs);
        model.Series.Add(_seriesTc);

        plotView.Model = model;
    }

    private static Label MakeTempLabel(string name, ref int y, Panel parent)
    {
        var lbl = new Label
        {
            Text = $"{name}: --.- °C",
            Font = new Font("Consolas", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 200, 50),
            Location = new Point(10, y), Size = new Size(280, 35),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(60, 60, 65),
            BorderStyle = BorderStyle.FixedSingle
        };
        y += 40;
        parent.Controls.Add(lbl);
        return lbl;
    }

    private static Label MakeInfoLabel(string text, ref int y, Panel parent, int fontSize, Color color)
    {
        var lbl = new Label
        {
            Text = text, Font = new Font("Microsoft YaHei", fontSize),
            ForeColor = color, Location = new Point(10, y), Size = new Size(280, 25)
        };
        y += 30;
        parent.Controls.Add(lbl);
        return lbl;
    }

    private Button MakeButton(string text, ref int y, Panel parent, Color backColor)
    {
        var btn = new Button
        {
            Text = text, Font = new Font("Microsoft YaHei", 10),
            Location = new Point(10, y), Size = new Size(280, 35),
            FlatStyle = FlatStyle.Flat, BackColor = backColor, ForeColor = Color.White
        };
        btn.FlatAppearance.BorderSize = 0;
        y += 42;
        parent.Controls.Add(btn);

        switch (text)
        {
            case "新建试验": btn.Click += (s, e) => OnNewTest(); break;
            case "开始升温": btn.Click += (s, e) => OnStartHeating(); break;
            case "停止升温": btn.Click += (s, e) => OnStopHeating(); break;
            case "开始记录": btn.Click += (s, e) => OnStartRecording(); break;
            case "停止记录": btn.Click += (s, e) => OnStopRecording(); break;
            case "试验记录": btn.Click += (s, e) => OnSaveRecord(); break;
        }
        return btn;
    }

    #endregion

    #region Tab: 记录查询

    private void BuildQueryTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(240, 240, 245);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };

        topPanel.Controls.Add(new Label { Text = "从:", Location = new Point(10, 15), Font = new Font("Microsoft YaHei", 9) });
        dtpFrom = new DateTimePicker { Location = new Point(40, 12), Width = 130, Format = DateTimePickerFormat.Short };
        topPanel.Controls.Add(dtpFrom);

        topPanel.Controls.Add(new Label { Text = "到:", Location = new Point(180, 15), Font = new Font("Microsoft YaHei", 9) });
        dtpTo = new DateTimePicker { Location = new Point(210, 12), Width = 130, Format = DateTimePickerFormat.Short };
        topPanel.Controls.Add(dtpTo);

        btnSearch = new Button
        {
            Text = "查询", Location = new Point(355, 10), Size = new Size(80, 30),
            BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += (s, e) => RefreshQueryResults();
        topPanel.Controls.Add(btnSearch);

        var btnExport = new Button
        {
            Text = "导出Excel", Location = new Point(445, 10), Size = new Size(100, 30),
            BackColor = Color.FromArgb(0, 150, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.Click += (s, e) => ExportQueryResults();
        topPanel.Controls.Add(btnExport);

        tab.Controls.Add(topPanel);

        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true, AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        tab.Controls.Add(dgvRecords);

        dtpFrom.Value = DateTime.Now.AddMonths(-1);
        dtpTo.Value = DateTime.Now;
    }

    private void RefreshQueryResults()
    {
        var tests = _app.Db.QueryTests(dtpFrom.Value, dtpTo.Value, "", "");
        dgvRecords.DataSource = null;
        if (tests.Count == 0) return;

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

    private void ExportQueryResults()
    {
        var tests = _app.Db.QueryTests(dtpFrom.Value, dtpTo.Value, "", "");
        if (tests.Count == 0) { MessageBox.Show("没有查询结果可导出", "提示"); return; }

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        using var package = new OfficeOpenXml.ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("试验记录");
        sheet.Cells["A1"].Value = "试验ID";
        sheet.Cells["B1"].Value = "样品编号";
        sheet.Cells["C1"].Value = "试验日期";
        sheet.Cells["D1"].Value = "操作员";
        sheet.Cells["E1"].Value = "时长(秒)";
        sheet.Cells["F1"].Value = "失重率(%)";
        sheet.Cells["G1"].Value = "温升(°C)";
        for (int i = 0; i < tests.Count; i++)
        {
            sheet.Cells[i + 2, 1].Value = tests[i].TestId;
            sheet.Cells[i + 2, 2].Value = tests[i].ProductId;
            sheet.Cells[i + 2, 3].Value = tests[i].TestDate.ToString("yyyy-MM-dd");
            sheet.Cells[i + 2, 4].Value = tests[i].Operator;
            sheet.Cells[i + 2, 5].Value = tests[i].TotalTestTime;
            sheet.Cells[i + 2, 6].Value = tests[i].LostWeightPer;
            sheet.Cells[i + 2, 7].Value = tests[i].DeltaTf;
        }

        var dir = _app.Config.ReportOutputDirectory;
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"查询导出_{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        package.SaveAs(filePath);
        MessageBox.Show($"导出成功: {filePath}", "提示");
    }

    #endregion

    #region Tab: 设备校准

    private void BuildCalibrationTab(TabPage tab)
    {
        tab.BackColor = Color.FromArgb(240, 240, 245);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };

        lblCalibTemp = new Label
        {
            Text = "校准温度: --.- °C",
            Font = new Font("Consolas", 20, FontStyle.Bold),
            Location = new Point(10, 15), Size = new Size(350, 40),
            ForeColor = Color.FromArgb(200, 100, 0)
        };
        topPanel.Controls.Add(lblCalibTemp);

        var btnRecordCalib = new Button
        {
            Text = "记录校准数据",
            Location = new Point(400, 20), Size = new Size(130, 35),
            BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRecordCalib.FlatAppearance.BorderSize = 0;
        btnRecordCalib.Click += (s, e) =>
        {
            var record = new ISO11820.Data.CalibrationRecord
            {
                CalibrationType = "Surface",
                ApparatusId = 0,
                Operator = _app.TestController.OperatorName,
                TemperatureData = $"{{ \"temp\": {_app.TestController.SensorValues["TCal"]} }}",
                Remarks = "手动记录",
                PassedCriteria = 1
            };
            _app.Db.InsertCalibrationRecord(record);
            AddLog("校准数据已记录", Color.LimeGreen);
        };
        topPanel.Controls.Add(btnRecordCalib);

        tab.Controls.Add(topPanel);

        var dgvCalib = new DataGridView
        {
            Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true, AllowUserToAddRows = false
        };
        tab.Controls.Add(dgvCalib);
    }

    #endregion

    #region Events and Updates

    private void SubscribeEvents()
    {
        _app.TestController.DataBroadcast += OnDataBroadcast;
    }

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(() => OnDataBroadcast(sender, e));
            return;
        }

        lblTf1.Text = $"炉温1 (TF1): {e.SensorValues["TF1"]:F1} °C";
        lblTf2.Text = $"炉温2 (TF2): {e.SensorValues["TF2"]:F1} °C";
        lblTs.Text = $"表面温 (TS): {e.SensorValues["TS"]:F1} °C";
        lblTc.Text = $"中心温 (TC): {e.SensorValues["TC"]:F1} °C";
        lblTCal.Text = $"校准温 (TCal): {e.SensorValues["TCal"]:F1} °C";
        lblCalibTemp.Text = $"校准温度: {e.SensorValues["TCal"]:F1} °C";

        lblState.Text = e.CurrentState switch
        {
            TestState.Idle => "状态: 空闲",
            TestState.Preparing => "状态: 升温中",
            TestState.Ready => "状态: 就绪 ✓",
            TestState.Recording => "状态: 记录中 ●",
            TestState.Complete => "状态: 完成",
            _ => "状态: 未知"
        };
        lblTimer.Text = $"计时: {e.ElapsedSeconds} 秒";
        lblProductId.Text = $"样品编号: {e.ProductId}";

        _plotTime += 1;
        _plotData.Enqueue((_plotTime,
            e.SensorValues["TF1"], e.SensorValues["TF2"],
            e.SensorValues["TS"], e.SensorValues["TC"]));
        while (_plotData.Count > 600) _plotData.Dequeue();

        foreach (var msg in e.Messages)
        {
            Color color = msg.Message.Contains("终止") ? Color.Yellow :
                          msg.Message.Contains("稳定") ? Color.LimeGreen : Color.White;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText($"{msg.Time}  {msg.Message}\n");
            rtbLog.ScrollToCaret();
        }

        UpdateButtonStates();
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

        double drift = _app.TestController.CalculateTemperatureDrift();
        lblDrift.Text = $"温漂: {drift:F2} °C/10min";
    }

    private void UpdateButtonStates()
    {
        var state = _app.TestController.CurrentState;
        btnNewTest.Enabled = state == TestState.Idle;
        btnStartHeat.Enabled = state == TestState.Idle;
        btnStopHeat.Enabled = state == TestState.Preparing || state == TestState.Ready;
        btnStartRecord.Enabled = state == TestState.Ready;
        btnStopRecord.Enabled = state == TestState.Recording;
        btnSaveRecord.Enabled = state == TestState.Complete;
    }

    #endregion

    #region Button Actions

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

    private void AddLog(string message, Color color)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(() => AddLog(message, color));
            return;
        }
        var time = DateTime.Now.ToString("HH:mm:ss");
        rtbLog.SelectionColor = color;
        rtbLog.AppendText($"{time}  {message}\n");
        rtbLog.ScrollToCaret();
    }

    #endregion
}

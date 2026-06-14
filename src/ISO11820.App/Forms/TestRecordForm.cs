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

        Label L(string text) => new Label
        {
            Text = text, Location = new Point(20, y + 3),
            Font = new Font("Microsoft YaHei", 9), Size = new Size(140, 25)
        };
        void Next() => y += 35;

        Controls.Add(L("试验前质量 (g):"));
        Controls.Add(new Label
        {
            Text = _app.TestController.PreWeight.ToString("F1"),
            Location = new Point(170, y + 3),
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold), Size = new Size(100, 25)
        });
        Next();

        Controls.Add(L("试验后质量 (g): *"));
        txtPostWeight = new TextBox { Location = new Point(170, y), Width = 200, Font = new Font("Microsoft YaHei", 9) };
        Controls.Add(txtPostWeight);
        Next();

        chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰", Location = new Point(20, y),
            Font = new Font("Microsoft YaHei", 9), Size = new Size(200, 25)
        };
        chkFlame.CheckedChanged += (s, e) =>
        {
            nudFlameTime.Enabled = chkFlame.Checked;
            nudFlameDuration.Enabled = chkFlame.Checked;
        };
        Controls.Add(chkFlame);
        Next();

        Controls.Add(L("火焰发生时刻 (秒):"));
        nudFlameTime = new NumericUpDown
        {
            Location = new Point(170, y), Width = 200,
            Minimum = 0, Maximum = 99999, Enabled = false, Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(nudFlameTime);
        Next();

        Controls.Add(L("火焰持续时间 (秒):"));
        nudFlameDuration = new NumericUpDown
        {
            Location = new Point(170, y), Width = 200,
            Minimum = 0, Maximum = 99999, Enabled = false, Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(nudFlameDuration);
        Next();

        y += 5;
        Controls.Add(L("备注:"));
        txtMemo = new TextBox
        {
            Location = new Point(20, y + 25), Width = 360, Height = 60,
            Multiline = true, Font = new Font("Microsoft YaHei", 9)
        };
        Controls.Add(txtMemo);
        y += 95;

        var btnSave = new Button
        {
            Text = "保存记录", Location = new Point(120, y), Size = new Size(120, 38),
            BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 10)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => DoSave();
        Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text = "取消", Location = new Point(260, y), Size = new Size(100, 38),
            FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 10)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);
    }

    private void DoSave()
    {
        if (!double.TryParse(txtPostWeight.Text, out double postWeight))
        { MessageBox.Show("请输入有效的试验后质量", "提示"); return; }

        double preWeight = _app.TestController.PreWeight;
        double lostPer = (preWeight - postWeight) / preWeight * 100.0;
        double ambTemp = _app.TestController.AmbientTemp;
        int totalTime = _app.TestController.ElapsedSeconds;

        var sv = _app.TestController.SensorValues;
        double deltaTs = sv["TS"] - ambTemp;
        double deltaTc = sv["TC"] - ambTemp;
        double deltaTf1 = sv["TF1"] - ambTemp;
        double deltaTf2 = sv["TF2"] - ambTemp;
        double deltaTf = deltaTs;

        var history = _app.TestController.SensorHistory;
        double maxTf1 = history.Count > 0 ? history.Max(h => h.Temp1) : sv["TF1"];
        double maxTf2 = history.Count > 0 ? history.Max(h => h.Temp2) : sv["TF2"];
        double maxTs = history.Count > 0 ? history.Max(h => h.TempSurface) : sv["TS"];
        double maxTc = history.Count > 0 ? history.Max(h => h.TempCenter) : sv["TC"];
        int maxTf1Time = history.FirstOrDefault(h => h.Temp1 == maxTf1)?.Time ?? totalTime;
        int maxTf2Time = history.FirstOrDefault(h => h.Temp2 == maxTf2)?.Time ?? totalTime;
        int maxTsTime = history.FirstOrDefault(h => h.TempSurface == maxTs)?.Time ?? totalTime;
        int maxTcTime = history.FirstOrDefault(h => h.TempCenter == maxTc)?.Time ?? totalTime;

        double finalTf1 = sv["TF1"], finalTf2 = sv["TF2"], finalTs = sv["TS"], finalTc = sv["TC"];

        int flameTime = chkFlame.Checked ? (int)nudFlameTime.Value : 0;
        int flameDuration = chkFlame.Checked ? (int)nudFlameDuration.Value : 0;
        string phenoCode = chkFlame.Checked ? "Flame" : "";

        _app.Db.UpdateTestResult(
            _app.TestController.CurrentProductId, _app.TestController.CurrentTestId,
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

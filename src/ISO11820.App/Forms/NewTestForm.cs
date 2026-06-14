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

        Label MakeLabel(string text, int x) => new Label
        {
            Text = text, Location = new Point(x, y + 3),
            Font = new Font("Microsoft YaHei", 9), Size = new Size(105, 25)
        };

        TextBox MakeTextBox(int x, string defaultVal = "") => new TextBox
        {
            Location = new Point(x + 110, y),
            Width = 280, Font = new Font("Microsoft YaHei", 9), Text = defaultVal
        };

        void NextRow() => y += 35;

        Controls.Add(MakeLabel("样品编号:", 20));
        txtProductId = MakeTextBox(20, DateTime.Now.ToString("yyyyMMdd") + "-001"); Controls.Add(txtProductId); NextRow();

        Controls.Add(MakeLabel("样品名称:", 20));
        txtProductName = MakeTextBox(20, "岩棉隔热板"); Controls.Add(txtProductName); NextRow();

        Controls.Add(MakeLabel("规格型号:", 20));
        txtSpecific = MakeTextBox(20, "100×50×25mm"); Controls.Add(txtSpecific); NextRow();

        Controls.Add(MakeLabel("直径 (mm):", 20));
        txtDiameter = MakeTextBox(20, "100"); Controls.Add(txtDiameter); NextRow();

        Controls.Add(MakeLabel("高度 (mm):", 20));
        txtHeight = MakeTextBox(20, "50"); Controls.Add(txtHeight); NextRow();

        Controls.Add(MakeLabel("环境温度 (°C):", 20));
        txtAmbTemp = MakeTextBox(20, "25.0"); Controls.Add(txtAmbTemp); NextRow();

        Controls.Add(MakeLabel("环境湿度 (%):", 20));
        txtAmbHumi = MakeTextBox(20, "50.0"); Controls.Add(txtAmbHumi); NextRow();

        Controls.Add(MakeLabel("试验前质量 (g):", 20));
        txtPreWeight = MakeTextBox(20, "50.0"); Controls.Add(txtPreWeight); NextRow();

        Controls.Add(MakeLabel("时长模式:", 20));
        cmbDuration = new ComboBox
        {
            Location = new Point(130, y), Width = 130,
            Font = new Font("Microsoft YaHei", 9),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbDuration.Items.AddRange(new[] { "标准60分钟", "自定义分钟" });
        cmbDuration.SelectedIndex = 0;
        Controls.Add(cmbDuration);

        nudCustomMinutes = new NumericUpDown
        {
            Location = new Point(270, y), Width = 90,
            Minimum = 1, Maximum = 120, Value = 30,
            Font = new Font("Microsoft YaHei", 9), Enabled = false
        };
        Controls.Add(nudCustomMinutes);
        cmbDuration.SelectedIndexChanged += (s, e) => nudCustomMinutes.Enabled = cmbDuration.SelectedIndex == 1;
        NextRow();
        y += 15;

        var btnOk = new Button
        {
            Text = "创建试验", Location = new Point(140, y), Size = new Size(130, 38),
            BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 10)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => DoCreate();
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消", Location = new Point(290, y), Size = new Size(100, 38),
            FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 10)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(btnCancel);

        y += 50;
        Controls.Add(new Label
        {
            Text = $"操作员: {_app.TestController.OperatorName} | 设备: FURNACE-01 一号试验炉",
            Location = new Point(20, y), Size = new Size(400, 25),
            Font = new Font("Microsoft YaHei", 8), ForeColor = Color.Gray
        });
    }

    private void DoCreate()
    {
        if (string.IsNullOrWhiteSpace(txtProductId.Text))
        { MessageBox.Show("请输入样品编号", "提示"); return; }
        if (!double.TryParse(txtPreWeight.Text, out double preWeight))
        { MessageBox.Show("请输入有效的试验前质量", "提示"); return; }

        try
        {
            string productId = txtProductId.Text.Trim();
            string testId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            int duration = cmbDuration.SelectedIndex == 1 ? (int)nudCustomMinutes.Value * 60 : 3600;

            _app.TestController.TargetDurationSeconds = duration;
            _app.TestController.CreateTest(productId,
                txtProductName.Text.Trim(), txtSpecific.Text.Trim(),
                double.TryParse(txtDiameter.Text, out double d) ? d : 100,
                double.TryParse(txtHeight.Text, out double h) ? h : 50,
                preWeight,
                double.TryParse(txtAmbTemp.Text, out double at) ? at : 25.0,
                double.TryParse(txtAmbHumi.Text, out double ah) ? ah : 50.0,
                testId);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建试验失败: {ex.Message}", "错误");
        }
    }
}

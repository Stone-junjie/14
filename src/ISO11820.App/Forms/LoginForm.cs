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
            Size = new Size(220, 25),
            Checked = true
        };

        rbExperimenter = new RadioButton
        {
            Text = "试验员 (experimenter)",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(100, 140),
            Size = new Size(220, 25)
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

        Controls.AddRange(new Control[] { lblTitle, lblRole, rbAdmin, rbExperimenter,
            lblPwd, txtPassword, btnLogin, lblError });
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

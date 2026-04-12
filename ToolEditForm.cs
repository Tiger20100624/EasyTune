using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EasyTune
{
    public partial class ToolEditForm : Form
    {
        public ToolItem Tool { get; private set; }

        private TextBox txtName, txtExe;
        private ComboBox cmbCat;
        private Button btnOk, btnCancel;

        public ToolEditForm(ToolItem tool = null)
        {
            Tool = tool ?? new ToolItem();
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "添加/编辑工具";
            this.Size = new Size(420, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // 名称
            Label lblName = new Label { Text = "名称:", Location = new Point(20, 20), AutoSize = true };
            txtName = new TextBox { Location = new Point(90, 18), Size = new Size(280, 23) };

            // 程序路径
            Label lblExe = new Label { Text = "程序:", Location = new Point(20, 55), AutoSize = true };
            txtExe = new TextBox { Location = new Point(90, 53), Size = new Size(220, 23) };
            Button btnBrowseExe = new Button { Text = "浏览", Location = new Point(315, 52), Size = new Size(60, 23) };
            btnBrowseExe.Click += (s, e) =>
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Filter = "可执行文件|*.exe|所有文件|*.*";
                    dlg.Title = "选择工具程序";

                    // 设置初始目录为程序所在目录下的 Tool 文件夹
                    string toolFolder = Path.Combine(Application.StartupPath, "Tool");
                    if (Directory.Exists(toolFolder))
                        dlg.InitialDirectory = toolFolder;
                    else
                        dlg.InitialDirectory = Application.StartupPath;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        txtExe.Text = dlg.FileName;
                    }
                }
            };

            // 分类
            Label lblCat = new Label { Text = "分类:", Location = new Point(20, 90), AutoSize = true };
            cmbCat = new ComboBox
            {
                Location = new Point(90, 88),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // 注意：这里没有“主界面”选项
            cmbCat.Items.AddRange(new[] { "CPU工具", "主板工具", "内存工具", "显卡工具", "硬盘工具", "其他工具" });

            // 确定、取消按钮
            btnOk = new Button { Text = "确定", Location = new Point(180, 140), Size = new Size(75, 23) };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtExe.Text))
                {
                    MessageBox.Show("名称和程序路径不能为空！");
                    return;
                }
                Tool.Name = txtName.Text.Trim();
                Tool.ExePath = txtExe.Text.Trim();
                Tool.Category = cmbCat.SelectedItem?.ToString() ?? "其他工具";
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel = new Button { Text = "取消", Location = new Point(270, 140), Size = new Size(75, 23) };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] {
                lblName, txtName,
                lblExe, txtExe, btnBrowseExe,
                lblCat, cmbCat,
                btnOk, btnCancel
            });
        }

        private void LoadData()
        {
            txtName.Text = Tool.Name;
            txtExe.Text = Tool.ExePath;

            // 如果旧分类是“主界面”，强制改为“其他工具”
            if (Tool.Category == "主界面")
                Tool.Category = "其他工具";

            cmbCat.SelectedItem = Tool.Category;
        }
    }
}
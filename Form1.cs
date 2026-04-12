using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace EasyTune
{
    public partial class Form1 : Form
    {
        private List<ToolItem> tools = new List<ToolItem>();
        private FlowLayoutPanel panelTools;
        private Button[] menuBtns;
        private Label lblTime;
        private Label txtHardware;
        private string configPath = Application.StartupPath + "\\ToolsConfig.json";
        private string currentCategory = "主界面";
        private Panel infoPanel;   // 信息面板
        private Button btnAdd;     // 添加按钮（也方便后面调整）
        private Panel rightPanel;
        private Label lblWarning;   // 警示标签

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            LoadTools();
            RefreshToolsPanel();
            StartClock();
            ShowHardware();


            LoadBackgroundFromFolder();
            this.Icon = Properties.Resources.EasyTune;

        }

        /// <summary>
        /// 从程序根目录下的 Wallpaper 文件夹中自动加载背景图片
        /// </summary>
        /// <summary>
        /// 将绝对路径转换为相对于程序所在目录的相对路径（如果可能）
        /// </summary>
        private string MakeRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;

            string startupPath = Application.StartupPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(absolutePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // 如果不在同一个驱动器，无法转为相对路径，则保持原样（极少数情况）
            if (!fullPath.StartsWith(startupPath, StringComparison.OrdinalIgnoreCase))
                return absolutePath;

            string relativePath = fullPath.Substring(startupPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relativePath;
        }

        /// <summary>
        /// 将相对路径（或绝对路径）解析为当前运行环境下的绝对路径
        /// </summary>
        private string ResolveToolPath(string storedPath)
        {
            if (string.IsNullOrEmpty(storedPath))
                return storedPath;

            // 如果已经是绝对路径（如 C:\...），且文件存在，直接返回
            if (Path.IsPathRooted(storedPath) && File.Exists(storedPath))
                return storedPath;

            // 否则视为相对路径，基于 Application.StartupPath 拼接
            string fullPath = Path.Combine(Application.StartupPath, storedPath);
            return fullPath;
        }
        private void LoadBackgroundFromFolder()
        {
            string wallpaperFolder = Path.Combine(Application.StartupPath, "Wallpaper");
            if (!Directory.Exists(wallpaperFolder))
                return;

            string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
            List<string> imageFiles = new List<string>();
            foreach (string ext in imageExtensions)
                imageFiles.AddRange(Directory.GetFiles(wallpaperFolder, ext));

            if (imageFiles.Count == 0)
                return;

            string firstImage = imageFiles[0];
            try
            {
                // 设置右侧面板的背景图
                if (rightPanel.BackgroundImage != null)
                {
                    rightPanel.BackgroundImage.Dispose();
                    rightPanel.BackgroundImage = null;
                }
                rightPanel.BackgroundImage = Image.FromFile(firstImage);
                rightPanel.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch
            {
                // 忽略错误
            }
        }
        /// <summary>
        /// 根据 panelTools 中当前显示的顺序，重新整理 tools 列表中当前分类的工具项顺序
        /// </summary>
        private void ReorderToolsByCurrentDisplay()
        {
            // 获取当前分类下所有工具项（按原顺序）
            var catTools = tools.Where(t => t.Category == currentCategory).ToList();

            // 根据卡片控件当前显示顺序，构建一个新的有序列表
            List<ToolItem> orderedTools = new List<ToolItem>();
            foreach (Control c in panelTools.Controls)
            {
                if (c.Tag is ToolItem tool && tool.Category == currentCategory)
                    orderedTools.Add(tool);
            }

            // 如果数量对不上，说明有异常，放弃本次排序
            if (orderedTools.Count != catTools.Count)
                return;

            // 从全局 tools 列表中移除当前分类的所有项
            tools.RemoveAll(t => t.Category == currentCategory);

            // 按新顺序插入回全局列表（保持其他分类的顺序不变）
            // 简单起见，我们找到原分类第一个项的位置，然后批量插入
            
            // 找到第一个不属于当前分类的项之前的插入点（通常在末尾或开头，这里简化处理：直接追加到末尾，因为分类间顺序无要求）
            // 但为了保持全局列表整洁，我们按原分类在全局列表中的大致位置插入
            // 简便做法：直接遍历原列表，重建整个列表
            List<ToolItem> newTools = new List<ToolItem>();
            foreach (var t in tools)
            {
                if (t.Category != currentCategory)
                    newTools.Add(t);
            }
            // 将排序后的当前分类项追加到末尾（也可以插入到原位置，但分类顺序不影响功能）
            newTools.AddRange(orderedTools);
            tools = newTools;

            SaveTools();
        }
        
        private void BuildUI()
        {
            this.Text = "Easy Tune";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 左侧菜单栏
            Panel left = new Panel
            {
                Size = new Size(150, ClientSize.Height),
                Location = Point.Empty,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            Controls.Add(left);

            string[] cats = { "主界面", "CPU工具", "主板工具", "内存工具", "显卡工具", "硬盘工具", "其他工具" };
            menuBtns = new Button[cats.Length];
            for (int i = 0; i < cats.Length; i++)
            {
                Button btn = new Button
                {
                    Text = cats[i],
                    Size = new Size(150, 35),
                    Location = new Point(0, i * 35),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 60, 65),
                    ForeColor = Color.White,
                    Font = new Font("微软雅黑", 10),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(15, 0, 0, 0),
                    Tag = cats[i]
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += Menu_Click;
                menuBtns[i] = btn;
                left.Controls.Add(btn);
            }



            // 右侧主区域
            rightPanel = new Panel
            {
                Location = new Point(150, 0),
                Size = new Size(ClientSize.Width - 150, ClientSize.Height),
                BackColor = Color.White
            };
            Controls.Add(rightPanel);

            // 信息面板
            infoPanel = new Panel
            {
                Size = new Size(rightPanel.Width, 180),
                Location = new Point(0, 10),
                BackColor = Color.FromArgb(0, 255, 255, 255)  // 半透明白色，200是透明度（0-255），数值越小越透明
            };
            // 警示标签（仅主界面可见）
            lblWarning = new Label
            {
                Text = "🔴 请将绿色工具放置在 U盘:\\Tool 文件夹内，否则无法运行！",
                Location = new Point(20, 208),          // 位于硬件信息面板下方（infoPanel 高度180 + 间距15）
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.Red,
                BackColor = Color.FromArgb(80, 255, 255, 255),
                Visible = false                         // 初始隐藏，由 RefreshToolsPanel 控制
            };
            rightPanel.Controls.Add(lblWarning);
            rightPanel.Controls.Add(infoPanel);

            // 时间标签
            lblTime = new Label
            {
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            infoPanel.Controls.Add(lblTime);   // 原来是 info.Controls.Add(lblTime)，现改为 infoPanel

            // 硬件信息文本框
            txtHardware = new Label
            {
                Location = new Point(20, 50),
                Size = new Size(infoPanel.Width - 40, 110),
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,   // 完全透明，让 infoPanel 的半透明背景透出
                ForeColor = Color.Black,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None
            };
            infoPanel.Controls.Add(txtHardware);

            btnAdd = new Button
            {
                Text = "➕ 添加工具",
                Size = new Size(110, 35),                // 000
                Location = new Point(10, rightPanel.Height - 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Regular)
            };
            // 下面是新增的边框和悬停效果，让按钮有“按钮样”
            btnAdd.FlatAppearance.BorderSize = 1;
            btnAdd.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);
            btnAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 140, 240);
            btnAdd.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 80, 160);

            btnAdd.Click += (s, e) => AddTool();
            rightPanel.Controls.Add(btnAdd);
            btnAdd.BringToFront();   // 保证按钮在最上层，不被其他东西挡住

            panelTools = new FlowLayoutPanel
            {
                Location = new Point(10, 200),
                Size = new Size(rightPanel.Width - 20, rightPanel.Height - 210),
                AutoScroll = true,
                BackColor = Color.FromArgb(80, 255, 255, 255),  // 同样设为半透明白色
                Padding = new Padding(0)
            };
            rightPanel.Controls.Add(panelTools);

            // 高亮默认菜单
            menuBtns[0].BackColor = Color.FromArgb(0, 122, 204);
        }

        private void Menu_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            currentCategory = btn.Tag.ToString();
            RefreshToolsPanel();
            foreach (Button b in menuBtns)
                b.BackColor = (b == btn) ? Color.FromArgb(0, 122, 204) : Color.FromArgb(60, 60, 65);
        }

        private void RefreshToolsPanel()
        {
            panelTools.Controls.Clear();
            var catTools = tools.Where(t => t.Category == currentCategory).ToList();
            foreach (var tool in catTools)
                AddToolButton(tool);

            // 控制信息面板和时间的显示
            bool isMainPage = (currentCategory == "主界面");
            infoPanel.Visible = isMainPage;
            lblTime.Visible = isMainPage;
            lblWarning.Visible = isMainPage;

            // ----- 新增：动态调整工具面板的位置和大小 -----
            // 获取右侧面板的引用（假设我们在 BuildUI 中把它存为了类字段，如果没有，我们先加一个字段）
            // 简便方法：通过 panelTools.Parent 获取父控件（就是 right 面板）
            Control rightPanel = panelTools.Parent;
            if (rightPanel != null)
            {
                if (isMainPage)
                {
                    // 主界面：工具面板放在硬件信息下方（Y=200），高度缩小
                    panelTools.Location = new Point(10, 200);
                    panelTools.Size = new Size(rightPanel.Width - 20, rightPanel.Height - 210);
                }
                else
                {
                    // 其他分类：工具面板紧贴顶部（Y=10），高度撑满
                    panelTools.Location = new Point(10, 10);
                    panelTools.Size = new Size(rightPanel.Width - 20, rightPanel.Height - 20);
                }
            }
        }

        private void AddToolButton(ToolItem tool)
        {
            Panel pnl = new Panel
            {
                Size = new Size(80, 60),
                Margin = new Padding(1),
                BackColor = Color.Transparent
            };
            pnl.Tag = tool;

            PictureBox pic = new PictureBox
            {
                Size = new Size(36, 36),
                Location = new Point(23, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            // 图标提取
            Image iconImage = null;
            try
            {
                string exePath = ResolveToolPath(tool.ExePath);
                if (!string.IsNullOrEmpty(exePath) && !File.Exists(exePath) && !exePath.Contains("\\"))
                {
                    string sysPath = Path.Combine(Environment.SystemDirectory, exePath);
                    if (File.Exists(sysPath)) exePath = sysPath;
                }
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    using (Icon ico = Icon.ExtractAssociatedIcon(exePath))
                    {
                        if (ico != null) iconImage = ico.ToBitmap();
                    }
                }
            }
            catch { }
            if (iconImage == null)
            {
                try { iconImage = SystemIcons.Application.ToBitmap(); }
                catch { iconImage = new Bitmap(32, 32); }
            }
            pic.Image = iconImage;

            // 左键点击启动（避免右键误启动）
            pic.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    LaunchTool(tool);
            };
            pnl.Controls.Add(pic);

            Label lbl = new Label
            {
                Text = tool.Name,
                AutoSize = false,
                Size = new Size(80, 20),
                Location = new Point(2, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 8F),//改字号
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            lbl.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    LaunchTool(tool);
            };
            pnl.Controls.Add(lbl);

            // 右键菜单
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem 上移 = new ToolStripMenuItem("上移");
            上移.Click += (s, e) =>
            {
                int currentIndex = panelTools.Controls.IndexOf(pnl);
                if (currentIndex > 0)
                {
                    panelTools.Controls.SetChildIndex(pnl, currentIndex - 1);
                    ReorderToolsByCurrentDisplay();
                }
            };
            menu.Items.Add(上移);

            ToolStripMenuItem 下移 = new ToolStripMenuItem("下移");
            下移.Click += (s, e) =>
            {
                int currentIndex = panelTools.Controls.IndexOf(pnl);
                if (currentIndex >= 0 && currentIndex < panelTools.Controls.Count - 1)
                {
                    panelTools.Controls.SetChildIndex(pnl, currentIndex + 1);
                    ReorderToolsByCurrentDisplay();
                }
            };
            menu.Items.Add(下移);

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("编辑", null, (s, e) => EditTool(tool));
            menu.Items.Add("删除", null, (s, e) =>
            {
                tools.Remove(tool);
                SaveTools();
                panelTools.Controls.Remove(pnl);
            });

            // 绑定右键菜单到所有控件
            pnl.ContextMenuStrip = menu;
            pic.ContextMenuStrip = menu;
            lbl.ContextMenuStrip = menu;

            panelTools.Controls.Add(pnl);
        }


        private void LaunchTool(ToolItem tool)
        {
            try
            {
                string runPath = ResolveToolPath(tool.ExePath);
                if (!string.IsNullOrEmpty(runPath) && !File.Exists(runPath) && !runPath.Contains("\\"))
                {
                    string sysPath = Path.Combine(Environment.SystemDirectory, runPath);
                    if (File.Exists(sysPath))
                        runPath = sysPath;
                }

                // 获取工具所在目录（工作目录）
                string workingDir = Path.GetDirectoryName(runPath) ?? Application.StartupPath;

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = runPath,
                    WorkingDirectory = workingDir
                    // 注意：这里先不设置 Arguments
                };

                // ----- 新增：根据文件名智能判断是否需要启动参数 -----
                string fileName = Path.GetFileName(runPath).ToLowerInvariant();

                if (fileName.Contains("cpuz"))
                {
                    // CPU-Z 需要 -cfg 参数指定配置路径
                    psi.Arguments = "-cfg=" + workingDir;
                }
                else if (fileName.Contains("tm5"))
                {
                    // TM5 不支持任何命令行参数，必须清空
                    psi.Arguments = "";
                }
                // 其他工具不设置 Arguments，保持默认即可
                // ---------------------------------------------

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void AddTool()
        {
            ToolItem newTool = new ToolItem { Category = currentCategory };
            var dlg = new ToolEditForm(newTool);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // 将绝对路径转换为相对于程序目录的路径
                dlg.Tool.ExePath = MakeRelativePath(dlg.Tool.ExePath);
                tools.Add(dlg.Tool);
                SaveTools();
                RefreshToolsPanel();
            }
        }

        private void EditTool(ToolItem tool)
        {
            var dlg = new ToolEditForm(tool);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                dlg.Tool.ExePath = MakeRelativePath(dlg.Tool.ExePath);
                SaveTools();
                RefreshToolsPanel();
            }
        }

        

        private void LoadTools()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    tools = JsonConvert.DeserializeObject<List<ToolItem>>(File.ReadAllText(configPath)) ?? new List<ToolItem>();
                }
                catch { tools = new List<ToolItem>(); }

                // 升级旧配置：将绝对路径转换为相对路径
                bool needSave = false;
                foreach (var t in tools)
                {
                    string oldPath = t.ExePath;
                    string newPath = MakeRelativePath(oldPath);
                    if (oldPath != newPath)
                    {
                        t.ExePath = newPath;
                        needSave = true;
                    }
                }
                if (needSave)
                    SaveTools();
            }
            else
            {
                tools = new List<ToolItem>
        {
          
        };
                SaveTools();
            }
        }

        private void SaveTools()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(tools, Newtonsoft.Json.Formatting.Indented));
        }

        private void StartClock()
        {
            Timer t = new Timer { Interval = 1000 };
            t.Tick += (s, e) => lblTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss dddd");
            t.Start();
        }

        private void ShowHardware()
        {
            txtHardware.Text = HardwareInfoService.GetSummary();
        }
    }
}
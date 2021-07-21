using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tbm_launcher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        class LaunchInfo {
            private Label _obj_status;
            private Label _obj_name;
            private Label _port;
            private Button _lnk_launch;
            private Button _lnk_change_port;
            private Button _lnk_inst_req;
            private Panel _parent_control;

            private bool depend_resolvable;
            private string name;
            private string command;
            private int port;
            private int status;
            private string requirement_test;

            bool manual_terminate = false;


            public LaunchInfo(string name, string command, int port, int status, string req, int index, Panel container) {
                this.name = name;
                this.command = command;
                this.port = port;
                this.status = status;
                this.requirement_test = req;
                this._parent_control = container;

                const int height = 30;

                _obj_name = new Label();
                _obj_name.Text = name;
                _obj_name.Location = new Point(0, height * index);
                _obj_name.Width = 195;
                container.Controls.Add(_obj_name);

                _obj_status = new Label();
                _obj_status.Text = "获取运行信息...";
                _obj_status.ForeColor = Color.Gray;
                _obj_status.Location = new Point(200, height * index);
                container.Controls.Add(_obj_status);

                _port = new Label();
                _port.Text = port.ToString();
                _port.Location = new Point(300, height * index);
                container.Controls.Add(_port);
                _port.Width = 50;
                _port.Click += (object s, EventArgs e) =>
                {
                    if (Status == 1)
                    {
                        Process.Start("http://127.0.0.1:" + port + "/");
                    }
                };
                _port.MouseEnter += (object s, EventArgs e) => { 
                    if (Status == 1)
                    {
                        _port.ForeColor = Color.Purple;
                        //Cursor.Current = Cursors.Hand;
                    }
                };
                _port.MouseLeave += (object sender, EventArgs e) =>
                {
                    if (Status == 1)
                    {
                        _port.ForeColor = Color.Blue;
                        //Cursor.Current = Cursors.Default;
                    }
                };

                _lnk_launch = new Button();
                _lnk_launch.Text = "启动";
                _lnk_launch.Location = new Point(360, height * index);
                _lnk_launch.FlatStyle = FlatStyle.System;
                _lnk_launch.Enabled = false;
                _lnk_launch.Click += (object sender, EventArgs e) =>
                {
                    
                    if (Status == 1) end();
                    else start();
                };
                container.Controls.Add(_lnk_launch);

                _lnk_change_port = new Button();
                _lnk_change_port.Text = "Change Port";
                _lnk_change_port.Location = new Point(420, height * index);
                _lnk_change_port.Visible = false;
                _lnk_change_port.FlatStyle = FlatStyle.System;
                container.Controls.Add(_lnk_change_port);

                _lnk_inst_req = new Button();
                _lnk_inst_req.Text = "安装依赖";
                _lnk_inst_req.Visible = false;
                _lnk_inst_req.Location = new Point(260, height * index);
                _lnk_inst_req.FlatStyle = FlatStyle.System;
                container.Controls.Add(_lnk_inst_req);

                RetriveRunningInformation();
            }

            public void RetriveRunningInformation()
            {
                Status = 0;
                Thread th = new Thread(() => {
                    //bool isOpened = IsPortOpen("127.0.0.1", port, TimeSpan.FromMilliseconds(1000));
                    TcpHelperUtil tcpHelper = new TcpHelperUtil();
                    bool isOpened = tcpHelper.GetPortDetails(port).Item1;
                    Status = isOpened ? 1 : 2;
                    if (isOpened) return;
                    bool reqSat = check_requirements();
                    if (!reqSat) Status = 3;
                });
                th.Start();
            }

            public string Name
            {
                get { return name; }
                set { name = value; _obj_name.Text = name; }
            }

            public int Port
            {
                get { return port; }
                set { port = value; _port.Text = value.ToString(); }
            }

            private int Status
            {
                get { return status; }
                set {
                    status = value;
                    if (status == 0) { _obj_status.Text = "获取运行信息..."; _lnk_launch.Show(); _obj_status.ForeColor = Color.Gray; _lnk_launch.Enabled = false; _lnk_launch.Text = "启动"; }
                    else if (status == 1) { _obj_status.Text = "正在运行"; _obj_status.ForeColor = Color.Green; _lnk_launch.Enabled = true; _lnk_launch.Text = "停止"; }
                    else if (status == 2) { _obj_status.Text = "已停止"; _obj_status.ForeColor = Color.Gray; _lnk_launch.Enabled = true; _lnk_launch.Text = "启动"; _lnk_inst_req.Hide(); }
                    else if (status == 3) { _obj_status.Text = "缺少依赖"; _obj_status.ForeColor = Color.Red; _lnk_launch.Hide(); _lnk_inst_req.Show(); }
                    else if (status == 4) { _obj_status.Text = "正在处理"; _obj_status.ForeColor = Color.Black; _lnk_inst_req.Hide(); _lnk_launch.Enabled = false; }
                    else if (status == 6) { _obj_status.Text = "运行失败"; _lnk_launch.Text = "启动"; _obj_status.ForeColor = Color.Red; }

                    if (status == 1) { _port.ForeColor = Color.Blue; _port.Cursor = Cursors.Hand; _port.Font = new Font(_parent_control.Font, FontStyle.Underline); }
                    else { _port.ForeColor = Color.Black; _port.Cursor = Cursors.Default; _port.Font = _parent_control.Font; }
                }
            }

            private int launch_get_return_code(string command) {
                string executable = command.Split(new char[] { ' ' })[0];
                string args = executable.Length == command.Length ? "" : command.Substring(executable.Length);
                try
                {
                    Process p = new Process();
                    p.StartInfo.FileName = executable;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    p.WaitForExit();
                    return p.ExitCode;
                }
                catch (Exception ee)
                {
                    return -1;
                }
            }

            private void end()
            {
                manual_terminate = true;
                Status = 4;
                Thread th2 = new Thread(() =>
                {
                    TcpHelperUtil tcpHelper = new TcpHelperUtil();
                    var details = tcpHelper.GetPortDetails(port);
                    if (details.Item1)
                    {
                        var p = new Process();
                        p.StartInfo.FileName = ("taskkill.exe");
                        p.StartInfo.Arguments = "/pid " + details.Item2.ProcessID + " /f";
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        p.Start();
                        p.WaitForExit();
                        if (p.ExitCode == 0)
                            Status = 2;
                        else
                            MessageBox.Show("Unknown error occurred. (EXITCODE=" + p.ExitCode + ")");
                    }

                });
                th2.Start();
            }

            private void start()
            {
                manual_terminate = false;
                Thread th1 = new Thread(() => {
                    Status = 4;
                    run_program(command.Replace("{port}", "" + port));
                });
                th1.Start();
            }

            public void Start()
            {
                if (Status == 2 || Status == 6)
                    start();
            }

            public void End()
            {
                if (Status == 1)
                    end();
            }

            private bool check_requirements()
            {
                return launch_get_return_code(requirement_test) == 0;
            }

            bool IsPortOpen(string host, int port, TimeSpan timeout)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        var result = client.BeginConnect(host, port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(timeout);
                        client.EndConnect(result);
                        return success;
                    }
                }
                catch
                {
                    return false;
                }
            }

            private void run_program(string command)
            {
                string executable = command.Split(new char[] { ' ' })[0];
                string args = executable.Length == command.Length ? "" : command.Substring(executable.Length);
                try
                {
                    Process p = new Process();
                    p.StartInfo.FileName = executable;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    Status = 1;

                    p.WaitForExit();
                    if (p.ExitCode == 0 || manual_terminate)
                    {
                        Status = 2;
                        manual_terminate = false;
                    }
                    else
                        Status = 6;
                }
                catch
                {
                    Status = 6;
                }
            }
         }

        LaunchInfo[] LI;

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            LI = new LaunchInfo[4];
            LI[0] = new LaunchInfo("数据采集和实时数据提供", "tbm.exe -p {port} -D tbm.db", 7706, 0, "tbm.exe -v", 0, panel1);
            LI[1] = new LaunchInfo("实时采集数据可视化", "http-server.cmd ./webclient/ -p {port} -P http://localhost:7706", 8080, 0, "http-server.cmd -v", 1, panel1);
            LI[2] = new LaunchInfo("旧数据提供", "tbm.exe -p {port} -D new.db --disable-collector", 7707, 0, "tbm.exe -v", 2, panel1);
            LI[3] = new LaunchInfo("旧数据的可视化", "http-server  ./webclient/ -p {port} -P http://localhost:7707", 8081, 0, "http-server -v", 3, panel1);
    
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Application.Exit();
        }

        private void btn_launch_Click(object sender, EventArgs e)
        {
            btn_launch.Enabled = false;
            foreach (LaunchInfo l in LI)
                l.Start();
            btn_launch.Enabled = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            btn_stop.Enabled = false;
            foreach (LaunchInfo l in LI)
                l.End();
            btn_stop.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (LaunchInfo l in LI)
                l.RetriveRunningInformation();
        }
    }
}

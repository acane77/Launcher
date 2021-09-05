using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tbm_launcher
{
    class LaunchInfo
    {
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
        private string status_check_method;

        bool manual_terminate = false;


        public LaunchInfo(string name, string command, int port, int status, string req, int index, string status_check_method, Panel container)
        {
            this.name = name;
            this.command = command;
            this.port = port;
            this.status = status;
            this.requirement_test = req;
            this.status_check_method = status_check_method;
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
            if (Port > 0)
                container.Controls.Add(_port);

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
            //container.Controls.Add(_lnk_change_port);

            _lnk_inst_req = new Button();
            _lnk_inst_req.Text = "安装依赖";
            _lnk_inst_req.Visible = false;
            _lnk_inst_req.Location = new Point(260, height * index);
            _lnk_inst_req.FlatStyle = FlatStyle.System;
            container.Controls.Add(_lnk_inst_req);

            RetriveRunningInformation();
        }

        bool getProcessRunningStatus()
        {
            string program_name = command.Split(" ".ToCharArray())[0].Split("\\/".ToCharArray()).Last().Trim();
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
                if (program_name.ToLower() == p.ProcessName.ToLower() || program_name.ToLower() == p.ProcessName.ToLower() + ".exe")
                    return true;
            return false;
        }

        public void RetriveRunningInformation(bool slient = false)
        {
            if (!slient)  Status = RunningStatus.RETERVING_RUNNING_STATUS;
            Thread th = new Thread(() =>
            {
                //bool isOpened = IsPortOpen("127.0.0.1", port, TimeSpan.FromMilliseconds(1000));

                bool isOpened = false;
                if (status_check_method == IniConfigReader.StatusCheckMethod.CHECK_PORT_USAGE)
                {
                    TcpHelperUtil tcpHelper = new TcpHelperUtil();
                    isOpened = tcpHelper.GetPortDetails(port).Item1;
                }
                else if (status_check_method == IniConfigReader.StatusCheckMethod.CHECK_EXECUTABLE_EXISTANCE)
                {
                    isOpened = getProcessRunningStatus();
                }
                else
                {
                    Status = RunningStatus.CONFIG_ERROR;
                    return;
                }
                if (isOpened) {
                    Status = RunningStatus.RUNNING;
                    return;
                }
                bool reqSat = check_requirements();
                if (!reqSat) Status = RunningStatus.LACK_OF_DEPENDENCIES;
                else Status = RunningStatus.STOPPED;
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

        public class RunningStatus
        {
            public const int RETERVING_RUNNING_STATUS = 0;
            public const int RUNNING = 1;
            public const int STOPPED = 2;
            public const int LACK_OF_DEPENDENCIES = 3;
            public const int PROCESSING = 4;
            public const int FAILED = 6;
            public const int CONFIG_ERROR = 7;
        } 

        private int Status
        {
            get { return status; }
            set
            {
                status = value;
                if (status == 0) { _obj_status.Text = "获取运行信息..."; _lnk_launch.Show(); _obj_status.ForeColor = Color.Gray; _lnk_launch.Enabled = false; _lnk_launch.Text = "启动"; }
                else if (status == 1) { _obj_status.Text = "正在运行"; _obj_status.ForeColor = Color.Green; _lnk_launch.Enabled = true; _lnk_launch.Text = "停止"; }
                else if (status == 2) { _obj_status.Text = "已停止"; _obj_status.ForeColor = Color.Gray; _lnk_launch.Enabled = true; _lnk_launch.Text = "启动"; _lnk_inst_req.Hide(); }
                else if (status == 3) { _obj_status.Text = "缺少依赖"; _obj_status.ForeColor = Color.Red; _lnk_launch.Hide(); _lnk_inst_req.Show(); }
                else if (status == 4) { _obj_status.Text = "正在处理"; _obj_status.ForeColor = Color.Black; _lnk_inst_req.Hide(); _lnk_launch.Enabled = false; }
                else if (status == 6) { _obj_status.Text = "运行失败"; _lnk_launch.Text = "启动"; _obj_status.ForeColor = Color.Red; }
                else if (status == 7) { _obj_status.Text = "配置文件错误"; _obj_status.ForeColor = Color.Red; _lnk_launch.Hide(); _lnk_inst_req.Show(); }
                if (status == 1) { _port.ForeColor = Color.Blue; _port.Cursor = Cursors.Hand; _port.Font = new Font(_parent_control.Font, FontStyle.Underline); }
                else { _port.ForeColor = Color.Black; _port.Cursor = Cursors.Default; _port.Font = _parent_control.Font; }
            }
        }

        private int launch_get_return_code(string command)
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
            Status = RunningStatus.PROCESSING;
            Thread th2 = new Thread(() =>
            {
                var p = new Process();
                p.StartInfo.FileName = ("taskkill.exe");
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (status_check_method == IniConfigReader.StatusCheckMethod.CHECK_PORT_USAGE)
                {
                    TcpHelperUtil tcpHelper = new TcpHelperUtil();
                    var details = tcpHelper.GetPortDetails(port);
                    if (details.Item1)
                        p.StartInfo.Arguments = "/pid " + details.Item2.ProcessID + " /f"; 
                }
                else if (status_check_method == IniConfigReader.StatusCheckMethod.CHECK_EXECUTABLE_EXISTANCE)
                {
                    string program_name = command.Split(" ".ToCharArray())[0].Split("\\/".ToCharArray()).Last().Trim();
                    p.StartInfo.Arguments = "/im " + program_name + " /f";
                }
                p.Start();
                p.WaitForExit();
                if (p.ExitCode == 0)
                    Status = RunningStatus.STOPPED;
                else
                {
                    MessageBox.Show(_parent_control.Parent, "在此之前该程序已经停止运行 (EXITCODE=" + p.ExitCode + ")");
                    Status = RunningStatus.STOPPED;
                }
            });
            th2.Start();
        }

        private void start()
        {
            manual_terminate = false;
            Thread th1 = new Thread(() => {
                Status = RunningStatus.PROCESSING;
                run_program(command.Replace("{port}", "" + port));
            });
            th1.Start();
        }

        public void Start()
        {
            if (Status == RunningStatus.STOPPED || Status == RunningStatus.FAILED)
                start();
        }

        public void End()
        {
            if (Status == RunningStatus.RUNNING)
                end();
        }

        private bool check_requirements()
        {
            if (requirement_test == "CHECK_EXISTANCE")
            {
                return File.Exists(command.Split(" ".ToCharArray())[0].Trim());
            }
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
                Status = RunningStatus.RUNNING;

                p.WaitForExit();
                if (p.ExitCode == 0 || manual_terminate)
                {
                    Status = RunningStatus.STOPPED;
                    manual_terminate = false;
                }
                else
                    Status = RunningStatus.FAILED;
            }
            catch
            {
                Status = RunningStatus.FAILED;
            }
        }
    }

}

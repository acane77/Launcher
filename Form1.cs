using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        List<LaunchInfo> LI = new List<LaunchInfo>();

        private void Form1_Load(object sender, EventArgs e)
        {
            const string CONFIG_FILENAME = ProgramGlobalConfig.CONFIG_FILENAME;
            bool has_error = false;
            string err_msg = "配置文件存在以下错误：";
            if (!File.Exists(CONFIG_FILENAME))
                File.Create(CONFIG_FILENAME);
            try
            {
                IniConfigReader configReader = new IniConfigReader(CONFIG_FILENAME);
                configReader.Container = panel1;
                configReader.onLoadConfigError = (LaunchInfo L, IniConfigReader.ParseError err) =>
                {
                    LI.Add(L);
                    has_error = true;
                    err_msg += "\r\nLine " + err.Line + ": " + err.What;
                    
                };
                configReader.OnReadConfigItem = (LaunchInfo L) =>
                {
                    LI.Add(L);
                };
                configReader.LoadConfig();
                timerRefresh.Enabled = true;
                if (configReader.ProgramTitle != "")
                    label1.Text = this.Text = configReader.ProgramTitle;
                if (has_error)
                    MessageBox.Show(err_msg);
            }
            catch (Exception ee)
            {
                MessageBox.Show("加载配置文件失败:" + ee.Message);
            }
            Control.CheckForIllegalCrossThreadCalls = false;

            //LI.Add(new LaunchInfo("数据采集和实时数据提供", "tbm.exe -p {port} -D tbm.db", 7706, 0, "tbm.exe -v", 0, panel1));
            //LI.Add(new LaunchInfo("实时采集数据可视化", "http-server.cmd ./webclient/ -p {port} -P http://localhost:7706", 8080, 0, "http-server.cmd -v", 1, panel1));
            //LI.Add(new LaunchInfo("旧数据提供", "tbm.exe -p {port} -D new.db --disable-collector", 7707, 0, "tbm.exe -v", 2, panel1));
            //LI.Add(new LaunchInfo("旧数据的可视化", "http-server  ./webclient/ -p {port} -P http://localhost:7707", 8081, 0, "http-server -v", 3, panel1));
            
            if (ProgramGlobalConfig.StartWithConfigureFlag)
            {
                //buttonConfig.Show();
                Hide();
                button2_Click(null, null);
            }
            //buttonConfig.Show(); // todo remove it
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
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
                l.RetriveRunningInformation(true);
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            foreach (LaunchInfo l in LI)
                l.RetriveRunningInformation(true);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var form = new FormConfigure();
            form.IniConfigureList = LI;
            form.SystemTitle = Text;
            form.ShowDialog();
        }
    }
}

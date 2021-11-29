using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static tbm_launcher.IniConfigReader;

namespace tbm_launcher
{
    public partial class FormConfigure : Form
    {
        public FormConfigure()
        {
            InitializeComponent();
        }

        public List<LaunchInfo> IniConfigureList = null;
        List<MetaInformation<LaunchInfoPlain>> settingItemConfigs = new List<MetaInformation<LaunchInfoPlain>>();
        public string SystemTitle = "";

        void InitializeConfigItem()
        {
            settingItemConfigs.Clear();
            settingItemConfigs.Add(new MetaInformation<LaunchInfoPlain>
            {
                ConfigName = "name",
                FriendlyConfigName = "服务名称",
                ConfigType = MetaInformation<LaunchInfoPlain>.CONFIG_TYPE_STRING,
                GetValueHandler = (LaunchInfoPlain p) => { return p.Name; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.Name = val; }
            });;

            settingItemConfigs.Add(new MetaInformation<LaunchInfoPlain>
            {
                ConfigName = "command",
                FriendlyConfigName = "命令",
                ConfigType = MetaInformation<LaunchInfoPlain>.CONFIG_TYPE_FILE,
                GetValueHandler = (LaunchInfoPlain p) => { return p.Command; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.Command = val; }
            });

            settingItemConfigs.Add(new MetaInformation<LaunchInfoPlain>
            {
                ConfigName = "port",
                FriendlyConfigName = "端口",
                ConfigType = MetaInformation<LaunchInfoPlain>.CONFIG_TYPE_INT,
                GetValueHandler = (LaunchInfoPlain p) => { return p.PortNumber.ToString(); },
                SetValueHandler = (LaunchInfoPlain p, string val) => {
                    try { p.PortNumber = Int32.Parse(val); }
                    catch { p.PortNumber = 0; }    
                }
            });

            settingItemConfigs.Add(new MetaInformation<LaunchInfoPlain>
            {
                ConfigName = "requirement_command",
                FriendlyConfigName = "依赖检查命令",
                ConfigType = MetaInformation<LaunchInfoPlain>.CONFIG_TYPE_FILE,
                ListItems = new List<MetaInformation<LaunchInfoPlain>.ListItem>
                {
                    new MetaInformation<LaunchInfoPlain>.ListItem{ Name = "检查文件是否存在", Value = "CHECK_EXISTANCE" },
                    new MetaInformation<LaunchInfoPlain>.ListItem{ Name = "不检查依赖", Value = "DO_NOT_CHECK" },
                },
                GetValueHandler = (LaunchInfoPlain p) => { return p.RequirementCommand; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.RequirementCommand = val; }
            });

            settingItemConfigs.Add(new MetaInformation<LaunchInfoPlain>
            {
                ConfigName = "status_check_method",
                FriendlyConfigName = "运行状态检查",
                ConfigType = MetaInformation<LaunchInfoPlain>.CONFIG_TYPE_LIST,
                ListItems = new List<MetaInformation<LaunchInfoPlain>.ListItem>
                {
                    new MetaInformation<LaunchInfoPlain>.ListItem{ Name = "检查端口占用", Value = StatusCheckMethod.CHECK_PORT_USAGE },
                    new MetaInformation<LaunchInfoPlain>.ListItem{ Name = "检查进程是否存在", Value = StatusCheckMethod.CHECK_EXECUTABLE_EXISTANCE },
                    new MetaInformation<LaunchInfoPlain>.ListItem{ Name = "不检查运行状态", Value = StatusCheckMethod.NO_CHECK }
                },
                GetValueHandler = (LaunchInfoPlain p) => { return p.StatusCheckMethod; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.StatusCheckMethod = val; }
            });

            settingItemConfigs.Add(new MetaInformation<LaunchInfoPlain>
            {
                ConfigName = "run_background",
                FriendlyConfigName = "在后台运行",
                ConfigType = MetaInformation<LaunchInfoPlain>.CONFIG_TYPE_BOOL,
                GetValueHandler = (LaunchInfoPlain p) => { return p.RunBackground ? "1" : "0"; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.RunBackground = val == "1"; }
            });

            MetaInformation<LaunchInfoPlain>.ValueFillHandler = FillValueOfValuedSettingItem;
        }

        string FillValueOfValuedSettingItem(string nameInConfig, LaunchInfoPlain info)
        {
            switch (nameInConfig)
            {
                case "name":
                    return info.Name;
                case "command":
                    return info.Command;
                case "port":
                    return info.PortNumber.ToString();
                case "requirement_command":
                    return info.RequirementCommand;
                case "status_check_method":
                    return info.StatusCheckMethod;
                case "run_background":
                    return info.RunBackground ? "1" : "0";
            }
            return "<Error Value>";
        }

        string GenerateIniString()
        {
            string str = "[SYSTEM]" + CRLF;
            str += "title=" + textBox1.Text + CRLF + CRLF;
            foreach (object o in listConfig.Items)
            {
                LaunchInfoPlain p = o as LaunchInfoPlain;
                str += MetaInformation<LaunchInfoPlain>.GenerateIniString(settingItemConfigs, p);
            }
            return str;
        }

        void RenderConfigItemList()
        {
            listConfig.Items.Clear();
            foreach (LaunchInfo info in IniConfigureList)
            {
                listConfig.Items.Add(info.GetLauncherInfoPlain());
            }
            if (listConfig.Items.Count > 1)
                listConfig.SelectedIndex = 0;
        }

        private void FormConfigure_Load(object sender, EventArgs e)
        {
            InitializeConfigItem();
            RenderConfigItemList();
            textBox1.Text = SystemTitle;
        }

        private void listConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listConfig.SelectedIndex == -1)
            {
                panel_config.Controls.Clear();
                return;
            }
            LastSelectedConfigItem = listConfig.SelectedIndex;
            MetaInformation<LaunchInfoPlain>.RenderControlGroup(settingItemConfigs, 
                listConfig.SelectedItem as LaunchInfoPlain, panel_config);
        }

        private void FormConfigure_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void btn_launch_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string str = GenerateIniString();
            File.WriteAllText(ProgramGlobalConfig.CONFIG_FILENAME, str);
            Close();
        }

        int new_config_count = 1;
        private void button2_Click(object sender, EventArgs e)
        {
            string newConfigName = "新建配置项 #" + new_config_count;
            new_config_count++;
            LaunchInfoPlain L = new LaunchInfoPlain
            {
                Name = newConfigName,
                PortNumber = 0,
                Command = "",
                RequirementCommand = "",
                StatusCheckMethod = StatusCheckMethod.CHECK_PORT_USAGE
            };
            listConfig.Items.Add(L);
            listConfig.SelectedIndex = listConfig.Items.Count - 1;
        }

        const string CRLF = "\r\n";

        int LastSelectedConfigItem = -1;

        private void button3_Click(object sender, EventArgs e)
        {
            if (listConfig.SelectedIndex == -1)
                return;
            int sel = listConfig.SelectedIndex;
            listConfig.Items.RemoveAt(sel);
            listConfig.SelectedIndex = Math.Min(sel, listConfig.Items.Count - 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string str = GenerateIniString();
            MessageBox.Show(str);
        }
    }
}

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

        class BaseSettingItemConfig<ValueType>
        {
            public class ListItem
            {
                public string Name;
                public string Value;
            }

            public class ValidateResult {
                public int Code;
                public string Message;
                private int v1;
                private string v2;

                public ValidateResult(int v1, string v2)
                {
                    this.v1 = v1;
                    this.v2 = v2;
                }
            }
            public readonly ValidateResult VALIDATER_PASSED = new ValidateResult(0, "Success");

            public delegate bool Validator<T>(T value);
            public delegate string GetValueT(ValueType valueObj);
            public delegate void SetValueT(ValueType valueObj, string value);
            public Validator<string> ValidateInputHandler = null;
            public GetValueT GetValueHandler = null;
            public SetValueT SetValueHandler = null;

            public const int CONFIG_TYPE_LIST = 1;
            public const int CONFIG_TYPE_STRING = 2;
            public const int CONFIG_TYPE_FILE = 3;
            public const int CONFIG_TYPE_INT = 4;
            public const int CONFIG_TYPE_DECIMAL = 5;
            public string ConfigName;
            public string FriendlyConfigName;
            public int ConfigType;
            public List<ListItem> ListItems;
        }

        class SettingItemConfig : BaseSettingItemConfig<LaunchInfoPlain> { }

        public List<LaunchInfo> IniConfigureList = null;
        List<SettingItemConfig> settingItemConfigs = new List<SettingItemConfig>();

        void InitializeConfigItem()
        {
            settingItemConfigs.Clear();
            settingItemConfigs.Add(new SettingItemConfig
            {
                ConfigName = "name",
                FriendlyConfigName = "服务名称",
                ConfigType = SettingItemConfig.CONFIG_TYPE_STRING,
                GetValueHandler = (LaunchInfoPlain p) => { return p.Name; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.Name = val; }
            });;

            settingItemConfigs.Add(new SettingItemConfig
            {
                ConfigName = "command",
                FriendlyConfigName = "命令",
                ConfigType = SettingItemConfig.CONFIG_TYPE_STRING,
                GetValueHandler = (LaunchInfoPlain p) => { return p.Command; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.Command = val; }
            });

            settingItemConfigs.Add(new SettingItemConfig
            {
                ConfigName = "port",
                FriendlyConfigName = "端口",
                ConfigType = SettingItemConfig.CONFIG_TYPE_INT,
                GetValueHandler = (LaunchInfoPlain p) => { return p.PortNumber.ToString(); },
                SetValueHandler = (LaunchInfoPlain p, string val) => {
                    try { p.PortNumber = Int32.Parse(val); }
                    catch { p.PortNumber = 0; }    
                }
            });

            settingItemConfigs.Add(new SettingItemConfig
            {
                ConfigName = "requirement_command",
                FriendlyConfigName = "依赖检查命令",
                ConfigType = SettingItemConfig.CONFIG_TYPE_STRING,
                ListItems = new List<SettingItemConfig.ListItem>
                {
                    new SettingItemConfig.ListItem{ Name = "CHECK_EXISTANCE", Value = "CHECK_EXISTANCE" },
                },
                GetValueHandler = (LaunchInfoPlain p) => { return p.RequirementCommand; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.RequirementCommand = val; }
            });

            settingItemConfigs.Add(new SettingItemConfig
            {
                ConfigName = "status_check_method",
                FriendlyConfigName = "运行状态检查",
                ConfigType = SettingItemConfig.CONFIG_TYPE_LIST,
                ListItems = new List<SettingItemConfig.ListItem>
                {
                    new SettingItemConfig.ListItem{ Name = "检查端口占用", Value = StatusCheckMethod.CHECK_PORT_USAGE },
                    new SettingItemConfig.ListItem{ Name = "检查进程是否存在", Value = StatusCheckMethod.CHECK_EXECUTABLE_EXISTANCE },
                },
                GetValueHandler = (LaunchInfoPlain p) => { return p.StatusCheckMethod; },
                SetValueHandler = (LaunchInfoPlain p, string val) => { p.StatusCheckMethod = val; }
            });
        }

        void RenderConfigInputItem(int index, string value, LaunchInfoPlain info) 
        {
            int baseHeight = index * 30 + 15;
            int baseTitleLeft = 20;
            int baseValueLeft = 120; 
            SettingItemConfig settingItem = settingItemConfigs[index];
            Label labelConfigName = new Label();
            labelConfigName.Text = settingItem.FriendlyConfigName;
            labelConfigName.Location = new Point(baseTitleLeft, baseHeight);
            labelConfigName.Size = new Size(baseValueLeft - baseTitleLeft, 22);
            panel_config.Controls.Add(labelConfigName);

            Control valueControl = null;
            if (settingItem.ConfigType == SettingItemConfig.CONFIG_TYPE_STRING
                || settingItem.ConfigType == SettingItemConfig.CONFIG_TYPE_FILE
                || settingItem.ConfigType == SettingItemConfig.CONFIG_TYPE_INT
                || settingItem.ConfigType == SettingItemConfig.CONFIG_TYPE_DECIMAL)
            {
                if (settingItem.ListItems != null)
                {
                    ComboBox comboBox = new ComboBox();
                    foreach (SettingItemConfig.ListItem item in settingItem.ListItems)
                        comboBox.Items.Add(item.Name ?? item.Value);
                    valueControl = comboBox;
                }
                else
                {
                    TextBox valueCtrl = new TextBox();
                    valueControl = valueCtrl;
                }
                valueControl.TextChanged += (object s_, EventArgs e_) => {
                    settingItem.SetValueHandler(info, (s_ as Control).Text);
                };
                valueControl.Text = value;
            }
            else if (settingItem.ConfigType == SettingItemConfig.CONFIG_TYPE_LIST)
            {
                ComboBox dropDownList = new ComboBox();
                valueControl = dropDownList;
                dropDownList.DropDownStyle = ComboBoxStyle.DropDownList;
                if (settingItem.ListItems != null)
                {
                    int selectedIndex = 0; // 默认检查端口占用，所以选0
                    int _currentIndex = 0;
                    foreach (SettingItemConfig.ListItem item in settingItem.ListItems)
                    {
                        dropDownList.Items.Add(item.Name ?? item.Value);
                        dropDownList.SelectedIndexChanged += (object sender_, EventArgs e_) => {
                            settingItem.SetValueHandler(info, settingItem.ListItems[(sender_ as ComboBox).SelectedIndex].Value);
                        };
                        if (item.Value == value)
                            selectedIndex = _currentIndex;
                        _currentIndex++;
                    }
                    dropDownList.Tag = settingItem.ListItems;
                    if (selectedIndex >= 0)
                        dropDownList.SelectedIndex = selectedIndex;
                }
            }
            valueControl.Location = new Point(baseValueLeft, baseHeight);
            valueControl.Size = new Size(panel_config.Width - baseValueLeft - 10, 22);
            panel_config.Controls.Add(valueControl);
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
            }
            return "<Error Value>";
        }

        void RenderConfigInputPanel(LaunchInfoPlain info)
        {
            panel_config.Controls.Clear();
            for (int i = 0; i < settingItemConfigs.Count; i++)
            {
                string value = FillValueOfValuedSettingItem(settingItemConfigs[i].ConfigName, info);
                RenderConfigInputItem(i, value, info);
            }
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
            
        }

        private void listConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listConfig.SelectedIndex == -1)
            {
                panel_config.Controls.Clear();
                return;
            }
            RenderConfigInputPanel(listConfig.SelectedItem as LaunchInfoPlain);
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
            string str = "";
            foreach (object o in listConfig.Items)
            {
                LaunchInfoPlain p = o as LaunchInfoPlain;
                str += GenerateIniString(p);
            }
            //MessageBox.Show(str);
            File.WriteAllText(ProgramGlobalConfig.CONFIG_FILENAME, str);
            Close();
        }

        int new_config_count = 1;

        private void button2_Click(object sender, EventArgs e)
        {
            string newConfigName = "new_config_" + new_config_count;
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

        string GenerateIniString(LaunchInfoPlain p)
        {
            string str = "[custom_program]\r\n";
            foreach (SettingItemConfig conf in settingItemConfigs) {
                str += conf.ConfigName + "=" + conf.GetValueHandler(p).Replace("\r","").Replace("\n","") + "\r\n";
            }
            return str + CRLF;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listConfig.SelectedIndex == -1)
                return;
            int sel = listConfig.SelectedIndex;
            listConfig.Items.RemoveAt(sel);
            listConfig.SelectedIndex = Math.Min(sel, listConfig.Items.Count - 1);
        }

        
    }
}

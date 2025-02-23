using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace tbm_launcher
{

    

    class IniConfigReader
    {
        string content;

        public Panel Container = null;

        public string ProgramTitle = "TBM Service Launcher";

        public delegate void OnReadConfigItemDelegate(LaunchInfo LI);
        public delegate void OnLoadConfigError(LaunchInfo LI, ParseError err);

        public OnReadConfigItemDelegate OnReadConfigItem = null;
        public OnLoadConfigError onLoadConfigError = null;

        public IniConfigReader(string filename)
        {
            content = File.ReadAllText(filename);
        }

        public class ParseError : Exception
        {
            public int Line;
            public string What;
            public ParseError(int line, string what)
            {
                Line = line; What = what;
            }

            public override string Message => What;
        }

        delegate void EmitParseErrorFuncTy(string what);

        public void LoadConfig()
        {
            string[] lines = content.Split("\n".ToCharArray());

            LaunchInfoData data = new LaunchInfoData();

            bool initialized = false;
            int line_no = 0;
            int count = 0;
            bool on_error = false;
            bool is_system_config = false;

            EmitParseErrorFuncTy EmitParseError = (string what) =>
            {
                if (is_system_config) return;
                LaunchInfo LI = new LaunchInfo(data, count, Container);
                ParseError err = new ParseError(line_no, what);
                onLoadConfigError?.Invoke(LI, err);
                on_error = true;
            };
            foreach (string line in lines)
            {
                line_no++;
                if (line.Trim() == "")
                    continue;
                if (line.Trim()[0] == '[')
                {
                    if (initialized)
                    {
                        if (!on_error && !is_system_config)
                        {
                            LaunchInfo LI = new LaunchInfo(data, count, Container);
                            data = new LaunchInfoData();
                            OnReadConfigItem?.Invoke(LI);
                            count++;
                        }
                    }
                    else
                        initialized = true;
                    if (line.Trim() == "[SYSTEM]")
                    {
                        is_system_config = true;
                    }
                    else is_system_config = false;
                    data.Name = line.Replace("[", "").Replace("]", "");
                    data.Command = "";
                    data.PortNumber = 0;
                    data.RequirementCommand = "";
                    on_error = false;
                    continue;
                }
                if (!initialized)
                {
                    EmitParseError("配置文件格式错误，需要分组头");
                }
                if (is_system_config)
                {
                    string[] partsS = line.Split("=".ToCharArray());
                    string settingNameS = partsS[0].Trim().ToLower();
                    string valueS = line.Substring(partsS[0].Length + 1).Trim();
                    switch (settingNameS)
                    {
                        case "title":
                            ProgramTitle = valueS;
                            break;
                        case "comment":
                            break;
                    }
                    continue;
                }
                // if (on_error) continue;
                string[] parts = line.Split("=".ToCharArray(), 2);
                if (parts.Length < 2)
                {
                    EmitParseError("配置项格式错误");
                }
                string settingName = parts[0].Trim().ToLower();
                string value = line.Substring(parts[0].Length+1).Trim();
                try
                {
                    data.SetFieldsFromString(settingName, value);
                }
                catch (InvalidConfigureValueError e)
                {
                    EmitParseError(e.Message);
                }
                
            }

            if (!on_error && !is_system_config)
            {
                LaunchInfo LI = new LaunchInfo(data, count, Container);
                data = new LaunchInfoData();
                OnReadConfigItem?.Invoke(LI);
            }
        }
    }
}

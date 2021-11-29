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

        public class StatusCheckMethod {
            public static string CHECK_PORT_USAGE = "PORT_USAGE";
            public static string CHECK_EXECUTABLE_EXISTANCE = "EXECUTABLE_EXISTANCE";
            public static string NO_CHECK = "NO_CHECK";
        }

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

            string name = "";
            string command = "";
            int port = 0;
            string req_check_cmd = "";
            bool run_background = true;
            string status_check_method = "CHECK_PORT_USAGE"; // CHECK_EXECUTABLE_EXISTANCE    CHECK_PORT_USAGE
            bool initialized = false;

            int line_no = 0;
            int count = 0;
            bool on_error = false;
            bool is_system_config = false;

            EmitParseErrorFuncTy EmitParseError = (string what) =>
            {
                if (is_system_config) return;
                LaunchInfo LI = new LaunchInfo(name, command, port, LaunchInfo.RunningStatus.CONFIG_ERROR, 
                        req_check_cmd, count, status_check_method, run_background, Container);
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
                            LaunchInfo LI = new LaunchInfo(name, command, port, LaunchInfo.RunningStatus.RETERVING_RUNNING_STATUS, 
                                req_check_cmd, count, status_check_method, run_background, Container);
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
                    name = line.Replace("[", "").Replace("]", "");
                    command = "";
                    port = 0;
                    req_check_cmd = "";
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
                string[] parts = line.Split("=".ToCharArray());
                if (parts.Length < 2)
                {
                    EmitParseError("配置项格式错误");
                }
                string settingName = parts[0].Trim().ToLower();
                string value = line.Substring(parts[0].Length+1).Trim();
                switch (settingName)
                {
                    case "comment":
                        break;
                    case "name":
                        name = value;
                        break;
                    case "command":
                        command = value;
                        break;
                    case "port":
                        try { 
                            port = Int32.Parse(value);
                            if (port < 0 || port > 65535)
                                EmitParseError("端口号取值范围是 0-65535.");
                        }
                        catch (Exception ee) { EmitParseError(ee.Message); }
                        break;
                    case "requirement_command":
                        req_check_cmd = value;
                        break;
                    case "status_check_method":
                        if (value != StatusCheckMethod.CHECK_EXECUTABLE_EXISTANCE && value != StatusCheckMethod.CHECK_PORT_USAGE && value != StatusCheckMethod.NO_CHECK)
                            EmitParseError("配置项：错误的启动状态检查方式：" + value +
                                ", 可选的值有：" + StatusCheckMethod.CHECK_PORT_USAGE + "," + StatusCheckMethod.NO_CHECK + "和" + StatusCheckMethod.CHECK_EXECUTABLE_EXISTANCE);
                        status_check_method = value;
                        break;
                    case "run_background":
                        run_background = value == "1";
                        break;
                    default:
                        EmitParseError("没有此配置项：" + settingName);
                        break;
                }
            }

            if (!on_error && !is_system_config)
            {
                LaunchInfo LI = new LaunchInfo(name, command, port, LaunchInfo.RunningStatus.RETERVING_RUNNING_STATUS,
                    req_check_cmd, count, status_check_method, run_background, Container);
                OnReadConfigItem?.Invoke(LI);
            }
        }
    }
}

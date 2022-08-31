using System;
using System.IO;
using System.Text;
using IniParser;

namespace XShell2MobaXterm
{
    internal class Program
    {
        private static int _idx = 0;
        public static int GetNextId() => _idx++;

        static void Main(string[] args)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (args.Length < 0)
            {
                Console.WriteLine("缺少xshell session目录参数");
                return;
            }
            var xshellSessionDirectory = args[0];
            if (!Directory.Exists(xshellSessionDirectory))
            {
                Console.WriteLine("目录不存在:" + xshellSessionDirectory);
            }
            else
            {
                var sb = new StringBuilder();
                Generate(xshellSessionDirectory, sb, xshellSessionDirectory);

                File.WriteAllText("MobaXterm Sessions.mxtsessions", sb.ToString(), Encoding.GetEncoding("GB2312"));

                Console.WriteLine("转换完成");
            }
        }

        static void HandleDirectory(string rootDirectory, StringBuilder sb, string directory, int id)
        {
            if (id <= 0)
            {
                sb.AppendLine("[Bookmarks]");
                sb.AppendLine("SubRep=");
                sb.AppendLine("ImgNum=42");
                sb.AppendLine("WSL-Ubuntu=#105#14%1%");
                sb.AppendLine("WSL-Ubuntu-20.04=#105#14%Ubuntu-20.04%");
            }
            else
            {
                var directoryName = directory.Replace(rootDirectory, string.Empty).Remove(0, 1);
                sb.AppendLine($"[Bookmarks_{id}]");
                sb.AppendLine($"SubRep={directoryName}");
                sb.AppendLine("ImgNum=42");
            }

            var sessionFiles = Directory.GetFiles(directory, "*.xsh");
            foreach (var sessionFile in sessionFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(sessionFile);
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(sessionFile);
                var protocol = data["CONNECTION"]["Protocol"];
                var host = data["CONNECTION"]["Host"];
                var port = data["CONNECTION"]["Port"];
                var userName = data["CONNECTION:AUTHENTICATION"]["UserName"];
                var userKey = data["CONNECTION:AUTHENTICATION"]["UserKey"];
                var fontSize = data["TERMINAL:WINDOW"]["FontSize"];
                var fontFace = data["TERMINAL:WINDOW"]["FontFace"];
                var baudRate = data["CONNECTION:SERIAL"]["BaudRate"];

                if (protocol.Equals("SSH", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(userKey))
                        userKey = $@"_ProfileDir_\.ssh\{userKey}";

                    sb.AppendLine(
                        $"{fileName}=#109#0%{host}%{port}%{userName}%%-1%-1%%%%%0%0%0%{userKey}%%-1%0%0%0%%1080%%0%0%1#{fontFace}%{fontSize}%0%0%-1%15%236,236,236%30,30,30%180,180,192%0%-1%0%%xterm%-1%-1%_Std_Colors_0_%80%24%0%1%-1%<none>%%0%1%-1#0# #-1");
                }
                else if (protocol.Equals("TELNET", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(
                        $"{fileName}=#98#1%{host}%{port}%%%2%%%%%0%0%%1080%#{fontFace}%{fontSize}%0%0%-1%15%236,236,236%30,30,30%180,180,192%0%-1%0%%xterm%-1%-1%_Std_Colors_0_%80%24%0%1%-1%<none>%%0%1%-1#0# #-1");
                }
                else if (protocol.Equals("SERIAL", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(
                        $"{fileName}= #131#8%-2%100{baudRate}%3%0%0%1%2%{port}#{fontFace}%{fontSize}%0%0%-1%15%236,236,236%30,30,30%180,180,192%0%-1%0%%xterm%-1%-1%_Std_Colors_0_%80%24%0%1%-1%<none>%%0%1%-1#0# #-1");
                }
                else
                {
                    throw new NotImplementedException("不支持转换此协议:" + protocol);
                }
            }

            sb.AppendLine();
        }

        static void Generate(string rootDirectory, StringBuilder sb, string directory)
        {
            var items = Directory.GetFileSystemEntries(directory);
            HandleDirectory(rootDirectory, sb, directory, GetNextId());

            foreach (var item in items)
            {
                var attr = File.GetAttributes(item);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    Generate(rootDirectory, sb, item);
                }
            }
        }
    }
}
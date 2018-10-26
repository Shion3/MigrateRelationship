using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateRelationship
{
    class LoggerHandler : IDisposable
    {
        public string loggingPrefix = "MigrationRepairTool";
        public string loggingPostfix = "";
        public static StreamWriter writer = null;
        public static string logFileName = "";
        public static string logNameDatetimePostfix = "";
        public LoggerHandler(Type type)
        {
            loggingPrefix = type.Name;
            InitLogSetting();
        }
        public void InitLogSetting()
        {
            if (string.IsNullOrEmpty(logNameDatetimePostfix))
            {
                logNameDatetimePostfix = DateTime.Now.ToString("yyyyMMddHHmmss");
                logFileName = "MigrationRepairTool" + logNameDatetimePostfix + ".log";
                writer = File.AppendText(@"MigrationRepairTool" + logNameDatetimePostfix + ".log");
            }
        }
        public void WriteLog(LogLevel level, string formatStr, params object[] args)
        {
            if (writer == null)
            {
                InitLogSetting();
            }
            try
            {
                string finalMsg = GetFinalMessage(formatStr, args);
                writer.WriteLine("{0} {1} {2}", level.ToString().ToUpper(), DateTime.Now.ToString("MM-dd HH:mm:ss:fffff"), finalMsg);
                writer.Flush();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void Debug(string formatStr, params object[] args)
        {
            WriteLog(LogLevel.Debug, formatStr, args);
        }

        public void Warn(string formatStr, params object[] args)
        {
            WriteLog(LogLevel.Warn, formatStr, args);
        }

        public void Info(string formatStr, params object[] args)
        {
            WriteLog(LogLevel.Info, formatStr, args);
        }

        public void Error(string formatStr, params object[] args)
        {
            WriteLog(LogLevel.Error, formatStr, args);
        }
        private string GetFinalMessage(string formatStr, params object[] args)
        {
            string finalMsg = formatStr;
            try
            {
                if (args.Length == 0)
                {
                    finalMsg = formatStr; //兼容原来的 (string msg) 函数
                }
                else if (args.Length >= 1 && formatStr.IndexOf("{0}", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    //finalMsg = string.Format("{0}\t{1}", formatStr, args[0]);//兼容原来的 (string msg，Exception e) 函数

                    var builder = new StringBuilder();
                    builder.Append(formatStr);

                    foreach (var item in args)
                    {
                        builder.Append("; ");
                        builder.Append(item);
                    }

                    finalMsg = builder.ToString();
                }
                else
                {
                    finalMsg = string.Format(formatStr, args);//兼容原来的 (string formatStr, params object[] args) 函数
                }

                if (!string.IsNullOrEmpty(loggingPrefix))
                {
                    finalMsg = loggingPrefix + "    " + finalMsg;
                }
                if (!string.IsNullOrEmpty(loggingPostfix))
                {
                    finalMsg = finalMsg + "    " + loggingPostfix;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
            Trace.WriteLine(finalMsg);
            return finalMsg;
        }
        public void Dispose()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
            }
        }
        public enum LogLevel
        {
            Info,
            Debug,
            Warn,
            Error
        }
    }
}

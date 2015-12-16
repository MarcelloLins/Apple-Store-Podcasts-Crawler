using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Log
{
    public class LogSetup
    {
        public static void InitializeLog (string logFileName = null, string logLevel = null)
        {
            // default parameters initialization from config file
            if (String.IsNullOrEmpty (logFileName))
            {
                logFileName = System.Configuration.ConfigurationManager.AppSettings["logFilename"] ?? ("${basedir}/log/default_log_name.log");
            }
            if (String.IsNullOrEmpty (logLevel))
            {
                logLevel = System.Configuration.ConfigurationManager.AppSettings["logLevel"] = "Info";
            }   

            // Trying to Parse log Level
            LogLevel currentLogLevel;
            try 
            { 
                currentLogLevel   = LogLevel.FromString (logLevel); 
            }
            catch 
            { 
                currentLogLevel = LogLevel.Info; 
            }

            // Preparing Log Configuration
            var config = new NLog.Config.LoggingConfiguration ();

            // Console Output Config
            if (!Console.IsOutputRedirected)
            {
                var consoleTarget    = new NLog.Targets.ColoredConsoleTarget ();
                consoleTarget.Layout = "${longdate}\t${callsite}\t${level}\t${message}\t${onexception: \\:[Exception] ${exception:format=tostring}}";

                config.AddTarget ("console", consoleTarget);

                var rule1 = new NLog.Config.LoggingRule ("*", LogLevel.Trace, consoleTarget);
                config.LoggingRules.Add (rule1);
            }

            // File Output
            var fileTarget                    = new NLog.Targets.FileTarget ();
            fileTarget.FileName               = "${basedir}/log/" + logFileName;
            fileTarget.Layout                 = "${longdate}\t${callsite}\t${level}\t\"${message}${onexception: \t [Exception] ${exception:format=tostring}}\"";
            fileTarget.ConcurrentWrites       = true;
            fileTarget.AutoFlush              = true;
            fileTarget.KeepFileOpen           = true;
            fileTarget.DeleteOldFileOnStartup = false;
            fileTarget.ArchiveAboveSize       = 2 * 1024 * 1024;  // 2 Mb
            fileTarget.MaxArchiveFiles        = 10;
            fileTarget.ArchiveNumbering       = NLog.Targets.ArchiveNumberingMode.Date;
            fileTarget.ArchiveDateFormat      = "yyyyMMdd_HHmmss";

            // Setting output file writing to Async Mode
            var wrapper = new NLog.Targets.Wrappers.AsyncTargetWrapper (fileTarget);

            // Adding "File" as one of the log targets
            config.AddTarget ("file", wrapper);

            // Configuring Log from Config File          
            fileTarget.FileName = logFileName;
            var rule2 = new NLog.Config.LoggingRule ("*", currentLogLevel, fileTarget);
            config.LoggingRules.Add (rule2);

            // Saving Configurations
            LogManager.Configuration = config;
        }
    }
}

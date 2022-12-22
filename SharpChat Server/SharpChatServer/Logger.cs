using System.Configuration;

namespace ChatThreadTest
{
    internal enum LogLevel
    {
        DEBUG = 0,
        INFO = 1,
        WARNING = 2,
        ERROR = 3,
        FATAL = 4,
    }

    /// <summary>
    /// Standard class to log messages in terminal and into a file
    /// </summary>
    internal class Logger
    {
        private string datenow;
        private string logPath;
        private StreamWriter sw;
        private bool log;
        private LogLevel FileLogLevel;
        private LogLevel ConsoleLogLevel;

        public Logger()
        {
            // Get Log Level Values from App.Config
            FileLogLevel = LogLevel.INFO;
            ConsoleLogLevel = LogLevel.INFO;
            try
            {
                FileLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), (ConfigurationManager.AppSettings.Get("LogFileLogLevel") + "").ToUpper());
                ConsoleLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), (ConfigurationManager.AppSettings.Get("ConsoleLogLevel")+"").ToUpper());
            }
            catch (Exception e)
            {
                WriteError("Invalid value ConsoleLogLevel or LogFileLogLevel in App.config. Setting logging level to INFO", e);
            }

            WriteDebug("Initializing Logger");

            log = false;
            // Gets a value from App.Config to see if logging to file is Enabled
            try
            {
                log = Convert.ToBoolean(value: ConfigurationManager.AppSettings.Get("LoggingEnabled"));
            }
            catch (Exception e)
            {
                WriteError("Invalid value LoggingEnabled in App.config. Logging is Disabled until error is fixed", e);
            }


            // Only Proceed with Log Related Operation if logging is enabled
            if (log)
            {
                // Create a Filename for the log file using the current time 
                datenow = DateTime.Now + "";
                datenow = datenow.Replace(" ", "_").Replace(":", "_").Replace(".", "_");
                logPath = datenow + ".log";

                // Get The current working directory
                string currentDir = Directory.GetCurrentDirectory();
                string logDir = currentDir + Path.DirectorySeparatorChar + ConfigurationManager.AppSettings.Get("LoggingDirectory") + Path.DirectorySeparatorChar;
                WriteDebug("Logging directory set to " + logDir);
                if (!Directory.Exists(logDir))
                {
                    WriteDebug("Log Directory didn't exist, creating");
                    Directory.CreateDirectory(logDir);
                }

                // Try to initiate a StreamWriter
                try
                {
                    sw = new StreamWriter(logDir + @"\" + logPath);
                    sw.AutoFlush = true;
                }
                catch (Exception e)
                {
                    WriteError("There was an error creating the log file", e);
                    log = false;
                }
            }

            WriteDebug("Logger Initialized");
        }

        // --- DEBUG ---
        /// <summary>
        /// Logs the defined message at the DEBUG Log Level
        /// </summary>
        /// <param name="info_message">Message to log</param>
        public void WriteDebug(string info_message)
        {
            WriteLine(info_message, LogLevel.DEBUG);
        }

        // --- INFO ---
        /// <summary>
        /// Logs the defined message at the INFO Log Level
        /// </summary>
        /// <param name="info_message">Message to log</param>
        public void WriteInfo(string info_message)
        {
            WriteLine(info_message, LogLevel.INFO);
        }

        /// <summary>
        /// Logs the defined message at the INFO Log Level but with custom color
        /// </summary>
        /// <param name="info_message">Message to log</param>
        /// <param name="consoleColor">Custom Color</param>
        public void WriteInfo(string info_message, ConsoleColor consoleColor)
        {
            WriteLine(info_message, LogLevel.INFO, consoleColor);
        }

        // --- WARN ---
        /// <summary>
        /// Logs the defined message at the WARN Log Level
        /// </summary>
        /// <param name="warn_message">Message to log</param>
        public void WriteWarn(string warn_message)
        {
            WriteLine(warn_message, LogLevel.WARNING);
        }

        // --- ERROR ---
        /// <summary>
        /// Logs the defined message at the ERROR Log Level
        /// </summary>
        /// <param name="error_message">Message to log</param>
        public void WriteError(string error_message)
        {
            WriteLine(error_message, LogLevel.ERROR);
        }

        /// <summary>
        /// Logs an Message of given Exception at the ERROR Log Level
        /// </summary>
        /// <param name="e">Exception</param>
        public void WriteError(Exception e)
        {
            WriteError(e.Message);
        }

        /// <summary>
        /// Logs an Message of given Exception at the ERROR Log Level. Allows to include an accompanying message.
        /// </summary>
        /// <param name="e">Exception</param>
        /// <param name="error_message">Message to log alongside the exception</param>
        public void WriteError(string error_message, Exception e)
        {
            WriteError(error_message + Environment.NewLine + e.Message);
        }

        // --- FATAL ---
        /// <summary>
        /// Logs the defined message at the FATAL Log Level
        /// </summary>
        /// <param name="error_message">Message to log</param>
        public void WriteFatal(string error_message)
        {
            WriteLine(error_message, LogLevel.FATAL);
        }

        /// <summary>
        /// Logs an Message of given Exception at the FATAL Log Level
        /// </summary>
        /// <param name="e">Exception</param>
        public void WriteFatal(Exception e)
        {
            WriteFatal(e.Message);
        }

        /// <summary>
        /// Logs an Message of given Exception at the FATAL Log Level. Allows to include an accompanying message.
        /// </summary>
        /// <param name="e">Exception</param>
        /// <param name="error_message">Message to log alongside the exception</param>
        public void WriteFatal(string error_message, Exception e)
        {
            WriteFatal(error_message + Environment.NewLine + e.Message);
        }

        // Universal - used by all the others
        /// <summary>
        /// Logs the given message at the given Log Level. If enabled also logs to a file
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="level">Level</param>
        public void WriteLine(string message, LogLevel level)
        {
            string logmsg = string.Empty;

            // Log Into Terminal
            switch (level)
            {
                case LogLevel.DEBUG:
                    logmsg += "[DEBUG] ";
                    Console.ForegroundColor= ConsoleColor.Gray;
                    break;

                case LogLevel.INFO:
                    Console.ForegroundColor = ConsoleColor.White;
                    logmsg += "[INFO] ";
                    break;

                case LogLevel.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    logmsg += "[WARN] ";
                    break;

                case LogLevel.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    logmsg += "[ERROR] ";
                    break;

                case LogLevel.FATAL:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    logmsg += "[FATAL] ";
                    break;
            }
            logmsg += message;
            if(level >= ConsoleLogLevel)
            {
                Console.WriteLine(logmsg);
            }
            Console.ForegroundColor = ConsoleColor.White;

            // Log Into Log File
            if (log&& sw != null)
            {
                if(level >= FileLogLevel)
                {
                    sw.WriteLine(logmsg);
                }
            }
        }

        /// <summary>
        /// Logs the given message at the given Log Level and uses the defined color instead of the default one. If enabled also logs to a file.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="level">Level</param>
        /// <param name="consoleColor">Color</param>
        public void WriteLine(string message, LogLevel level, ConsoleColor consoleColor)
        {
            string logmsg = string.Empty;

            // Log Into Terminal
            Console.ForegroundColor = consoleColor;
            switch (level)
            {
                case LogLevel.DEBUG:
                    logmsg += "[DEBUG] ";
                    break;

                case LogLevel.INFO:
                    logmsg += "[INFO] ";
                    break;

                case LogLevel.WARNING:
                    logmsg += "[WARN] ";
                    break;

                case LogLevel.ERROR:
                    logmsg += "[ERROR] ";
                    break;

                case LogLevel.FATAL:
                    logmsg += "[FATAL] ";
                    break;
            }

            logmsg += message;
            if (level >= ConsoleLogLevel)
            {
                Console.WriteLine(logmsg);
            }
            Console.ForegroundColor = ConsoleColor.White;

            // Log Into Log File
            if (log && sw != null)
            {
                if (level >= FileLogLevel)
                {
                    sw.WriteLine(logmsg);
                }
            }
        }
    }
}

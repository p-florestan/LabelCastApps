using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelCast
{
    /// <summary>
    /// Event Logger
    /// </summary>
    public static class Logger
    {
        #region Fields 

        private static String mBaseDir = @"c:\Program Info\LabelCast\Logs";

        // default log file 
        private static String mLogFile = @"c:\Program Info\LabelCast\Logs\WebappLog.txt";

        private static object lockObj = new object();

        #endregion

        #region Properties

        // Set overall log level. This controls what gets logged
        public static Level CurrentLogLevel { get; set; } = Level.Notice;


        // Path of log file to send messages to
        public static String CurrentLogFile
        {
            get { return mLogFile; }
            set { mLogFile = value; }
        }

        #endregion

        #region API

        /// <summary>
        /// Write a message to the log<br/>
        /// If the "level" parameter is higher than the currently set log level, nothing gets written.
        /// Higher log levels mean more granular messages. (The highest level prints DEBUG messages
        /// which we generally would not want to log all the time.)
        /// </summary>
        public static void Write(Level level, String txt, bool addCRLF = false)
        {
            lock (lockObj)
            {
                if (level > CurrentLogLevel)
                    return;

                try
                {
                    if (!Directory.Exists(mBaseDir))
                        Directory.CreateDirectory(mBaseDir);

                    using (FileStream fs = new FileStream(mLogFile, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            try
                            {
                                if (String.IsNullOrEmpty(txt))
                                    sw.Write("\r\n");
                                else
                                {
                                    if (addCRLF)
                                        sw.Write("\r\n");

                                    sw.Write(DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " " + level.ToString() + ": " + txt + "\r\n");

                                    if (addCRLF)
                                        sw.Write("\r\n");
                                }
                                //
                                sw.Flush();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }


        /// <summary>
        /// Writes the specified message to the log (as Error) and throws an ApplicationException
        /// </summary>
        /// <param name="msg"></param>
        public static void LogAndThrowAppException(String msg)
        {
            Write(Level.Error, msg);
            throw new ApplicationException(msg);
        }


        /// <summary>
        /// Writes the specified message to the log (as Error) and throws an IOException
        /// </summary>
        /// <param name="msg"></param>
        public static void LogAndThrowIOException(String msg)
        {
            Write(Level.Error, msg);
            throw new IOException(msg);
        }

        /// <summary>
        /// Writes the specified message to the log (as Error) and throws an ArgumentException.
        /// </summary>
        /// <param name="msg"></param>
        public static void LogAndThrowArgEx(String msg)
        {
            Logger.Write(Level.Error, msg);
            throw new ArgumentException(msg);
        }

        #endregion
    }




    /// <summary>
    /// Log Severity Levels
    /// </summary>
    public enum Level
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Notice = 3,
        Debug = 4
    }

}



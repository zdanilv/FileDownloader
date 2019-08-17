using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace File_Downloader
{
    class Log
    {
        public static string logPath = "log.txt", enableLog = "1";

        public static void Logs()
        {
            if (enableLog == "1")
            {
                try
                {
                    File.AppendAllText(logPath, Environment.NewLine + DateTime.Now.ToString("yyyy:MM:dd"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка в создании Лог-файлов:" + ex);
                }
            }
        }

        public static void writeLog(string log)
        {
            if (enableLog == "1")
            {
                try
                {
                    log = DateTime.Now.ToString("HH:mm:ss ") + log;
                    File.AppendAllText(logPath, Environment.NewLine + log);
                    log = "";
                }
                catch(Exception ex)
                {

                }
            }
        }
    }
}

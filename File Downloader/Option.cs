using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace File_Downloader
{
    class Option
    {
        public static string cfgPort, cfgSerializeSize, cfgFilePath, cfgLog, cfgMapMemory;
        FileInfo cfgFile;

        public void StartClass()
        {
            cfg();
            AcceptOptions();
            Log.Logs();
        }

        void AcceptOptions()
        {
            try
            {
                MainWindow.portServer = Int32.Parse(cfgPort);
                MainWindow.bufSerializeSize = Int32.Parse(cfgSerializeSize);
                MainWindow.Path = cfgFilePath;
                MainWindow.portMessage = Int32.Parse(cfgPort) + 1;
                Log.enableLog = cfgLog;

                if (cfgMapMemory == "0")
                {
                    MainWindow.MapMemory = false;
                    MainWindow.Instance.ToggleSwitchMemory.IsChecked = false;
                }
                else if (cfgMapMemory == "1")
                {
                    MainWindow.MapMemory = true;
                    MainWindow.Instance.ToggleSwitchMemory.IsChecked = true;
                }

                if (cfgLog == "0")
                    MainWindow.Instance.ToggleSwitchLog.IsChecked = false;
                else if (cfgLog == "1")
                    MainWindow.Instance.ToggleSwitchLog.IsChecked = true;

                MainWindow.Instance.TextboxPortInput.Text = cfgPort;
                MainWindow.Instance.TextboxPortMessageInput.Text = cfgLog + 1;
                MainWindow.Instance.BufferSerializeNumirc.Value = Convert.ToInt32(cfgSerializeSize);
            }
            catch(Exception ex)
            {
                Log.writeLog(ex.Message);
            }

        }

        void cfg()
        {
            try
            {
                Directory.CreateDirectory("Download");
                cfgFile = new FileInfo("cfg.ini");
                if (!cfgFile.Exists)
                {
                    File.WriteAllText("cfg.ini", "Port 8888");
                    File.AppendAllText("cfg.ini", Environment.NewLine + "SerializeSize 4096");
                    File.AppendAllText("cfg.ini", Environment.NewLine + "FilePath " + "Download");
                    File.AppendAllText("cfg.ini", Environment.NewLine + "EnabledLog 1");
                    File.AppendAllText("cfg.ini", Environment.NewLine + "EnabledMapMemory 0");
                }

                cfgPort = File.ReadLines("cfg.ini").Skip(0).First();
                cfgPort = cfgPort.Substring(cfgPort.IndexOf("Port", 0));
                cfgPort = cfgPort.Replace(" ", "");
                cfgPort = cfgPort.Replace("Port", "");

                cfgSerializeSize = File.ReadLines("cfg.ini").Skip(1).First();
                cfgSerializeSize = cfgSerializeSize.Substring(cfgSerializeSize.IndexOf("SerializeSize", 0));
                cfgSerializeSize = cfgSerializeSize.Replace(" ", "");
                cfgSerializeSize = cfgSerializeSize.Replace("SerializeSize", "");

                cfgFilePath = File.ReadLines("cfg.ini").Skip(2).First();
                cfgFilePath = cfgFilePath.Substring(cfgFilePath.IndexOf("FilePath", 0));
                cfgFilePath = cfgFilePath.Replace(" ", "");
                cfgFilePath = cfgFilePath.Replace("FilePath", "");

                cfgLog = File.ReadLines("cfg.ini").Skip(3).First();
                if (cfgLog.IndexOf("1") != -1)
                    cfgLog = "1";
                else if (cfgLog.IndexOf("0") != -1)
                    cfgLog = "0";

                cfgMapMemory = File.ReadLines("cfg.ini").Skip(4).First();
                if (cfgMapMemory.IndexOf("1") != -1)
                    cfgMapMemory = "1";
                else if (cfgMapMemory.IndexOf("0") != -1)
                    cfgMapMemory = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка в настройках. \nНастройки будут сброшены. " + ex);
                Log.writeLog(ex.Message);
                cfgFile.Delete();
                cfg();
            }
        }

        public static void SaveCfg(string cPort, string cSerialize, string cFilePath, string cLog,
            string cMapMemory)
        {
            try
            {
                File.WriteAllText("cfg.ini", "Port " + cPort);
                File.AppendAllText("cfg.ini", Environment.NewLine + "SerializeSize " + cSerialize);
                File.AppendAllText("cfg.ini", Environment.NewLine + "FilePath " + cFilePath);
                File.AppendAllText("cfg.ini", Environment.NewLine + "EnabledLog " + cLog);
                File.AppendAllText("cfg.ini", Environment.NewLine + "EnabledMapMemory " + cMapMemory);

                MainWindow.portServer = Int32.Parse(cPort);
                MainWindow.bufSerializeSize = Int32.Parse(cSerialize);
                MainWindow.Path = cFilePath;
                MainWindow.portMessage = Int32.Parse(cPort) + 1;
                Log.enableLog = cLog;

                if (cMapMemory == "0")
                    MainWindow.MapMemory = false;
                else if (cMapMemory == "1")
                    MainWindow.MapMemory = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка в сохранении настроек: " + ex);
                Log.writeLog(ex.Message);
            }
        }
    }
}

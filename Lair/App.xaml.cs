﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Ionic.Zip;
using Library;
using Library.Io;
using Library.Net.Lair;

namespace Lair
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static Version LairVersion { get; private set; }
        public static Dictionary<string, string> DirectoryPaths { get; private set; }
        public static LairColors LairColors { get; private set; }

        private List<Process> _processList = new List<Process>();

        public App()
        {
            App.LairVersion = new Version(2, 0, 5);

            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            App.DirectoryPaths = new Dictionary<string, string>();

            App.DirectoryPaths["Base"] = @"..\";
            App.DirectoryPaths["Configuration"] = Path.Combine(@"..\", "Configuration");
            App.DirectoryPaths["Update"] = Path.GetFullPath(Path.Combine(@"..\", "Update"));
            App.DirectoryPaths["Log"] = Path.Combine(@"..\", "Log");
            App.DirectoryPaths["Input"] = Path.Combine(@"..\", "Input");
            App.DirectoryPaths["Work"] = Path.Combine(@"..\", "Work");

            App.DirectoryPaths["Core"] = @".\";
            App.DirectoryPaths["Icons"] = "Icons";
            App.DirectoryPaths["Languages"] = "Languages";

            foreach (var item in App.DirectoryPaths.Values)
            {
                try
                {
                    if (!Directory.Exists(item))
                    {
                        Directory.CreateDirectory(item);
                    }
                }
                catch (Exception)
                {

                }
            }

            Thread.GetDomain().UnhandledException += App_UnhandledException;
        }

        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            for (int index = 1; ; index++)
            {
                string text = string.Format(@"{0}\{1} ({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (!File.Exists(text))
                {
                    return text;
                }
            }
        }

        private static FileStream GetUniqueFileStream(string path)
        {
            if (!File.Exists(path))
            {
                try
                {
                    FileStream fs = new FileStream(path, FileMode.CreateNew);
                    return fs;
                }
                catch (DirectoryNotFoundException)
                {
                    throw;
                }
                catch (IOException)
                {
                    throw;
                }
            }

            for (int index = 1, count = 0; ; index++)
            {
                string text = string.Format(
                    @"{0}\{1} ({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (!File.Exists(text))
                {
                    try
                    {
                        FileStream fs = new FileStream(text, FileMode.CreateNew);
                        return fs;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        throw;
                    }
                    catch (IOException)
                    {
                        count++;
                        if (count > 1024)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null) return;

            Log.Error(exception);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 2 && e.Args[0] == "Relate")
            {
                if (e.Args[1] == "on")
                {
                    try
                    {
                        string extension = ".box";
                        string commandline = "\"" + Path.GetFullPath(Path.Combine(App.DirectoryPaths["Core"], "Lair.exe")) + "\" \"%1\"";
                        string fileType = "Lair";
                        string description = "Lair Box";
                        string verb = "open";
                        string iconPath = Path.GetFullPath(Path.Combine(App.DirectoryPaths["Icons"], "Box.ico"));

                        using (var regkey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension))
                        {
                            regkey.SetValue("", fileType);
                        }

                        using (var shellkey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(fileType))
                        {
                            shellkey.SetValue("", description);

                            using (var shellkey2 = shellkey.CreateSubKey("shell\\" + verb))
                            {
                                using (var shellkey3 = shellkey2.CreateSubKey("command"))
                                {
                                    shellkey3.SetValue("", commandline);
                                    shellkey3.Close();
                                }
                            }
                        }

                        using (var iconkey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(fileType + "\\DefaultIcon"))
                        {
                            iconkey.SetValue("", "\"" + iconPath + "\"");
                        }
                    }
                    catch (Exception)
                    {

                    }

                    this.Shutdown();

                    return;
                }
                else if (e.Args[1] == "off")
                {
                    try
                    {
                        string extension = ".box";
                        string fileType = "Lair";

                        Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension);
                        Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(fileType);
                    }
                    catch (Exception)
                    {

                    }

                    this.Shutdown();

                    return;
                }
            }
            if (e.Args.Length >= 2 && e.Args[0] == "Download")
            {
                try
                {
                    if (!Directory.Exists(App.DirectoryPaths["Input"]))
                        Directory.CreateDirectory(App.DirectoryPaths["Input"]);

                    using (FileStream stream = App.GetUniqueFileStream(Path.Combine(App.DirectoryPaths["Input"], "seed.txt")))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        foreach (var item in e.Args.Skip(1))
                        {
                            if (string.IsNullOrWhiteSpace(item)) continue;
                            writer.WriteLine(item);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
            else if (e.Args.Length == 1 && e.Args[0].EndsWith(".box") && File.Exists(e.Args[0]))
            {
                try
                {
                    if (Path.GetExtension(e.Args[0]).ToLower() == ".box")
                    {
                        if (!Directory.Exists(App.DirectoryPaths["Input"]))
                            Directory.CreateDirectory(App.DirectoryPaths["Input"]);

                        File.Copy(e.Args[0], App.GetUniqueFilePath(Path.Combine(App.DirectoryPaths["Input"], Path.GetRandomFileName() + "_temp.box")));
                    }
                }
                catch (Exception)
                {

                }
            }

            // 多重起動防止
            {
                Process currentProcess = Process.GetCurrentProcess();

                // 同一パスのプロセスが存在する場合、終了する。
                foreach (Process p in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (p.Id == currentProcess.Id) continue;

                    try
                    {
                        if (p.MainModule.FileName == Path.GetFullPath(Assembly.GetEntryAssembly().Location))
                        {
                            this.Shutdown();

                            return;
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                string updateInformationFilePath = Path.Combine(App.DirectoryPaths["Configuration"], "Lair.update");

                // アップデート中の場合、終了する。
                if (File.Exists(updateInformationFilePath))
                {
                    using (FileStream stream = new FileStream(updateInformationFilePath, FileMode.Open))
                    using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
                    {
                        var updateExeFilePath = reader.ReadLine();

                        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(updateExeFilePath)))
                        {
                            try
                            {
                                if (Path.GetFileName(p.MainModule.FileName) == updateExeFilePath)
                                {
                                    this.Shutdown();

                                    return;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    File.Delete(updateInformationFilePath);
                }
            }

            try
            {
                // アップデート
                if (Directory.Exists(App.DirectoryPaths["Update"]))
                {
                Restart: ;

                    string zipFilePath = null;

                    {
                        Regex regex = new Regex(@"Lair ((\d*)\.(\d*)\.(\d*)).*\.zip");
                        Version version = App.LairVersion;

                        foreach (var path in Directory.GetFiles(App.DirectoryPaths["Update"]))
                        {
                            string name = Path.GetFileName(path);

                            if (name.StartsWith("Lair"))
                            {
                                var match = regex.Match(name);

                                if (match.Success)
                                {
                                    var tempVersion = new Version(match.Groups[1].Value);

                                    if (version < tempVersion)
                                    {
                                        version = tempVersion;
                                        zipFilePath = path;
                                    }
                                    else
                                    {
                                        if (File.Exists(path))
                                            File.Delete(path);
                                    }
                                }
                            }
                        }
                    }

                    if (zipFilePath != null)
                    {
                        string workDirectioryPath = App.DirectoryPaths["Work"];
                        var tempCoreDirectoryPath = Path.Combine(workDirectioryPath, "Core");

                        if (Directory.Exists(tempCoreDirectoryPath))
                            Directory.Delete(tempCoreDirectoryPath, true);

                        try
                        {
                            using (ZipFile zipfile = new ZipFile(zipFilePath))
                            {
                                zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                                zipfile.UseUnicodeAsNecessary = true;
                                zipfile.ExtractAll(tempCoreDirectoryPath);
                            }
                        }
                        catch (Exception)
                        {
                            if (File.Exists(zipFilePath))
                                File.Delete(zipFilePath);

                            goto Restart;
                        }

                        var tempUpdateExeFilePath = Path.Combine(workDirectioryPath, "Library.Update.exe");

                        if (File.Exists(tempUpdateExeFilePath))
                            File.Delete(tempUpdateExeFilePath);

                        File.Copy("Library.Update.exe", tempUpdateExeFilePath);

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = tempUpdateExeFilePath;
                        startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                            Process.GetCurrentProcess().Id,
                            Path.Combine(tempCoreDirectoryPath, "Core"),
                            Directory.GetCurrentDirectory(),
                            Path.Combine(Directory.GetCurrentDirectory(), "Lair.exe"),
                            Path.GetFullPath(zipFilePath));
                        startInfo.WorkingDirectory = Path.GetDirectoryName(tempUpdateExeFilePath);

                        var process = Process.Start(startInfo);
                        process.WaitForInputIdle();

                        string updateInformationFilePath = Path.Combine(App.DirectoryPaths["Configuration"], "Lair.update");

                        using (FileStream stream = new FileStream(updateInformationFilePath, FileMode.Create))
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.WriteLine(Path.GetFileName(tempUpdateExeFilePath));
                        }

                        this.Shutdown();

                        return;
                    }
                }
            }
            finally
            {
                this.CheckProcess();
            }

            this.RunProcess();

            // Colors
            {
                if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings")))
                {
                    using (StreamWriter writer = new StreamWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings"), false, new UTF8Encoding(false)))
                    {
                        writer.WriteLine(string.Format("Tree_Hit {0}", System.Windows.Media.Colors.LightPink));
                    }
                }

                App.LairColors = new LairColors();

                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Colors.settings"), new UTF8Encoding(false)))
                {
                    var items = reader.ReadLine().Split(' ');
                    var name = items[0];
                    var value = items[1];

                    if (name == "Tree_Hit") App.LairColors.Tree_Hit = (Color)ColorConverter.ConvertFromString(value);
                }
            }

            this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
        }

        private void CheckProcess()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"))) return;

            var runList = new List<RunItem>();

            using (StreamReader r = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"), new UTF8Encoding(false)))
            using (XmlTextReader xml = new XmlTextReader(r))
            {
                while (xml.Read())
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        if (xml.LocalName == "Process")
                        {
                            string path = null;
                            string arguments = null;
                            string workingDirectory = null;

                            using (var xmlReader = xml.ReadSubtree())
                            {
                                while (xmlReader.Read())
                                {
                                    if (xmlReader.NodeType == XmlNodeType.Element)
                                    {
                                        if (xmlReader.LocalName == "Path")
                                        {
                                            try
                                            {
                                                path = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xml.LocalName == "Arguments")
                                        {
                                            try
                                            {
                                                arguments = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xmlReader.LocalName == "WorkingDirectory")
                                        {
                                            try
                                            {
                                                workingDirectory = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                    }
                                }
                            }

                            runList.Add(new RunItem()
                            {
                                Path = path,
                                Arguments = arguments,
                                WorkingDirectory = workingDirectory
                            });
                        }
                    }
                }
            }

            Parallel.ForEach(runList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, item =>
            {
                foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(item.Path)))
                {
                    try
                    {
                        if (p.MainModule.FileName == Path.GetFullPath(item.Path))
                        {
                            try
                            {
                                p.Kill();
                                p.WaitForExit();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        private void RunProcess()
        {
            Version version = new Version();

            if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version")))
            {
                using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version"), new UTF8Encoding(false)))
                {
                    version = new Version(reader.ReadLine());
                }
            }

            if (version <= new Version(0, 1, 11))
            {
                try
                {
                    File.Delete(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"));
                }
                catch (Exception)
                {

                }
            }

            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml")))
            {
                using (XmlTextWriter xml = new XmlTextWriter(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"), new UTF8Encoding(false)))
                {
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartDocument();

                    xml.WriteStartElement("Configuration");

                    {
                        var path = Path.Combine(App.DirectoryPaths["Work"], "Tor");
                        Directory.CreateDirectory(path);

                        xml.WriteStartElement("Process");
                        xml.WriteElementString("Path", @"Tor\tor.exe");
                        xml.WriteElementString("Arguments", "-f torrc DataDirectory " + @"..\..\Work\Tor");
                        xml.WriteElementString("WorkingDirectory", "Tor");

                        xml.WriteEndElement(); //Process
                    }

                    {
                        xml.WriteStartElement("Process");
                        xml.WriteElementString("Path", @"Polipo\polipo.exe");
                        xml.WriteElementString("Arguments", "-c polipo.conf");
                        xml.WriteElementString("WorkingDirectory", "Polipo");

                        xml.WriteEndElement(); //Process
                    }

                    xml.WriteEndElement(); //Configuration

                    xml.WriteEndDocument();
                    xml.Flush();
                }
            }

            var runList = new List<RunItem>();

            using (StreamReader r = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"), new UTF8Encoding(false)))
            using (XmlTextReader xml = new XmlTextReader(r))
            {
                while (xml.Read())
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        if (xml.LocalName == "Process")
                        {
                            string path = null;
                            string arguments = null;
                            string workingDirectory = null;

                            using (var xmlReader = xml.ReadSubtree())
                            {
                                while (xmlReader.Read())
                                {
                                    if (xmlReader.NodeType == XmlNodeType.Element)
                                    {
                                        if (xmlReader.LocalName == "Path")
                                        {
                                            try
                                            {
                                                path = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xml.LocalName == "Arguments")
                                        {
                                            try
                                            {
                                                arguments = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else if (xmlReader.LocalName == "WorkingDirectory")
                                        {
                                            try
                                            {
                                                workingDirectory = xmlReader.ReadString();
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                    }
                                }
                            }

                            runList.Add(new RunItem()
                            {
                                Path = path,
                                Arguments = arguments,
                                WorkingDirectory = workingDirectory
                            });
                        }
                    }
                }
            }

            Parallel.ForEach(runList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, item =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = item.Path;
                    process.StartInfo.Arguments = item.Arguments;
                    process.StartInfo.WorkingDirectory = item.WorkingDirectory;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();

                    _processList.Add(process);
                }
                catch (Exception)
                {

                }
            });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Parallel.ForEach(_processList, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, p =>
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch (Exception)
                {

                }
            });
        }

        private class RunItem
        {
            public string Path { get; set; }
            public string Arguments { get; set; }
            public string WorkingDirectory { get; set; }
        }
    }

    public class LairColors
    {
        public LairColors()
        {
            this.Tree_Hit = System.Windows.Media.Colors.LightPink;
        }

        public System.Windows.Media.Color Tree_Hit { get; set; }
    }
}

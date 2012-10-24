using System;
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
using System.Windows.Threading;
using System.Xml;
using Ionic.Zip;
using Library;
using Library.Collections;
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
        public static string SelectTab { get; set; }
        private FileStream _lockStream = null;
        private LockedList<Process> _processList = new LockedList<Process>();

        public App()
        {
            //System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            App.LairVersion = new Version(0, 0, 29);

            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            App.DirectoryPaths = new Dictionary<string, string>();

            App.DirectoryPaths["Base"] = @"..\";
            App.DirectoryPaths["Configuration"] = Path.Combine(@"..\", "Configuration");
            App.DirectoryPaths["Update"] = Path.Combine(@"..\", "Update");
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

            Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(App_UnhandledException);
        }

        void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null)
                return;

            Log.Error(exception);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _lockStream = new FileStream(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.lock"), FileMode.Create);
            }
            catch (IOException)
            {
                this.Shutdown();

                return;
            }

            try
            {
                if (File.Exists("update"))
                {
                    using (FileStream stream = new FileStream("update", FileMode.Open))
                    using (StreamReader r = new StreamReader(stream))
                    {
                        var text = r.ReadLine();

                        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(text)))
                        {
                            try
                            {
                                if (Path.GetFileName(p.MainModule.FileName) == text)
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

                    File.Delete("update");
                }
            }
            catch (Exception)
            {
                this.Shutdown();

                return;
            }

            // Update
            try
            {
                if (Directory.Exists(App.DirectoryPaths["Update"]))
                {
                Restart: ;

                    Regex regex = new Regex(@"Lair ((\d*)\.(\d*)\.(\d*)).*\.zip");
                    Version version = App.LairVersion;
                    string updateZipPath = null;

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
                                    updateZipPath = path;
                                }
                                else
                                {
                                    if (File.Exists(path))
                                        File.Delete(path);
                                }
                            }
                        }
                    }

                    if (updateZipPath != null)
                    {
                        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + "-Update");

                        if (Directory.Exists(tempPath))
                            Directory.Delete(tempPath, true);

                        try
                        {
                            using (ZipFile zipfile = new ZipFile(updateZipPath))
                            {
                                zipfile.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                                zipfile.UseUnicodeAsNecessary = true;
                                zipfile.ExtractAll(tempPath);
                            }
                        }
                        catch (Exception)
                        {
                            if (File.Exists(updateZipPath))
                                File.Delete(updateZipPath);

                            goto Restart;
                        }

                        try
                        {
                            File.Move(updateZipPath, Path.Combine(App.DirectoryPaths["Base"], Path.GetFileName(updateZipPath)));
                        }
                        catch (Exception)
                        {

                        }

                        try
                        {
                            File.Delete(updateZipPath);
                        }
                        catch (Exception)
                        {

                        }

                        updateZipPath = Path.Combine(App.DirectoryPaths["Base"], Path.GetFileName(updateZipPath));

                        var tempUpdateExePath = Path.Combine(Path.GetTempPath(), "-" + Path.GetRandomFileName() + "-Library.Update.exe");

                        if (File.Exists(tempUpdateExePath))
                            File.Delete(tempUpdateExePath);

                        File.Copy("Library.Update.exe", tempUpdateExePath);

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = tempUpdateExePath;
                        startInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                            Process.GetCurrentProcess().Id,
                            Path.Combine(tempPath, "Core"),
                            Directory.GetCurrentDirectory(),
                            Path.Combine(Directory.GetCurrentDirectory(), "Lair.exe"),
                            Path.GetFullPath(updateZipPath));
                        startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);

                        var process = Process.Start(startInfo);

                        using (FileStream stream = new FileStream("update", FileMode.Create))
                        using (StreamWriter w = new StreamWriter(stream))
                        {
                            w.WriteLine(Path.GetFileName(tempUpdateExePath));
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

            // Setting
            {
                Version version = new Version();

                if (File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version")))
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(App.DirectoryPaths["Configuration"], "Lair.version"), new UTF8Encoding(false)))
                    {
                        version = new Version(reader.ReadLine());
                    }
                }

                if (version <= new Version(0, 0, 5))
                {
                    try
                    {
                        File.Delete(Path.Combine(App.DirectoryPaths["Configuration"], @"Library\Net\Lair\LairManager\ConnectionManager\Messages.gz"));
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            this.StartupUri = new Uri("Windows/MainWindow.xaml", UriKind.Relative);
        }

        private void CheckProcess()
        {
            if (!File.Exists(Path.Combine(App.DirectoryPaths["Configuration"], "Run.xml"))) return;

            var runList = new List<dynamic>();

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

                            runList.Add(new
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
                foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension((string)item.Path)))
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

            var runList = new List<dynamic>();

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

                            runList.Add(new
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
            if (_lockStream != null)
            {
                _lockStream.Close();
                _lockStream = null;
            }

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
    }
}

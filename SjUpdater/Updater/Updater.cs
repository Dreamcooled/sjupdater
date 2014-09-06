using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SjUpdater.Updater
{
    internal class Updater : INotifyPropertyChanged
    {
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);

        public event ErrorEventHandler errorEvent;

        private readonly string infofileurl;

        private int _numFiles;
        private int _currentFileNum;
        private int _totalBytes;
        private int _totalBytesDownloaded;
        private string _currentFilename;
        private int _currentFileBytes;
        private int _currentFileBytesDownloaded;
        private bool _updateAvailable;
        private bool _isChecking;
        private bool _isGettingChangelog;
        private UpdateFile[] _updatefiles;
        private string _changelog;

        public int NumFiles
        {
            get { return _numFiles; }
            private set
            {
                if (value != _numFiles)
                {
                    _numFiles = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int CurrentFileNum
        {
            get { return _currentFileNum; }
            private set
            {
                if (value != _currentFileNum)
                {
                    _currentFileNum = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int TotalBytes
        {
            get { return _totalBytes; }
            private set
            {
                if (value != TotalBytes)
                {
                    _totalBytes = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int TotalBytesDownloaded
        {
            get { return _totalBytesDownloaded; }
            private set
            {
                if (value != _totalBytesDownloaded)
                {
                    _totalBytesDownloaded = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string CurrentFilename
        {
            get { return _currentFilename; }
            private set
            {
                if (value != _currentFilename)
                {
                    _currentFilename = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int CurrentFileBytes
        {
            get { return _currentFileBytes; }
            private set
            {
                if (value != _currentFileBytes)
                {
                    _currentFileBytes = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int CurrentFileBytesDownloaded

        {
            get { return _currentFileBytesDownloaded; }
            private set
            {
                if (value != _currentFileBytesDownloaded)
                {
                    _currentFileBytesDownloaded = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool UpdateAvailable
        {
            get
            {
                return _updateAvailable;
            }
            private set
            {
                if (value != _updateAvailable)
                {
                    _updateAvailable = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsChecking
        {
            get
            {
                return _isChecking;
            }
            private set
            {
                if (value != _isChecking)
                {
                    _isChecking = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsGettingChangelog
        {
            get
            {
                return _isGettingChangelog;
            }
            private set
            {
                if (value != _isGettingChangelog)
                {
                    _isGettingChangelog = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsUpdating
        {
            get { return _isUpdating; }
            set
            {
                if (value != _isUpdating)
                    _isUpdating = value;
            }
        }

        public UpdateFile[] UpdateFiles
        {
            get { return _updatefiles; }
            private set
            {
                if (value != _updatefiles)
                {
                    _updatefiles = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Changelog
        {
            get { return _changelog; }
            set
            {
                if (value != _changelog)
                {
                    _changelog = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private readonly string _baseurl = "";
        private bool _isUpdating;

        public Updater(string infofileurl)
        {
            this.infofileurl = infofileurl;
            _baseurl = infofileurl.Substring(0, infofileurl.LastIndexOf('/') + 1);

            UpdateAvailable = false;
            IsChecking = false;
            IsGettingChangelog = false;
        }

        public async Task CheckForUpdates()
        {
            IsChecking = true;
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(infofileurl);
                request.AllowAutoRedirect = true;

                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());

                List<UpdateFile> files = new List<UpdateFile>();

                while (reader.Peek() >= 0)
                {
                    string line = (await reader.ReadLineAsync()).Trim();

                    if (line.ToLower() == "[###changelog###]")
                        break;

                    if (!line.Contains(":"))
                        continue;

                    if (line.StartsWith("#"))
                        continue;

                    string[] linesplit = (line).Split(new char[] {':'});
                    files.Add(new UpdateFile(linesplit[0].Trim(), linesplit[1].Trim().ToLower()));
                }

                List<UpdateFile> newfiles = new List<UpdateFile>();

                foreach (var file in files)
                {
                    bool addFile = false;

                    if (string.IsNullOrWhiteSpace(file.name))
                        continue;

                    if (!File.Exists(file.name))
                    {
                        addFile = true;
                    }

                    if (!addFile)
                    {
                        using (FileStream fs = new FileStream(file.name, FileMode.Open, FileAccess.Read))
                        {
                            if (file.length > 0 && file.length != fs.Length)
                            {
                                addFile = true;
                            }
                            else
                            {
                                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                                byte[] hashbytes = md5.ComputeHash(fs);
                                string hashstring = BitConverter.ToString(hashbytes).ToLower().Replace("-", "");

                                if (file.md5hash != hashstring)
                                    addFile = true;
                            }
                        }
                    }

                    if (addFile)
                    {
                        request = WebRequest.CreateHttp(_baseurl + file.name);
                        request.AllowAutoRedirect = true;

                        response = await request.GetResponseAsync() as HttpWebResponse;

                        int length = (int) response.ContentLength;
                        response.Close();

                        newfiles.Add(new UpdateFile(file.name, file.md5hash, length));
                    }
                }

                UpdateAvailable = newfiles.Count > 0;

                UpdateFiles = newfiles.ToArray();

            }
            catch (Exception e)
            {
                if (errorEvent != null)
                    errorEvent(this, new ErrorEventArgs(e));
            }

            IsChecking = false;
        }

        public async Task DoUpdate(bool restart = false, string exectuable = "", string parameter = "")
        {
            IsUpdating = true;
            try
            {

                if (_updatefiles.Length == 0) return;

                string tempdir = (Path.GetRandomFileName() + Path.GetRandomFileName()).Replace(".", "");
                Directory.CreateDirectory(tempdir);

                NumFiles = _updatefiles.Length;

                int lengthsum = 0;
                for (int i = 0; i < _updatefiles.Length; i++)
                {
                    lengthsum += _updatefiles[i].length;
                }

                TotalBytes = lengthsum;

                int bytesCounter = 0;

                for (int i = 0; i < _updatefiles.Length; i++)
                {
                    CurrentFileNum = i + 1;
                    CurrentFilename = _updatefiles[i].name;

                    using (FileStream fs = new FileStream(Path.Combine(tempdir, _updatefiles[i].name), FileMode.Create, FileAccess.Write))
                    {
                        HttpWebRequest request = WebRequest.CreateHttp(_baseurl + _updatefiles[i].name);
                        request.AllowAutoRedirect = true;

                        HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                        Stream responseStream = response.GetResponseStream();

                        CurrentFileBytes = (int) response.ContentLength;

                        var buffer = new byte[1024];
                        var bytesread = 0;
                        while (fs.Position < response.ContentLength)
                        {
                            bytesread = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                            await fs.WriteAsync(buffer, 0, bytesread);

                            CurrentFileBytesDownloaded = (int) fs.Position;
                            TotalBytesDownloaded = bytesCounter + CurrentFileBytesDownloaded;
                        }
                    }

                    bytesCounter += CurrentFileBytes;
                }

                string command =
                    "/C @echo off & for /l %a in (0) do TaskList /FI \"IMAGENAME eq " + exectuable + "\" 2>NUL | Find \"" + exectuable + "\" >NUL || "+ //Waits on app termination
                    "(move /Y " + Path.Combine(tempdir, "*.*") + " . && rd /s /q " + tempdir + (restart ? (" && start " + exectuable + " " + parameter) : "") + " & EXIT)"; //actual update
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", command);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                Process cmd = new Process();
                cmd.StartInfo = psi;
                cmd.Start();
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                if (errorEvent != null)
                    errorEvent(this, new ErrorEventArgs(e));
            }

            IsUpdating = false;
        }

        public async Task<string> GetChangelog()
        {
            IsGettingChangelog = true;

            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(infofileurl);
                request.AllowAutoRedirect = true;

                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());

                while (!reader.EndOfStream && (await reader.ReadLineAsync()).Trim().ToLower() != "[###changelog###]") ;

                string changelog = "";

                if (!reader.EndOfStream)
                    changelog = (await reader.ReadToEndAsync()).Trim();

                Changelog = changelog;

                IsGettingChangelog = false;
                return changelog;
            }
            catch (Exception e)
            {
                if (errorEvent != null)
                    errorEvent(this, new ErrorEventArgs(e));
            }

            IsGettingChangelog = false;

            return "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public struct UpdateFile
    {
        public string name;
        public string md5hash;
        public int length;

        public UpdateFile(string name, string md5hash, int length=0)
        {
            this.name = name;
            this.md5hash = md5hash;
            this.length = length;
        }
    }
}

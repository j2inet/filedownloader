using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DownloadImage.ViewModels
{
    //https://blog.j2i.net/2023/03/21/updated-viewmodelbase-for-my-wpf-projects/
    /// <summary>
    /// 
    /// </summary>
    public class MainViewModel: ViewModelBase
    {

        Thread _downloadThread = null;
        Queue<PayloadInformation> _downloadQueue = new Queue<PayloadInformation>();
        PayloadInformation _currentPayload = null;
        public PayloadInformation CurrentPayload
        {
            get => _currentPayload;
            set
            {
                if (_currentPayload != value)
                {
                    _currentPayload = value;
                    OnPropertyChanged("CurrentPayload");
                }
            }
        }
        AutoResetEvent _downloadCompleteWait = new AutoResetEvent(false);

        float _downloadProgress = 0;
        public float DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                if (_downloadProgress != value)
                {
                    _downloadProgress = value;
                    OnPropertyChanged("DownloadProgress");
                }
            }
        }

        private string _downloadLabelText;
        public string DownloadLabelText
        {
            get => _downloadLabelText;
            set
            {
                if (_downloadLabelText != value)
                {
                    _downloadLabelText = value;
                    OnPropertyChanged("DownloadLabelText");
                }
            }
        }


        string _phase;
        public string Phase
        {             get => _phase;
                   set
            {
                if (_phase != value)
                {
                    _phase = value;
                    OnPropertyChanged("Phase");
                }
            }
        }


        public MainViewModel()
        {
            DownloadUrl = ApplicationSettings.Default.DefaultURL;
            TempFolder = Path.GetTempPath();
            DownloadLabelText = ApplicationSettings.Default.DownloadButtonLabelText;
            BackgroundImage = ApplicationSettings.Default.BackgroundImage;
        }


        String _backgroundImage = "";
        public string BackgroundImage
        {
            get => _backgroundImage;
            set => SetValueIfChanged(() => BackgroundImage, () => _backgroundImage, value);
        }

        public string _downloadUrl;
        public string DownloadUrl
        {
            get { return _downloadUrl; }
            set
            {
                if (_downloadUrl != value)
                {
                    _downloadUrl = value;
                    OnPropertyChanged("DownloadUrl");
                }
            }
        }

        string _tempFolder = "";
        public string TempFolder
        {
            get => _tempFolder;
            set
            {
                if (_tempFolder != value)
                {
                    _tempFolder = value;
                    OnPropertyChanged("TempFolder");
                }
            }
        }

        async Task<List<PayloadInformation>> GetPayloadList()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(DownloadUrl);
            var stringContent = await response.Content.ReadAsStringAsync();
            var payloadList = JsonSerializer.Deserialize<List<PayloadInformation>>(stringContent);
            payloadList.ForEach(p =>
            {
                if (!String.IsNullOrEmpty(p.TargetPath))
                {
                    if (p.TargetPath.Contains("..") || p.TargetPath.Contains(":") || p.TargetPath.StartsWith("\\") || p.TargetPath.StartsWith("/"))
                    {
                        throw new Exception("Invalid Target Path");
                    }
                }
            });
            return payloadList;
        }


        void DownloadProgressChanged(Object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            System.Diagnostics.Debug.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...", 
                               (string)e.UserState, e.BytesReceived,e.TotalBytesToReceive,e.ProgressPercentage);

            DownloadProgress = e.ProgressPercentage;
        }


        async void DownloadRoutine()
        {

            var autoRedirect = true;
            var handler = new SocketsHttpHandler()
            {
                AllowAutoRedirect = autoRedirect,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                CookieContainer = new CookieContainer(),
                PooledConnectionLifetime = TimeSpan.FromMinutes(2) // <= Adjust as required
            };
            var client = new HttpClient(handler, true) { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10; Win64; x64; rv:56.0) Gecko/20100101 Firefox/56.0");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            // Keep true if you download resources from different collections of URLs each time
            // Remove or set to false if you use the same URLs multiple times and frequently
            client.DefaultRequestHeaders.ConnectionClose = true;
            while (_downloadQueue.Count > 0)
            {
                Phase = "Downloading...";
                var payload = _downloadQueue.Dequeue();
                DownloadProgress = 0;
                CurrentPayload = payload;
                switch (payload.PayloadType)
                {
                    case PayloadType.File:
                        {
                            Phase="Downloading";
                            var response = client.GetAsync(payload.FileURL).Result;
                            var content = response.Content.ReadAsByteArrayAsync().Result;
                            var tempFilePath = Path.Combine(TempFolder, payload.TargetPath);
                            var fileName = Path.GetFileName(payload.FileURL);
                            File.WriteAllBytes(tempFilePath, content);
                            File.Move(tempFilePath, payload.TargetPath, true);
                        }
                        break;
                    case PayloadType.Folder:
                        {
                            Phase = "Creating Directory";
                            var directoryName = payload.TargetPath.Replace('/', Path.DirectorySeparatorChar);
                            var directoryInfo = new DirectoryInfo(directoryName);
                            if (!directoryInfo.Exists)
                            {
                                directoryInfo.Create();
                            }
                        }
                        break;
                    case PayloadType.ZipFile:
                        {
                            WebClient webClient = new WebClient();
                            webClient.DownloadProgressChanged += DownloadProgressChanged;
                            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                            var tempFilePath = Path.Combine(TempFolder, Path.GetTempFileName()) + ".zip";
                            var fileName = Path.GetFileName(payload.FileURL);
                            var directoryName = payload.TargetPath.Replace('/', Path.DirectorySeparatorChar);

                            if (String.IsNullOrEmpty(directoryName))
                            {
                                directoryName = ".";
                            }
                            var directoryInfo = new DirectoryInfo(directoryName);
                            if (!directoryInfo.Exists)
                            {
                                directoryInfo.Create();
                            }
                            webClient.DownloadFileAsync(new Uri(payload.FileURL), tempFilePath);
                            _downloadCompleteWait.WaitOne();
                            Phase = "Decompressing";
                            System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, directoryInfo.FullName,true);

                        }
                        break;
                    default:
                        break;
                }
            }
            Phase = ApplicationSettings.Default.CompletedMessage;
            _downloadThread = null;
            DownloadCommand.RaiseCanExecuteChanged();
        }

        private void WebClient_DownloadFileCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            DownloadProgress = 0;
            _downloadCompleteWait.Set();
        }
        bool CanDownload(string downloadURL)
        {
            return true;
        }
        DelegateCommand _downloadCommand;
        public DelegateCommand DownloadCommand
        {
            get
            {
                if (_downloadCommand == null)
                {
                    _downloadCommand = new DelegateCommand(async () =>
                    {
                        var payloadList = await GetPayloadList();

                        _downloadQueue = new Queue<PayloadInformation>(payloadList);
                        _downloadThread = new Thread(DownloadRoutine);
                        _downloadThread.Start();
                        DownloadCommand.RaiseCanExecuteChanged();
                    }, (object o) => _downloadThread == null);
                }
                return _downloadCommand;
            }
        }

    }
}

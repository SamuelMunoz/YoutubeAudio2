using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using YoutubeExtractor;

namespace YoutubeAudio2
{
    public partial class Form1 : Form
    {
        private string _filepath;
        private string _videotitle;
        private string _audiotitle;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnDescargar_Click(object sender, EventArgs e)
        {
            btnDescargar.Enabled = false;
            btnDescargar.Text = @"Descargando...";          
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            var videos = DownloadUrlResolver.GetDownloadUrls(txtDireccion.Text);
            var video = videos.First(p => p.VideoType == VideoType.Mp4 && p.Resolution == 360);
            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);
            _filepath = Path.Combine(Application.StartupPath + "\\", video.Title + video.VideoExtension);
            _videotitle = video.Title + video.VideoExtension;
            _audiotitle = $"{video.Title}.mp3";
            var downloader = new VideoDownloader(video, _filepath);
            downloader.DownloadProgressChanged += Downloader_DownloadProgressChanged;
            var trd = new Thread(() =>
            {
                downloader.Execute();
                var ffmpeg = Application.StartupPath + "\\ffmpeg.exe";
                var args = $"-i \"{_videotitle}\" \"{_audiotitle}\" -y";
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        Arguments = args
                    }
                };
                proc.Start();
                proc.WaitForExit();
                File.Delete($"{Application.StartupPath}\\{_videotitle}");
                var savedialog = new SaveFileDialog {FileName = _audiotitle};
                if (savedialog.ShowDialog() != DialogResult.OK) return;
                var fi = new FileInfo(savedialog.FileName);
                if (fi.Exists) fi.Delete();
                File.Move($"{Application.StartupPath}\\{_audiotitle}", savedialog.FileName);
                Invoke(new MethodInvoker(delegate
                {
                    txtDireccion.Text = "";
                    progressBar.Value = 0;
                    lblPercentage.Text = @"0.00%";
                    btnDescargar.Enabled = true;
                    btnDescargar.Text = @"Descargar!";
                }));
            }) {IsBackground = true};
            trd.SetApartmentState(ApartmentState.STA);
            trd.Start();
        }

        private void Downloader_DownloadProgressChanged(object sender, ProgressEventArgs e)
        {
            Invoke(new MethodInvoker(delegate
            {
                progressBar.Value = (int) e.ProgressPercentage;
                lblPercentage.Text = $"{e.ProgressPercentage:0.##}%";
                progressBar.Update();
            }));
        }
    }
}

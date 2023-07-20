using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace VideoDownloader
{
    public partial class MainWindow : Window
    {
        private Semaphore semaphore = new Semaphore(3, 3);
        private ProgressBar[] progressBars;

        public MainWindow()
        {
            InitializeComponent();
            progressBars = new ProgressBar[] { ProgressBar1, ProgressBar2, ProgressBar3 };
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = UrlTextBox.Text;
            string savePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            await Task.Run(() =>
            {
                try
                {
                    semaphore.WaitOne();

                    if (string.IsNullOrWhiteSpace(videoUrl) || string.IsNullOrWhiteSpace(savePath))
                    {
                        MessageBox.Show("Please enter a valid URL and save path.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    ProgressBar progressBar = FindAvailableProgressBar();

                    if (progressBar == null)
                    {
                        MessageBox.Show("Maximum number of downloads reached.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    DownloadVideo(videoUrl, savePath, progressBar);
                    MessageBox.Show("Video downloaded!", "Successfully!", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                finally
                {
                    semaphore.Release();
                }
            });
        }

        private async void DownloadVideo(string videoUrl, string savePath, ProgressBar progressBar)
        {
            using (var client = new YoutubeClient())
            {
                var video = await client.GetVideoAsync(videoUrl);
                var streamManifest = await client.GetVideoMediaStreamInfosAsync(video.Id);
                var streamInfo = streamManifest.Muxed.WithHighestVideoQuality();

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;

                var progressHandler = new Progress<double>(value =>
                {
                    progressBar.Value = value;
                });

                await client.DownloadMediaStreamAsync(streamInfo, savePath, progressHandler);
            }
        }

        private ProgressBar FindAvailableProgressBar()
        {
            foreach (ProgressBar progressBar in progressBars)
            {
                if (progressBar.Value == 0)
                {
                    return progressBar;
                }
            }

            return null;
        }
    }
}
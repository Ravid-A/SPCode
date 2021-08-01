﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ByteSizeLib;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using SPCode.UI;

namespace SPCode.Utils
{
    public class JavaInstallation
    {
        private const int JavaVersionForLysis = 11;
        private ProgressDialogController dwJavaInCourse;
        private readonly string OutFile = Environment.ExpandEnvironmentVariables(Constants.JavaDownloadFile);
        private readonly string JavaLink = Environment.Is64BitOperatingSystem ? Constants.JavaDownloadSite64 : Constants.JavaDownloadSite32;
        private readonly MainWindow _win;
        private readonly MetroDialogSettings _metroDialogOptions;

        public JavaInstallation()
        {
            _win = Program.MainWindow;
            _metroDialogOptions = _win.MetroDialogOptions;
        }
        public enum JavaResults
        {
            Outdated,
            Absent,
            Correct
        }
       
        public async Task InstallJava()
        {
            // Spawn progress dialog when downloading Java
            dwJavaInCourse = await _win.ShowProgressAsync(Program.Translations.GetLanguage("DownloadingJava") + "...",
                Program.Translations.GetLanguage("FetchingJava"), false, _metroDialogOptions);
            dwJavaInCourse.SetProgress(0.0);
            MainWindow.ProcessUITasks();

            // Setting up event callbacks to change download percentage, amount downloaded and amount left
            using var wc = new WebClient();
            wc.DownloadProgressChanged += DownloadProgressed;
            wc.DownloadFileCompleted += DownloadCompleted;
            wc.DownloadFileAsync(new Uri(JavaLink), OutFile);
        }

        private void DownloadProgressed(object sender, DownloadProgressChangedEventArgs e)
        {
            // Handles percentage and MB downloaded/left
            dwJavaInCourse.SetMessage(
                $"{e.ProgressPercentage}% {Program.Translations.GetLanguage("AmountCompleted")}, " +
                $"{Program.Translations.GetLanguage("AmountDownloaded")} {Math.Round(ByteSize.FromBytes(e.BytesReceived).MegaBytes),0} MB / " +
                $"{Math.Round(ByteSize.FromBytes(e.TotalBytesToReceive).MegaBytes),0} MB");

            // Handles progress bar
            dwJavaInCourse.SetProgress(e.ProgressPercentage * 0.01d);
        }

        private async void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            await dwJavaInCourse.CloseAsync();
            if (File.Exists(OutFile))
            {
                // If file downloaded properly, it should open
                Process.Start(OutFile);
                await _win.ShowMessageAsync(
                    Program.Translations.GetLanguage("JavaOpened"),
                    Program.Translations.GetLanguage("JavaSuggestRestart"),
                    MessageDialogStyle.Affirmative);
            }
            else
            {
                // Otherwise, just offer a manual download
                if (await _win.ShowMessageAsync(
                    Program.Translations.GetLanguage("JavaDownErrorTitle"),
                    Program.Translations.GetLanguage("JavaDownErrorMessage"),
                    MessageDialogStyle.AffirmativeAndNegative, _metroDialogOptions) == MessageDialogResult.Affirmative)
{
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = JavaLink,
                        UseShellExecute = true
                    });
                    await _win.ShowMessageAsync(
                    Program.Translations.GetLanguage("JavaOpenedBrowser"),
                    Program.Translations.GetLanguage("JavaSuggestRestart"),
                    MessageDialogStyle.Affirmative);
                }
            }
        }

        public JavaResults GetJavaStatus()
        {
            var process = new Process();
            process.StartInfo.FileName = "javac";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = "-version";

            string output;
            try
            {
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception) // no java in the PATH directory
            {
                return JavaResults.Absent;
            }

            return int.Parse(output.Split(' ')[1].Split('.')[0]) < JavaVersionForLysis ? JavaResults.Outdated : JavaResults.Correct;
        }
    }
}

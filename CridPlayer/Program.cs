using FlyleafLib;

namespace CridPlayer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Engine.Start(new EngineConfig()
            {
                FFmpegPath = AppDomain.CurrentDomain.BaseDirectory+"\\FFmpeg",
                FFmpegDevices = false,    // Prevents loading avdevice/avfilter dll files. Enable it only if you plan to use dshow/gdigrab etc.

#if RELEASE
                FFmpegLogLevel      = Flyleaf.FFmpeg.LogLevel.Quiet,
                LogLevel            = LogLevel.Quiet,

#else
                FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
                LogLevel = LogLevel.Debug,
                LogOutput = ":debug",
                //LogOutput         = ":console",
                //LogOutput         = @"C:\Flyleaf\Logs\flyleaf.log",                
#endif

                //PluginsPath       = @"C:\Flyleaf\Plugins",

                UIRefresh = false,    // Required for Activity, BufferedDuration, Stats in combination with Config.Player.Stats = true
                UIRefreshInterval = 250,      // How often (in ms) to notify the UI
                UICurTimePerSecond = true,     // Whether to notify UI for CurTime only when it's second changed or by UIRefreshInterval
            });

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
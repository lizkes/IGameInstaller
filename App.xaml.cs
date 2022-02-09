using IGameInstaller.Model;
using Microsoft.Web.WebView2.Wpf;
using NLog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace IGameInstaller
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static int ResourceId { get; } = 12;
        public static string Name { get; set; } = "IGame安装器";
        public static string EnglishName { get; set; } = "IGameInstaller";
        public static string ProjectName { get; set; } = "IGame";
        public static Version Version { get; set; } = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);

        public static int resourceId;
        public static SplashScreen SplashScreen {get; set;} = new SplashScreen("SplashScreen.png");
        public static ResourceInstallInfo ResourceInstallInfo { get; set; }
        public static InstallConfig InstallConfig { get; set; } 
        public static WebView2 WebView { get; set; }


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 初始化闪屏
            SplashScreen.Show(false, true);

            // 初始化配置
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "run.log" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            LogManager.Configuration = config;
        }
    }
}

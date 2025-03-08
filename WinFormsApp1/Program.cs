using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsFormsHosting;

namespace WinFormsApp1
{
    internal static class Program
    {
        private static ILogger? _logger;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // 未処理例外の扱い
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic); // 構成ファイルに従う

            // HostApplicationBuilder
            var builder = Host.CreateApplicationBuilder(args);
            // ロギング構成
            builder.Logging.ClearProviders()
                           .AddDebug();

            // WindowsFormsをGenericHostに載せる開始Form(MainForm) -> IHostedServiceの実装
            builder.UseWindowsForms<Form1>(Application_ApplicationExit,
                                           Application_ThreadException,
                                           CurrentDomain_UnhandledException);

            var host = builder.Build();
            _logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger(categoryName: "Program"); // ILogger<Program> はProgramがstatic classなため使えない
            host.Run();
        }

        private static void Application_ApplicationExit(object? sender, EventArgs e)
        {
            _logger?.LogInformation("Application Exit!!!");
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Application_ThreadException!!!");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger?.LogError(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException!!!");
        }
    }
}
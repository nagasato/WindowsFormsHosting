using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsFormsHosting;

namespace WinFormsNetFx48App1
{
    static class Program
    {
        private static ILogger _logger;

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 未処理例外の扱い
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic); // 構成ファイルに従う

            Application.ApplicationExit += Application_ApplicationExit;
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // HostApplicationBuilder
            var builder = Host.CreateApplicationBuilder(args);
            // ロギング構成
            builder.Logging.ClearProviders()
                           .AddDebug()
                           .SetMinimumLevel(LogLevel.Trace);

            // WindowsFormsをGenericHostに載せる
            builder.AddWinFormsHosting<Form1>();
            // --- FormFactoryとFormの登録 --- 
            builder.Services.AddSingleton<IFormFactory, FormFactory>(); // Singleton
            builder.Services.AddTransient<Form2>(); // 場合に応じてSingleton

            var host = builder.Build();
            _logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger(categoryName: "Program"); // ILogger<Program> はProgramがstatic classなため使えない
            host.Run();
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            _logger.LogInformation("Application Exit!!!");
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Application_ThreadException!!!");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException!!!");
        }
    }
}

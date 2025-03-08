using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

// 実装参考
// * https://github.com/alex-oswald/WindowsFormsLifetime/blob/main/src/WindowsFormsLifetime/WindowsFormsHostedService.cs

namespace WindowsFormsHosting
{
    /// <summary>
    /// WindowsFormsをIHostedServiceとして動作させるためのクラス
    /// </summary>
    /// <typeparam name="TStartForm"></typeparam>
    public class WindowsFormsHostedService<TStartForm> : IHostedService where TStartForm : Form
    {
        private readonly ILogger<WindowsFormsHostedService<TStartForm>> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IServiceProvider _serviceProvider;

        private Thread _uiThread;
        //private CancellationTokenRegistration _applicationStoppingRegistration;
        private readonly EventHandler _applicationExitAction;
        private readonly ThreadExceptionEventHandler _threadExceptionAction;
        private readonly UnhandledExceptionEventHandler _currentDomainUnhandledExceptionAction;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger"></param>
        public WindowsFormsHostedService(IHostApplicationLifetime hostApplicationLifetime,
                                         IServiceProvider serviceProvider,
                                         ILogger<WindowsFormsHostedService<TStartForm>> logger,
                                         EventHandler applicationExitAction,
                                         ThreadExceptionEventHandler threadExceptionAction,
                                         UnhandledExceptionEventHandler currentDomainUnhandledExceptionAction)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _serviceProvider = serviceProvider;

            _applicationExitAction = applicationExitAction;
            _threadExceptionAction = threadExceptionAction;
            _currentDomainUnhandledExceptionAction = currentDomainUnhandledExceptionAction;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // see. https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.extensions.hosting.ihostapplicationlifetime?view=net-8.0

            _hostApplicationLifetime.ApplicationStarted.Register(state =>
            {
                // アプリケーション ホストが完全に起動されたときにトリガーされる。
                _logger.LogInformation("ApplicationStarted!!!");
            },
            this);

            //_applicationStoppingRegistration = _hostApplicationLifetime.ApplicationStopping.Register(state =>
            _hostApplicationLifetime.ApplicationStopping.Register(state =>
            {
                // アプリケーション ホストが正常なシャットダウンを実行したときにトリガーされる。
                _logger.LogInformation("ApplicationStopping!!!");

                var form = _serviceProvider.GetRequiredService<TStartForm>();
                if (form.IsHandleCreated && !form.IsDisposed)
                {
                    // 他のHostedServiceが停止要求に応える (ここはFormのSTAスレッドと別のスレッド)
                    form.Invoke(new Action(() =>
                    {
                        form.Close();
                        // TODO: 外部からのシャットダウン要求からのFormClosingでキャンセルされた場合には未対応
                    }));
                }
            },
            this);

            _hostApplicationLifetime.ApplicationStopped.Register(state =>
            {
                // アプリケーション ホストが正常なシャットダウンを実行しているときにトリガーされる。
                // このイベントが完了するまで、シャットダウンはブロックされる。
                _logger.LogInformation("ApplicationStopped!!!");
            },
            this);

            // STAスレッドの起動
            // (1回こっきりのスレッド起動なので生スレッドで良い)
            _uiThread = new Thread(StartUiThread)
            {
                Name = "WindowsFormsHostedService UI Thread"
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // IHostApplicationLifetime.StopApplication()で、このメソッドが呼ばれる
            return Task.CompletedTask;
        }

        private void StartUiThread()
        {
            // Note:
            // STAスレッドだがスレッドID=1ではない。
            // Program.csのHost.Run()するまでのSTAスレッド(スレッドID=1)とは異なるスレッドで動く。
            // Formはこのスレッドで動作する。そのため未処理例外トラップをこのスレッドでセットしている。
            //
            // なお、スレッドID=1でなないSTAスレッドでFormを動して本当に問題ないかはわからない。
            // 問題なく動いているように見えるが、UI部品をProgram.csのHost.Run()前に作って、
            // それをその後生成されるFormで使うとスレッドIDが異なるので問題が発生するかもしれない。

            Application.ApplicationExit += _applicationExitAction;
            // 未処理例外トラップ
            Application.ThreadException += _threadExceptionAction;
            AppDomain.CurrentDomain.UnhandledException += _currentDomainUnhandledExceptionAction;

            // WinFormsアプリ開始(開始Formを取得して Application.Run())
            var startForm = _serviceProvider.GetRequiredService<TStartForm>(); // ここでForm1とハードコーディングしてはだめ(抽象化の意味がない)
            Application.Run(startForm);
            _logger.LogInformation("Exit Application.Run()!!!");

            AppDomain.CurrentDomain.UnhandledException -= _currentDomainUnhandledExceptionAction;
            Application.ThreadException -= _threadExceptionAction;
            AppDomain.CurrentDomain.UnhandledException -= _currentDomainUnhandledExceptionAction;

            // WindowsFormsの終わりはアプリの終わりとする
            _hostApplicationLifetime.StopApplication(); // 停止要求
        }
    }

    /// <summary>
    /// IHostBuilder/IHostApplicationBuilder拡張メソッド
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Windows Fromsをホストする (IHostBuilder版)
        /// </summary>
        /// <typeparam name="TStartForm"></typeparam>
        /// <param name="hostBuilder"></param>
        /// <param name="applicationExitAction"></param>
        /// <param name="threadExceptionAction"></param>
        /// <param name="currentDomainUnhandledExceptionAction"></param>
        /// <returns></returns>
        public static IHostBuilder UseWindowsForms<TStartForm>(this IHostBuilder hostBuilder,
                                                    EventHandler applicationExitAction = null,
                                                    ThreadExceptionEventHandler threadExceptionAction = null,
                                                    UnhandledExceptionEventHandler currentDomainUnhandledExceptionAction = null
                                                    ) where TStartForm : Form
        {
            return hostBuilder.ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<TStartForm>() // 開始フォームをDIコンテナに登録
                        .AddHostedService(provider =>
                        {
                            // イベントハンドラをインスタンスに渡したいので自前でインスタンス化
                            var appLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                            var logger = provider.GetRequiredService<ILogger<WindowsFormsHostedService<TStartForm>>>();
                            return new WindowsFormsHostedService<TStartForm>(appLifetime, provider, logger,
                                        applicationExitAction, threadExceptionAction, currentDomainUnhandledExceptionAction);
                        });
            });
        }

        /// <summary>
        /// Windows Fromsをホストする (IHostApplicationBuilder版)
        /// </summary>
        /// <typeparam name="TStartForm"></typeparam>
        /// <param name="hostAppBuilder"></param>
        /// <param name="applicationExitAction"></param>
        /// <param name="threadExceptionAction"></param>
        /// <param name="currentDomainUnhandledExceptionAction"></param>
        /// <returns></returns>
        public static IHostApplicationBuilder UseWindowsForms<TStartForm>(this IHostApplicationBuilder hostAppBuilder,
                                                    EventHandler applicationExitAction = null,
                                                    ThreadExceptionEventHandler threadExceptionAction = null,
                                                    UnhandledExceptionEventHandler currentDomainUnhandledExceptionAction = null
                                                    ) where TStartForm : Form
        {
            hostAppBuilder.Services.AddSingleton<TStartForm>() // 開始フォームをDIコンテナに登録
                                                               //.AddHostedService<WindowsFormsHostedService<TStartForm>>();
                                   .AddHostedService(provider =>
                                   {
                                       // イベントハンドラをインスタンスに渡したいので自前でインスタンス化
                                       var appLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
                                       var logger = provider.GetRequiredService<ILogger<WindowsFormsHostedService<TStartForm>>>();
                                       return new WindowsFormsHostedService<TStartForm>(appLifetime, provider, logger,
                                                        applicationExitAction, threadExceptionAction, currentDomainUnhandledExceptionAction);
                                   });
            return hostAppBuilder;
        }
    }
}

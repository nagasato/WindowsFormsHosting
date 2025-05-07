using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WindowsFormsHosting
{
    /// <summary>
    /// WinFormsHostedService
    /// (WindowsFormsをIHostedServiceとして動作させるためのクラス)
    /// </summary>
    public class WinFormsHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<WinFormsHostedService> _logger;

        private readonly IShutdownRequestHandler _shutdownHandler;
        private readonly ApplicationContext _appContext;
        private readonly IHostApplicationLifetime _hostLifetime;
        private readonly IHostEnvironment _environment;
        private CancellationTokenRegistration _applicationStoppingRegistration;
        private Task _uiThreadTask = Task.CompletedTask;
        private ManualResetEventSlim _uiThreadStarted = new ManualResetEventSlim(false); // UIスレッド開始同期用

        /// <summary>
        /// WinFormsHostedService Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="appContext"></param>
        /// <param name="shutdownHandler"></param>
        /// <param name="hostLifetime"></param>
        /// <param name="environment"></param>
        public WinFormsHostedService(
            ILogger<WinFormsHostedService> logger,
            ApplicationContext appContext,
            IShutdownRequestHandler shutdownHandler,
            IHostApplicationLifetime hostLifetime,
            IHostEnvironment environment)
        {
            _logger = logger;
            _appContext = appContext;
            _shutdownHandler = shutdownHandler;
            _hostLifetime = hostLifetime;
            _environment = environment;
        }

        #region --- IHostedService Implementation ---
        /// <summary>
        /// StartAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Start WinFormsHostedService ({Environment})...", _environment.EnvironmentName);

            // ApplicationStopping コールバックで IShutdownRequestHandler を使う
            _applicationStoppingRegistration = _hostLifetime.ApplicationStopping.Register(() =>
            {
                _logger.LogTrace("IShutdownRequestHandler.RequestShutdownFromHost() has been called.");
                _shutdownHandler.RequestShutdownFromHost();
            });


            // STAスレッドの起動
            // (1回こっきりのスレッド起動なので生スレッドで良い)
            var uiThread = new Thread(StartUIThread)
            {
                Name = "WinForms UI Thread",
            };
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();

            // UIスレッドの開始（AppContext取得まで）を待つTaskを生成
            _uiThreadTask = Task.Run(() =>
            {
                _logger.LogTrace("Waiting for UI thread to start...");
                _uiThreadStarted.Wait(cancellationToken); // 開始またはキャンセルされるまで待機
                cancellationToken.ThrowIfCancellationRequested();

                // スレッド終了まで待機
                if (uiThread.IsAlive) 
                {
                    uiThread.Join(); 
                } 
            }, cancellationToken);

            _logger.LogTrace("UI Thread started.");
            // StartAsync は UI スレッドの開始"準備"完了を待たずに完了させるか、
            // _uiThreadStarted.Wait を Task.Run でラップしたものを返すなどが考えられる
            // ここではすぐ完了させる
            return Task.CompletedTask;
        }

        /// <summary>
        /// StopAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stop WinFormsHostedService.");
            // ApplicationStopping トークンの監視を解除 (Dispose で行うのでここでは不要かも)
            // _applicationStoppingRegistration.Dispose();

            if (_uiThreadTask != null 
                && !_uiThreadTask.IsCompleted)
            {
                _logger.LogTrace("Waiting for UI thread to terminate...");
                // cancellationToken を使って待機をキャンセル可能にする (例: 5秒待つ)
                var timeout = TimeSpan.FromSeconds(5);
                var completedTask = await Task.WhenAny(_uiThreadTask, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

                if (completedTask == _uiThreadTask)
                {
                    // _uiThreadTask が完了した場合の処理 (エラーチェックなど)
                    if (_uiThreadTask.IsFaulted)
                    {
                        _logger.LogError(_uiThreadTask.Exception?.GetBaseException(), "UI thread terminated with an error.");
                    }
                    else if (_uiThreadTask.IsCanceled)
                    {
                        _logger.LogWarning("UI thread start/wait cancelled.");
                    }
                    else
                    {
                        _logger.LogTrace("UI thread terminated normally.");
                    }
                }
                else
                {
                    // タイムアウトまたは外部からのキャンセル

                    _logger.LogWarning("Waiting for the UI thread to terminate timed out ({Timeout}s) or canceled.", timeout.TotalSeconds);
                    // ここで強制終了処理を試みることもできるが、通常は避けるべき
                }
            }
            else
            {
                _logger.LogWarning("UI thread has already terminated or has never been started.");
            }
        }
        #endregion

        /// <summary>
        /// Start UI Thread
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void StartUIThread()
        {
            // Note:
            // ここで開始するSTAスレッドはスレッドID=1ではない。
            // Program.csのHost.RunするまでのSTAスレッド(スレッドID=1)とは異なるスレッドで動く。
            // Formはこのスレッドで動作する。
            //
            // なお、スレッドID=1でなないSTAスレッドでFormを動して本当に問題ないかはわからない。
            // 要件はSTAかつ同一スレッド内ということだけか？
            // 問題なく動いているように見える。

            if (_appContext.MainForm == null) { throw new InvalidOperationException("ApplicationContext.MainForm is not set."); }

            // UI スレッドの準備完了を通知
            _uiThreadStarted.Set();

            // メッセージループを開始
            Application.Run(_appContext);
            _logger.LogTrace("Application.Run() terminated.");
        }

        #region --- IDisposable Implementation ---
        bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // マネージドリソースの解放
                    _applicationStoppingRegistration.Dispose();
                    _uiThreadStarted.Dispose();
                }

                // アンマネージドリソースの解放 (あれば)

                _disposedValue = true;
                _logger.LogTrace("WinFormsHostedService has been disposed.");
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

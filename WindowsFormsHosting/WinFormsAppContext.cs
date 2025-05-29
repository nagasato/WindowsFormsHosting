using System;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WindowsFormsHosting
{
    /// <summary>
    /// WindowsForms Hosting ApplicationContext
    /// </summary>
    /// <typeparam name="TForm"></typeparam>
    public class WinFormsAppContext<TForm> : ApplicationContext, IShutdownRequestHandler
        where TForm : Form
    {
        private readonly ILogger<WinFormsAppContext<TForm>> _logger;

        private readonly IHostApplicationLifetime _hostLifetime;
        private readonly TForm _mainForm;

        /// <summary>
        /// MainAppContext Constructor
        /// </summary>
        /// <param name="hostLifetime"></param>
        /// <param name="mainForm"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public WinFormsAppContext(
            IHostApplicationLifetime hostLifetime,
            TForm mainForm,
            ILogger<WinFormsAppContext<TForm>> logger)
        {
            _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.MainForm = _mainForm; // ApplicationContext.MainFormの外部からの変更には非対応. _mainFormでハンドルする
            _mainForm.FormClosed += this.OnFormClosed;
        }

        #region -- IShutdownRequestHandler Implementation --
        /// <summary>
        /// 外部(Host)からのシャットダウン要求を処理
        /// </summary>
        public void RequestShutdownFromHost()
        {
            // フォームが無効でないか確認
            if (!_mainForm.IsDisposed && _mainForm.IsHandleCreated)
            {
                _logger.LogTrace($"Try to close the MainForm.");
                
                _mainForm.BeginInvoke(new Action(() =>
                {
                    if (!_mainForm.IsDisposed)
                    {
                        _mainForm.Close();
                    }
                }));
            }
            else
            {
                _logger.LogWarning("MainForm is in an invalid state.");
                // 必要なら強制的に StopApplication を呼ぶ
                // if (!_hostLifetime.ApplicationStopping.IsCancellationRequested) { _hostLifetime.StopApplication(); }
            }
        }
        #endregion

        /// <summary>
        /// Formが閉じられたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            // イベントハンドラ解除
            if (sender is TForm form) { form.FormClosed -= this.OnFormClosed; }

            _logger.LogTrace("MainForm ({FormType}) closed. (CloseReason: {Reason})", typeof(TForm).Name, e.CloseReason);

            // Hostがシャットダウン中でなければ、シャットダウンを要求
            if (!_hostLifetime.ApplicationStopping.IsCancellationRequested)
            {
                _logger.LogTrace("MainForm has been closed, requesting that the Host be shutdown.");
               _hostLifetime.StopApplication();
            }

            // ApplicationContext のメッセージループを終了させる
            try
            {
                this.ExitThread();
                _logger.LogTrace("MainAppContext<{FormType}>.ExitThread() has been called.", typeof(TForm).Name);
            }
            catch (ObjectDisposedException)
            {
                _logger.LogTrace("MainAppContext<{FormType}> had already been disposed.", typeof(TForm).Name);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _mainForm?.Dispose(); }

            base.Dispose(disposing);
             _logger.LogTrace("MainAppContext<{FormType}> has been disposed.", typeof(TForm).Name);
        }
    }
}

using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WindowsFormsHosting
{
    /// <summary>
    /// IWinFormsProvider の実装
    /// (DIコンテナを使用してFormを取得する)
    /// </summary>
    public class WinFormsProvider : IWinFormsProvider
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// FormFactory Constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public WinFormsProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 指定された型のFormインスタンスを取得する
        /// </summary>
        public TForm GetForm<TForm>() where TForm : Form
        {
            // DIコンテナから登録された TForm を取得する
            return _serviceProvider.GetRequiredService<TForm>();
        }
    }
}

using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormsSampleApp1_NetFx48
{
    /// <summary>
    /// IFormFactory の実装
    /// (IServiceProviderを使用してFormを取得する)
    /// </summary>
    public class FormFactory : IFormFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// FormFactory Constructor
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FormFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// 指定された型のFormインスタンスを取得する
        /// </summary>
        public TForm CreateForm<TForm>() where TForm : Form
        {
            // DIコンテナから登録された TForm を取得する
            return _serviceProvider.GetRequiredService<TForm>();
        }
    }
}

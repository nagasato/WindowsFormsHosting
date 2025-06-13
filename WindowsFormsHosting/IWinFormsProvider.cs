using System.Windows.Forms;

namespace WindowsFormsHosting
{
    /// <summary>
    /// Formのインスタンスを生成するためのFactory Interface
    /// </summary>
    public interface IWinFormsProvider
    {
        /// <summary>
        /// 指定された型のフォームインスタンスを取得する
        /// </summary>
        /// <typeparam name="TForm"></typeparam>
        /// <returns></returns>
        TForm GetForm<TForm>() where TForm : Form;
    }
}

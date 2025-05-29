namespace WinFormsSampleApp1
{
    /// <summary>
    /// フォームのインスタンスを生成するためのFactory Interface
    /// </summary>
    public interface IFormFactory
    {
        /// <summary>
        /// 指定された型のフォームインスタンスを生成する
        /// </summary>
        /// <typeparam name="TForm"></typeparam>
        /// <returns></returns>
        TForm CreateForm<TForm>() where TForm : Form;
    }
}

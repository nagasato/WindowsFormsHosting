namespace WindowsFormsHosting
{
    /// <summary>
    /// 外部からのシャットダウン要求ハンドリング Interface
    /// </summary>
    public interface IShutdownRequestHandler
    {
        /// <summary>
        /// 外部(ホスト)からのシャットダウン要求を処理
        /// </summary>
        void RequestShutdownFromHost();
    }
}

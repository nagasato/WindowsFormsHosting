using Microsoft.Extensions.Logging;
using WindowsFormsHosting;

namespace WinFormsSampleApp1
{
    public partial class Form1 : Form
    {
        private readonly ILogger<Form1> _logger;
        private readonly IWinFormsProvider _winFormsProvider;

        public Form1(
            IWinFormsProvider winFormsProvider,
            ILogger<Form1> logger)
        {
            InitializeComponent();

            _logger = logger;
            _winFormsProvider = winFormsProvider;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Program.cs �̗�O�g���b�v���m�F���邽�߂ɁA�Ӑ}�I�ɗ�O���X���[����
            throw new ApplicationException("Test Exception");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (var f = _winFormsProvider.GetForm<Form2>())
            {
                _logger.LogInformation("Opening Form2...");
                f.ShowDialog(this);
            }
        }
    }
}

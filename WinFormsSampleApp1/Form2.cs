using Microsoft.Extensions.Logging;

namespace WinFormsSampleApp1
{
    public partial class Form2 : Form
    {
        private ILogger<Form2> _logger;

        public Form2(ILogger<Form2> logger)
        {
            InitializeComponent();

            _logger = logger;

            this.Load += (s, e) =>
            {
                _logger.LogInformation("Form2 Loaded.");
            };

            this.FormClosed += (s, e) =>
            {
                _logger.LogInformation("Form2 Closed.");
            };
        }
    }
}

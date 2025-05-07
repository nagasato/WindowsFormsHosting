namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly IFormFactory _formFactory;

        public Form1(IFormFactory formFactory)
        {
            InitializeComponent();

            _formFactory = formFactory ?? throw new ArgumentNullException(nameof(formFactory));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            throw new ApplicationException("Test Exception");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (var f = _formFactory.CreateForm<Form2>())
            {
                f.ShowDialog(this);
            }
        }
    }
}

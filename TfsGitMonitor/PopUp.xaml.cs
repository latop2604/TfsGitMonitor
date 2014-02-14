using System.Windows.Controls;

namespace TfsGitMonitor
{
    /// <summary>
    /// Logique d'interaction pour PopUp.xaml
    /// </summary>
    public partial class PopUp : UserControl
    {
        private int _commitCount;

        public int CommitCount
        {
            get { return _commitCount; }
            set
            {
                _commitCount = value;
                CommitCounTextBlock.Text = value.ToString();
            }
        }

        public string Text
        {
            get { return TextBlock.Text; }
            set { TextBlock.Text = value; }
        }

        public PopUp()
        {
            InitializeComponent();
        }
    }
}

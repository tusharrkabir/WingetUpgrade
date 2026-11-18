using System.Windows;

namespace WingetUpgrade.Views
{
    public partial class FullLogWindow : Window
    {
        public FullLogWindow(string logContent)
        {
            InitializeComponent();
            LogTextBox.Text = logContent;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(LogTextBox.Text);
                MessageBox.Show("Log copied to clipboard.", "Winget Upgrade", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

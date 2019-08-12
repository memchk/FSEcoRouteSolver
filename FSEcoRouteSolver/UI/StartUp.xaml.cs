namespace FSEcoRouteSolver.UI
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Windows;

    /// <summary>
    /// Interaction logic for StartUp.xaml
    /// </summary>
    public partial class StartUp : Window
    {
        public StartUp()
        {
            this.InitializeComponent();

            var expireDate = new DateTime(2019, 09, 30);

            var apiKey = Properties.Settings.Default.APIKey;
            if (apiKey.Length != 0)
            {
                this.tAPIKey.Text = apiKey;
            }
        }

        private void BStart_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = this.tAPIKey.Text;
            if (apiKey.Length != 0)
            {
                Properties.Settings.Default.APIKey = apiKey;
                Properties.Settings.Default.Save();

                var mainWindow = new MainWindow(apiKey);
                mainWindow.Show();
                mainWindow.Activate();
                this.Close();
            }
        }
    }
}
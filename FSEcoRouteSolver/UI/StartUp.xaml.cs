using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

namespace FSEcoRouteSolver.UI
{
    /// <summary>
    /// Interaction logic for StartUp.xaml
    /// </summary>
    public partial class StartUp : Window
    {
        public StartUp()
        {
            this.InitializeComponent();

            var dtTime = GetNetworkTime();
            var expireDate = new DateTime(2019, 09, 30);
            if (dtTime > expireDate)
            {
                MessageBox.Show("This beta has expired! Please get a new one on the forums.");
                Application.Current.Shutdown();
            }

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

        private static DateTime GetNetworkTime()
        {
            // default Windows time server
            const string ntpServer = "time.nist.gov";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            // Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            // The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            // NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        // stackoverflow.com/a/3294698/162671
        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}

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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using RestSharp;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Threading;

namespace TwitchDesktopNotifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public static List<channel> chans = new List<channel>();
        private void Ekle_Click(object sender, RoutedEventArgs e)
        {
            bool checkDup(string nm)
            {
                foreach(channel chanName in chans)
                {
                    if (chanName.username == nm)
                    {
                        return true;
                    }
                }
                return false;
            }
            if (textbox_username.Text == String.Empty)
            {
                MessageBox.Show("İsim boş olamaz!", "Uyarı");
                return;
            }
            else if (checkDup(textbox_username.Text))
            {
                textbox_username.Text = String.Empty;
                return;
            };
            channel ChanToAppend = new channel() { username = textbox_username.Text.ToLower(), isOnline = "Unknown"};
            isimler.Items.Add(ChanToAppend);
            chans.Add(ChanToAppend);
            textbox_username.Text = String.Empty;
        }

        public class channel : INotifyPropertyChanged
        {
            private string _isOnline;
            public string username { get; set; }
            public string isOnline
            {
                get { return _isOnline; }
                set
                {
                    if (_isOnline != value)
                    {
                        _isOnline = value;
                        if (isOnline == "Online")
                        {
                            MessageBox.Show(String.Format("{0} yaymaya başladı!", this.username), "Online Alert");
                        }

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isOnline"));
                    }
                }

            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private async void Dinle_Click(object sender, RoutedEventArgs e)
        {
            List<Task<(channel, bool)>> listenertasks = new List<Task<(channel, bool)>>();

            foreach (channel oneChan in chans)
            {
                listenertasks.Add(isOnline(oneChan));
            }
            var results = await Task.WhenAll(listenertasks);

            while (true)
            {
                foreach (var result in results)
                {
                    if (result.Item2)
                    {
                        result.Item1.isOnline = "Online";
                    }
                    else
                    {
                        result.Item1.isOnline = "Offline";
                    }
                }
                await Task.Run(() => Thread.Sleep(10));
            }
                
        }

        private async Task<(channel, bool)> isOnline(channel channel)
        {
            var client = new RestClient();
            var request = new RestRequest($"https://api.twitch.tv/kraken/users?login={channel.username}");
            request.AddHeader("Accept", "application/vnd.twitchtv.v5+json");
            request.AddHeader("Client-ID", "***CLIENT-ID***");
            var response = await client.ExecuteAsync(request);
            JsonDataModel jsonresponse = JsonConvert.DeserializeObject<JsonDataModel>(response.Content);
            if (jsonresponse._total == 0)
            {
               return (channel, false);
            }
            string _id = jsonresponse.users[0]["_id"];

            var request2 = new RestRequest($"https://api.twitch.tv/kraken/streams/{_id}");
            request2.AddHeader("Accept", "application/vnd.twitchtv.v5+json");
            request2.AddHeader("Client-ID", "***CLIENT-ID***");
            var response2 = await client.ExecuteAsync(request2);
            JsonDataModel2 jsonresponse2 = JsonConvert.DeserializeObject<JsonDataModel2>(response2.Content);
            if (jsonresponse2.stream == null)
            {
                return (channel, false);
            }
            else
            {
                return (channel, true);
            }
            
        }

        public class JsonDataModel
        {
            public int _total { get; set; }
            public List<Dictionary<string,string>> users { get; set; }
        }
        public class JsonDataModel2
        {
            public Dictionary<string, object> stream { get; set; }
        }
    }
}

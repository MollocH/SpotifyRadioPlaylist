using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpotifyAPI.SpotifyWebAPI;
using SpotifyAPI.SpotifyWebAPI.Models;
using SpotifyAPI.SpotifyLocalAPI;
using SpotifyEventHandler = SpotifyAPI.SpotifyLocalAPI.SpotifyEventHandler;

namespace SpotifyRadioPlaylist
{
    public partial class frmMain : Form
    {
        SpotifyLocalAPIClass spotifyLocal;
        SpotifyWebAPIClass spotifyWeb;
        SpotifyMusicHandler mh;
        SpotifyEventHandler eh;
        ImplicitGrantAuth auth;

        Boolean isAuth = false;
        String playlist = null;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            spotifyLocal = new SpotifyLocalAPIClass();

            /*
            if (!SpotifyLocalAPIClass.IsSpotifyWebHelperRunning())
            {
                DialogResult result = MessageBox.Show("Spotify Webhelper is not running. Should I start it for you ?", "Spotify Webhelper not running", MessageBoxButtons.YesNo);
                if(result == DialogResult.Yes)
                {
                    spotifyLocal.RunSpotifyWebHelper();
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            */
            
            if (!SpotifyLocalAPIClass.IsSpotifyRunning())
            {
                DialogResult result = MessageBox.Show("Spotify is not running. Should I start it for you ?", "Spotify not running", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    spotifyLocal.RunSpotify();
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            
            authentication();

            while(isAuth == false)
            {
                System.Threading.Thread.Sleep(300);
            }

            mh = spotifyLocal.GetMusicHandler();
            eh = spotifyLocal.GetEventHandler();
            eh.OnTrackChange += new SpotifyEventHandler.TrackChangeEventHandler(trackchange);

            populatePlaylist();
        }   

        private void authentication()
        {
            // Webauthentication
            auth = new ImplicitGrantAuth()
            {
                //Your client Id
                ClientId = "13d7960cb513461a85ee264b88b8f6b7",
                //Set this to localhost if you want to use the built-in HTTP Server
                RedirectUri = "http://localhost:43000",
                //How many permissions we need?
                Scope = Scope.USER_READ_PRIVATE | Scope.USER_READ_EMAIL | Scope.PLAYLIST_READ_PRIVATE | Scope.USER_LIBRARAY_READ | Scope.USER_LIBRARY_MODIFY | Scope.USER_READ_PRIVATE
                        | Scope.USER_FOLLOW_MODIFY | Scope.USER_FOLLOW_READ | Scope.PLAYLIST_MODIFY_PRIVATE | Scope.USER_READ_BIRTHDATE | Scope.PLAYLIST_MODIFY_PUBLIC | Scope.PLAYLIST_MODIFY_PRIVATE
            };

            //Start the internal http server
            auth.StartHttpServer(43000);
            //When we got our response
            auth.OnResponseReceivedEvent += auth_OnResponseReceivedEvent;
            //Start
            auth.DoAuth();
            // Local Authentication
        }

        public void auth_OnResponseReceivedEvent(Token token, string state, string error)
        {
            //stop the http server
            auth.StopHttpServer();

            if (error != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + error);
                //return;
            }
            spotifyWeb = new SpotifyWebAPIClass()
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType,
                UseAuth = true
            };
            
            spotifyLocal.Connect();



            this.isAuth = true;
        }

        public void trackchange(TrackChangeEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate {
                updateList(e);
            });
            List<String> l = new List<String>();
            l.Add(e.new_track.GetTrackURI());
            
            spotifyWeb.AddTracks(spotifyWeb.GetPublicProfile().Id, this.playlist, l); 
        }

        public void updateList(TrackChangeEventArgs e)
        {
            string[] row = { e.new_track.GetArtistName(), e.new_track.GetTrackURI()};
            System.Diagnostics.Debug.WriteLine(e.new_track.GetTrackURI());
            listView1.Items.Add(e.new_track.GetTrackName()).SubItems.AddRange(row);
        }
        
        public void populatePlaylist()
        {
            comboBox1.Items.Clear();
            Paging<SimplePlaylist> playlists = spotifyWeb.GetUserPlaylists(spotifyWeb.GetPublicProfile().Id);
            DataTable dataTable = new DataTable("");
            dataTable.Columns.Add("Value");
            dataTable.Columns.Add("Name");
            foreach (SimplePlaylist playlist in playlists.Items)
            {
                dataTable.Rows.Add(playlist.Id, playlist.Name);
            }
            comboBox1.DataSource = dataTable;
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Value";        
        }  

        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text == "Start") { 
                button1.Text = "Stop";
                DataRow selectedDataRow = ((DataRowView)comboBox1.SelectedItem).Row;
                this.playlist = selectedDataRow["Value"].ToString();
                eh.ListenForEvents(true);
            }
            else
            {
                button1.Text = "Start";
                eh.ListenForEvents(false);
            }

        }
        private void frmMain_Closed(Object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.IO;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using System.Xml;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace scoreboardManager
{
    /*
    reminder for in-app images/videos/content/etc
    Logo.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Scoreman Logo.png"));
    */
    public partial class Startup : Window
    {
        private string settingsPath = "";
        private string teamDatabasePath = "";
        private string studioURL = "http://studio.cbtg.us/scoreman/login.php"; //Username: MrTest Pass: Chrisdw1945!
        private string userDocumentPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/scoreman/";
        private int screenIndex; //which app screen [0 = login] [1 = event id] [2 = pick a sport]
        private bool settings;
        private bool[] databaseChanges = { true, true, true };
        private string currDatabaseVersion; //0.0.0
        private int carouselIndex; //[0 = Baseball] [1 = Football] [2 = Soccer]
        private bool selectedSport;
        Utils utils = new Utils();

        public Startup()
        {
            InitializeComponent();//XAML built in initalization
            AppInitalize();

            screenIndex = 0;
            UpdateUI(false);

            carouselIndex = 3;//starting index
            SportLeft2.IsEnabled = false;
            SportLeft1.IsEnabled = false;
            SportCenter.IsEnabled = true;
            SportRight1.IsEnabled = false;
            SportRight2.IsEnabled = false;
            CycleCarosuel(false);
        }
        private void AppInitalize()
        {
            //---------------Check connection
            if (utils.CheckInternetConnection(studioURL) == false)//change in the future, for offline use
                return;
            //---------------Check if the directory exists
            settingsPath = Directory.Exists(userDocumentPath) ? userDocumentPath + "scoreman.ini" : System.IO.Directory.CreateDirectory(userDocumentPath) + "scoreman.ini";
            //---------------Check if the scoreman.ini exists
            if (!File.Exists(settingsPath))
            {
                System.Windows.Forms.MessageBox.Show("Please Select a path containing the Team Database", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    DialogResult folderBrowserDialogResult = folderBrowserDialog.ShowDialog();

                    if (folderBrowserDialogResult == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
                        teamDatabasePath = folderBrowserDialog.SelectedPath;
                    else
                        System.Windows.Forms.MessageBox.Show("Team Data not found! Must be a root folder containing data and logos, Configure this under the options menu.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                using (StreamWriter f = File.CreateText(settingsPath))
                {
                    f.WriteLine(settingsPath);//path
                    f.WriteLine(teamDatabasePath);//team path
                    f.WriteLine(".");//username
                    f.WriteLine(".");//password
                    f.WriteLine("100");//databaseVersion
                }
                using (StreamWriter w = File.CreateText(teamDatabasePath + "/version.txt"))
                    w.WriteLine("000");
                databaseChanges[0] = false;
                databaseChanges[1] = true;
                databaseChanges[2] = false;
            }
            //--------------Read ini file
            string[] lines = File.ReadAllLines(settingsPath);// 0 = settingsPath, 1 = teamDatabasePath, 2 = username, 3 = password

            //if (lines[1] != null)
            //     System.Windows.MessageBox.Show("Team Data could not be found, please re-assign these paths");
            if (lines[1] != ".")
            {
                DatabasePath.Text = lines[1];
                databaseChanges[0] = true;
                databaseChanges[1] = false;
                databaseChanges[2] = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Team Data could not be found, please re-assign these paths", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                databaseChanges[0] = false;
                databaseChanges[1] = true;
                databaseChanges[2] = false;
            }

            try
            {
                if (lines[2] != "." || lines[3] != ".")
                {
                    Username.Text = lines[2];
                    Password.Password = lines[3];
                }
            }
            catch
            {
                Username.Text = "";
                Password.Password = "";
            }
            try { currDatabaseVersion = lines[4]; } catch { currDatabaseVersion = "Unknown"; }
        }
        private void UpdateTeamDatabase (string path)
        {
            if(File.Exists(path))
            {
                string data = File.ReadAllText(path);
                if(data.ToString() == currDatabaseVersion)
                    System.Windows.Forms.MessageBox.Show("Database is already up-to-date", "Update", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                {
                    DialogResult prompt = System.Windows.Forms.MessageBox.Show("Are you sure you want to Update?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (prompt == System.Windows.Forms.DialogResult.Yes)
                    {
                        databaseChanges[2] = false;
                    }
                    else
                    {
                        databaseChanges[2] = true;
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Version Identifer not found", "Update", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                using (StreamWriter w = File.CreateText(path))
                    w.WriteLine("000");
            }
        }
        private void ApplyChangesToINI()
        {
            using (StreamWriter writer = new StreamWriter(File.Open(settingsPath, FileMode.Truncate)))
            {
                writer.Flush();
                writer.WriteLine(settingsPath);
                writer.WriteLine(DatabasePath.Text);
                if (RememberLogin.IsChecked == true)
                {
                    writer.WriteLine(Username.Text);
                    writer.WriteLine(Password.Password);
                }
                writer.Close();
            }
        }
        private void ApplicationLogIn ()
        {
            if (utils.VerifyAccountOnServer(studioURL, Username.Text, Password.Password))
            {
                ApplyChangesToINI();
                screenIndex = 1;
                UpdateUI(false);
            }
            else
            {
                screenIndex = 0;
                UpdateUI(false);
            }
        }
        private void CycleCarosuel (bool goingRight)
        {
            System.Windows.Controls.Image[] sportsImages = { SportLeft2_Image, SportLeft1_Image, SportCenter_Image, SportRight1_Image, SportRight2_Image };

            carouselIndex = goingRight ? carouselIndex += 1 : carouselIndex -= 1;
            carouselIndex = (carouselIndex < 0) ? 0 : ((carouselIndex > 7) ? 7 : carouselIndex);

            if (carouselIndex == 0)
            {
                sportsImages[0].Source = null;
                sportsImages[1].Source = null;
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Baseball-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Basketball-512x512.png"));
                sportsImages[4].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Football-512x512.png"));
            }
            else if (carouselIndex == 1)
            {
                sportsImages[0].Source = null;
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Baseball-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Basketball-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Football-512x512.png"));
                sportsImages[4].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Soccer-512x512.png"));
            }
            else if (carouselIndex == 2)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Baseball-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Basketball-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Football-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Soccer-512x512.png"));
                sportsImages[4].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Softball-512x512.png"));
            }
            else if (carouselIndex == 3)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Basketball-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Football-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Soccer-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Softball-512x512.png"));
                sportsImages[4].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Tennis-512x512.png"));
            }
            else if (carouselIndex == 4)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Football-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Soccer-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Softball-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Tennis-512x512.png"));
                sportsImages[4].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Vollyball-512x512.png"));
            }
            else if (carouselIndex == 5)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Soccer-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Softball-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Tennis-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Vollyball-512x512.png"));
                sportsImages[4].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Wrestling-512x512.png"));
            }
            else if (carouselIndex == 6)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Softball-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Tennis-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Vollyball-512x512.png"));
                sportsImages[3].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Wrestling-512x512.png"));
                sportsImages[4].Source = null;
            }
            else if (carouselIndex == 7)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Tennis-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Vollyball-512x512.png"));
                sportsImages[2].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Wrestling-512x512.png"));
                sportsImages[3].Source = null;
                sportsImages[4].Source = null;
            }
            else if (carouselIndex == 8)
            {
                sportsImages[0].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Vollyball-512x512.png"));
                sportsImages[1].Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Wrestling-512x512.png"));
                sportsImages[2].Source = null;
                sportsImages[3].Source = null;
                sportsImages[4].Source = null;
            }
        }
        private void UpdateUI (bool atSettingsScreen)
        {
            if (!atSettingsScreen)
            {
                if (screenIndex == 0)//login
                {
                    Login_Screen.Visibility = Visibility.Visible;
                    EventID_Screen.Visibility = Visibility.Hidden;
                    SportSelection_Screen.Visibility = Visibility.Hidden;
                    Settings_Screen.Visibility = Visibility.Hidden;

                    FirstCircle.Opacity = 1.0;
                    MiddleCircle.Opacity = 0.25;
                    LastCircle.Opacity = 0.25;
                }
                else if (screenIndex == 1)//event id
                {
                    Login_Screen.Visibility = Visibility.Hidden;
                    EventID_Screen.Visibility = Visibility.Visible;
                    SportSelection_Screen.Visibility = Visibility.Hidden;
                    Settings_Screen.Visibility = Visibility.Hidden;

                    FirstCircle.Opacity = 0.25;
                    MiddleCircle.Opacity = 1.0;
                    LastCircle.Opacity = 0.25;
                }
                else//sport selection
                {
                    Login_Screen.Visibility = Visibility.Hidden;
                    EventID_Screen.Visibility = Visibility.Hidden;
                    SportSelection_Screen.Visibility = Visibility.Visible;
                    Settings_Screen.Visibility = Visibility.Hidden;

                    FirstCircle.Opacity = 0.25;
                    MiddleCircle.Opacity = 0.25;
                    LastCircle.Opacity = 1.0;
                }
            }
            else
            {
                Login_Screen.Visibility = Visibility.Hidden;
                EventID_Screen.Visibility = Visibility.Hidden;
                SportSelection_Screen.Visibility = Visibility.Hidden;
                Settings_Screen.Visibility = Visibility.Visible;

                FirstCircle.Opacity = 0.25;
                MiddleCircle.Opacity = 0.25;
                LastCircle.Opacity = 0.25;
            }
            //------database settings
            RemoveDatabase.Opacity = databaseChanges[0] ? 1.0f : 0.25f;
            RemoveDatabase.IsEnabled = databaseChanges[0] ? true : false;
            DownloadDatabase.Opacity = databaseChanges[1] ? 1.0f : 0.25f;
            DownloadDatabase.IsEnabled = databaseChanges[1] ? true : false;
            UpdateDatabase.Opacity = databaseChanges[2] ? 1.0f : 0.25f;
            UpdateDatabase.IsEnabled = databaseChanges[2] ? true : false;
            DatabaseIdentifer.Content = utils.ValidateDatabasePath(teamDatabasePath + "/") ? "Offline Database Linked : Version " + currDatabaseVersion : "Offline Database Not Found : Version " + currDatabaseVersion;
        }
        //------------LINK OUR FUNCTIONS TO XAML FUNCTIONS--------------
        protected override void OnClosing(CancelEventArgs e)
        {
            utils.CloseWindow(e, true, "", selectedSport);
        }
        private void Browse_TeamData_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fb1 = new FolderBrowserDialog();
            DialogResult r1 = fb1.ShowDialog();

            if (r1 == System.Windows.Forms.DialogResult.Cancel)
                DatabasePath.Text = DatabasePath.Text;
            else if (r1 == System.Windows.Forms.DialogResult.OK)
            {
                if (utils.ValidateDatabasePath(fb1.SelectedPath))
                {
                    DatabasePath.Text = fb1.SelectedPath;
                    ApplyChangesToINI();
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Database could not be validated! Please check the path for any misspelled or missing folders. Must contain 'data' and 'logos'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            UpdateUI(true);
        }
        private void EventID_Next(object sender, RoutedEventArgs e)
        {
            if (EventID.Text == "Event ID")
                System.Windows.Forms.MessageBox.Show("Enter an Event ID", "Event ID", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else
            {
                screenIndex = 2;
                UpdateUI(false);
            }
        }
        private void EventID_Offline(object sender, RoutedEventArgs e)
        {
            screenIndex = 2;
            UpdateUI(false);
        }
        private void RemoveDatabase_Click(object sender, RoutedEventArgs e)
        {
            DialogResult prompt = System.Windows.Forms.MessageBox.Show("Are you sure you want to remove the Offline Team Database?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if(prompt == System.Windows.Forms.DialogResult.Yes)
            {
                databaseChanges[0] = false;
                databaseChanges[1] = true;
                databaseChanges[2] = false;

                //Directory.Delete(teamDatabasePath, true);
                //ApplyChangesToINI();

                UpdateUI(true);
            }
        }
        private void OnPressEnter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (screenIndex == 0)
                    ApplicationLogIn();
            }
        }
        private void DownloadDatabase_Click(object sender, RoutedEventArgs e)
        {
            databaseChanges[0] = true;
            databaseChanges[1] = false;
            databaseChanges[2] = true;
            UpdateUI(true);
        }
        private void UpdateDatabase_Click(object sender, RoutedEventArgs e)
        {
            UpdateTeamDatabase(teamDatabasePath + "/version.txt");
            UpdateUI(true);
        }
        private void Ellipse_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Ellipse elp = sender as Ellipse;
            elp.Fill = new SolidColorBrush(Colors.White);
        }
        private void Ellipse_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Ellipse elp = sender as Ellipse;
            elp.Fill = new SolidColorBrush(Colors.Black);
        }
        private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button b = sender as System.Windows.Controls.Button;
            b.Foreground = new SolidColorBrush(Colors.Black);
        }
        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button b = sender as System.Windows.Controls.Button;
            b.Foreground = new SolidColorBrush(Colors.White);
        }
        private void StartScoring_Click(object sender, RoutedEventArgs e)
        {
            //if (utils.ValidateDatabasePath(teamDatabasePath))
            //{
                if (carouselIndex == 0)
                {
                    selectedSport = true;
                    Baseball b = new Baseball();
                    b.Show();
                    Close();
                }
                else if (carouselIndex == 1)
                {

                }
                else if (carouselIndex == 2)
                {
                    selectedSport = true;
                    Football f = new Football();
                    f.Show();
                    Close();
                }
                else if (carouselIndex == 3)
                {
                    selectedSport = true;
                    Soccer s = new Soccer();
                    s.Show();
                    Close();
                }
                else if (carouselIndex == 4)
                {
                    selectedSport = true;
                    Baseball b = new Baseball();
                    b.Show();
                    Close();
                }
                else if (carouselIndex == 5)
                {

                }
                else if (carouselIndex == 6)
                {

                }
                else if (carouselIndex == 7)
                {

                }
            //}
            //else
            //{
                //System.Windows.MessageBox.Show("Team Database Path is missing!");
                //UpdateUI(true);
           // }
        }
        private void LogOn_Click(object sender, RoutedEventArgs e) { ApplicationLogIn(); }
        private void SettingsButt_Click(object sender, RoutedEventArgs e) { settings = !settings; UpdateUI(settings); }
        private void MiddleCircle_MouseDown(object sender, MouseButtonEventArgs e) { screenIndex = 1; UpdateUI(false); }
        private void FirstCircle_MouseDown(object sender, MouseButtonEventArgs e) { screenIndex = 0; UpdateUI(false); }
        private void LastCircle_MouseDown(object sender, MouseButtonEventArgs e) { screenIndex = 2; UpdateUI(false); }
        private void ArrowRight_Click(object sender, RoutedEventArgs e) { CycleCarosuel(true); }
        private void ArrowLeft_Click(object sender, RoutedEventArgs e) { CycleCarosuel(false); }
    }
}
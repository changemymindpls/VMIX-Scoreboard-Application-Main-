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
    public partial class Soccer : Window
    {
        private DispatcherTimer countdownDispatchTimer;

        private int countdown = 2400;//40 minutes = 2400 secconds
        private int team1Score = 0;
        private int team2Score = 0;

        private bool playAds;
        private bool stopCountdown;
        private bool playAnnoucement;
        private bool team1Bonus;
        private bool team2Bonus;
        private bool teamPosession; //0 = team 1, 1 = team 2

        private string currTeam1Icon = "";
        private string currTeam1Col = "";
        private string currTeam2Icon = "";
        private string currTeam2Col = "";
        private string currTeam1Name = "";
        private string currTeam2Name = "";

        //---------PATHS
        private string databasePath = "";
        private string databaseTeamDataPath = "";
        private string databaseTeamImgPath = "";
        private string appSettingsPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/scoreman/scoreman.ini";
        private string tempScoreDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/scoreman/soccer_score.txt";
        private List<String> teamNames = new List<string>();
        private string[] teamPaths;

        //---------TEMP VALUES
        private int[] team1Timeouts = { 1, 1, 1, 1, 1 };
        private int[] team2Timeouts = { 1, 1, 1, 1, 1 };
        Utils utils = new Utils();

        public Soccer()
        {
            PreInitalization();
            InitializeComponent();//XAML Initalization
            InitalizeTeamData();
            UpdateTempScoreData();

            countdownDispatchTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            countdownDispatchTimer.Tick += UpdateCountdownTimer;
            countdownDispatchTimer.Start();

            TimerButton(true, false);
        }
        private void PreInitalization()
        {
            using (StreamWriter f = File.CreateText(tempScoreDataPath)) //write up the temporary score data .txt
            {
                f.WriteLine("0");//team1 score
                f.WriteLine("0");//team2 score
                f.WriteLine("0");//timer ('0' = stop, '1' = start, '2' = reset, 'c=' = custom)
                f.WriteLine("0");//possesion (0 = team 1, 1 = team 2)
                f.WriteLine("1");//quarter (1 = 1st, 2 = 2nd, 3 = 3rd, 4 = 4th, Overtime = 0)
                f.WriteLine("0");//announcement on (0 = off, 1 = on)
                f.WriteLine("1");//announcement type (0 = timeout, 1 = halftime, 2 = penalty)
                f.WriteLine("#ffffff");//team1 col
                f.WriteLine("#ffffff");//team2 col
                f.WriteLine("");//team1 icon path
                f.WriteLine("");//team2 icon path
                f.WriteLine("Team 1");//team1 name
                f.WriteLine("Team 2");//team2 name
                f.WriteLine("11111");//team1 timeouts
                f.WriteLine("11111");//team2 timeouts
                f.WriteLine("0");//team1 wins
                f.WriteLine("0");//team1 loss
                f.WriteLine("0");//team2 wins
                f.WriteLine("0");//team2 loss
                f.WriteLine("0");//team1 bonus
                f.WriteLine("0");//team2 bonus
            }
            using (StreamReader sr = new StreamReader(appSettingsPath))
            {
                string firstLine = sr.ReadLine();
                string seccondLine = sr.ReadLine();

                if (seccondLine != " ")
                    databasePath = seccondLine + "/";

                databaseTeamDataPath = databasePath + "data/";
                databaseTeamImgPath = databasePath + "logos/";
            }
        }
        private void UpdateTempScoreData()
        {
            if (utils.IsFileUsedByOtherProcess(tempScoreDataPath))
                return;
            else
            {
                //----------------------TIMEOUTS
                try
                {
                    System.Windows.Controls.CheckBox[] team1TimeoutsCheckboxes = { Team1_Timeout1, Team1_Timeout2, Team1_Timeout3, Team1_Timeout4, Team1_Timeout5 };
                    System.Windows.Controls.CheckBox[] team2TimeoutsCheckboxes = { Team2_Timeout1, Team2_Timeout2, Team2_Timeout3, Team2_Timeout4, Team2_Timeout5 };
                    for (int i = 0; i < team1TimeoutsCheckboxes.Length; i++)
                    {
                        team1Timeouts[i] = team1TimeoutsCheckboxes[i].IsChecked == true ? 1 : 0;
                    }
                    for (int i = 0; i < team2TimeoutsCheckboxes.Length; i++)
                    {
                        team2Timeouts[i] = team2TimeoutsCheckboxes[i].IsChecked == true ? 1 : 0;
                    }

                    string team1Timeout = team1Timeouts[0].ToString() + team1Timeouts[1].ToString() + team1Timeouts[2].ToString() + team1Timeouts[3].ToString() + team1Timeouts[4].ToString();
                    string team2Timeout = team2Timeouts[0].ToString() + team2Timeouts[1].ToString() + team2Timeouts[2].ToString() + team2Timeouts[3].ToString() + team2Timeouts[4].ToString();

                    string[] lines = { team1Score.ToString(), team2Score.ToString(), Timer_Counter.Content.ToString(), teamPosession.ToString(), Quarter.SelectedItem.ToString(), playAnnoucement.ToString(), Announcments.SelectedItem.ToString(), currTeam1Col, currTeam2Col, databaseTeamImgPath + currTeam1Icon, databaseTeamImgPath + currTeam2Icon, currTeam1Name, currTeam2Name, team1Timeout, team2Timeout, Team1_Record_Wins.Content.ToString(), Team1_Record_Losses.Content.ToString(), Team2_Record_Wins.Content.ToString(), Team2_Record_Losses.Content.ToString(), team1Bonus.ToString(), team2Bonus.ToString() };
                    File.WriteAllLines(tempScoreDataPath, lines);
                } catch { }
            }
        }
        private void UpdateCountdownTimer(object sender, EventArgs e) //UPDATE FUNCTION, EXECUTES EVERY 1 SECCOND
        {
            if (!stopCountdown)
            {
                countdown = (countdown < 0) ? 0 : ((countdown > 2400) ? 2400 : countdown);
                if (countdown != 0)
                {
                    countdown--;
                    Timer_Counter.Content = countdown % 60 < 10 ? string.Format("{0}:0{1}", countdown / 60, countdown % 60) : Timer_Counter.Content = string.Format("{0}:{1}", countdown / 60, countdown % 60);
                }
            }
            UpdateTempScoreData();
        }
        private void InitalizeTeamData() //INITALIZING BOTH TEAM DROPDOWN MENUS WITH NAMES AND PATHS
        {
            int amountOfTeams = Directory.GetFiles(databaseTeamDataPath).Length;
            teamPaths = Directory.GetFiles(databaseTeamDataPath);

            for (int i = 0; i < amountOfTeams; i++)
            {
                teamNames.Add(System.IO.Path.GetFileNameWithoutExtension(teamPaths[i]));
                Team1_Dropdown.Items.Add(teamNames[i]);
                Team2_Dropdown.Items.Add(teamNames[i]);
            }

            //Sets some default values when you start the app
            GetTeamInfo(0, false);
            GetTeamInfo(1, true);

            Team1_Dropdown.SelectedIndex = 0;
            Team2_Dropdown.SelectedIndex = 1;
        }
        private void GetTeamInfo(int index, bool isSeccondTeam) //READING TEAM DATA
        {
            using (var reader = new StreamReader(teamPaths[index])) //reading the team data document
            {
                if (!isSeccondTeam)
                {
                    currTeam1Name = reader.ReadLine();//get team name
                    currTeam1Col = reader.ReadLine();//get color
                    currTeam1Icon = reader.ReadLine();//get image name
                    if (currTeam1Name == null)
                        currTeam1Name = "Team 1";
                    if (currTeam1Col == null)
                        currTeam1Col = "#ffffff";
                    if (currTeam2Icon == null)
                        currTeam1Icon = "";
                }
                else
                {
                    currTeam2Name = reader.ReadLine();//get team name
                    currTeam2Col = reader.ReadLine();//get color
                    currTeam2Icon = reader.ReadLine();//get image name
                    if (currTeam2Name == null)
                        currTeam2Name = "Team 2";
                    if (currTeam2Col == null)
                        currTeam2Col = "#ffffff";
                    if (currTeam2Icon == null)
                        currTeam2Icon = "";
                }
            }
            if (!isSeccondTeam)
            {
                //set team color
                SolidColorBrush fTeam1Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(currTeam1Col));
                Team1_Box.Fill = fTeam1Color;
                ImageBoxT1.Fill = fTeam1Color;
                Team1_Score.Background = fTeam1Color;
                Team1_TimeoutBox.Fill = fTeam1Color;
                Team1_Scorebox.Fill = fTeam1Color;
                Team1_RecordBox.Fill = fTeam1Color;

                //set team icon
                string team1IconPath = databaseTeamImgPath + currTeam1Icon;
                if (File.Exists(team1IconPath))
                    Team1_Icon.Source = new BitmapImage(new Uri(databaseTeamImgPath + currTeam1Icon, UriKind.RelativeOrAbsolute));
                else
                {
                    System.Windows.Forms.MessageBox.Show("Team 1 Logo could not be found!");
                    currTeam1Icon = "";
                }
            }
            else
            {
                //set team color
                SolidColorBrush fTeam2Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(currTeam2Col));
                Team2_Box.Fill = fTeam2Color;
                Team2_IconBox.Fill = fTeam2Color;
                Team2_Score.Background = fTeam2Color;
                Team2_TimeoutBox.Fill = fTeam2Color;
                Team2_Scorebox.Fill = fTeam2Color;
                Team2_RecordBox.Fill = fTeam2Color;

                //set team icon
                string team2IconPath = databaseTeamImgPath + currTeam2Icon;
                if (File.Exists(team2IconPath))
                    Team2_Icon.Source = new BitmapImage(new Uri(databaseTeamImgPath + currTeam2Icon, UriKind.RelativeOrAbsolute));
                else
                {
                    System.Windows.Forms.MessageBox.Show("Team 2 Logo could not be found! Check Database.");
                    currTeam2Icon = "";
                }
            }
        }
        private void UpdateScore(bool isSeccondTeam, bool subtracting, int value)
        {
            if (isSeccondTeam)
            {
                team2Score = subtracting ? team2Score -= value : team2Score += value;
                team2Score = (team2Score < 0) ? 0 : ((team2Score > 99) ? 99 : team2Score);
                Team2_Score.HorizontalContentAlignment = team2Score > 99 ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Center;
                Team2_Score.Content = team2Score.ToString();
            }
            else
            {
                team1Score = subtracting ? team1Score -= value : team1Score += value;
                team1Score = (team1Score < 0) ? 0 : ((team1Score > 99) ? 99 : team1Score);
                Team1_Score.HorizontalContentAlignment = team1Score > 99 ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Center;
                Team1_Score.Content = team1Score.ToString();
            }

            UpdateTempScoreData();
        }
        private void TeamPosession(bool isSeccondTeam)//0 = team 1 has possesion, 1 = team 2 has posession
        {
            if (utils.Toggle(ref teamPosession))
            {
                if (!isSeccondTeam)
                {
                    Team1_Pos.IsChecked = true;
                    Team2_Pos.IsChecked = false;
                    teamPosession = false;
                }
            }
            else
            {
                if (isSeccondTeam)
                {
                    Team1_Pos.IsChecked = false;
                    Team2_Pos.IsChecked = true;
                    teamPosession = true;
                }
            }
        }
        private void TimerButton(bool reset, bool newTime) //0 = stop, 1 = start, 2 = reset
        {
            if (reset)
            {
                stopCountdown = true;
                TimerStart.Content = "Start";
                countdown = 2400;//reset to 8 minutes
                Timer_Counter.Content = countdown % 60 < 10 ? string.Format("{0}:0{1}", countdown / 60, countdown % 60) : string.Format("{0}:{1}", countdown / 60, countdown % 60);
            }
            else
            {
                TimerStart.Content = utils.Toggle(ref stopCountdown) ? "Start" : "Stop";
            }
            if (newTime)
            {
                stopCountdown = true;
                TimerStart.Content = "Start";

                string countdownString;
                string editResult = "";
                int secconds = 0;
                int minutes = 0;

                countdownString = countdown % 60 < 10 ? string.Format("{0}0{1}", countdown / 60, countdown % 60) : string.Format("{0}{1}", countdown / 60, countdown % 60);
                //try because if the user hits cancel on the input dialog, it will crash the application because edit result = null
                try
                {
                    editResult = Interaction.InputBox("Change countdown value, EXAMPLE 730 = 7:30", "Edit Countdown", countdownString);
                    Timer_Counter.Content = (countdown < 600) && (editResult != "") ? string.Format("{0}:{1}", editResult.Remove(1, 2).ToString(), editResult.Remove(0, 1).ToString()) : string.Format("{0}:{1}", editResult.Remove(2, 2).ToString(), editResult.Remove(0, 2).ToString());

                    if (countdown < 600 && editResult != "")//10 mins
                    {
                        minutes = Int32.Parse(editResult.Remove(1, 2));
                        secconds = Int32.Parse(editResult.Remove(0, 1));
                    }
                    else
                    {
                        minutes = Int32.Parse(editResult.Remove(2, 2));
                        secconds = Int32.Parse(editResult.Remove(0, 2));
                    }
                    countdown = (minutes * 60) + secconds;
                    TimerStart.Content = "Start";
                } catch { }
            }
            UpdateTempScoreData();
        }
        private void Bonus(bool isSeccondTeam)
        {
            if (!isSeccondTeam)
            {
                Team1_Bonus.Background = utils.Toggle(ref team1Bonus) ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.White);
            }
            else
            {
                Team2_Bonus.Background = utils.Toggle(ref team2Bonus) ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.White);
            }
            UpdateTempScoreData();
        }
        //--------------------XAML FUNCTIONS (mostly just linking our functions to the xaml functions)---------------------
        protected override void OnClosing(CancelEventArgs e)
        {
            utils.CloseWindow(e, false, tempScoreDataPath);
        }
        private void PlayAds_Click(object sender, RoutedEventArgs e)
        {
            PlayAds.Content = utils.Toggle(ref playAds) ? "Stop Ads" : "Play Ads";
        }
        private void Announcement_Player_Click(object sender, RoutedEventArgs e)
        {
            if (utils.Toggle(ref playAnnoucement))
            {
                Announcement_Player.Content = "Stop Announcement";
                Announcments.Background = new SolidColorBrush(Colors.Green);
            }
            else
            {
                Announcement_Player.Content = "Play Announcement";
                Announcments.Background = new SolidColorBrush(Colors.White);
            }
            UpdateTempScoreData();
        }
        private void Team1_Record_Wins_TextChanged(object sender, MouseButtonEventArgs e)
        {
            Team1_Record_Wins.Content = utils.EditTextPrompt(Team1_Record_Wins.Content, "Change Team 1 Record Wins", "Edit Record Wins");
            UpdateTempScoreData();
        }
        private void Team1_Record_Losses_TextChanged(object sender, MouseButtonEventArgs e)
        {
            Team1_Record_Losses.Content = utils.EditTextPrompt(Team1_Record_Losses.Content, "Change Team 1 Record Losses", "Edit Record Losses");
            UpdateTempScoreData();
        }
        private void Team2_Record_Wins_TextChanged(object sender, MouseButtonEventArgs e)
        {
            Team2_Record_Wins.Content = utils.EditTextPrompt(Team2_Record_Wins.Content, "Change Team 2 Record Wins", "Edit Record Wins");
            UpdateTempScoreData();
        }
        private void Team2_Record_Losses_TextChanged(object sender, MouseButtonEventArgs e)
        {
            Team2_Record_Losses.Content = utils.EditTextPrompt(Team2_Record_Losses.Content, "Change Team 2 Record Losses", "Edit Record Losses");
            UpdateTempScoreData();
        }
        private void TimerStart_Click(object sender, RoutedEventArgs e) { TimerButton(false, false); } //STARTS THE COUNTDOWN TIMER
        private void TimerReset_Click(object sender, RoutedEventArgs e) { TimerButton(true, false); } //RESETS THE COUNTDOWN TIMER
        private void Timer_Countdown_MouseDoubleClick(object sender, MouseButtonEventArgs e) { TimerButton(false, true); } //CUSTOM COUNTDOWN TIMER VALUE
        private void team1_add1_Click(object sender, RoutedEventArgs e) { UpdateScore(false, false, 1); }
        private void team1_add2_Click(object sender, RoutedEventArgs e) { UpdateScore(false, false, 2); }
        private void team1_add3_Click(object sender, RoutedEventArgs e) { UpdateScore(false, false, 3); }
        private void team1_sub1_Click(object sender, RoutedEventArgs e) { UpdateScore(false, true, 1); }
        private void team1_sub2_Click(object sender, RoutedEventArgs e) { UpdateScore(false, true, 2); }
        private void team1_sub3_Click(object sender, RoutedEventArgs e) { UpdateScore(false, true, 3); }
        private void team2_add1_Click(object sender, RoutedEventArgs e) { UpdateScore(true, false, 1); }
        private void team2_add2_Click(object sender, RoutedEventArgs e) { UpdateScore(true, false, 2); }
        private void team2_add3_Click(object sender, RoutedEventArgs e) { UpdateScore(true, false, 3); }
        private void team2_sub1_Click(object sender, RoutedEventArgs e) { UpdateScore(true, true, 1); }
        private void team2_sub2_Click(object sender, RoutedEventArgs e) { UpdateScore(true, true, 2); }
        private void team2_sub3_Click(object sender, RoutedEventArgs e) { UpdateScore(true, true, 3); }
        private void Dropdown1_SelectionChanged(object sender, SelectionChangedEventArgs e) { GetTeamInfo(Team1_Dropdown.SelectedIndex, false); UpdateTempScoreData(); }
        private void Dropdown2_SelectionChanged(object sender, SelectionChangedEventArgs e) { GetTeamInfo(Team2_Dropdown.SelectedIndex, true); UpdateTempScoreData(); }
        private void Quarter_SelectionChanged(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Announcments_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateTempScoreData(); }
        private void Team1_Pos_Checked(object sender, RoutedEventArgs e) { TeamPosession(false); UpdateTempScoreData(); }
        private void Team2_Pos_Checked(object sender, RoutedEventArgs e) { TeamPosession(true); UpdateTempScoreData(); }
        private void Team1_Timeout1_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team1_Timeout2_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team1_Timeout3_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team1_Timeout4_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team1_Timeout5_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team2_Timeout1_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team2_Timeout2_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team2_Timeout3_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team2_Timeout4_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team2_Timeout5_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void Team1_Bonus_Click(object sender, RoutedEventArgs e) { Bonus(false); }
        private void Team2_Bonus_Click(object sender, RoutedEventArgs e) { Bonus(true); }
        private void Text_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button b = sender as System.Windows.Controls.Button;
            b.Foreground = new SolidColorBrush(Colors.Black);
        }
        private void Text_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button b = sender as System.Windows.Controls.Button;
            b.Foreground = new SolidColorBrush(Colors.White);
        }
    }
}
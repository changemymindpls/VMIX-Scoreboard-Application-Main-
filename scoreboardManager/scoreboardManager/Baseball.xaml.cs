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
    public partial class Baseball : Window
    {
        private int team1Score = 0;
        private int team2Score = 0;
        private bool playAds = false;
        private bool teamPosession = false;

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
        private string tempScoreDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/scoreman/baseball_score.txt";
        private List<String> teamNames = new List<string>();
        private string[] teamPaths;
        private string bases;
        int[] b = { 0, 0, 0, 0 };
        Utils utils = new Utils();

        public Baseball()
        {
            PreInitalization();
            InitializeComponent();//XAML Initalization
            InitalizeTeamData();
            UpdateTempScoreData();
        }
        private void PreInitalization()
        {
            using (StreamWriter f = File.CreateText(tempScoreDataPath)) //write up the temporary score data .txt
            {
                f.WriteLine("0");//team1 score
                f.WriteLine("0");//team2 score
                f.WriteLine("0");//possesion (0 = team 1, 1 = team 2)
                f.WriteLine("1");//innings
                f.WriteLine("#ffffff");//team1 col
                f.WriteLine("#ffffff");//team2 col
                f.WriteLine("");//team1 icon path
                f.WriteLine("");//team2 icon path
                f.WriteLine("Team 1");//team1 name
                f.WriteLine("Team 2");//team2 name
                f.WriteLine("0");//outs
                f.WriteLine("0");//count
                f.WriteLine("0");//team1 wins
                f.WriteLine("0");//team1 loss
                f.WriteLine("0");//team2 wins
                f.WriteLine("0");//team2 loss
                f.WriteLine("0000");//bases
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
                try
                {
                    b[0] = Base1.IsChecked == true ? 1 : 0;
                    b[1] = Base2.IsChecked == true ? 1 : 0;
                    b[2] = Base3.IsChecked == true ? 1 : 0;
                    b[3] = Base4.IsChecked == true ? 1 : 0;
                    bases = b[0].ToString() + b[1].ToString() + b[2].ToString() + b[3].ToString();

                    string[] lines = { team1Score.ToString(), team2Score.ToString(), teamPosession.ToString(), Innings.SelectedItem.ToString(), currTeam1Col, currTeam2Col, databaseTeamImgPath + currTeam1Icon, databaseTeamImgPath + currTeam2Icon, currTeam1Name, currTeam2Name, Outs.Content.ToString(), Count.Content.ToString(), Team1_Record_Wins.Content.ToString(), Team1_Record_Losses.Content.ToString(), Team2_Record_Wins.Content.ToString(), Team2_Record_Losses.Content.ToString(), bases };
                    File.WriteAllLines(tempScoreDataPath, lines);
                } catch { }
            }
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
                Team2_Score.Content = team2Score.ToString();
            }
            else
            {
                team1Score = subtracting ? team1Score -= value : team1Score += value;
                team1Score = (team1Score < 0) ? 0 : ((team1Score > 99) ? 99 : team1Score);
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
        //--------------------XAML FUNCTIONS (mostly just linking our functions to the xaml functions)---------------------
        protected override void OnClosing(CancelEventArgs e)
        {
            utils.CloseWindow(e, false, tempScoreDataPath);
        }
        private void PlayAds_Click(object sender, RoutedEventArgs e)
        {
            PlayAds.Content = utils.Toggle(ref playAds) ? "Stop Ads" : "Play Ads";
        }
        private void Outs_TextChanged(object sender, MouseButtonEventArgs e)
        {
            Outs.Content = utils.EditTextPrompt(Outs.Content, "Change Outs", "Edit Outs");
            UpdateTempScoreData();
        }
        private void Count_TextChanged(object sender, MouseButtonEventArgs e)
        {
            Count.Content = utils.EditTextPrompt(Count.Content, "Change Count", "Edit Count");
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
        private void team1_add1_Click(object sender, RoutedEventArgs e) { UpdateScore(false, false, 1); }
        private void team1_sub1_Click(object sender, RoutedEventArgs e) { UpdateScore(false, true, 1); }
        private void team2_add1_Click(object sender, RoutedEventArgs e) { UpdateScore(true, false, 1); }
        private void team2_sub1_Click(object sender, RoutedEventArgs e) { UpdateScore(true, true, 1); }
        private void Dropdown1_SelectionChanged(object sender, SelectionChangedEventArgs e) { GetTeamInfo(Team1_Dropdown.SelectedIndex, false); UpdateTempScoreData(); }
        private void Dropdown2_SelectionChanged(object sender, SelectionChangedEventArgs e) { GetTeamInfo(Team2_Dropdown.SelectedIndex, true); UpdateTempScoreData(); }
        private void Innings_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateTempScoreData(); }
        private void Team2_Pos_Checked(object sender, RoutedEventArgs e) { TeamPosession(true); UpdateTempScoreData(); }
        private void Team1_Pos_Checked(object sender, RoutedEventArgs e) { TeamPosession(false); UpdateTempScoreData(); }
        private void CheckBox_Checked(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void CheckBox_Checked_2(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
        private void CheckBox_Checked_3(object sender, RoutedEventArgs e) { UpdateTempScoreData(); }
    }
}
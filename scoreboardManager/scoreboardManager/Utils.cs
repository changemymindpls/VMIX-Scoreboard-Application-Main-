using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.ComponentModel;
using Microsoft.VisualBasic;

namespace scoreboardManager
{
    public class Utils //functions use multiple times, or duplicate functions usually go in here
    {
        private string[] messages = { "Missing Password", "Missing Username", "Wrong Password", "Wrong Username", "Database error, contact your administrator", "Login Sucessful" };
        public bool ValidateDatabasePath(string path)
        {
            return Directory.Exists(path + "/data") ? true : false && Directory.Exists(path + "/logos") ? true : false;
        }
        public bool Toggle(ref bool b)
        {
            b = !b;
            return b;
        }
        public object EditTextPrompt(object obj, string prompt, string title, bool editExistingText = true)
        {
            string storePrompt = obj.ToString();
            string response = Interaction.InputBox(prompt, title, editExistingText ? obj.ToString() : "");
            return response == "" ? obj : response;
        }
        public bool CheckInternetConnection(string url)
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead(url))
                    return true;
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Unable to establish connection with server, please check your internet connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        public bool IsFileUsedByOtherProcess(string path)
        {
            bool Locked = false;
            try
            {
                FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                fs.Close();
            }
            catch
            {
                Locked = true;
            }
            return Locked;
        }
        public void CloseWindow (CancelEventArgs e, bool isStartup, string tempScoreDataPath = "", bool selectedSport = false)
        {
            if (!isStartup)
            {
                DialogResult prompt = System.Windows.Forms.MessageBox.Show("Do you want to return to the Main Menu?", "Quit Application", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (prompt == System.Windows.Forms.DialogResult.Yes)
                {
                    File.Delete(tempScoreDataPath);
                    Startup startup = new Startup();
                    startup.Show();
                    e.Cancel = false;
                }
                else if (prompt == System.Windows.Forms.DialogResult.No)
                {
                    File.Delete(tempScoreDataPath);
                    System.Windows.Application.Current.Shutdown();
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                if (!selectedSport)
                    System.Windows.Application.Current.Shutdown();
                e.Cancel = false;
            }
        }
        public bool VerifyAccountOnServer(string url, string user, string pass)
        {
            WebRequest request = WebRequest.Create(url);
            string postData = "user_name=" + user;
            postData += "&user_password=" + pass;
            byte[] data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd(); //server reponse format = [num], [username], [apikey]
            string msg = messages[Int32.Parse(responseString.Remove(1, responseString.Length - 1))].ToString();

            if (responseString.Remove(1, responseString.Length - 1) != "5")
                System.Windows.Forms.MessageBox.Show(msg);

            return responseString.Remove(1, responseString.Length - 1) == "5" ? true : false;
        }
    }
}

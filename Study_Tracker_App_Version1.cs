// StudyTracker Version 1.0
// Features: Log study sessions, basic input validation, save/load from file

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Study_Tracker_App
{
    public partial class Form1 : Form
    {
        private List<string> sessions = new List<string>();
        private string filePath = "studysessions.txt";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnAddSession_Click(object sender, EventArgs e)
        {
            string subject = txtSubject.Text.Trim();
            string timeSpent = txtTime.Text.Trim();

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(timeSpent))
            {
                MessageBox.Show("Please enter both subject and time spent.");
                return;
            }

            if (!int.TryParse(timeSpent, out int minutes) || minutes <= 0)
            {
                MessageBox.Show("Please enter a valid number of minutes.");
                return;
            }

            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm} - {subject} - {minutes} mins";
            sessions.Add(entry);
            lstSessions.Items.Add(entry);

            txtSubject.Clear();
            txtTime.Clear();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            File.WriteAllLines(filePath, sessions);
            MessageBox.Show("Sessions saved successfully.");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (File.Exists(filePath))
            {
                sessions.Clear();
                lstSessions.Items.Clear();
                sessions.AddRange(File.ReadAllLines(filePath));
                lstSessions.Items.AddRange(sessions.ToArray());
                MessageBox.Show("Sessions loaded successfully.");
            }
            else
            {
                MessageBox.Show("No saved file found.");
            }
        }
    }
}


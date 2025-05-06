using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace StudyTrackerAppVersion4
{
    public partial class Form1 : Form
    {
        // List to store study sessions
        private List<StudySession> sessions = new List<StudySession>();
        private string currentUser = "";

        public Form1()
        {
            InitializeComponent();
        }

        // User login and authentication
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (AuthenticateUser(username, password))
            {
                currentUser = username;
                lblUserStatus.Text = $"Logged in as: {currentUser}";
                LoadSessionsForUser();
            }
            else
            {
                MessageBox.Show("Invalid username or password.");
            }
        }

        // Authenticate user from file
        private bool AuthenticateUser(string username, string password)
        {
            string path = "users.txt";
            if (!File.Exists(path)) return false;

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var user = line.Split(',');
                if (user[0] == username && user[1] == password)
                {
                    return true;
                }
            }
            return false;
        }

        // Load sessions for the current user
        private void LoadSessionsForUser()
        {
            string path = $"{currentUser}_sessions.txt";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                var session = new StudySession
                {
                    Date = DateTime.Parse(parts[0]),
                    Subject = parts[1],
                    Category = parts[2],
                    Minutes = int.Parse(parts[3])
                };
                sessions.Add(session);
            }

            UpdateSessionList();
        }

        // Add a new session
        private void btnAddSession_Click(object sender, EventArgs e)
        {
            // Ensure the user has selected a category
            if (string.IsNullOrEmpty(txtSubject.Text) || string.IsNullOrEmpty(txtTime.Text) || cmbCategory.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill in all fields and select a category.");
                return;
            }

            // Get data from the form
            string subject = txtSubject.Text;
            int time = int.Parse(txtTime.Text);
            string category = cmbCategory.SelectedItem.ToString(); // Get the selected category

            // Create a new study session
            StudySession newSession = new StudySession(DateTime.Now, subject, category, time);

            // Add the session to the list
            sessions.Add(newSession);

            // Save the session to file
            SaveSessionToFile(newSession);

            // Update the session list and chart
            UpdateSessionList();
            UpdateChart();
        }

        // Save session to a file
        private void SaveSessionToFile(StudySession session)
        {
            string path = $"{currentUser}_sessions.txt";
            string sessionData = $"{session.Date},{session.Subject},{session.Category},{session.Minutes}";
            File.AppendAllText(path, sessionData + Environment.NewLine);
        }

        // Update the session list display
        private void UpdateSessionList(List<StudySession> sessionsToShow = null)
        {
            lstSessions.Items.Clear();
            var sessionsToDisplay = sessionsToShow ?? sessions;

            foreach (var session in sessionsToDisplay)
            {
                lstSessions.Items.Add($"{session.Subject} - {session.Minutes} mins ({session.Category})");
            }
        }

        // View summary of study sessions per subject
        private void btnSummary_Click(object sender, EventArgs e)
        {
            var subjectSummary = sessions
                .GroupBy(s => s.Subject)
                .Select(g => new { Subject = g.Key, TotalTime = g.Sum(s => s.Minutes) });

            string summary = "Subject - Total Time (Minutes)\n";
            foreach (var entry in subjectSummary)
            {
                summary += $"{entry.Subject} - {entry.TotalTime} minutes\n";
            }

            MessageBox.Show(summary);
        }

        // Filter sessions to only show the last 7 days
        private void btnFilterWeek_Click(object sender, EventArgs e)
        {
            var filteredSessions = sessions.Where(s => s.Date >= DateTime.Now.AddDays(-7)).ToList();
            UpdateSessionList(filteredSessions);
            UpdateChart(filteredSessions);
        }

        // Update chart with total time per subject
        private void UpdateChart(List<StudySession> sessionsToShow = null)
        {
            chartSummary.Series.Clear();
            var data = sessionsToShow ?? sessions;

            var subjectSummary = data
                .GroupBy(s => s.Subject)
                .Select(g => new { Subject = g.Key, TotalTime = g.Sum(s => s.Minutes) })
                .ToList();

            var series = new Series("Study Time")
            {
                ChartType = SeriesChartType.Column
            };

            foreach (var entry in subjectSummary)
            {
                series.Points.AddXY(entry.Subject, entry.TotalTime);
            }

            chartSummary.Series.Add(series);
        }

        // Register new user and save to users.txt
        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            // Check if username already exists
            if (UsernameExists(username))
            {
                MessageBox.Show("Username already exists. Please choose another one.");
                return;
            }

            // Register the user
            RegisterUser(username, password);
            MessageBox.Show($"User {username} registered successfully!");
        }

        // Check if a username already exists in users.txt
        private bool UsernameExists(string username)
        {
            string path = "users.txt";
            if (!File.Exists(path)) return false;

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var user = line.Split(',');
                if (user[0] == username)
                {
                    return true;
                }
            }
            return false;
        }

        // Register new user and append to users.txt
        private void RegisterUser(string username, string password)
        {
            string path = "users.txt";

            // If the file doesn't exist, create it
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            // Append new user to the file
            string userData = $"{username},{password}";
            File.AppendAllText(path, userData + Environment.NewLine);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Add categories to the ComboBox (cmbCategory)
            cmbCategory.Items.Add("Math");
            cmbCategory.Items.Add("Science");
            cmbCategory.Items.Add("English");
            cmbCategory.Items.Add("History");
            cmbCategory.Items.Add("Programming");
            cmbCategory.Items.Add("Geography");
            cmbCategory.Items.Add("Business");
            cmbCategory.Items.Add("Digital Technologies");
            cmbCategory.Items.Add("Other");

            // Optionally, set a default selected category
            cmbCategory.SelectedIndex = 0; // This sets "Math" as the default selected category
        }
    }

    // Study session model class
    public class StudySession
    {
        public DateTime Date { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public int Minutes { get; set; }

        public StudySession() { }

        public StudySession(DateTime date, string subject, string category, int minutes)
        {
            Date = date;
            Subject = subject;
            Category = category;
            Minutes = minutes;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.VisualBasic;

namespace StudyTrackerAppVersion5
{
    public partial class Form1 : Form
    {
        // List to store all study sessions for the logged-in user
        private List<StudySession> sessions = new List<StudySession>();

        // Stores the username of the currently logged-in user
        private string currentUser = "";

        public Form1()
        {
            InitializeComponent();
        }

        // Event handler for when the form loads
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Populate the category dropdown with example subjects
            cmbCategory.Items.AddRange(new string[] { "Math", "Science", "English", "History", "Programming" });
            cmbCategory.SelectedIndex = 0;
            lblUserStatus.Text = "Not logged in.";
        }

        // ---------------- USER AUTHENTICATION ----------------

        // Login button event
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (AuthenticateUser(username, password))
            {
                currentUser = username;
                lblUserStatus.Text = $"Logged in as: {currentUser}";
                LoadSessionsForUser();
            }
            else
            {
                MessageBox.Show("Invalid login. Try again.");
            }
        }

        // Register button event
        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (username == "" || password == "")
            {
                MessageBox.Show("Enter both username and password.");
                return;
            }

            if (UsernameExists(username))
            {
                MessageBox.Show("Username already exists.");
                return;
            }

            RegisterUser(username, password);
            MessageBox.Show("Registration successful!");
        }

        // Validates user credentials against users.txt file
        private bool AuthenticateUser(string username, string password)
        {
            if (!File.Exists("users.txt")) return false;

            return File.ReadAllLines("users.txt")
                       .Any(line => line == $"{username},{password}");
        }

        // Writes a new username and password to users.txt
        private void RegisterUser(string username, string password)
        {
            if (!File.Exists("users.txt")) File.Create("users.txt").Close();

            File.AppendAllText("users.txt", $"{username},{password}{Environment.NewLine}");
        }

        // Checks if a username already exists in the users.txt file
        private bool UsernameExists(string username)
        {
            if (!File.Exists("users.txt")) return false;

            return File.ReadAllLines("users.txt")
                       .Any(line => line.StartsWith(username + ","));
        }

        // ---------------- SESSION MANAGEMENT ----------------

        // Loads all sessions for the current user from their file
        private void LoadSessionsForUser()
        {
            sessions.Clear();
            lstSessions.Items.Clear();

            string path = $"{currentUser}_sessions.txt";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 4)
                {
                    sessions.Add(new StudySession
                    {
                        Date = DateTime.Parse(parts[0]),
                        Subject = parts[1],
                        Category = parts[2],
                        Minutes = int.Parse(parts[3])
                    });
                }
            }

            UpdateSessionList();
            UpdateChart();
        }

        // Adds a new study session and saves it to the user file
        private void btnAddSession_Click(object sender, EventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtSubject.Text) ||
                string.IsNullOrWhiteSpace(txtTime.Text) ||
                cmbCategory.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            if (!int.TryParse(txtTime.Text, out int time) || time <= 0)
            {
                MessageBox.Show("Enter a valid number of minutes.");
                return;
            }

            // Create a new session object
            var session = new StudySession
            {
                Date = DateTime.Now,
                Subject = txtSubject.Text,
                Category = cmbCategory.SelectedItem.ToString(),
                Minutes = time
            };

            sessions.Add(session);       // Add to the list
            SaveAllSessions();           // Save to file
            UpdateSessionList();         // Refresh UI
            UpdateChart();               // Refresh chart
        }

        // Saves all current sessions to the user’s session file
        private void SaveAllSessions()
        {
            string path = $"{currentUser}_sessions.txt";
            var lines = sessions.Select(s => $"{s.Date},{s.Subject},{s.Category},{s.Minutes}");
            File.WriteAllLines(path, lines);
        }

        // Updates the ListBox with session data (optional filter)
        private void UpdateSessionList(List<StudySession> listToShow = null)
        {
            lstSessions.Items.Clear();
            var source = listToShow ?? sessions;

            foreach (var session in source)
            {
                lstSessions.Items.Add($"{session.Date.ToShortDateString()} - {session.Subject} ({session.Category}) - {session.Minutes} min");
            }
        }

        // Updates the bar chart with session totals (optional filter)
        private void UpdateChart(List<StudySession> listToShow = null)
        {
            var source = listToShow ?? sessions;
            chartSummary.Series.Clear();

            var series = new Series("Minutes")
            {
                ChartType = SeriesChartType.Column
            };

            // Group sessions by subject and total the minutes
            var summary = source
                .GroupBy(s => s.Subject)
                .Select(g => new { Subject = g.Key, Total = g.Sum(s => s.Minutes) });

            foreach (var item in summary)
            {
                series.Points.AddXY(item.Subject, item.Total);
            }

            chartSummary.Series.Add(series);
        }

        // Filters the sessions to show only those from the last 7 days
        private void btnFilterWeek_Click(object sender, EventArgs e)
        {
            var filtered = sessions.Where(s => s.Date >= DateTime.Now.AddDays(-7)).ToList();
            UpdateSessionList(filtered);
            UpdateChart(filtered);
        }

        // Filters sessions by a custom date range (from-to)
        private void btnFilterRange_Click(object sender, EventArgs e)
        {
            DateTime from = dtpFrom.Value.Date;
            DateTime to = dtpTo.Value.Date;

            var filtered = sessions.Where(s => s.Date.Date >= from && s.Date.Date <= to).ToList();
            UpdateSessionList(filtered);
            UpdateChart(filtered);
        }

        // Shows a summary of total time studied per subject
        private void btnSummary_Click(object sender, EventArgs e)
        {
            var summary = sessions
                .GroupBy(s => s.Subject)
                .Select(g => new { Subject = g.Key, Total = g.Sum(s => s.Minutes) });

            string msg = "Summary:\n";
            foreach (var item in summary)
            {
                msg += $"{item.Subject}: {item.Total} min\n";
            }

            MessageBox.Show(msg);
        }
        // ---------------- EDITING A SESSION ----------------

        private void btnEdit_Click(object sender, EventArgs e)
        {
            int index = lstSessions.SelectedIndex;

            // Validate selection
            if (index < 0 || index >= sessions.Count)
            {
                MessageBox.Show("Please select a session to edit.");
                return;
            }

            // Get the selected session
            StudySession selected = sessions[index];

            // Ask user for new values
            string newSubject = PromptInput("Edit Subject:", selected.Subject);
            if (string.IsNullOrWhiteSpace(newSubject)) return;

            string newCategory = PromptInput("Edit Category:", selected.Category);
            if (string.IsNullOrWhiteSpace(newCategory)) return;

            string newMinutes = PromptInput("Edit Minutes:", selected.Minutes.ToString());
            if (!int.TryParse(newMinutes, out int updatedMinutes) || updatedMinutes <= 0)
            {
                MessageBox.Show("Invalid minutes.");
                return;
            }

            // Update session
            selected.Subject = newSubject;
            selected.Category = newCategory;
            selected.Minutes = updatedMinutes;

            SaveAllSessions();
            UpdateSessionList();
            UpdateChart();
        }

        // Reusable input prompt using InputBox-like method
        private string PromptInput(string title, string currentValue)
        {
            return Microsoft.VisualBasic.Interaction.InputBox(title, "Edit", currentValue);
        }

        // ---------------- DELETING A SESSION ----------------

        private void btnDelete_Click(object sender, EventArgs e)
        {
            int index = lstSessions.SelectedIndex;

            // Validate selection
            if (index < 0 || index >= sessions.Count)
            {
                MessageBox.Show("Please select a session to delete.");
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this session?", "Confirm Delete", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                sessions.RemoveAt(index);     // Remove from list
                SaveAllSessions();            // Save updated list
                UpdateSessionList();          // Refresh UI
                UpdateChart();                // Refresh chart
            }
        }
    }

    // ---------------- STUDY SESSION CLASS ----------------

    // Class used to store session data for each user
    public class StudySession
    {
        public DateTime Date { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public int Minutes { get; set; }
    }
}
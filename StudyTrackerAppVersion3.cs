// StudyTracker Version 3.0
// Features: Added chart visualization, weekly filter

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace StudyTrackerAppVersion3
{
    public partial class Form1 : Form
    {
        private List<StudySession> sessions = new List<StudySession>();
        private string filePath = "studysessions.txt";

        public Form1()
        {
            InitializeComponent();
            InitializeChart();
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

            var session = new StudySession(DateTime.Now, subject, minutes);
            sessions.Add(session);
            lstSessions.Items.Add(session.ToString());
            UpdateChart();

            txtSubject.Clear();
            txtTime.Clear();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            List<string> lines = new List<string>();
            foreach (var session in sessions)
            {
                lines.Add(session.ToFileString());
            }
            File.WriteAllLines(filePath, lines);
            MessageBox.Show("Sessions saved successfully.");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (File.Exists(filePath))
            {
                sessions.Clear();
                lstSessions.Items.Clear();
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var session = StudySession.FromFileString(line);
                    sessions.Add(session);
                    lstSessions.Items.Add(session.ToString());
                }
                UpdateChart();
                MessageBox.Show("Sessions loaded successfully.");
            }
            else
            {
                MessageBox.Show("No saved file found.");
            }
        }

        private void btnSummary_Click(object sender, EventArgs e)
        {
            var summary = sessions
                .GroupBy(s => s.Subject)
                .Select(g => $"{g.Key}: {g.Sum(s => s.Minutes)} minutes")
                .ToArray();

            MessageBox.Show(string.Join("\n", summary), "Summary by Subject");
        }

        private void btnFilterWeek_Click(object sender, EventArgs e)
        {
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            var recent = sessions.Where(s => s.Date >= oneWeekAgo).ToList();

            lstSessions.Items.Clear();
            foreach (var session in recent)
            {
                lstSessions.Items.Add(session.ToString());
            }

            UpdateChart(recent);
        }

        private void InitializeChart()
        {
            chartSummary.Series.Clear();
            chartSummary.ChartAreas.Add(new ChartArea("MainArea"));
            chartSummary.Series.Add("StudyMinutes");
            chartSummary.Series["StudyMinutes"].ChartType = SeriesChartType.Column;
            chartSummary.Series["StudyMinutes"].XValueType = ChartValueType.String;
        }

        private void UpdateChart(List<StudySession> data = null)
        {
            var source = data ?? sessions;

            var summary = source
                .GroupBy(s => s.Subject)
                .Select(g => new { Subject = g.Key, TotalMinutes = g.Sum(s => s.Minutes) })
                .ToList();

            chartSummary.Series["StudyMinutes"].Points.Clear();
            foreach (var item in summary)
            {
                chartSummary.Series["StudyMinutes"].Points.AddXY(item.Subject, item.TotalMinutes);
            }
        }
    }

    public class StudySession
    {
        public DateTime Date { get; set; }
        public string Subject { get; set; }
        public int Minutes { get; set; }

        public StudySession(DateTime date, string subject, int minutes)
        {
            Date = date;
            Subject = subject;
            Minutes = minutes;
        }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd HH:mm} - {Subject} - {Minutes} mins";
        }

        public string ToFileString()
        {
            return $"{Date.Ticks}|{Subject}|{Minutes}";
        }

        public static StudySession FromFileString(string line)
        {
            var parts = line.Split('|');
            return new StudySession(new DateTime(long.Parse(parts[0])), parts[1], int.Parse(parts[2]));
        }
    }
}

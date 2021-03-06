﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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

namespace BowlingLeague
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// <summary>
    public partial class MainWindow : Window
    {
        private List<Label> bowlerLabels;
        private int currentWeek = 1;

        /// <summary> If at any point the bowler we're looking at is invalid for scoring reasons, this flag is true. </summary>
        bool invalidBowler;

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists("C:\\Bowling\\league.bin"))
                LoadData();
            else
                League.WriteToFile();
        }

        private void importMatchupsButton_Click(object sender, RoutedEventArgs e)
        {
            League.SetMatchups(GetFileName());
        }

        private void importTeamsButton_Click(object sender, RoutedEventArgs e)
        {
            League.SetTeams(GetFileName());
            CreateLabels();
        }

        private String GetFileName()
        {
            OpenFileDialog open = new OpenFileDialog();
            open.InitialDirectory = "C:\\Bowling";
            open.Filter = "txt files (*.txt)|*.txt";
            open.FilterIndex = 2;
            open.RestoreDirectory = true;
            if (open.ShowDialog() == true)
                return open.FileName;
            return null;
        }

        /// <summary> Loads the League through LeagueHelper. </summary>
        private void LoadData()
        {
            using (Stream s = File.Open("C:\\Bowling\\league.bin", FileMode.Open))
            {
                BinaryFormatter bin = new BinaryFormatter();
                LeagueHelper l = (LeagueHelper) bin.Deserialize(s);
                s.Close();
                League.teams = l.GetTeams();
                League.matchups = l.GetMatchups();
                League.nextWeek = l.GetWeek();
                currentWeek = l.GetWeek();
                weekText.Text = "Week " + currentWeek;
                League.UpdateBowlers(true, currentWeek);
                CreateLabels();
                LeagueStats le = new LeagueStats(currentWeek);
            }
        }

        /// <summary> Creates the clickable labels for the sidebar. </summary>
        private void CreateLabels()
        {
            bowlerLabels = new List<Label>();
            if (League.teams != null && League.teams.Count != 0)
            {
                stackPanel.MaxHeight = 0;
                foreach (Team team in League.teams)
                {
                    Label teamLabel = new Label();
                    teamLabel.Content = team.GetName();
                    teamLabel.Background = new SolidColorBrush(Colors.White);
                    teamLabel.FontSize = 38;
                    teamLabel.BorderThickness = new Thickness(0, 3, 0, 3);
                    teamLabel.FontWeight = FontWeights.Bold;
                    teamLabel.Height = 68;
                    teamLabel.Width = 217;
                    stackPanel.Children.Add(teamLabel);
                    stackPanel.MaxHeight += 68;
                    foreach (Bowler bowler in team.GetBowlers())
                    {
                        // Don't create a label for an inactive bowler.
                        if (!bowler.IsActive(currentWeek))
                            continue;
                        Label bowlerLabel = new Label();
                        bowlerLabel.Content = bowler.GetName();
                        bowlerLabel.BorderThickness = new Thickness(0, 1, 0, 1);
                        bowlerLabel.Background = new SolidColorBrush(Colors.White);
                        bowlerLabel.FontSize = 25;
                        bowlerLabel.Height = 48;
                        bowlerLabel.Width = 217;
                        bowlerLabel.MouseLeftButtonDown += bowlerLabelOnClick;
                        bowlerLabels.Add(bowlerLabel);
                        stackPanel.Children.Add(bowlerLabel);
                        stackPanel.MaxHeight += 48;
                    }
                }
                
                if(SelectedBowler.bowler == null)
                {
                    ChangeSelection(bowlerLabels[0]);
                }
            }
        }

        /// <summary> Used when changing currentWeeks. Update bowler list and labels so they're in sync. </summary>
        private void UpdateLabels()
        {
            stackPanel.Children.Clear();
            League.UpdateBowlers(true, currentWeek);
            CreateLabels();
        }

        /// <summary> Updates information panel to show the selected bowler's information. </summary>
        private void UpdateUI()
        {
            if (SelectedBowler.bowler == null)
                return;
            teamNameLabel.Content = SelectedBowler.bowler.GetTeam().GetName();
            playerNameTextBox.Text = SelectedBowler.bowler.GetName();
            playerNameLabel.Content = SelectedBowler.bowler.GetName();
            playerInitialAverageTextBox.Text = SelectedBowler.bowler.GetInitialAverage().ToString();
            replacementInitialAverageTextBox.Text = "90";
            meanScoreLabel.Content = SelectedBowler.bowler.GetMean(currentWeek, true).ToString();
            List<int> scores = SelectedBowler.bowler.GetScores(currentWeek);
            string scoreText = "";
            for(int i = 0; i < 3; i++)
                scoreText += scores[i] + " ";
            scoreTextBox.Text = scoreText.Trim();
            scoreTextBox.SelectAll();
        }

        /// <summary> Used for changing the selected bowler. </summary>
        private void AdvanceSelection()
        {
            if (invalidBowler)
            {
                MessageBox.Show("Invalid scores entered.");
                UpdateUI();
                invalidBowler = false;
                return;
            }

            // Any score changes made will be saved.
            League.WriteToFile();

            int bIndex = bowlerLabels.IndexOf(SelectedBowler.label);
            if (bIndex >= bowlerLabels.Count - 1)
                return;

            if (SelectedBowler.bowler != null)
                ChangeSelection(bowlerLabels[bIndex + 1]);
        }

        /// <summary> The various ways of changing the selection. </summary>
        private void nextPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            AdvanceSelection();
        }

        private void bowlerLabelOnClick(object sender, MouseButtonEventArgs e)
        {
            ChangeSelection(sender as Label);
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AdvanceSelection();
            }
        }

        /// <summary> Highlights the chosen label and scrolls the label selection panel. </summary>
        private void ChangeSelection(Label l)
        {
            scoreTextBox.Focus();

            if (SelectedBowler.bowler != null) 
                SelectedBowler.label.Background = new SolidColorBrush(Colors.White);

            SelectedBowler.bowler = League.bowlers[bowlerLabels.IndexOf(l)];
            l.Background = new SolidColorBrush(Colors.LightGray);
            SelectedBowler.label = l;


            Point relativePoint = l.TransformToAncestor(stackPanel).Transform(new Point(0, 0));

            if (relativePoint.Y >= scrollViewer.ActualHeight)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ActualHeight + (relativePoint.Y - scrollViewer.ActualHeight));
            }

            UpdateUI();
        }

        /// <summary> Replaces the bowler with either an "open slot" or a replacement bowler designated by the user. </summary>
        private void removePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            // Add in "Are you sure?"

            if (!CheckAverage(replacementInitialAverageTextBox.Text))
            {
                MessageBox.Show("Invalid initial average.");
                replacementInitialAverageTextBox.Text = "90";
                return;
            }

            if(!replacementNameTextBox.Text.Trim().Contains(" "))
            {
                MessageBox.Show("Invalid name.");
                replacementNameTextBox.Text = "OPEN SLOT";
                return;
            }

            Team team = SelectedBowler.bowler.GetTeam();
            Bowler replacementBowler = new Bowler(replacementNameTextBox.Text, team, Double.Parse(replacementInitialAverageTextBox.Text), currentWeek, (bool) openSlotCheckBox.IsChecked);
            if (team.ReplaceBowler(SelectedBowler.bowler, replacementBowler, currentWeek))
            {
                League.UpdateBowlers(false, currentWeek);
                SelectedBowler.bowler = replacementBowler;
                SelectedBowler.label.Content = replacementBowler.GetName();
                UpdateUI();
            }
            else
            {
                MessageBox.Show("Replacement failed.");
            }
        }

        /// <summary> Saves changes made in the edit panel. </summary>
        private void updatePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBowler.bowler.SetName(playerNameTextBox.Text) && CheckAverage(playerInitialAverageTextBox.Text))
            {
                SelectedBowler.bowler.SetInitialAverage(Double.Parse(playerInitialAverageTextBox.Text));
                MessageBox.Show(playerNameTextBox.Text + " updated.");
                SelectedBowler.label.Content = SelectedBowler.bowler.GetName();
                League.UpdateBowlers(false, currentWeek);
                UpdateUI();
                League.WriteToFile();
            }
            else
            {
                MessageBox.Show("Name or average not valid.");
                playerNameTextBox.Text = SelectedBowler.bowler.GetName();
            }
        }

        /// <summary> Checks and submits scores from the textbox. </summary>
        private void scoreTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // A valid score [x y z] must have at least five characters.
            if (scoreTextBox.Text.Trim().Length < 5) {
                invalidBowler = true;
                return;
            }
            
            double mean = SelectedBowler.bowler.SetScores(currentWeek, (sender as TextBox).Text); 
            // The user has input invalid scores into the textbox.
            if(mean < 0)
            {
                invalidBowler = true;
            }
            else {
                meanScoreLabel.Content = mean.ToString();
                invalidBowler = false;
            }
        }

        /// <summary> Will print to PDF the desired information. </summary>
        private void printButton_Click(object sender, RoutedEventArgs e)
        {
            if (!invalidBowler)
            {
                League.nextWeek = currentWeek + 1;
                League.WriteToFile();
                League.CalculateMatchups(currentWeek);
                LeagueStats stats = new LeagueStats(currentWeek);
                RecordWriter.Write(currentWeek);
            }
            else
            {
                MessageBox.Show("Invalid scores entered.");
                UpdateUI();
                invalidBowler = false;
            }
        }

        private void weekButton_Click(object sender, RoutedEventArgs e)
        {
            if(weekContextMenu.Items.Count == 0)
            {
                for(int i = 1; i < 31; i++)
                {
                    MenuItem item = new MenuItem();
                    item.Header = "Week " + i;
                    item.Click += weekItem_Click;
                    weekContextMenu.Items.Add(item);
                }
            }
            weekContextMenu.IsOpen = true;
        }

        /// <summary> Week selector code. </summary>
        private void weekItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            String[] splitHeader = item.Header.ToString().Split(' ');
            // Since all weeks numbers are the second word in their respective week items, currentWeek = splitHeader[1].
            currentWeek = int.Parse(splitHeader[1]);
            weekText.Text = item.Header.ToString();
            SelectedBowler.bowler = null;
            UpdateLabels();
        }

        /// <summary> Toggles valid textboxes when removing players. </summary>
        private void openSlotCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool) openSlotCheckBox.IsChecked;
            SolidColorBrush background = new SolidColorBrush(Colors.White);

            if (isChecked)
            {
                replacementNameTextBox.Text = "OPEN SLOT";
                replacementInitialAverageTextBox.Text = "90";
                background.Color = Colors.LightGray;
            }

            replacementNameTextBox.IsReadOnly = isChecked;
            replacementInitialAverageTextBox.IsReadOnly = isChecked;
            replacementNameTextBox.Background = background;
            replacementInitialAverageTextBox.Background = background;
        }

        /// <summary> Checks that averages are valid. </summary>
        private bool CheckAverage(string avg)
        {
            double value;
            if (!Double.TryParse(avg, out value) || value >= 300 || value < 0)
                return false;
            return true;
        }
    }
}

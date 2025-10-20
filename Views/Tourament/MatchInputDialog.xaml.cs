using System;
using System.Collections.Generic;
using System.Windows;
using BadmintonClub.Services;

namespace BadmintonClub.Views.Tournaments
{
    public partial class MatchInputDialog : Window
    {
        public string TeamAName { get; set; } = "";
        public string TeamBName { get; set; } = "";
        public List<MatchService.GameScore>? GameScores { get; private set; }

        public MatchInputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var scores = new List<MatchService.GameScore>();

                // Validate Game 1 & 2 (bắt buộc)
                if (!int.TryParse(TxtG1A.Text, out int g1a) ||
                    !int.TryParse(TxtG1B.Text, out int g1b))
                    throw new Exception("Game 1 phải nhập đầy đủ tỷ số.");

                if (!int.TryParse(TxtG2A.Text, out int g2a) ||
                    !int.TryParse(TxtG2B.Text, out int g2b))
                    throw new Exception("Game 2 phải nhập đầy đủ tỷ số.");

                scores.Add(new MatchService.GameScore { A = g1a, B = g1b });
                scores.Add(new MatchService.GameScore { A = g2a, B = g2b });

                // Game 3 (optional)
                if (!string.IsNullOrWhiteSpace(TxtG3A.Text) &&
                    !string.IsNullOrWhiteSpace(TxtG3B.Text))
                {
                    if (int.TryParse(TxtG3A.Text, out int g3a) &&
                        int.TryParse(TxtG3B.Text, out int g3b))
                    {
                        scores.Add(new MatchService.GameScore { A = g3a, B = g3b });
                    }
                }

                // Validate best-of-3
                int winsA = 0, winsB = 0;
                foreach (var g in scores)
                {
                    if (g.A > g.B) winsA++;
                    else if (g.B > g.A) winsB++;
                }

                if (Math.Max(winsA, winsB) < 2)
                    throw new Exception("Phải thắng ít nhất 2/3 game!");

                GameScores = scores;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi nhập liệu",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

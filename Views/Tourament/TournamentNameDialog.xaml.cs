using System;
using System.Windows;

namespace BadmintonClub.Views.Tournaments
{
    public partial class TournamentNameDialog : Window
    {
        public string TournamentName { get; private set; } = string.Empty;
        public DateTime StartDate { get; private set; }

        public TournamentNameDialog()
        {
            InitializeComponent();

            // Set ngày mặc định là hôm nay
            DpStartDate.SelectedDate = DateTime.Now;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            TournamentName = TxtTournamentName.Text.Trim();

            // Validate tên giải
            if (string.IsNullOrWhiteSpace(TournamentName))
            {
                MessageBox.Show("Vui lòng nhập tên giải đấu!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtTournamentName.Focus();
                return;
            }

            // Lấy ngày bắt đầu
            if (DpStartDate.SelectedDate.HasValue)
                StartDate = DpStartDate.SelectedDate.Value;
            else
                StartDate = DateTime.Now;

            // Đóng dialog với kết quả OK
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Đóng dialog với kết quả Cancel
            DialogResult = false;
            Close();
        }
    }
}

using System.Windows.Controls;
using BadmintonClub.ViewModels;
using BadmintonClub.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BadmintonClub.Views.Tournaments
{
    public partial class EloRankingView : UserControl
    {
        private EloRankingViewModel? _vm;

        public EloRankingView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Tạo ViewModel sau khi load
            if (_vm == null)
            {
                var db = new BadmintonClubContext();
                _vm = new EloRankingViewModel(db);
                DataContext = _vm;

                // Load data
                _ = _vm.LoadCommand.ExecuteAsync(null);
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void CbFilterGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbFilterGroup.SelectedItem is ComboBoxItem selected && _vm != null)
            {
                string filterValue = selected.Tag?.ToString() ?? "All";
                _vm.FilterGroup = filterValue;

                // Gọi filter command
                _ = _vm.ApplyFilterCommand.ExecuteAsync(null);
            }
        }
    }
}

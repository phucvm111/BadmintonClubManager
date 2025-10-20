using System.Windows;
using System.Windows.Controls;
using BadmintonClub.ViewModels;

namespace BadmintonClub.Views.Finance
{
    public partial class FinanceView : UserControl
    {
        public FinanceView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is FinanceViewModel vm && vm.TaiThuChiCommand.CanExecute(null))
                vm.TaiThuChiCommand.Execute(null);
        }
    }
}

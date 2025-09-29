using System.Windows.Controls;
using BadmintonClub.ViewModels;

namespace BadmintonClub.Views;

public partial class AttendanceView : UserControl
{
    public AttendanceView()
    {
        InitializeComponent();
        Loaded += AttendanceView_Loaded;
    }

    private void AttendanceView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AttendanceViewModel vm)
        {
            vm.CommitEditRequested = () =>
            {
                AttGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                AttGrid.CommitEdit(DataGridEditingUnit.Row, true);
            };
        }
    }
}

using System.Windows.Controls;
using BadmintonClub.ViewModels;

namespace BadmintonClub.Views;

public partial class MembersView : UserControl
{
    public MembersView()
    {
        InitializeComponent();
        Loaded += MembersView_Loaded;
    }

    private void MembersView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MembersViewModel vm)
        {
            // Cho ViewModel yêu cầu commit edit trước khi Save/Delete
            vm.CommitEditRequested = () =>
            {
                MembersGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                MembersGrid.CommitEdit(DataGridEditingUnit.Row, true);
            };
        }
    }
}

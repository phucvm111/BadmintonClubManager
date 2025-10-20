using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BadmintonClub.Models;

namespace BadmintonClub.Views.Tournaments
{
    public partial class MemberSelectorDialog : Window
    {
        public List<Member> SelectedMembers { get; private set; } = new List<Member>();

        public MemberSelectorDialog(List<Member> availableMembers)
        {
            InitializeComponent();
            LstAvailable.ItemsSource = availableMembers;
            UpdateCount();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selected = LstAvailable.SelectedItems.Cast<Member>().ToList();
            foreach (var m in selected)
            {
                if (!SelectedMembers.Any(x => x.MemberId == m.MemberId))
                {
                    SelectedMembers.Add(m);
                }
            }
            RefreshSelected();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = LstSelected.SelectedItems.Cast<Member>().ToList();
            foreach (var m in selected)
            {
                SelectedMembers.RemoveAll(x => x.MemberId == m.MemberId);
            }
            RefreshSelected();
        }

        private void BtnAddAll_Click(object sender, RoutedEventArgs e)
        {
            var all = LstAvailable.ItemsSource as List<Member>;
            if (all != null)
            {
                SelectedMembers = all.ToList();
                RefreshSelected();
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            SelectedMembers.Clear();
            RefreshSelected();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMembers.Count < 2)
            {
                MessageBox.Show("Cần chọn ít nhất 2 thành viên để tạo giải đấu.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RefreshSelected()
        {
            LstSelected.ItemsSource = null;
            LstSelected.ItemsSource = SelectedMembers;
            UpdateCount();
        }

        private void UpdateCount()
        {
            TxtCount.Text = SelectedMembers.Count.ToString();
        }
    }
}

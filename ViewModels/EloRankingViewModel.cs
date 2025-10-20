using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BadmintonClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BadmintonClub.ViewModels
{
    public partial class EloRankingViewModel : ObservableObject
    {
        private readonly BadmintonClubContext _db;

        [ObservableProperty]
        private ObservableCollection<Member> _rankings = new();

        [ObservableProperty]
        private Member? _selectedMember;

        [ObservableProperty]
        private ObservableCollection<EloHistory> _memberHistory = new();

        [ObservableProperty]
        private string _filterGroup = "All";

        private List<Member> _allMembers = new(); // Cache toàn bộ members

        public EloRankingViewModel(BadmintonClubContext db)
        {
            _db = db;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            // Load tất cả members Active và sắp xếp theo Elo
            _allMembers = await _db.Members
                .Where(m => m.TinhTrang == "Active")
                .OrderByDescending(m => m.Elo)
                .ToListAsync();

            // Áp dụng filter
            await ApplyFilterAsync();
        }

        [RelayCommand]
        private async Task ApplyFilterAsync()
        {
            IEnumerable<Member> filtered;

            if (FilterGroup == "All")
            {
                filtered = _allMembers;
            }
            else
            {
                filtered = _allMembers.Where(m => m.NhomTrinhDo == FilterGroup);
            }

            Rankings = new ObservableCollection<Member>(filtered);
        }

        [RelayCommand]
        private async Task LoadMemberHistoryAsync()
        {
            if (SelectedMember == null) return;

            var history = await _db.EloHistories
                .Where(h => h.MemberId == SelectedMember.MemberId)
                .OrderByDescending(h => h.Timestamp)
                .Take(50) // Lấy 50 bản ghi gần nhất
                .ToListAsync();

            MemberHistory = new ObservableCollection<EloHistory>(history);
        }
    }
}

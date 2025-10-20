using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BadmintonClub.Models;
using BadmintonClub.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BadmintonClub.Views.Tournaments;

namespace BadmintonClub.ViewModels
{
    public partial class TournamentDetailViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentSvc;
        private readonly BracketService _bracketSvc;
        private readonly BadmintonClubContext _db;

        [ObservableProperty] private Tournament? _tournament;
        [ObservableProperty] private ObservableCollection<TournamentEvent> _events = new();
        [ObservableProperty] private TournamentEvent? _selectedEvent;
        [ObservableProperty] private ObservableCollection<Match> _matches = new();

        public TournamentDetailViewModel(
            TournamentService tSvc,
            BracketService bSvc,
            BadmintonClubContext db)
        {
            _tournamentSvc = tSvc;
            _bracketSvc = bSvc;
            _db = db;
        }

        public async void LoadTournament(int id)
        {
            Tournament = await _tournamentSvc.GetByIdAsync(id);
            if (Tournament?.TournamentEvents != null)
            {
                Events = new ObservableCollection<TournamentEvent>(Tournament.TournamentEvents);
            }
        }

        [RelayCommand]
        private async Task AddEventAsync(string? hangMuc)
        {
            if (Tournament == null || string.IsNullOrEmpty(hangMuc))
            {
                MessageBox.Show("Vui lòng chọn hạng mục.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Events.Any(e => e.HangMuc == hangMuc))
            {
                MessageBox.Show($"Hạng mục '{hangMuc}' đã tồn tại!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var evt = await _tournamentSvc.AddEventAsync(Tournament.TournamentId, hangMuc);
                Events.Add(evt);
                SelectedEvent = evt;

                MessageBox.Show($"Đã thêm hạng mục '{hangMuc}'.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Tạo bracket THỦ CÔNG - chọn thành viên qua dialog
        /// </summary>
        [RelayCommand]
        private async Task GenerateBracketManualAsync()
        {
            if (SelectedEvent == null)
            {
                MessageBox.Show("Vui lòng chọn hạng mục trước.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Lấy danh sách thành viên Active
                var members = await _db.Members
                    .Where(m => m.TinhTrang == "Active")
                    .OrderByDescending(m => m.Elo)
                    .ToListAsync();

                if (members.Count < 2)
                {
                    MessageBox.Show("Cần ít nhất 2 thành viên Active.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. Mở dialog chọn thành viên
                var dialog = new MemberSelectorDialog(members);
                if (dialog.ShowDialog() != true)
                    return;

                var selectedMembers = dialog.SelectedMembers;
                bool isDoubles = SelectedEvent.HangMuc.Contains("Đôi");

                // 3. Kiểm tra số người hợp lệ cho đôi
                if (isDoubles && selectedMembers.Count % 2 != 0)
                {
                    MessageBox.Show("Hạng mục đôi cần số thí sinh chẵn (mỗi đội 2 người).",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 4. Tạo Teams
                var teams = await _bracketSvc.CreateTeamsFromMembersAsync(
                    SelectedEvent.EventId,
                    selectedMembers,
                    isDoubles);

                // 5. Tạo bracket knockout
                var matchCount = await _bracketSvc.CreateKnockoutBracketAsync(
                    SelectedEvent.EventId,
                    teams);

                // 6. Load lại matches
                await LoadMatchesAsync();

                MessageBox.Show(
                    $"✓ Đã tạo bracket thành công!\n\n" +
                    $"Số đội: {teams.Count}\n" +
                    $"Trận vòng đầu: {matchCount}",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}\n\nChi tiết: {ex.InnerException?.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Nhập kết quả trận đấu (double-click vào Match)
        /// </summary>
        [RelayCommand]
        private async Task InputMatchResultAsync(Match? match)
        {
            if (match == null || match.Completed) return;

            var dialog = new MatchInputDialog
            {
                TeamAName = match.TeamA?.TenDoi ?? "Đội A",
                TeamBName = match.TeamB?.TenDoi ?? "Đội B"
            };

            if (dialog.ShowDialog() == true && dialog.GameScores != null)
            {
                var matchSvc = new MatchService(_db);
                bool success = await matchSvc.SubmitResultAsync(match.MatchId, dialog.GameScores);

                if (success)
                {
                    // Tạo trận tiếp theo (nếu có)
                    await CreateNextRoundMatchesAsync();

                    await LoadMatchesAsync();

                    MessageBox.Show("Đã cập nhật kết quả và Elo!",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Lỗi khi cập nhật kết quả.",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Tự động tạo trận vòng tiếp theo
        /// </summary>
        private async Task CreateNextRoundMatchesAsync()
        {
            if (SelectedEvent == null) return;

            var allMatches = await _db.Matches
                .AsNoTracking()
                .Where(m => m.EventId == SelectedEvent.EventId)
                .OrderBy(m => m.Round)
                .ToListAsync();

            var rounds = allMatches.GroupBy(m => m.Round).OrderBy(g => g.Key).ToList();

            foreach (var roundGrp in rounds)
            {
                var roundMatches = roundGrp.OrderBy(m => m.MatchId).ToList();

                for (int i = 0; i < roundMatches.Count; i += 2)
                {
                    if (i + 1 >= roundMatches.Count) continue;

                    var m1 = roundMatches[i];
                    var m2 = roundMatches[i + 1];

                    if (!m1.Completed || !m2.Completed) continue;
                    if (!m1.WinnerTeamId.HasValue || !m2.WinnerTeamId.HasValue) continue;

                    var nextRound = m1.Round - 1;
                    if (nextRound < 0) continue; // Đã là chung kết

                    // Kiểm tra trận tiếp theo đã tồn tại chưa
                    var exists = allMatches.Any(m =>
                        m.Round == nextRound &&
                        ((m.TeamAid == m1.WinnerTeamId && m.TeamBid == m2.WinnerTeamId) ||
                         (m.TeamAid == m2.WinnerTeamId && m.TeamBid == m1.WinnerTeamId)));

                    if (exists) continue;

                    // Tạo trận mới
                    var nextMatch = new Match
                    {
                        EventId = SelectedEvent.EventId,
                        Round = nextRound,
                        TeamAid = m1.WinnerTeamId.Value,
                        TeamBid = m2.WinnerTeamId.Value,
                        Completed = false,
                        PrevMatchAid = m1.MatchId,
                        PrevMatchBid = m2.MatchId
                    };

                    _db.Matches.Add(nextMatch);
                }
            }

            await _db.SaveChangesAsync();
        }

        [RelayCommand]
        private async Task LoadMatchesAsync()
        {
            if (SelectedEvent == null) return;

            try
            {
                var list = await _db.Matches
                    .Include(m => m.TeamA)
                    .Include(m => m.TeamB)
                    .Where(m => m.EventId == SelectedEvent.EventId)
                    .OrderBy(m => m.Round)
                    .ThenBy(m => m.MatchId)
                    .ToListAsync();

                Matches = new ObservableCollection<Match>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải trận: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

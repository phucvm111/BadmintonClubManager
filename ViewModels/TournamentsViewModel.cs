using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BadmintonClub.Models;
using BadmintonClub.Services;
using BadmintonClub.Views.Tournaments;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace BadmintonClub.ViewModels
{
    public partial class TournamentsViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentSvc;
        private readonly Func<TournamentDetailViewModel> _detailFactory;
        private readonly BadmintonClubContext _db;

        [ObservableProperty]
        private ObservableCollection<Tournament> _tournaments = new();

        [ObservableProperty]
        private Tournament? _selectedTournament;

        [ObservableProperty]
        private object? _detailView;

        public TournamentsViewModel(
            TournamentService svc,
            Func<TournamentDetailViewModel> detailFactory,
            BadmintonClubContext db)
        {
            _tournamentSvc = svc;
            _detailFactory = detailFactory;
            _db = db;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            var list = await _tournamentSvc.GetAllAsync();
            Tournaments = new(list);
        }

        /// <summary>
        /// Tạo giải mới với dialog nhập tên
        /// </summary>
        [RelayCommand]
        private void Create()
        {
            // Mở dialog nhập tên
            var dialog = new TournamentNameDialog();
            if (dialog.ShowDialog() == true)
            {
                CreateTournamentAsync(dialog.TournamentName, dialog.StartDate);
            }
        }

        /// <summary>
        /// Thực tế tạo giải sau khi có tên từ dialog
        /// </summary>
        private async void CreateTournamentAsync(string name, DateTime startDate)
        {
            try
            {
                var tournament = new Tournament
                {
                    TenGiai = name,
                    NgayBatDau = DateOnly.FromDateTime(startDate),
                    Status = "Draft"
                };

                _db.Tournaments.Add(tournament);
                await _db.SaveChangesAsync();

                Tournaments.Insert(0, tournament);
                SelectedTournament = tournament;

                MessageBox.Show($"Đã tạo giải '{name}' thành công!",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Tự động mở detail view
                OpenDetail();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo giải: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cập nhật tên giải đấu (gọi từ nút Lưu trong DataGrid)
        /// </summary>
        [RelayCommand]
        private async Task UpdateTournamentName(Tournament? tournament)
        {
            if (tournament == null || string.IsNullOrWhiteSpace(tournament.TenGiai))
            {
                MessageBox.Show("Tên giải không được để trống.",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existing = await _db.Tournaments.FindAsync(tournament.TournamentId);
                if (existing != null)
                {
                    existing.TenGiai = tournament.TenGiai;
                    await _db.SaveChangesAsync();

                    MessageBox.Show($"Đã cập nhật tên giải thành '{tournament.TenGiai}'!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mở chi tiết giải đấu
        /// </summary>
        [RelayCommand]
        private void OpenDetail()
        {
            if (SelectedTournament == null) return;

            var vm = _detailFactory();
            vm.LoadTournament(SelectedTournament.TournamentId);

            // Tạo View và set DataContext
            var detailView = new TournamentDetailView
            {
                DataContext = vm
            };

            DetailView = detailView;
        }

        /// <summary>
        /// Xóa giải đấu
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync(Tournament? t)
        {
            if (t == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa giải '{t.TenGiai}'?\n\nThao tác này không thể hoàn tác!",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _tournamentSvc.DeleteAsync(t.TournamentId);
                    Tournaments.Remove(t);

                    MessageBox.Show("Đã xóa giải thành công!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Tự động mở detail khi chọn tournament
        /// </summary>
        partial void OnSelectedTournamentChanged(Tournament? value)
        {
            if (value != null)
            {
                OpenDetail();
            }
        }
    }
}

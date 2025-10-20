using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using BadmintonClub.Models;
using ClubMember = BadmintonClub.Models.Member;

namespace BadmintonClub.ViewModels;

public partial class MembersViewModel : ObservableObject
{
    private readonly BadmintonClubContext _db;

    [ObservableProperty] private ObservableCollection<ClubMember> _members = new();
    [ObservableProperty] private ClubMember? _selectedMember;
    [ObservableProperty] private string? _searchText;

    // View sẽ gán delegate này để commit edit
    public Action? CommitEditRequested { get; set; }

    public MembersViewModel(BadmintonClubContext db)
    {
        _db = db;
        // KHÔNG gọi LoadAsync() ở đây nữa vì MainViewModel sẽ gọi LoadCommand
    }

    // ← THÊM [RelayCommand] để sinh LoadCommand tự động
    [RelayCommand]
    private async Task LoadAsync()
    {
        Members = new ObservableCollection<ClubMember>(
            await _db.Members.AsNoTracking().OrderBy(x => x.HoTen).ToListAsync());
    }

    partial void OnSearchTextChanged(string? value)
        => _ = FilterAsync(value ?? string.Empty);

    private async Task FilterAsync(string text)
    {
        Members = new ObservableCollection<ClubMember>(
            await _db.Members.AsNoTracking()
                .Where(m => m.HoTen.Contains(text) || m.MemberCode.Contains(text))
                .OrderBy(m => m.HoTen).ToListAsync());
    }

    [RelayCommand]
    private void Add()
    {
        var tempCode = $"TMP{DateTime.Now:yyyyMMddHHmmssfff}";
        var m = new ClubMember
        {
            MemberId = Guid.NewGuid(),
            TinhTrang = "Active",
            NhomTrinhDo = "TrungBinh",
            Elo = 1000,
            MemberCode = tempCode,
            HoTen = string.Empty,
            GioiTinh = "Nam"
        };
        Members.Add(m);
        SelectedMember = m;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            CommitEditRequested?.Invoke();

            foreach (var m in Members)
            {
                if (string.IsNullOrWhiteSpace(m.MemberCode))
                    throw new InvalidOperationException("Mã thành viên không được để trống.");
                if (string.IsNullOrWhiteSpace(m.HoTen))
                    throw new InvalidOperationException($"Họ tên không được trống (Mã: {m.MemberCode}).");
                if (string.IsNullOrWhiteSpace(m.GioiTinh))
                    throw new InvalidOperationException($"Giới tính không được trống (Mã: {m.MemberCode}).");
            }

            var duplicates = Members
                .GroupBy(x => x.MemberCode, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicates.Any())
                throw new InvalidOperationException($"Mã thành viên trùng: {string.Join(", ", duplicates)}");

            foreach (var m in Members)
            {
                var entry = _db.Attach(m);
                entry.State = (await _db.Members.AsNoTracking().AnyAsync(x => x.MemberId == m.MemberId))
                              ? EntityState.Modified : EntityState.Added;
            }

            await _db.SaveChangesAsync();
            await LoadAsync();
            MessageBox.Show("Đã lưu thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show($"Lỗi khi lưu (DB): {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        try
        {
            CommitEditRequested?.Invoke();

            if (SelectedMember is null)
            {
                MessageBox.Show("Chưa chọn thành viên để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Xác nhận xóa thành viên: {SelectedMember.HoTen} (Mã: {SelectedMember.MemberCode})?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            bool hasFinance = await _db.FinanceEntries.AnyAsync(e => e.MemberId == SelectedMember.MemberId);
            if (hasFinance)
                throw new InvalidOperationException("Không thể xóa: thành viên có bản ghi tài chính.");

            foreach (var entry in _db.ChangeTracker.Entries<ClubMember>()
                         .Where(e => e.Entity.MemberId == SelectedMember.MemberId).ToList())
            {
                entry.State = EntityState.Detached;
            }

            var stub = new ClubMember { MemberId = SelectedMember.MemberId };
            _db.Attach(stub);
            _db.Members.Remove(stub);

            await _db.SaveChangesAsync();

            Members.Remove(SelectedMember);
            SelectedMember = null;

            MessageBox.Show("Đã xóa thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show($"Lỗi khi xóa (DB): {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

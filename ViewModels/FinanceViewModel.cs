using BadmintonClub.Models;
using BadmintonClub.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Apis.Gmail.v1;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BadmintonClub.ViewModels
{
    public partial class FinanceViewModel : ObservableObject
    {
        private readonly BadmintonClubContext _db;
        private readonly FinanceService _finance;
        private readonly SettingsService _settings;
        private readonly GmailAuthService _gmailAuth;

        // ====== Bộ lọc Thu/Chi ======
        [ObservableProperty] private DateTime tuNgay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        [ObservableProperty] private DateTime denNgay = DateTime.Today;
        [ObservableProperty] private int? selectedCategoryId;
        [ObservableProperty] private Guid? selectedMemberId;

        [ObservableProperty] private ObservableCollection<FinanceEntry> thuChi = new();
        [ObservableProperty] private decimal tongThu;
        [ObservableProperty] private decimal tongChi;
        [ObservableProperty] private decimal tongNet;

        [ObservableProperty] private ObservableCollection<FinanceCategory> danhMuc = new();
        [ObservableProperty] private ObservableCollection<Member> thanhVien = new();

        // ====== Hội phí ======
        [ObservableProperty] private string kyHienTai = $"{DateTime.Today:yyyy-MM}";
        [ObservableProperty] private decimal mucHoiPhi;
        [ObservableProperty] private bool chiChuaThanhToan = true;
        [ObservableProperty] private bool chiQuaHan;
        [ObservableProperty] private ObservableCollection<MembershipFee> hoiPhi = new();

        partial void OnTongThuChanged(decimal value) => TongNet = TongThu + TongChi;
        partial void OnTongChiChanged(decimal value) => TongNet = TongThu + TongChi;

        public FinanceViewModel(BadmintonClubContext db, FinanceService finance, SettingsService settings, GmailAuthService gmailAuth)
        {
            _db = db;
            _finance = finance;
            _settings = settings;
            _gmailAuth = gmailAuth;

            Task.Run(async () =>
            {
                var cats = await _db.FinanceCategories.OrderBy(c => c.Name).ToListAsync();
                cats.Insert(0, new FinanceCategory { CategoryId = 0, Name = "(Tất cả)" });
                var mems = await _db.Members.OrderBy(m => m.HoTen).ToListAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DanhMuc = new ObservableCollection<FinanceCategory>(cats);
                    ThanhVien = new ObservableCollection<Member>(mems);
                    SelectedCategoryId = 0; // mặc định: tất cả
                });
            });



            var cfg = _settings.Load();
            MucHoiPhi = cfg.DefaultMonthlyFee <= 0 ? 200000m : cfg.DefaultMonthlyFee;
        }
        [RelayCommand]
        private async Task DanhDauDaThuCoXacNhanAsync(MembershipFee? item)
        {
            if (item == null) { MessageBox.Show("Chọn một dòng hội phí."); return; }
            var ok = MessageBox.Show($"Xác nhận đã thu hội phí kỳ {item.Period}?", "Xác nhận",
                                     MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ok != MessageBoxResult.Yes) return;

            await DanhDauDaThuAsync(item); // tái sử dụng lệnh cũ
        }

        [RelayCommand]
        private async Task GuiNhacPhiQuaHanCoXacNhanAsync()
        {
            var ok = MessageBox.Show("Gửi email nhắc tất cả hội phí quá hạn của kỳ hiện tại?",
                                     "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ok != MessageBoxResult.Yes) return;

            await GuiNhacPhiQuaHanAsync(); // tái sử dụng lệnh cũ
        }


        // ====== Thu/Chi ======
        [RelayCommand]
        private async Task TaiThuChiAsync()
        {
            try
            {
                // ép khoảng ngày thành trọn tháng của DenNgay
                var first = new DateTime(DenNgay.Year, DenNgay.Month, 1);
                var last = new DateTime(DenNgay.Year, DenNgay.Month, DateTime.DaysInMonth(DenNgay.Year, DenNgay.Month));
                TuNgay = first; DenNgay = last;

                var from = DateOnly.FromDateTime(TuNgay);
                var to = DateOnly.FromDateTime(DenNgay);
                var (items, thu, chi) = await _finance.LoadEntriesAsync(from, to, SelectedCategoryId, null);

                ThuChi = new ObservableCollection<FinanceEntry>(items);
                TongThu = thu;
                TongChi = chi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải Thu/Chi: {ex.GetBaseException().Message}");
            }
        }

        [RelayCommand]
        private Task ThemThuChiAsync()
        {
            var catId = DanhMuc.FirstOrDefault()?.CategoryId ?? 0;
            ThuChi.Insert(0, new FinanceEntry
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Amount = 0,
                Description = "",
                CategoryId = catId
            });
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task LuuThuChiAsync(FinanceEntry? item)
        {
            if (item == null) return;
            if (item.CategoryId <= 0)
            {
                MessageBox.Show("Vui lòng chọn danh mục.");
                return;
            }
            try
            {
                await _finance.AddOrUpdateEntryAsync(item);
                await TaiThuChiAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi lưu Thu/Chi: {ex.Message}"); }
        }

        [RelayCommand]
        private async Task XoaThuChiAsync(FinanceEntry? item)
        {
            if (item == null || item.EntryId == 0) { if (item != null) ThuChi.Remove(item); return; }
            try
            {
                await _finance.DeleteEntryAsync(item.EntryId);
                ThuChi.Remove(item);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi xóa Thu/Chi: {ex.Message}"); }
        }

        [RelayCommand]
        private Task HuyThuChiAsync(FinanceEntry? item)
        {
            if (item == null) return Task.CompletedTask;
            if (item.EntryId == 0) ThuChi.Remove(item);
            else _ = TaiThuChiAsync();
            return Task.CompletedTask;
        }

        // ====== Hội phí ======
        [RelayCommand]
        private Task LuuMucHoiPhiAsync()
        {
            try
            {
                var cfg = _settings.Load();
                cfg.DefaultMonthlyFee = MucHoiPhi;
                _settings.Save(cfg);
                MessageBox.Show("Đã lưu mức hội phí mặc định.");
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi lưu cài đặt: {ex.Message}"); }
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task TaoHoiPhiKyAsync()
        {
            try
            {
                int created = await _finance.GenerateMonthlyFeesAsync(KyHienTai);
                MessageBox.Show(created > 0 ? $"Đã tạo {created} mục hội phí cho kỳ {KyHienTai}." : "Kỳ này đã có hội phí cho tất cả thành viên.");
                await TaiHoiPhiAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tạo hội phí: {ex.Message}"); }
        }

        [RelayCommand]
        private async Task TaiHoiPhiAsync()
        {
            try
            {
                var list = await _finance.LoadFeesAsync(KyHienTai, ChiChuaThanhToan, ChiQuaHan, DateOnly.FromDateTime(DateTime.Today));
                HoiPhi = new ObservableCollection<MembershipFee>(list);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tải hội phí: {ex.Message}"); }
        }

        [RelayCommand]
        private async Task DanhDauDaThuAsync(MembershipFee? item)
        {
            if (item == null) return;
            try
            {
                await _finance.MarkPaidAsync(item.FeeId, DateOnly.FromDateTime(DateTime.Today));
                await TaiHoiPhiAsync();
                await TaiThuChiAsync(); // reload Thu/Chi để thấy ngay dòng “Hội phí”
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi đánh dấu: {ex.Message}"); }
        }

        [RelayCommand]
        private async Task GuiNhacPhiQuaHanAsync()
        {
            try
            {
                var overdue = await _finance.LoadFeesAsync(KyHienTai, onlyUnpaid: true, onlyOverdue: true, DateOnly.FromDateTime(DateTime.Today));
                if (overdue.Count == 0) { MessageBox.Show("Không có hội phí quá hạn."); return; }

                var ids = overdue.Select(f => f.MemberId).ToHashSet();
                var emails = await _db.Members.Where(m => ids.Contains(m.MemberId) && m.Email != null && m.Email != "")
                                              .Select(m => m.Email!).ToListAsync();
                if (emails.Count == 0) { MessageBox.Show("Không có email hợp lệ."); return; }

                var cred = await _gmailAuth.AuthorizeAsync(CancellationToken.None);
                var gmailSvc = new GmailService(new Google.Apis.Services.BaseClientService.Initializer
                {
                    HttpClientInitializer = cred,
                    ApplicationName = "BadmintonClub"
                });
                var sender = new GmailSender(gmailSvc);

                string subject = $"Nhắc đóng hội phí kỳ {KyHienTai}";
                string body = $"<p>Xin chào,</p><p>Bạn còn hội phí kỳ <b>{KyHienTai}</b> chưa thanh toán. " +
                              $"Mức phí hiện tại: <b>{MucHoiPhi:N0} VND</b>. Vui lòng hoàn thành sớm để hỗ trợ CLB.</p><p>Xin cảm ơn!</p>";

                await sender.SendAsync("youremail@gmail.com", emails.Distinct(), subject, body, CancellationToken.None);
                MessageBox.Show($"Đã gửi nhắc phí tới {emails.Distinct().Count()} thành viên.");
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi gửi nhắc: {ex.Message}"); }
        }
    }
}

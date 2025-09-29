using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BadmintonClub.Models;
using BadmintonClub.Services;
using BadmintonClub.Services.ML; // AbsenceSample, AbsencePrediction, AbsencePredictor (đặt trong Services/ML)
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Apis.Gmail.v1;
using Microsoft.EntityFrameworkCore;
using OxyPlot;
using OxyPlot.Series;

namespace BadmintonClub.ViewModels;

public partial class AttendanceViewModel : ObservableObject
{
    private readonly BadmintonClubContext _db;
    private readonly GmailAuthService _gmailAuth;
    private readonly SettingsService _settings;

    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string selectedGroup = "TrungBinh";
    [ObservableProperty] private ObservableCollection<Attendance> rows = new();

    [ObservableProperty] private bool showReport = false;
    [ObservableProperty] private int selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private int selectedYear = DateTime.Today.Year;
    [ObservableProperty] private ObservableCollection<MonthlyStat> monthlyStats = new();
    [ObservableProperty] private PlotModel attendancePlotModel = new PlotModel();

    // Nhắc email
    private ReminderOptions? _reminder;
    public ReminderOptions? Reminder
    {
        get => _reminder;
        set => SetProperty(ref _reminder, value);
    }

    // Dự báo vắng
    [ObservableProperty] private ObservableCollection<AbsenceRiskVM> highRisk = new();

    public string[] Groups { get; } = new[] { "Cao", "TrungBinh", "Thap" };
    public string[] StatusOptions { get; } = new[] { "Present", "Absent", "Late", "Excused" };
    public int[] Months { get; } = Enumerable.Range(1, 12).ToArray();
    public int[] Years { get; } = Enumerable.Range(DateTime.Today.Year - 5, 10).ToArray();

    public Action? CommitEditRequested { get; set; }

    public AttendanceViewModel(BadmintonClubContext db, GmailAuthService gmailAuth, SettingsService settings)
    {
        _db = db;
        _gmailAuth = gmailAuth;
        _settings = settings;
    }

    // ========== Điểm danh ==========
    [RelayCommand]
    private async Task Load()
    {
        try
        {
            var d = DateOnly.FromDateTime(SelectedDate);

            var session = await _db.TrainingSessions
                .FirstOrDefaultAsync(s => s.Ngay == d && s.NhomTrinhDo == SelectedGroup);

            if (session == null)
            {
                session = new TrainingSession
                {
                    TenBuoi = $"Buổi {d}",
                    Ngay = d,
                    GioBatDau = new TimeOnly(18, 0),
                    GioKetThuc = new TimeOnly(20, 0),
                    NhomTrinhDo = SelectedGroup
                };
                _db.TrainingSessions.Add(session);
                await _db.SaveChangesAsync();
            }

            var members = await _db.Members
                .Where(m => m.NhomTrinhDo == SelectedGroup)
                .OrderBy(m => m.HoTen)
                .ToListAsync();

            var att = await _db.Attendances
                .Where(a => a.SessionId == session.SessionId)
                .ToListAsync();

            foreach (var m in members)
            {
                if (!att.Any(a => a.MemberId == m.MemberId))
                {
                    _db.Attendances.Add(new Attendance
                    {
                        SessionId = session.SessionId,
                        MemberId = m.MemberId,
                        Status = "Absent"
                    });
                }
            }
            await _db.SaveChangesAsync();

            Rows = new ObservableCollection<Attendance>(
                await _db.Attendances.Where(a => a.SessionId == session.SessionId)
                    .Include(a => a.Member)
                    .OrderBy(a => a.Member.HoTen)
                    .ToListAsync());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi nạp buổi: {ex.GetBaseException().Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            CommitEditRequested?.Invoke();
            await _db.SaveChangesAsync();
            MessageBox.Show("Đã lưu điểm danh.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu điểm danh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ========== Báo cáo tháng (OxyPlot) ==========
    [RelayCommand]
    private void ToggleReport() => ShowReport = !ShowReport;

    [RelayCommand]
    private async Task CalculateReport()
    {
        try
        {
            var from = new DateOnly(SelectedYear, SelectedMonth, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var sessions = await _db.TrainingSessions
                .Where(s => s.Ngay >= from && s.Ngay <= to && s.NhomTrinhDo == SelectedGroup)
                .ToListAsync();

            var totalSessions = sessions.Count;
            if (totalSessions == 0)
            {
                MessageBox.Show("Không có buổi trong tháng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var members = await _db.Members
                .Where(m => m.NhomTrinhDo == SelectedGroup)
                .ToListAsync();

            var stats = new ObservableCollection<MonthlyStat>();
            double avgPresent = 0;
            double avgAbsent = 0;
            int memberCount = members.Count;
            if (memberCount == 0) memberCount = 1;

            foreach (var m in members)
            {
                var present = await _db.Attendances.CountAsync(a => a.MemberId == m.MemberId
                    && sessions.Select(s => s.SessionId).Contains(a.SessionId)
                    && (a.Status == "Present" || a.Status == "Late"));

                var absent = totalSessions - present;

                stats.Add(new MonthlyStat
                {
                    MemberName = m.HoTen,
                    TotalSessions = totalSessions,
                    PresentCount = present,
                    AbsentCount = absent,
                    AttendanceRate = totalSessions > 0 ? (double)present / totalSessions * 100 : 0
                });

                avgPresent += present;
                avgAbsent += absent;
            }

            MonthlyStats = stats;

            var plotModel = new PlotModel { Title = "Tỷ lệ trung bình toàn nhóm" };
            var pieSeries = new PieSeries
            {
                StrokeThickness = 1,
                InsideLabelPosition = 0.6,
                AngleSpan = 360,
                StartAngle = 0
            };
            pieSeries.Slices.Add(new PieSlice("Có mặt", avgPresent / memberCount) { Fill = OxyColors.Green });
            pieSeries.Slices.Add(new PieSlice("Vắng", avgAbsent / memberCount) { Fill = OxyColors.Red });
            plotModel.Series.Add(pieSeries);
            AttendancePlotModel = plotModel;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tính báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ========== Nhắc email qua Gmail ==========
    public class ReminderOptions
    {
        public string Subject { get; set; } = "";
        public string HtmlBody { get; set; } = "";
        public string FromEmail { get; set; } = "";
        // All / RecentAbsent / Manual
        public string Mode { get; set; } = "All";
        public List<RecipientVM> Recipients { get; set; } = new();
    }

    public class RecipientVM
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool Selected { get; set; }
    }

    [RelayCommand]
    private void OpenReminderDialog()
    {
        var activeMembers = _db.Members
            .Where(m => m.TinhTrang == "Active" && m.NhomTrinhDo == SelectedGroup && m.Email != null && m.Email != "")
            .OrderBy(m => m.HoTen).ToList();

        Reminder = new ReminderOptions
        {
            Subject = $"Nhắc buổi tập {SelectedDate:dd/MM} - Nhóm {SelectedGroup}",
            HtmlBody = $"<p>Chào mọi người,</p><p>CLB nhắc lịch tập ngày {SelectedDate:dd/MM} (Nhóm {SelectedGroup}).</p><p>Vui lòng phản hồi xác nhận tham gia.</p>",
            FromEmail = "youremail@gmail.com",
            Mode = "All",
            Recipients = activeMembers.Select(m => new RecipientVM
            {
                Name = m.HoTen,
                Email = m.Email!,
                Selected = true
            }).ToList()
        };

        var dlg = new BadmintonClub.Views.Views.Attendance.ReminderDialog
        {
            Owner = Application.Current?.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive),
            DataContext = this
        };
        dlg.ShowDialog();
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { var _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }

    [RelayCommand]
    private async Task SendReminderAsync()
    {
        if (Reminder == null) { MessageBox.Show("Chưa có thông tin nhắc."); return; }

        IEnumerable<RecipientVM> targets = Reminder.Recipients;

        if (Reminder.Mode == "RecentAbsent")
        {
            var since = SelectedDate.AddDays(-30);
            var recentSessions = await _db.TrainingSessions
                .Where(s => s.Ngay >= DateOnly.FromDateTime(since) && s.NhomTrinhDo == SelectedGroup)
                .Select(s => s.SessionId)
                .ToListAsync();

            var absentMemberIds = await _db.Attendances
                .Where(a => recentSessions.Contains(a.SessionId) && a.Status == "Absent")
                .Select(a => a.MemberId)
                .Distinct()
                .ToListAsync();

            targets = targets.Where(r => _db.Members.Any(m => absentMemberIds.Contains(m.MemberId) && m.Email == r.Email));
        }
        else if (Reminder.Mode == "Manual")
        {
            targets = targets.Where(r => r.Selected);
        }

        var all = targets.Select(t => (t.Email ?? "").Trim()).Distinct().ToList();
        var toList = all.Where(IsValidEmail).ToList();
        if (!IsValidEmail(Reminder.FromEmail))
        {
            MessageBox.Show("Trường From không phải email hợp lệ."); return;
        }
        if (toList.Count == 0) { MessageBox.Show("Không có người nhận phù hợp."); return; }

        try
        {
            var cred = await _gmailAuth.AuthorizeAsync(CancellationToken.None);
            var gmailSvc = new GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "BadmintonClub"
            });
            var sender = new BadmintonClub.Services.GmailSender(gmailSvc);
            await sender.SendAsync(Reminder.FromEmail!, toList, Reminder.Subject, Reminder.HtmlBody, CancellationToken.None);
            MessageBox.Show($"Đã gửi nhắc tới {toList.Count} người.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gửi email thất bại: {ex.Message}");
        }
    }

    // ========== Dự báo vắng (ML.NET) ==========
    [RelayCommand]
    private async Task PredictAbsenceAsync()
    {
        try
        {
            const double threshold = 0.6;
            var modelRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BadmintonClub", "Models");
            var modelPath = Path.Combine(modelRoot, "absence-model.zip");
            if (!File.Exists(modelPath))
            {
                MessageBox.Show("Chưa có model dự báo, hãy huấn luyện trước (absence-model.zip).");
                return;
            }

            var d = DateOnly.FromDateTime(SelectedDate);
            var session = await _db.TrainingSessions.FirstOrDefaultAsync(s => s.Ngay == d && s.NhomTrinhDo == SelectedGroup);
            if (session == null)
            {
                MessageBox.Show("Chưa nạp buổi để dự báo.");
                return;
            }

            var atts = await _db.Attendances
                .Where(a => a.SessionId == session.SessionId)
                .Include(a => a.Member)
                .ToListAsync();

            var predictor = new AbsencePredictor(modelPath);
            var risks = new List<AbsenceRiskVM>();

            foreach (var a in atts)
            {
                var hist = await _db.Attendances
                    .Where(x => x.MemberId == a.MemberId)
                    .Join(_db.TrainingSessions, x => x.SessionId, s => s.SessionId, (x, s) => new { x.Status, s.Ngay })
                    .Where(z => z.Ngay < session.Ngay)
                    .OrderBy(z => z.Ngay)
                    .ToListAsync();

                float rateN(int n)
                {
                    var last = hist.TakeLast(n).ToList();
                    if (last.Count == 0) return 0f;
                    int present = last.Count(z => z.Status == "Present" || z.Status == "Late");
                    return (float)present / last.Count;
                }

                float daysSincePresent()
                {
                    var lastPresent = hist.LastOrDefault(z => z.Status == "Present" || z.Status == "Late");
                    if (lastPresent == null) return 999f;
                    return (float)(session.Ngay.ToDateTime(TimeOnly.MinValue) - lastPresent.Ngay.ToDateTime(TimeOnly.MinValue)).TotalDays;
                }

                var sample = new AbsenceSample
                {
                    AttendanceRate4 = rateN(4),
                    AttendanceRate8 = rateN(8),
                    DaysSincePresent = daysSincePresent(),
                    HourSlot = session.GioBatDau.Hour,
                    DayOfWeek = ((DayOfWeek)((int)session.Ngay.DayOfWeek)).ToString(),
                    GroupLevel = a.Member.NhomTrinhDo,
                    Label = false
                };

                var pred = predictor.Predict(sample);
                if (pred.Probability >= threshold)
                {
                    risks.Add(new AbsenceRiskVM
                    {
                        MemberName = a.Member.HoTen,
                        Email = a.Member.Email ?? "",
                        Probability = pred.Probability
                    });
                }
            }

            HighRisk = new ObservableCollection<AbsenceRiskVM>(risks.OrderByDescending(r => r.Probability));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi dự báo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SendReminderForHighRiskAsync()
    {
        if (HighRisk.Count == 0)
        {
            MessageBox.Show("Chưa có danh sách rủi ro cao để gửi nhắc."); return;
        }

        var toList = HighRisk.Select(r => (r.Email ?? "").Trim()).Where(IsValidEmail).Distinct().ToList();
        if (toList.Count == 0)
        {
            MessageBox.Show("Danh sách rủi ro không có email hợp lệ."); return;
        }

        try
        {
            var cred = await _gmailAuth.AuthorizeAsync(CancellationToken.None);
            var gmailSvc = new GmailService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "BadmintonClub"
            });
            var sender = new BadmintonClub.Services.GmailSender(gmailSvc);

            var subject = $"Nhắc tham gia buổi {SelectedDate:dd/MM} - Nhóm {SelectedGroup}";
            var body = "<p>Hệ thống dự báo bạn có thể vắng buổi tới, vui lòng xác nhận tham gia giúp CLB.</p>";
            var from = Reminder?.FromEmail ?? "youremail@gmail.com";
            if (!IsValidEmail(from)) { MessageBox.Show("From email không hợp lệ."); return; }

            await sender.SendAsync(from, toList, subject, body, CancellationToken.None);
            MessageBox.Show($"Đã gửi nhắc tới {toList.Count} người rủi ro cao.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gửi email thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

// ====== DTOs báo cáo & dự báo ======
public class MonthlyStat
{
    public string MemberName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate { get; set; }
}

public sealed class AbsenceRiskVM
{
    public string MemberName { get; set; } = "";
    public string Email { get; set; } = "";
    public double Probability { get; set; }
}

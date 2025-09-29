// Services/ML/AttendanceFeaturesExtractor.cs
using BadmintonClub.Models;
using Microsoft.EntityFrameworkCore;

namespace BadmintonClub.Services.ML;

public sealed class AttendanceFeaturesExtractor
{
    private readonly BadmintonClubContext _db;

    public AttendanceFeaturesExtractor(BadmintonClubContext db) => _db = db;

    public async Task<List<AbsenceSample>> BuildTrainingSetAsync(string groupLevel)
    {
        // Lấy tất cả buổi của nhóm
        var sessions = await _db.TrainingSessions
            .Where(s => s.NhomTrinhDo == groupLevel)
            .OrderBy(s => s.Ngay)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.SessionId).ToHashSet();

        // Attendance đầy đủ của nhóm
        var attendance = await _db.Attendances
            .Where(a => sessionIds.Contains(a.SessionId))
            .Include(a => a.Member)
            .ToListAsync();

        // Group theo member
        var byMember = attendance.GroupBy(a => a.MemberId).ToDictionary(g => g.Key, g => g.OrderBy(x => sessions.First(s => s.SessionId == x.SessionId).Ngay).ToList());

        var samples = new List<AbsenceSample>();

        foreach (var s in sessions)
        {
            var dow = ((DayOfWeek)((int)s.Ngay.DayOfWeek)).ToString(); // Sunday..Saturday
            var hour = s.GioBatDau.Hour;

            // Attendance của buổi s
            var atts = attendance.Where(a => a.SessionId == s.SessionId).ToList();
            foreach (var a in atts)
            {
                // Lấy lịch sử 8/4 buổi gần nhất TRƯỚC ngày hiện tại
                var hist = byMember[a.MemberId].Where(x => sessions.First(xx => xx.SessionId == x.SessionId).Ngay < s.Ngay).ToList();

                float rateN(int n)
                {
                    var last = hist.TakeLast(n).ToList();
                    if (last.Count == 0) return 0f;
                    int present = last.Count(x => x.Status == "Present" || x.Status == "Late");
                    return (float)present / last.Count;
                }

                float daysSincePresent()
                {
                    var lastPresent = hist.LastOrDefault(x => x.Status == "Present" || x.Status == "Late");
                    if (lastPresent == null) return 999f;
                    var lastDate = sessions.First(xx => xx.SessionId == lastPresent.SessionId).Ngay;
                    return (float)(s.Ngay.ToDateTime(TimeOnly.MinValue) - lastDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
                }

                var label = a.Status == "Absent"; // 1 nếu vắng

                samples.Add(new AbsenceSample
                {
                    AttendanceRate4 = rateN(4),
                    AttendanceRate8 = rateN(8),
                    DaysSincePresent = daysSincePresent(),
                    HourSlot = hour,
                    DayOfWeek = dow,
                    GroupLevel = a.Member.NhomTrinhDo,
                    Label = label
                });
            }
        }
        return samples;
    }
}

using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class Member
{
    public Guid MemberId { get; set; }

    public string MemberCode { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string GioiTinh { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string TinhTrang { get; set; } = null!;

    public int Elo { get; set; }

    public DateOnly NgayThamGia { get; set; }

    public string NhomTrinhDo { get; set; } = null!;

    public string? AvatarPath { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<EloHistory> EloHistories { get; set; } = new List<EloHistory>();

    public virtual ICollection<FinanceEntry> FinanceEntries { get; set; } = new List<FinanceEntry>();

    public virtual ICollection<MembershipFee> MembershipFees { get; set; } = new List<MembershipFee>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}

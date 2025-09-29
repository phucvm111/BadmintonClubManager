using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class BadmintonClubContext : DbContext
{
    public BadmintonClubContext()
    {
    }

    public BadmintonClubContext(DbContextOptions<BadmintonClubContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<EloHistory> EloHistories { get; set; }

    public virtual DbSet<FinanceCategory> FinanceCategories { get; set; }

    public virtual DbSet<FinanceEntry> FinanceEntries { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MembershipFee> MembershipFees { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Tournament> Tournaments { get; set; }

    public virtual DbSet<TournamentEvent> TournamentEvents { get; set; }

    public virtual DbSet<TrainingSession> TrainingSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var ConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(ConnectionString);
        }

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261C9FBF0D73");

            entity.ToTable("Attendance");

            entity.HasIndex(e => e.MemberId, "IX_Att_Member");

            entity.HasIndex(e => e.SessionId, "IX_Att_Session");

            entity.Property(e => e.GhiChu).HasMaxLength(256);
            entity.Property(e => e.LyDoVang).HasMaxLength(256);
            entity.Property(e => e.Status).HasMaxLength(16);

            entity.HasOne(d => d.Member).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Att_Member");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_Att_Session");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AuditLog__A17F23984290758D");

            entity.Property(e => e.Action).HasMaxLength(32);
            entity.Property(e => e.Actor)
                .HasMaxLength(64)
                .HasDefaultValue("Owner");
            entity.Property(e => e.Entity).HasMaxLength(64);
            entity.Property(e => e.EntityId).HasMaxLength(64);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<EloHistory>(entity =>
        {
            entity.HasKey(e => e.EloHistoryId).HasName("PK__EloHisto__7D817B3D1A286C39");

            entity.HasIndex(e => e.MatchId, "IX_Elo_Match");

            entity.HasIndex(e => e.MemberId, "IX_Elo_Member");

            entity.HasIndex(e => e.TournamentId, "IX_Elo_Tournament");

            entity.Property(e => e.Timestamp).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Match).WithMany(p => p.EloHistories)
                .HasForeignKey(d => d.MatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Elo_Match");

            entity.HasOne(d => d.Member).WithMany(p => p.EloHistories)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Elo_Member");

            entity.HasOne(d => d.Tournament).WithMany(p => p.EloHistories)
                .HasForeignKey(d => d.TournamentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Elo_Tour");
        });

        modelBuilder.Entity<FinanceCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__FinanceC__19093A0B9062F59F");

            entity.HasIndex(e => e.Name, "UQ_FinCat_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<FinanceEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId).HasName("PK__FinanceE__F57BD2F77AB1BA11");

            entity.HasIndex(e => e.Date, "IX_Fin_Date");

            entity.HasIndex(e => e.MemberId, "IX_Fin_Member");

            entity.HasIndex(e => e.TournamentId, "IX_Fin_Tournament");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Date).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.Description).HasMaxLength(256);

            entity.HasOne(d => d.Category).WithMany(p => p.FinanceEntries)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fin_Category");

            entity.HasOne(d => d.Member).WithMany(p => p.FinanceEntries)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Fin_Member");
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.MatchId).HasName("PK__Matches__4218C817AD5F3DCE");

            entity.HasIndex(e => e.EventId, "IX_Match_Event");

            entity.HasIndex(e => e.Round, "IX_Match_Round");

            entity.Property(e => e.CourtNo).HasMaxLength(16);
            entity.Property(e => e.PrevMatchAid).HasColumnName("PrevMatchAId");
            entity.Property(e => e.PrevMatchBid).HasColumnName("PrevMatchBId");
            entity.Property(e => e.ScoreJson).HasMaxLength(400);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.TeamAid).HasColumnName("TeamAId");
            entity.Property(e => e.TeamBid).HasColumnName("TeamBId");

            entity.HasOne(d => d.Event).WithMany(p => p.Matches)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK_Match_Event");

            entity.HasOne(d => d.PrevMatchA).WithMany(p => p.InversePrevMatchA)
                .HasForeignKey(d => d.PrevMatchAid)
                .HasConstraintName("FK_Match_PrevA");

            entity.HasOne(d => d.PrevMatchB).WithMany(p => p.InversePrevMatchB)
                .HasForeignKey(d => d.PrevMatchBid)
                .HasConstraintName("FK_Match_PrevB");

            entity.HasOne(d => d.TeamA).WithMany(p => p.MatchTeamAs)
                .HasForeignKey(d => d.TeamAid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Match_TeamA");

            entity.HasOne(d => d.TeamB).WithMany(p => p.MatchTeamBs)
                .HasForeignKey(d => d.TeamBid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Match_TeamB");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasIndex(e => e.NhomTrinhDo, "IX_Members_Group");

            entity.HasIndex(e => e.HoTen, "IX_Members_Name");

            entity.HasIndex(e => e.MemberCode, "UQ_Members_MemberCode").IsUnique();

            entity.Property(e => e.MemberId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AvatarPath).HasMaxLength(260);
            entity.Property(e => e.DiaChi).HasMaxLength(256);
            entity.Property(e => e.DienThoai).HasMaxLength(20);
            entity.Property(e => e.Elo).HasDefaultValue(1000);
            entity.Property(e => e.Email).HasMaxLength(128);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(128);
            entity.Property(e => e.MemberCode).HasMaxLength(32);
            entity.Property(e => e.NgayThamGia).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.NhomTrinhDo).HasMaxLength(16);
            entity.Property(e => e.TinhTrang)
                .HasMaxLength(16)
                .HasDefaultValue("Active");
        });

        modelBuilder.Entity<MembershipFee>(entity =>
        {
            entity.HasKey(e => e.FeeId).HasName("PK__Membersh__B387B22948C0ACBE");

            entity.HasIndex(e => e.Period, "IX_Fee_Period");

            entity.HasIndex(e => new { e.MemberId, e.Period }, "UQ_Fee_Period").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(256);
            entity.Property(e => e.Period)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Member).WithMany(p => p.MembershipFees)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK_Fee_Member");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PK__Teams__123AE799E2B6C785");

            entity.HasIndex(e => e.EventId, "IX_Team_Event");

            entity.Property(e => e.TenDoi).HasMaxLength(128);

            entity.HasOne(d => d.Event).WithMany(p => p.Teams)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK_Team_Event");

            entity.HasMany(d => d.Members).WithMany(p => p.Teams)
                .UsingEntity<Dictionary<string, object>>(
                    "TeamMember",
                    r => r.HasOne<Member>().WithMany()
                        .HasForeignKey("MemberId")
                        .HasConstraintName("FK_TeamMembers_Member"),
                    l => l.HasOne<Team>().WithMany()
                        .HasForeignKey("TeamId")
                        .HasConstraintName("FK_TeamMembers_Team"),
                    j =>
                    {
                        j.HasKey("TeamId", "MemberId").HasName("PK__TeamMemb__82F5E32826724E67");
                        j.ToTable("TeamMembers");
                    });
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasKey(e => e.TournamentId).HasName("PK__Tourname__AC6313131CDF2E0A");

            entity.Property(e => e.GhiChu).HasMaxLength(256);
            entity.Property(e => e.Loai)
                .HasMaxLength(16)
                .HasDefaultValue("Knockout");
            entity.Property(e => e.Status)
                .HasMaxLength(16)
                .HasDefaultValue("Draft");
            entity.Property(e => e.TenGiai).HasMaxLength(128);
        });

        modelBuilder.Entity<TournamentEvent>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Tourname__7944C8101EE2E655");

            entity.HasIndex(e => e.TournamentId, "IX_Event_Tour");

            entity.Property(e => e.GhiChu).HasMaxLength(256);
            entity.Property(e => e.HangMuc).HasMaxLength(16);
            entity.Property(e => e.QuyTacSeed)
                .HasMaxLength(16)
                .HasDefaultValue("ByElo");

            entity.HasOne(d => d.Tournament).WithMany(p => p.TournamentEvents)
                .HasForeignKey(d => d.TournamentId)
                .HasConstraintName("FK_Event_Tour");
        });

        modelBuilder.Entity<TrainingSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Training__C9F492906B6B110C");

            entity.HasIndex(e => e.Ngay, "IX_Sessions_Date");

            entity.Property(e => e.GhiChu).HasMaxLength(256);
            entity.Property(e => e.NhomTrinhDo).HasMaxLength(16);
            entity.Property(e => e.TenBuoi).HasMaxLength(128);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

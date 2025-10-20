
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonClub.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Func<MembersViewModel> _membersFactory;
    private readonly Func<AttendanceViewModel> _attendanceFactory;
    private readonly Func<FinanceViewModel> _financeFactory;
    private readonly Func<TournamentsViewModel> _tournamentsFactory;
    private readonly Func<EloRankingViewModel> _eloRankingFactory;

    [ObservableProperty]
    private object? _currentView;

    public MainViewModel(
        Func<MembersViewModel> membersFactory,
        Func<AttendanceViewModel> attendanceFactory,
        Func<FinanceViewModel> financeFactory,
        Func<TournamentsViewModel> tournamentsFactory,
        Func<EloRankingViewModel> eloRankingFactory)
    {
        _membersFactory = membersFactory;
        _attendanceFactory = attendanceFactory;
        _financeFactory = financeFactory;
        _tournamentsFactory = tournamentsFactory;
        _eloRankingFactory = eloRankingFactory;

        // Mặc định mở trang Thành viên
        NavigateMembers();
    }

    [RelayCommand]
    private void NavigateMembers()
    {
        var vm = _membersFactory();
        // Gọi LoadCommand async (fire-and-forget)
        _ = vm.LoadCommand.ExecuteAsync(null);
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateAttendance()
    {
        CurrentView = _attendanceFactory();
    }

    [RelayCommand]
    private void NavigateFinance()
    {
        CurrentView = _financeFactory();
    }

    [RelayCommand]
    private void NavigateTournaments()
    {
        var vm = _tournamentsFactory();
        // Gọi LoadCommand async
        _ = vm.LoadCommand.ExecuteAsync(null);
        CurrentView = vm;
    }

    [RelayCommand]
    private void NavigateEloRanking()
    {
        var vm = _eloRankingFactory();
        // Gọi LoadCommand async
        _ = vm.LoadCommand.ExecuteAsync(null);
        CurrentView = vm;
    }
}

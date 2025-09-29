using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonClub.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Func<MembersViewModel> _membersFactory;

    [ObservableProperty] private object? currentView;

    public MainViewModel(Func<MembersViewModel> membersFactory)
    {
        _membersFactory = membersFactory;
        NavigateMembers();
    }

    [RelayCommand]
    private void NavigateMembers() => CurrentView = _membersFactory();
    private readonly Func<AttendanceViewModel> _attendanceFactory;
    public MainViewModel(Func<MembersViewModel> membersFactory,
                         Func<AttendanceViewModel> attendanceFactory)
    {
        _membersFactory = membersFactory;
        _attendanceFactory = attendanceFactory;
        NavigateMembers();
    }

    [RelayCommand] private void NavigateAttendance() => CurrentView = _attendanceFactory();

}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SampleApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [RelayCommand]
    private void Login()
    {
        StatusMessage = "Login successful";
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace StarliteDAppWallet.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}

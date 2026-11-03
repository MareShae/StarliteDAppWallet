using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StarliteDAppWallet.ViewModels;

/**
 * login view model ...
 *
 * [glossary]
 * ! [...strutor]
 * ! [.axaml]
 * ! [code-behind]
 * ! [event handlers]
 * ! [class inheritance]
 * ! [records]
 * !
**/
public partial class ProfileViewModel : ContentViewModelBase
{
    /**
     * ! [...strutor] 
     *
    **/

    private readonly static string LOG_ID = "ACCOUNT";

    /** constructor **/

    public ProfileViewModel(string uid, string alias)
    {
        DAppsList = new();

        RootInfo = new RootSelect(uid, alias);

        HDAccountInfo = null;

        // register to the Database changed event
        DatabaseService.Instance.NotifyChanged += OnDatabaseNotifyChanged;
    }

    /** destructor **/

    ~ProfileViewModel() {}


    /**
     * ! [.axaml]
     * ! show view properties
    **/

    public string DerivationTitle => 
        HDAccountInfo is null ? 
        "<Click to Select An Account>" :
        $"{HDAccountInfo?.Alias} #{HDAccountInfo?.HDIndex}";

    [RelayCommand]
    public void OnDerivationTitleClicked ()
    {
        var hdaVM = new HDAccountViewModel(RootInfo);

        /* register events in overlay */
        hdaVM.RequestFetchMessageEvent += OnRequestFetchMessageEvent;
        hdaVM.CloseEvent += OnCloseEvent;
        
        Overlay = hdaVM;
    }

    [ObservableProperty]
    public partial ObservableCollection<string> DAppsList {get; set;}


    [RelayCommand]
    public void OnDAppClicked(string url)
    {
        ;
    }
    
    [ObservableProperty]
    public partial ObservableObject? Overlay {get; set;}



    /**
     * ! [code-behind]
     * ! the logic to the view
    **/

    // this is information on the root (mnemonics)
    private RootSelect RootInfo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivationTitle))]
    // this is information on the current HD Account
    public partial HDAccountSelect?  HDAccountInfo {get; set;}

    // when an overlay (OverlayViewModelBase) invokes a RequestFetchMessageEvent
    private async void OnRequestFetchMessageEvent (object? sender, MainViewModel.OverlayMsgEventArgs e)
    {
        if (e.Message is null) return;

        HDAccountInfo = (HDAccountSelect) e.Message;
    }

    // when an overlay (OverlayViewModelBase) invokes a CloseEvent
    private async void OnCloseEvent (object? sender, EventArgs e)
    {
        Overlay = null;
    }

    /**
     * ! [event handlers]
     * ! events that the vm invokes
     * ! events that the main view handles from other vm
    **/

    private void OnDatabaseNotifyChanged(object? sender, DatabaseService.OperationEventArgs e)
    {
        LogService.Debug("notified check", LOG_ID);
        if (e.Table is null ) return;
        LogService.Debug("notified check I pass", LOG_ID);
        if (e.Key != DatabaseService.KeyExternallyOwnedAccounts.Value) return;
        LogService.Debug("notified check II pass", LOG_ID);
        if (e.Table != DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value) return;
        LogService.Debug("notified check III pass", LOG_ID);
    }


    /**
     * ! [class inheritance]
     * ! properties of inherited class
    **/

    // title of the vm
    public override string ViewTitle => "Account";

    public override event EventHandler<MainViewModel.SwitchContentEventArgs>? RequestSwitchContentEvent;
    public override event EventHandler? RequestBackContentEvent;
    public override event EventHandler? RequestForeContentEvent;

    public override void OnLeaveEvent() {}

    public override void OnFocusEvent() {}


    /**
     * ! [records]
     * ! where they are meant to be
    **/

    public record RootSelect (string UId, string Alias);
    public record HDAccountSelect(int SeedId, string Alias, int DerivationId, int HDIndex);
}

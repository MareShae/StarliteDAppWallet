using System;
using System.Collections.ObjectModel;
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
public partial class LoginViewModel : ContentViewModelBase
{
    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    public LoginViewModel()
    {
        Profiles = new();

        // register to the Database changed event
        DatabaseService.Instance.NotifyChanged += OnDatabaseNotifyChanged;

        // this loads the root list
        LoadMasterList();
    }

    /** destructor **/

    ~LoginViewModel() {}


    /**
     * ! [.axaml]
     * ! show view properties
    **/

    [ObservableProperty]
    public partial ObservableCollection<Root> Profiles {get; set;}

    [RelayCommand]
    public void OnProfileClicked(Root? profile)
    {
        try
        {
            if (profile is null) throw new ArgumentNullException("profile");

            // RequestSwitchContentEvent?.Invoke(
            //     // an empty MnemonicViewModel so that the user can enter/edit their mnemonics
            //     this, new MainViewModel.SwitchContentEventArgs(
            //         new MnemonicViewModel(
            //             MnemonicViewModel.MODE.READONLY,
            //             profile.Date,
            //             profile.Alias
            //         )
            //     )
            // );

            // RequestSwitchContentEvent?.Invoke(
            //     // an empty MnemonicViewModel so that the user can enter/edit their mnemonics
            //     this, new MainViewModel.SwitchContentEventArgs(
            //         new SeedViewModel(profile.UId, profile.Alias)
            //     )
            // );

            RequestSwitchContentEvent?.Invoke(
                // an empty MnemonicViewModel so that the user can enter/edit their mnemonics
                this, new MainViewModel.SwitchContentEventArgs(
                    new ProfileViewModel(profile.UId, profile.Alias)
                )
            );
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.StackTrace} | {e.Message}");
        }
    }

    [RelayCommand]
    public void ImportWallet() {
        RequestSwitchContentEvent?.Invoke(
            // an empty MnemonicViewModel so that the user can enter/edit their mnemonics
            this, new MainViewModel.SwitchContentEventArgs(
                new MnemonicViewModel(MnemonicViewModel.MODE.IMPORT)
            )
        );
    }

    [RelayCommand]
    public void CreateWallet() {
        RequestSwitchContentEvent?.Invoke(
            // a populated MnemonicViewModel so that the user cannot enter/edit their existing mnemonics
            this, new MainViewModel.SwitchContentEventArgs(
                new MnemonicViewModel(MnemonicViewModel.MODE.CREATE)
            )
        );
    }



    /**
     * ! [code-behind]
     * ! the logic to the view
    **/

    private void LoadMasterList()
    {
        try
        {
            // clear any previously listed
            Profiles.Clear();
            
            // get ONLY the [Date, Alias] from the master list
            var select = new DatabaseService.Builder(
                DatabaseService.KeyMaster.Value,
                DatabaseService.KeyMaster.Table.Methods.Value
            ).Select()
            .Set(DatabaseService.KeyMaster.Table.Methods.Column.UId)
            .Set(DatabaseService.KeyMaster.Table.Methods.Column.Alias)
            .Execute();
            
            // put the selected in the list
            if (select is null) throw new Exception ("failed to retrieve from database");
            foreach (var item in select)
            {

                Profiles.Add(new Root(
                    (string) item[0], (string) item[1]
                ));
            }
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.StackTrace} | {e.Message}");
        }
    }



    /**
     * ! [event handlers]
     * ! events that the vm invokes
     * ! events that the main view handles from other vm
    **/

    private void OnDatabaseNotifyChanged(object? sender, DatabaseService.OperationEventArgs e)
    {
        if (e.Table is null ) return;
        if (e.Key != DatabaseService.KeyMaster.Value) return;
        if (e.Table != DatabaseService.KeyMaster.Table.Methods.Value) return;

        LoadMasterList();
    }



    /**
     * ! [class inheritance]
     * ! properties of inherited class
    **/

    // title of the vm
    public override string ViewTitle => "Select or create a profile";

    public override event EventHandler<MainViewModel.SwitchContentEventArgs>? RequestSwitchContentEvent;
    public override event EventHandler? RequestBackContentEvent;
    public override event EventHandler? RequestForeContentEvent;

    public override void OnLeaveEvent() {}

    public override void OnFocusEvent() {}


    /**
     * ! [records]
     * ! where they are meant to be
    **/

    public record Root(string UId, string Alias);
}

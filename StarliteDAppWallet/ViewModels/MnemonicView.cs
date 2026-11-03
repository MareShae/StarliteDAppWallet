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
public partial class MnemonicViewModel : ContentViewModelBase
{
    /**
     * ! [...strutor] 
     *
    **/

    private readonly static string LOG_ID = "MNEMONIC";

    /** constructor **/

    public MnemonicViewModel(MODE mode)
    {   
        Mode = mode;

        switch (Mode) {
            case MODE.CREATE: {
                /* bip0039-generated */
                MessageOnMnemonic = "please protect your mnemonics";
                StatusOnMnemonic = "generating mnemonics ...";
                IsMnemonicReadOnly = true;
                Task.Run(async () =>
                {
                    await BIP0039MnemonicGenerate();
                });
                break;
            }
            case MODE.IMPORT: {
                /* user-pasted */
                MessageOnMnemonic = "please enter your existing mnemonics";
                StatusOnMnemonic = "nothing to show yet";
                IsMnemonicReadOnly = false;
                break;
            }
            default: 
                throw new Exception("unsupported mode");
        }
    }

    public MnemonicViewModel(MODE mode, string UId, string Alias)
    {
        Mode = mode;

        switch (Mode)
        {
            case MODE.READONLY: {
                // TODO:
                /* database-exported */
                MessageOnMnemonic = "please protect your mnemonics";
                IsMnemonicReadOnly = true;
                Task.Run(async () =>
                {
                    await DatabaseLoad(UId);
                });
                break;
            }
            default: 
                throw new Exception("unsupported mode");
        }
    }

    /** destructor **/

    ~MnemonicViewModel() {}


    /**
     * ! [.axaml]
     * ! show view properties
    **/

    [ObservableProperty]
    public partial string? Alias {get; set;}

    [ObservableProperty]
    // the message to be shown concerning the mode and mnemonics
    public partial string? MessageOnMnemonic {get; set;}


    [ObservableProperty]
    // user-entered or auto-retrieved mnemonics
    public partial string? Mnemonic {get; set;}

    /* sets the status, which track the mneomnic size */
    partial void OnMnemonicChanged(string? value)
    {
        // parse
        if (value is null) return;
        StatusOnMnemonic = $"{value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length}-word mnemonics";
    }

    
    [ObservableProperty]
    // set based on mode
    public partial bool? IsMnemonicReadOnly{get; set;}
// 

    [ObservableProperty]
    // auto-populated based on Mnemonic
    public partial string? StatusOnMnemonic {get; set;}

    [RelayCommand]
    public async Task OnContinueFromView()
    {
        try
        {
            if (Mode != MODE.READONLY) {
                LogService.Debug("asserting continue", LOG_ID);

                if (string.IsNullOrWhiteSpace(Alias)) throw new Exception("empty string: Alias");
                if (string.IsNullOrWhiteSpace(Mnemonic)) throw new Exception("empty string: Mnemonic");
                if (Mnemonic.Length < 12) throw new Exception("little entrophy: Mnemonic");

                // encrypt the mnemonic phrase
                LogService.Debug("encrypting seed phrase", LOG_ID);

                var encrypted =  ITrustedExecutionEnvironment.Instance.Encrypt(Mnemonic);
                if (encrypted is null) throw new Exception("failed to encrypt");
                
                /**
                * save to the database:
                * + master
                * + account
                *
                * database sends out an event that the login view listens to
                **/

                string UId = DateTime.UtcNow.ToString();
                
                // save to master
                LogService.Debug($"saving to database: {DatabaseService.KeyMaster.Value}", LOG_ID);
                
                new DatabaseService.Builder(
                    DatabaseService.KeyMaster.Value,
                    DatabaseService.KeyMaster.Table.Methods.Value
                ).Create()
                .Set(DatabaseService.KeyMaster.Table.Methods.Column.UId, UId)
                .Set(DatabaseService.KeyMaster.Table.Methods.Column.Alias, Alias)
                .Set(DatabaseService.KeyMaster.Table.Methods.Column.Method, DatabaseService.KeyExternallyOwnedAccounts.Value)
                .Execute();

                // save to account
                LogService.Debug($"saving to database: {DatabaseService.KeyExternallyOwnedAccounts.Value}", LOG_ID);
                
                new DatabaseService.Builder(
                    DatabaseService.KeyExternallyOwnedAccounts.Value,
                    DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Value
                ).Create()
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.UId, UId)
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Root, encrypted)
                .Execute();
            }

            // requests to be sent backward
            LogService.Debug("requesting to go navigate back", LOG_ID);
            RequestBackContentEvent?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.StackTrace} | {e.Message}");
        }
    }



    /**
     * ! [code-behind]
     * ! the logic to the view
    **/

    public enum MODE
    {
        IMPORT,
        CREATE,
        READONLY
    };

    private MODE Mode;

    private async Task BIP0039MnemonicGenerate()
    {
        try
        {
            Mnemonic = await BIP39Service.Instance.MnemonicGenerate(128);
            if (Mnemonic is null) throw new NullReferenceException("mnemonic not generated");
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }
    }

    private async Task DatabaseLoad(string UId)
    {
        try
        {
            // get the mnemonic from the database
            var select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Value
            ).Select()
             .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Root)
             .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.UId, UId)
             .Execute();
            
            // put the selected in the list
            if (select is null) throw new Exception ("failed to retrieve from database");

            // there should be only one item, one element returned
            // decrypt it
            var decrypted =  ITrustedExecutionEnvironment.Instance.Decrypt((byte[])select[0][0]);
            if (decrypted is null) throw new Exception("failed to decrypt");

            Mnemonic = decrypted;
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }
    }



    /**
     * ! [event handlers]
     * ! events that the vm invokes
     * ! events that the main view handles from other vm
    **/



    /**
     * ! [class inheritance]
     * ! properties of inherited class
    **/

    // title of the vm
    public override string ViewTitle => "Your Mnemonics";

    public override event EventHandler<MainViewModel.SwitchContentEventArgs>? RequestSwitchContentEvent;
    public override event EventHandler? RequestBackContentEvent;
    public override event EventHandler? RequestForeContentEvent;

    public override void OnLeaveEvent() {}

    public override void OnFocusEvent() {}


    /**
     * ! [records]
     * ! where they are meant to be
    **/
}

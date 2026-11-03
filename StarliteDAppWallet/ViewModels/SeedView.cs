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
public partial class SeedViewModel : ContentViewModelBase
{
    /**
     * ! [...strutor] 
     *
    **/

    private readonly static string LOG_ID = "SEED";

    /** constructor **/

    public SeedViewModel(string uid, string alias)
    {
        UId = uid;
        Alias = alias;
        Passphrase = string.Empty;

        Seeds = new();

        LoadSeedsOfAlias ();

        // register to the Database changed event
        DatabaseService.Instance.NotifyChanged += OnDatabaseNotifyChanged;
    }

    /** destructor **/

    ~SeedViewModel() {}


    /**
     * ! [.axaml]
     * ! show view properties
    **/

    [ObservableProperty]
    public partial ObservableCollection<PhraseSeed> Seeds {get; set;}


    [RelayCommand]
    public void OnSeedsClicked(string seed) {}

    /**
     * ! [code-behind]
     * ! the logic to the view
    **/

    private string UId {get; set;}
    private string Alias {get; set;}

    private void LoadSeedsOfAlias ()
    {
        try
        {
            // clear any previously listed
            Seeds.Clear();
            
            // get ONLY the [Date, Alias] from the master list
            var select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value
            ).Select()
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.PassPhrase)
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Seed)
            .Execute();
            
            // put the selected in the list
            if (select is null) throw new Exception ("failed to retrieve from database");
            LogService.Debug($"{select.Count} in the seeds list");
            foreach (var item in select)
            {
                string passphrase = (string) item[0];
                byte[] seed = (byte[]) item[1];
                Seeds.Add(new PhraseSeed(
                    string.IsNullOrWhiteSpace(passphrase) ? "<Default>" : passphrase,
                    seed
                ));
            }
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.StackTrace} | {e.Message}");
        }
        LogService.Debug($"{Seeds.Count} items in Seeds");
    }

    [ObservableProperty]
    public partial string Passphrase {get; set;}
    
    [RelayCommand]
    public void OnNewSeed()
    {
        try
        {
            LogService.Debug("reloading the seeds list", LOG_ID);

            // get the mnemonic from the database
            var select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Value
            ).Select()
             .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Id)
             .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Root)
             .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.UId, UId)
             .Execute();
            
            // put the selected in the list
            if (select is null) throw new Exception ("failed to retrieve from database");

            // there should be only one item, one element returned
            // decrypt it
            LogService.Debug("decrypting mnemonic", LOG_ID);
            string? decrypted =  ITrustedExecutionEnvironment.Instance.Decrypt((byte[])select[0][1]);
            if (decrypted is null) throw new Exception("failed to decrypt");

            // convert the mnemonic and passphrase into a seed
            LogService.Debug("calculating seed", LOG_ID);
            byte[]? seed = BIP39Service.Instance.MnemonicToSeedSync(decrypted, Passphrase);
            if (seed is null) throw new Exception("failed to generate seed");

            // save the passphrase and seed to the account
            LogService.Debug("saving seed to database", LOG_ID);
            new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value
            ).Create()
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.RootsId, (int)select[0][0])
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.PassPhrase, Passphrase)
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Seed, seed)
            .Execute();
        }
        catch (Exception e)
        {
            LogService.Debug(e.ToString());
        }
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

        LoadSeedsOfAlias();
    }


    /**
     * ! [class inheritance]
     * ! properties of inherited class
    **/

    // title of the vm
    public override string ViewTitle => "Seeds";

    public override event EventHandler<MainViewModel.SwitchContentEventArgs>? RequestSwitchContentEvent;
    public override event EventHandler? RequestBackContentEvent;
    public override event EventHandler? RequestForeContentEvent;

    public override void OnLeaveEvent() {}

    public override void OnFocusEvent() {}


    /**
     * ! [records]
     * ! where they are meant to be
    **/

    public record PhraseSeed(string? Passphrase, byte[] Seed);
}

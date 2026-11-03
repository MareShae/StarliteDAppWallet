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
public partial class HDAccountViewModel : OverlayViewModelBase
{
    /**
     * ! [...strutor] 
     *
    **/

    private readonly static string LOG_ID = "ACCOUNT";

    /** constructor **/

    public HDAccountViewModel(ProfileViewModel.RootSelect rootInfo)
    {
        RootUId = rootInfo.UId;
        RootAlias = rootInfo.Alias;
        Passlist = new();
        IsPasslist = true;
        Passphrase = string.Empty;
        PassphrasePlaceholder = "<Default>";
        PathPlaceholder = "<0>";
        HDIndex = string.Empty;

        // register to the Database changed event
        DatabaseService.Instance.NotifyChanged += OnDatabaseNotifyChanged;

        LoadFromDatabase();
    }

    /** destructor **/

    ~HDAccountViewModel() {}


    /**
     * ! [.axaml]
     * ! show view properties
    **/

    [ObservableProperty]
    public partial string RootAlias {get; set;}

    [RelayCommand]
    public void OnAlias()
    {
        IsPasslist = false;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSeedHDPath))]
    public partial bool IsPasslist {get; set;}

    [ObservableProperty]
    public partial ObservableCollection<ProfileViewModel.HDAccountSelect> Passlist {get; set;}

    [RelayCommand]
    public void OnPassClicked(ProfileViewModel.HDAccountSelect hdAccountSelect) {

        RequestFetchMessageEvent?.Invoke(
            this, new MainViewModel.OverlayMsgEventArgs (
                hdAccountSelect
        ));

        CloseEvent?.Invoke(this, EventArgs.Empty);

    }

    // ObservableProperty
    public bool IsSeedHDPath => ! IsPasslist;

    [ObservableProperty]
    public partial string Passphrase {get; set;}

    [ObservableProperty]
    public partial string PassphrasePlaceholder {get; set;}

    [ObservableProperty]
    public partial string PathPlaceholder {get; set;}

    [ObservableProperty]
    public partial string HDIndex {get; set;}

    [RelayCommand]
    public void OnSaveSeedHDPath()
    {

        try
        {
            /* save to both Seeds and Derivations (if they do not exist) */

            // 1. check if the Passphrase has already been used for the Roots.Id in Seeds
            //     if it has not, create it
            //     get the seedId

            // 2. chech if the HDIndex has already been used for the Seeds.Id in Derivations
            
            // get the mnemonic from the database
            var select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Value
            ).Select()
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Id)
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Root)
            .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.UId, RootUId)
            .Execute();
            if (select is null) throw new Exception ("failed to retrieve from database");

            // extract root values
            int rootId = (int)select[0][0];
            byte[] rootMnemonics = (byte[])select[0][1];

            int? seedId = null;
            
            // encrypt the passphrase
            byte[]? passEncrypted = ITrustedExecutionEnvironment.Instance.Encrypt(Passphrase);
            if (passEncrypted is null) throw new Exception($"failed to encrypt passphrase");

            // try twice to get seedId
            for (int i = 0; i < 2; i++)
            {
                // for the Roots.Id, check if this Passphrase has been used for an associated Seed
                select = new DatabaseService.Builder(
                    DatabaseService.KeyExternallyOwnedAccounts.Value,
                    DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value
                ).Select()
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Id)
                // where root and passphrase match
                .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.RootsId, rootId)
                .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.PassPhrase, passEncrypted)
                .Execute();
                if (select is null) throw new Exception ("failed to retrieve from database");
                
                // if it has, get its Seeds.Id
                if (select.Count > 0)
                {
                    LogService.Debug($"Seeds.Id found at {i}", LOG_ID);
                    seedId = (int) select[0][0];
                    break;
                }
                if (i > 0)
                {
                    throw new Exception("fetch Seeds.Id trials exceed");
                }

                // if it has not, create it
                // 1. decrypt the mnemonic
                // 2. convert the mnemonic and passphrase into a seed
                // 3. encrypt the seed
                LogService.Debug("compositing new seed from passphrase", LOG_ID);
                byte[]? seedEncrypted = ITrustedExecutionEnvironment.Instance.Encrypt(
                    BIP39Service.Instance.MnemonicToSeedSync(
                        ITrustedExecutionEnvironment.Instance.Decrypt(
                            rootMnemonics
                        )!,
                        Passphrase
                    )
                );
                if (seedEncrypted is null) throw new Exception("failed to composite seed");
                
                // save the passphrase and seed to the account
                LogService.Debug($"saving Seed, Passphrase to Seeds {i}", LOG_ID);
                new DatabaseService.Builder(
                    DatabaseService.KeyExternallyOwnedAccounts.Value,
                    DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value
                ).Create()
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.RootsId, rootId)
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Alias, GenerateAliasForPassphrase())
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.PassPhrase, passEncrypted)
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Seed, seedEncrypted)
                .Execute();

                // on the second loop, it should find the newly created Seeds.Id
            }
            if (seedId is null) throw new Exception ("failed to retrieve seedId");

            // for the Seeds.Id, check if the HDIndex has already been used in Derivations
            select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Value
            ).Select()
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Column.Id)
            .Execute();
            if (select is null) throw new Exception ("failed to retrieve from database");
            
            if (select.Count > 0) throw new Exception("Seed#Path already exists");

            // save the derivation to the account
            int hdIndex = string.IsNullOrEmpty(HDIndex) ? 0: int.Parse(HDIndex);
            LogService.Debug("saving HDPath to Derivations", LOG_ID);
            new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Value
            ).Create()
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Column.SeedsId, seedId)
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Column.HDIndex, hdIndex)
            .Execute();

            OnCloseSeedHDPath();
        }
        catch (Exception e)
        {
            LogService.Debug(e.ToString());
        }
    }

    [RelayCommand]
    public void OnCancelSeedHDPath()
    {
        OnCloseSeedHDPath();
    }




    /**
     * ! [code-behind]
     * ! the logic to the view
    **/

    private string RootUId;

    public void OnCloseSeedHDPath()
    {
        Passphrase = string.Empty;
        HDIndex = string.Empty;
        IsPasslist = true;
    }

    private string GenerateAliasForPassphrase()
    {
        return $"Wallet {DateTime.UtcNow}";
    }

    private void LoadFromDatabase()
    {
        try
        {
            // load the Derivations and their Seeds
            // strings them like ${passphrase}#${hdindex}
            
            Passlist.Clear();

            /* load the heirarchy from the database */

            // first get the Id of the Root given its UId
            // the Id is used to get Seeds associated with it
            var select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Value
            ).Select()
             // select just the Id column
             .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.Id)
             // where UId matches
             .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Roots.Column.UId, RootUId)
             .Execute();
            if (select is null) throw new Exception ("failed to retrieve from database");

            // only one value is expected to match
            int rootId = (int) select[0][0];

            // now it goes into the Seeds Tables
            // it fetches all the values associated with the Roots.Id
            
            // get the passphrase
            select = new DatabaseService.Builder(
                DatabaseService.KeyExternallyOwnedAccounts.Value,
                DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value
            ).Select()
            // fetch Id, Passphrase
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Id)
            .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.Alias)
            .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Column.RootsId, rootId)
            .Execute();
            if (select is null) throw new Exception ("failed to retrieve from database");
            
            // each Seed can have multiple Derivations
            foreach (var seedItem in select)
            {
                // extract seedId from the item
                int seedId = (int) seedItem[0];
                string seedAlias = (string) seedItem[1];

                var drvSelect = new DatabaseService.Builder(
                    DatabaseService.KeyExternallyOwnedAccounts.Value,
                    DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Value
                ).Select()
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Column.Id)
                .Set(DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Column.HDIndex)
                .Where(DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Column.SeedsId, seedId)
                .Execute();
                if (drvSelect is null) throw new Exception ("failed to retrieve from database");
                
                foreach (var drvItem in drvSelect)
                {
                    int drvId = (int)drvItem[0];
                    int drvHDIndex = (int)drvItem[1];
                    // append to the list
                    Passlist.Add(new ProfileViewModel.HDAccountSelect (
                        seedId,
                        seedAlias,
                        drvId,
                        drvHDIndex
                    ));
                }
            }
        }
        catch (Exception e)
        {
            LogService.Debug(e.ToString());
        }
        LogService.Debug($"Passlist has {Passlist.Count} items");
    }



    /**
     * ! [event handlers]
     * ! events that the vm invokes
     * ! events that the main view handles from other vm
    **/

    private async void OnDatabaseNotifyChanged(object? sender, DatabaseService.OperationEventArgs e)
    {
        if (e.Table is null ) return;
        if (e.Key != DatabaseService.KeyExternallyOwnedAccounts.Value) return;
        if (e.Table != DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Value) return;

        LoadFromDatabase();
    }


    /**
     * ! [class inheritance]
     * ! properties of inherited class
    **/

    public override event EventHandler<MainViewModel.OverlayMsgEventArgs>? RequestFetchMessageEvent;
    public override event EventHandler? CloseEvent;



    /**
     * ! [records]
     * ! where they are meant to be
    **/
}

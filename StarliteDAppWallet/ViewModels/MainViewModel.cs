using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarliteDAppWallet.ViewModels;



/**
 * main view model is simply a container for other view models
 *
 * this section however underlines the blueprint of the application
 * 
 * [1. purpose]
 * in my opinion,
 * * a wallet simply needs its keys
 * * a dapps browser simply needs a browser and a signing method
 * * anything a brand-wallet offers can be accessed through a DApps browser
 *   obviously with its limitations
 *
 * the main objectives of this application is 
 * * something that is truely yours
 *   though it can feel advanced for beginners
 * * exclusively mnemonics login
 *   no wallet tied to social handle
 * * a minimal wallet without all the bloat features
 *   it is literally just a wallet
 * * a massive multi-blockchain wallet
 *   main blockchain and virtual machine support
 * * an in-built dapps browser
 * * air connect (such as wallet-connect)
 *   so it is simply used as storage
 *   though it cannot be a real cold-storage
 *
 * [2. blueprint]
 *
 * [2.? Mnemonics]
 * the app has the following mnemonic features:
 * * importing an existing mnemonic sentence
 * * creating an a new mnemonic sentence
 *
 * ! to avoid problems, a mnemonic is not editable once entered into the system
 * a wrong mnemonics would have to be entered if the previous one was already saved
 *
 * ! for security, mnemonics are encrypted using Trusted Environment, if available, or falls back to software encryption
 *
 * [2.? Master/Key]
 * a profile is created for every new mnemonic
 * 
 * ! to avoid problems and duplicates, a profile is unique to a mnemonic
 *
 * profiles are saved into the database
 *
 * [2.? Master/Seeds]
 * sub-profiles are created when users include passphrases (the 25th word)
 *
 * a passphrase does not grant access like a vault does, in the traditional sense,
 * rather it is for further entropy used to calculate the seed,
 * which is then used to derive blockchain addresses
 *
 * as such passphrases are non-recoverable as with mnemonics
 *
 * by default, passphrases are empty
 *
 * ! for security, mnemonics are encrypted using Trusted Environment, if available, or falls back to software encryption
 *
 * [2.? Blockchain]
 * every seed chooses their blockchains and for blockchains their tokens
 *
 * [2.? HierarchicalDeterministic/Address]
 * seeds are used to derive multiple HD private/public addr/keypairs
 * every blockchain generates private/public address
 *
 * [2.? Overview]
    [{
        "mnemonic": ...,
        "seeds": [{
            "passphrase": ...,
            "seed": ...,
            "blockchain": [{
                "name": ...,
                "tokens": [],
                "addresses": {
                    $derivation: $value
                }
            }]
        },
        {...}]
    }, 
    {...}]
 *
 * [3. security]
 * for security, mnemonics are encrypted using Trusted Environment, if available
 * it falls back to software encryption like AES
 *
 * on Android, encryption is through the KeyStore
 *
 * [4. databases]
 * master database contains the WalletType
 * 
 * masterV$ database contains the blueprint as db sqlite
 *
 * Table: BlockChainMethods
 * BlockChain | Method
 *
 * Externally Owned Accounts
 *
 * Table 1: Roots
 * Id | Mnemonic
 *
 * Table 2: Seeds
 * Id | RootsId | PassPhrase | Seed
 * 
 * Table 3: Networks
 * Id | SeedsId | Chain | Token 
 * 
 * Table 4: Derivation
 * Id | NetworksId | PathIndex | Address
 *
 *
 *
 *
 *
 *
 *
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
public partial class MainViewModel : ViewModelBase
{
    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    public MainViewModel()
    {    
        // TestBIP0039: badge vessel zebra steel conduct rather capable trip service eight state rival
        // Dev: rifle virtual doctor shock zebra rocket silk trumpet old lock stumble economy

        try {
            // // delete the table for recreation
            // DatabaseService.Instance.Delete(
            //     DatabaseService.KeyExternallyOwnedAccounts.Value,
            //     DatabaseService.KeyExternallyOwnedAccounts.Table.Seeds.Value
            // );
            // DatabaseService.Instance.Delete(
            //     DatabaseService.KeyExternallyOwnedAccounts.Value,
            //     DatabaseService.KeyExternallyOwnedAccounts.Table.Derivations.Value
            // );
            // DatabaseService.Instance.Delete(
            //     DatabaseService.KeyExternallyOwnedAccounts.Value,
            //     DatabaseService.KeyExternallyOwnedAccounts.Table.Networks.Value
            // );

            
            LogService.Debug("In MainView Model");
            CurrentViewTitle = null;
            CurrentView = null;

            LogService.Debug("Landing on the Login page");
            NavigateToView(new LoginViewModel());
        }
        catch (Exception e)
        {
            LogService.Debug($"unable to start main > {e.Message}");
        }

        #if ANDROID
        LogService.Debug($"system is android");
        #endif
    }

    /** destructor **/

    ~MainViewModel() {}



    /**
     * ! [.axaml]
     * ! show view properties
    **/

    [ObservableProperty]
    public partial string? CurrentViewTitle {get; set;}
    
    [ObservableProperty]
    public partial ObservableObject? CurrentView {get; set;}

    // current page on property changed
    partial void OnCurrentViewChanged(ObservableObject? value)
    {
        if (value is null) return;

        // modify the title of the navigated page
        CurrentViewTitle = ((ContentViewModelBase)value).ViewTitle;
    }
    


    /**
     * ! [code-behind]
     *
    **/

    private Stack<ContentViewModelBase> _backstack = new();
    private Stack<ContentViewModelBase> _forestack = new();

    /**
     * attaches the request View
     * attaches event listener
    **/
    private void NavigateToView(ContentViewModelBase? view)
    {
        if (CurrentView is not null)
        {
            _backstack.Push((ContentViewModelBase)CurrentView);
        }
        _forestack.Clear();

        CurrentView = view;

        /* common inherited event(s) */
        view?.RequestSwitchContentEvent += OnRequestSwitchContentEvent;
        view?.RequestBackContentEvent += OnRequestBackContentEvent;
        view?.RequestForeContentEvent += OnRequestForeContentEvent;
    }

    private void NavigateBack()
    {
        try
        {
            if (CurrentView is not null)
            {
                // notify the page on leave
                ((ContentViewModelBase)CurrentView).OnLeaveEvent();
                // insert into the [fore] stack
                _forestack.Push((ContentViewModelBase)CurrentView);
            }

            // pluck the last in the [back] stack
            CurrentView = _backstack.Pop();
            if (CurrentView is not null) {
                // notify the page on focus
                ((ContentViewModelBase)CurrentView).OnFocusEvent();
            }
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.Source} | {e.Message}");
        }
    }

    public void NavigateFore()
    {
        try
        {
            if (CurrentView is not null)
            {   
                // notify the page on leave
                ((ContentViewModelBase)CurrentView).OnLeaveEvent();
                // insert into the [back] stack
                _backstack.Push((ContentViewModelBase)CurrentView);
            }

            // pluck the last in the [fore] stack
            CurrentView = _forestack.Pop();
            if (CurrentView is not null) {
                // notify the page on focus
                ((ContentViewModelBase)CurrentView).OnFocusEvent();
            }
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.Source} | {e.Message}");
        }
    }


    /**
     * when a ViewModel that inherits from ContentViewModelBase
     * requests for MainVM to switch to a new ViewModel
    **/
    private void OnRequestSwitchContentEvent(object? sender, SwitchContentEventArgs e)
    {
        NavigateToView(e.View);
    }

    private void OnRequestBackContentEvent(object? sender, EventArgs e)
    {
        NavigateBack();
    }

    private void OnRequestForeContentEvent(object? sender, EventArgs e)
    {
        NavigateFore();
    }



    /**
     * ! [event handlers]
     * ! events that the vm invokes
     * ! events that the main view handles from other vm
    **/

    /**
     * when a ViewModel that inherits from ContentViewModelBase
     * wants to request a new ViewModel
     * it uses this EventArgs which takes the current ViewModel
     * that was created by the previous ViewModel
     * 
     * it is handled by OnRequestSwitchContentEvent
    **/
    
    public class SwitchContentEventArgs: EventArgs
    {   
        /* Switchable ContentViewModelBase for the Main View Model  */
        
        public ContentViewModelBase View;

        /* constructor */

        public SwitchContentEventArgs (ContentViewModelBase view)
        {
            View = view;
        }
    }

    public class OverlayMsgEventArgs: EventArgs
    {
        public object? Message;

        public OverlayMsgEventArgs (object? message)
        {
            Message = message;
        }
    }

}



/**
 * this abstract class is used by all vms that are injected into contextcontrol.
 * it defines required properties that are used by the main view in switching context.
 *
 * since they are abstract c# requires that all its children overide the properties.
 * no defaults are given.
**/
public abstract class ContentViewModelBase : ViewModelBase
{
    // used by the navigation page title   
    abstract public string ViewTitle {get;}

    // used for navigation to other views
    abstract public event EventHandler<MainViewModel.SwitchContentEventArgs>? RequestSwitchContentEvent;

    // request back navigation
    abstract public event EventHandler? RequestBackContentEvent;

    // request fore navigation
    abstract public event EventHandler? RequestForeContentEvent;

    // called when context control switches away
    abstract public void OnLeaveEvent();

    // called when context control switches to (not on new)
    abstract public void OnFocusEvent();
}

public abstract class OverlayViewModelBase : ViewModelBase

{
    abstract public event EventHandler<MainViewModel.OverlayMsgEventArgs>? RequestFetchMessageEvent;

    // request back navigation
    abstract public event EventHandler? CloseEvent;
}




    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    // public ViewModel() {}

    /** destructor **/

    // ~ViewModel() {}


    /**
     * ! [.axaml]
     * ! show view properties
    **/



    /**
     * ! [code-behind]
     * ! the logic to the view
    **/



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
    // public override string ViewTitle => "";

    // public override void OnLeaveEvent() {}

    // public override void OnFocusEvent() {}


    /**
     * ! [records]
     * ! where they are meant to be
    **/
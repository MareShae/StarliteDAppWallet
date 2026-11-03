
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using StarliteDAppWallet.ViewModels;

namespace StarliteDAppWallet.Views;

public partial class dAppsView : UserControl
{
    public dAppsView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            LogService.Debug("DataContext changed for dAppsView");
            if (DataContext is dAppsViewModel dAVM)
            {
                LogService.Debug("DataContext register events");
                dAVM.WebViewChanged += OnWebViewChanged;
                dAVM.CurrentWebViewNavigationStarted += OnCurrentWebViewNavigationStarted;
                dAVM.CurrentWebViewNavigationCompleted += OnCurrentWebViewNavigationCompleted;
            }
        };
    }

    private void OnWebViewChanged(object? sender, dAppsViewModel.WebViewSwitchEventArgs e)
    {
        LogService.Debug("Attempting to set SafeWebView");

        // the name of the webview in the current tab
        string WEBVIEW_NAME = "JUDWFF9IDPWE0W";

        // remove the previous, if it exists
        var PrevWebView = WebViewGrid.Children.FirstOrDefault((p) => p.Name == WEBVIEW_NAME);
        if (PrevWebView is not null)
        {
            WebViewGrid.Children.Remove(PrevWebView);
        }

        // set the name of the CurrentWebView
        e.View.Name = WEBVIEW_NAME;

        // set the grid row of the view
        Grid.SetRow(e.View, 1);


        // add it to the named grid
        WebViewGrid.Children.Add(e.View);
    }

    // this is called in response to key down events in the textbox
    // it passes on all except Enter
    public void OnCurrentWebAddrKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter) return;

        var parameter = CurrentWebAddrEditGo.CommandParameter;
        if (CurrentWebAddrEditGo.Command!.CanExecute(parameter))
        {
            CurrentWebAddrEditGo.Command.Execute(parameter);
        }
        
    }

    // change text colour to indicate that Navigation Started
    public void OnCurrentWebViewNavigationStarted(object? sender, EventArgs e) {
        CurrentWebAddrBox.Foreground = Brushes.LightBlue;
    }

    // return text colour to indicate that Navigation Completed
    public void OnCurrentWebViewNavigationCompleted(object? sender, EventArgs e)
    {
        CurrentWebAddrBox.Foreground = Brushes.Black;
    }
}
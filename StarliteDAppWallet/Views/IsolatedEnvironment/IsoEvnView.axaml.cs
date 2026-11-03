using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarliteDAppWallet.Controls;

namespace StarliteDAppWallet.Views;

public partial class IsoEvnView : UserControl
{
    public IsoEvnView()
    {
        InitializeComponent();
    }

    public void CommandPlus(object? sender, RoutedEventArgs e)
    {
        LogService.Debug("Attempting javascript webview");
        
        try
        {
            // empty top-level for .js environment
            dummywebview.Navigate(new Uri("https://avaloniaui.net"));
            
            // swv.NavigateToString(SafeWebView.EmptyTopLevel);
        }
        catch (Exception ex)
        {
            LogService.Debug($"{ex.Message}");
        }
    }

    private async void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        LogService.Debug("OnNavigationStarted");

        LogService.Debug($"assert persistent function");
        await ((Web3View)sender!).InvokeScript("function persist_in_start() {}");
    }

    private async void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e) {

        LogService.Debug("OnNavigationCompleted");

        LogService.Debug($"assert invokeCSharpAction");

        await ((Web3View)sender!).ScriptBridgeReady();

        await ((Web3View)sender!).InvokeScript(
            """
            function persist_in_compt() {
                invokeCSharpAction("{'key': 10}");
            }
            """
        );
        await ((Web3View)sender!).InvokeScript("persist_in_compt();");

        string? result = await ((Web3View)sender!).InvokeScript("typeof persist_in_start");
        LogService.Debug($".persist_in_start is > {result}");
    }

    private async void OnMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        LogService.Debug($"webmessage received > {e.Body}");
    }
}
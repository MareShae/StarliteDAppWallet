using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace StarliteDAppWallet.Controls;


/**
 * an inherited WebView with rules for security
 *
 * javascript is enabled to help process wallet configuration
 *
 * security includes:
 * - https:// mode must always be enabled
 *
 * [glossary]
 * ! [...structor]
 * ! [attached properties]
 * ! [code-behind]
**/

public class Web3View: NativeWebView
{
    /**
     * ! [...strutor] 
     *
    **/

    static public readonly string EmptyTopLevel = @"
    <!DOCTYPE html>
    <html>
    <head></head>
    <body></body>
    </html>";

    /** constructor **/

    public Web3View()
    {
        /** set properties **/
    }



    /**
     * ! [attached properties]
     *
    **/



    /**
     * ! [code-behind]
     *
    **/

    /**
     * invokeCSharpAction is a .NET native messaging function
     *
     * it is not available immediately after NavigationComplete event
     * although, .js scripts still work before this is loaded
     *
     * rather than relying on in-InvokeScript to determine availability
     * wait for invokeCSharpAction to be available
     * although it is primitive
     * 
    **/
    public async Task ScriptBridgeReady()
    {
        try {
            LogService.Debug("waiting for invokeCSharpAction");
            string? result;
            while (true)
            {
                result = await this.InvokeScript("typeof invokeCSharpAction");
                if (result == "\"function\"")
                {
                    LogService.Debug("invokeCSharpAction found");
                    return;
                }
                LogService.Debug($"Avalonia bridge not ready yet {result}. Retry in 50ms");
                await Task.Delay(50);
            }
        }
        catch (Exception exception)
        {
            LogService.Debug($"wait failed: {exception.Message}");
        }
    }

}
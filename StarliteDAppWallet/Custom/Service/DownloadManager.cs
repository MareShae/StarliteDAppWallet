using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

/**
 * a singleton class for downloading assets
 * 
 * [glossary]
 * ! [singleton]
 * ! [...strutor]
 * ! [code-behind]
 * ! [event handlers]
 * ! [records]
**/
class DownloadManager
{
    /**
     * ! [singleton]
     * ! only one instance permitted
    **/ 
    
    public static DownloadManager Instance
    {
        private set
        {
            field = value;
        }
        get
        {
            field = (field is not null)? field : new DownloadManager();
            return field;
        }
    }


    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    private DownloadManager()
    {
        client.DefaultRequestHeaders.Add("User-Agent", "StarliteDAppWallet");
    }

    /** destructor **/

    ~DownloadManager() {}



    /**
     * ! [code-behind]
    **/ 

    private static readonly HttpClient client = new HttpClient();

    public static async Task DownloadAssetAsync(string url, string dst)
    {
        try
        {
            LogService.Debug($"Downloading from {url} to {dst}");

            // verify the destination
            if (! Directory.Exists(Path.GetDirectoryName(dst))) {
                // TODO: change dir to cache until re-attempt and copy
                throw new Exception("cannot download to non-existant directory");
            }

            // include user-agent
            

            // get network stream from server
            LogService.Debug($"Getting network stream for {url}");
            using (Stream netstream = await client.GetStreamAsync(url)) {
                // open file stream on disk
                LogService.Debug($"Getting file stream for {dst}");
                using (FileStream filestream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // save to local file directly from network
                    await netstream.CopyToAsync(filestream);
                }
            }

            LogService.Debug($"Download complete");
        }
        catch (Exception e)
        {
            LogService.Debug($"Download failed: {e.Message}");
        }
    }
}
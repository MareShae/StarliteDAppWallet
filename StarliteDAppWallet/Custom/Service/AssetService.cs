



using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StarliteDAppWallet;


/**
 * a singleton class for assets folders and files
 * 
 * [glossary]
 * ! [singleton]
 * ! [...strutor]
 * ! [code-behind]
 * ! [event handlers]
 * ! [records]
**/
class AssetService
{
    /**
     * ! [singleton]
     * ! only one instance permitted
    **/ 
    
    public static AssetService Instance
    {
        private set
        {
            field = value;
        }
        get
        {
            field = (field is not null)? field : new AssetService();
            return field;
        }
    }


    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    private AssetService()
    {
        root = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );

        if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
        {
            root = Path.Combine(root, App.PackageName);
        }

        /**
         * Construct dir if they do not exist
         * (let the '/' at the end indicate it is a directory)
         *
         * root/
         *    |assets/
         *       |_ bip39/
         *          |_ english.txt
         *    |databases/
        **/

        DirHelper dirHelper = new DirHelper(
            root,
            new List<DirHelper>
            {
                new DirHelper ("assets/", new List<DirHelper> {
                    new DirHelper ("bip0039/", new List<DirHelper> {
                        new DirHelper("english.txt", (self, dstpath) => {
                            // check if the files is at the dir
                            string srcpath = URLs["bip-0039/english.txt"];
                            if (! File.Exists(dstpath))
                            {
                                // dispatch to background
                                LogService.Debug("Passing to Download Manager");
                                Task.Run(async () => await DownloadManager.DownloadAssetAsync(srcpath, dstpath)).Wait();
                            }
                            PATH_BIP0039_ENGLISH = dstpath;
                            return File.Exists(dstpath);
                        })
                    })
                }),
                new DirHelper ("databases/", child:null, preinstruct:null, overrideinstruct:null, postinstruct:(self, dstpath) =>
                {
                    LogService.Debug($"Assigining database folder to {dstpath}");
                    // simply sets the path to the database/ dir
                    // the DatabaseService handles its connections
                    PATH_DATABASES = dstpath;
                    return Directory.Exists(dstpath);
                })
            }
        );
        (bool status, string? detail) = dirHelper.MkDir(null);
        if (! status) LogService.Debug($"Failed to complete MkDir: {detail}");
        LogService.Debug("DirHelper done");
    }

    /** destructor **/

    ~AssetService() {}



    /**
     * ! [code-behind]
    **/ 

    public string? root { get; private set; }

    private Dictionary<string, string> URLs = new()
    {
        {"bip-0039/english.txt", "https://raw.githubusercontent.com/bitcoin/bips/master/bip-0039/english.txt"}
    };

    public string? PATH_BIP0039_ENGLISH {get; private set;}
    public string? PATH_DATABASES {get; private set;}

    // get line based on the lambda
    public (bool, string?, string?) GetLineFromFile(string path, Func<string, bool>select)
    {
        try
        {
            if (! File.Exists(path)) throw new Exception("cannot open file that does not exist");

            using (StreamReader reader = new StreamReader(path))
            {
                string? result;
                while ((result = reader.ReadLine()) is not null)
                {
                    if (! select(result)) continue;
                    return (true, null, result);
                }
            }
        }
        catch (Exception e)
        {
            return (false, e.Message, null);
        }

        return (false, null, null);
    }

    // get line at the index zero-based index (i.e starts from 0)
    public (bool, string?, string?[]?) GetLinesFromFile(string path, int[] indices)
    {
        try
        {
            if (! File.Exists(path)) throw new Exception("cannot open file that does not exist");

            string?[]? result = new string[indices.Length];

            using (StreamReader reader = new StreamReader(path))
            {
                // read each line of the file
                string? line;
                int lineIdx = 0;

                while ((line = reader.ReadLine()) is not null)
                {
                    // check indices for all
                    int fill = 0;
                    for (int i = 0; i < indices.Length; i++)
                    {
                        // include the line if the indices match
                        if (lineIdx == indices[i])
                        {
                            result[i] = line;
                        }
                        // if it is filled, mark it
                        if (result[i] is not null) fill++;
                    }

                    // break if all indices are filled
                    if (fill == indices.Length) {
                        LogService.Debug($"filled lines from file > {path}");
                        break;
                    }

                    // increment to start at 1
                    lineIdx++;

                }
            }
            return (true, null, result);
        }
        catch (Exception e)
        {
            return (false, e.Message, null);
        }
    }
}




file class DirHelper
{
    public string? Name {get; private set;}

    // creation instruction
    // args nothing, returns bool
    public Func<DirHelper, string, bool>? PreInstruct {get; private set;}
    public Func<DirHelper, string, bool>? OverrideInstruct {get; private set;}
    public Func<DirHelper, string, bool>? PostInstruct {get; private set;}
    public List<DirHelper>? Children {get; private set;}

    public DirHelper(
        string name,
        Func<DirHelper, string, bool>? preinstruct,
        Func<DirHelper, string, bool>? overrideinstruct,
        Func<DirHelper, string, bool>? postinstruct,
        List<DirHelper>? child=null)
    {
        Name = name;
        PreInstruct = preinstruct;
        OverrideInstruct = overrideinstruct;
        PostInstruct = postinstruct;
        Children = child;
    }

    // specially for directories
    // only directories can have child dirs
    public DirHelper(string name, List<DirHelper>? child=null)
    {
        // include directory symbol
        Name = name.EndsWith('/')? name: $"{name}/";
        PreInstruct = null;
        OverrideInstruct = null;
        PostInstruct = null;
        Children = child;
    }

    // specifically for files
    // e.g download instead of create
    public DirHelper(string name, Func<DirHelper, string, bool>? overrideinstruct)
    {
        Name = name;
        PreInstruct = null;
        OverrideInstruct = overrideinstruct;
        PostInstruct = null;
        Children = null;
    }

    public (bool, string?) MkDir(string? parent)
    {
        try
        {
            // name cannot be null
            if (Name is null) throw new Exception ($"invalid directory: {Name}");

            // combine to absolute path
            string path = parent is null? string.Empty : parent;
            path = Path.Combine(path, Name);

            // path is not relative, it is absolute
            path = Path.GetFullPath(path);
            LogService.Debug($"Creating {path}");

            if (PreInstruct is not null) {
                LogService.Debug("Running PreInstruct");
                PreInstruct(this, path);
            }

            // run custom function if it exists
            // e.g. download instead of create
            if (OverrideInstruct is not null) 
            {
                LogService.Debug("Running OverrideInstruct");
                LogService.Debug($"Instruct returns: {OverrideInstruct(this, path)}");
            }

            /** directory creation **/
            
            else if (Name.EndsWith("/")) 
            {
                LogService.Debug("Auto-create dir");

                // creates the directory if it does not exist
                // it is redundant for the log
                if (! Directory.Exists(path))
                {
                    LogService.Debug($"Attempting to create dir {path}");
                    Directory.CreateDirectory(path);
                }
                else
                {
                    LogService.Debug($"{path} already exists");
                }
            }

            /** file creation **/

            else 
            {
                LogService.Debug("Auto-create file");

                // creates the file if it does not exist
                // it is redundant for the log
                if (! File.Exists(path))
                {
                    LogService.Debug($"Attempting to create file {path}");
                    File.Create(path);
                }
                else
                {
                    LogService.Debug($"{path} already exists");
                }
            }

            if (PostInstruct is not null) {
                LogService.Debug("Running PostInstruct");
                PostInstruct(this, path);
            }


            // for the child directories/files.
            if (Children is not null)
            {
                LogService.Debug("MkDir for children");
                foreach (var child in Children)
                {
                    child.MkDir(path);
                }
            }


            return (true, null);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }
}
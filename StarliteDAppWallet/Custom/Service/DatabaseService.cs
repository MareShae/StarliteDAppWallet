/**
    * Add the Microsoft SQL Client package, under the project:
    * > dotnet add package Microsoft.Data.Sqlite
**/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

/**
 * a singleton class for the relational database
 * loads all db used by the system
 * services use this rather than opening db in their space
 * on db modification, events are sent out
 * 
 * [glossary]
 * ! [singleton]
 * ! [...strutor]
 * ! [code-behind]
 *      ! {code-behind : general}
 *      ! {code-behind : tables}
 *      ! {code-behind : builder}
 *      ! {code-behind : typing}
 *      ! {code-behind : testing}
 * ! [event handlers]
 * ! [records]
 *
**/
class DatabaseService
{
    /**
     * ! [singleton]
     * ! only one instance permitted
    **/ 
    
    public static DatabaseService Instance {

        private set {
            field = value;
        }

        get {
            field = (field != null)? field : new DatabaseService();
            return field;
        }

    }


    /**
     * ! [...strutor] 
     *
    **/

    // constructor
    private DatabaseService() {}

    // destructor

    ~DatabaseService()
    {
        // close all connections
        CloseAllConnections();
    }



    /**
     * ! [code-behind]
    **/ 


    // TODO: remove unregistered databases

    // key for the master database
    public readonly static string __KeyTest = "__test.db";
    public readonly static (string Value, ((string Value, (string UId, string Alias, string Method) Column) Methods, string None) Table)
    KeyMaster = (
        Value: "master.db",
        Table: (
            Methods: (
                Value: "Methods",
                Column: (
                    Date: "Date",
                    Alias: "Alias",
                    Method: "Method"
                ) 
            ),
            None: "None"
        )
    );
    // key for the ExternallyOwnedAccounts database
    // these are account that use Seed Phrases
    public readonly static (string Value, ((string Value, (string Id, string UId, string Root) Column) Roots, (string Value, (string Id, string RootsId, string Alias, string PassPhrase, string Seed) Column) Seeds, (string Value, (string Id, string SeedsId, string HDIndex) Column) Derivations, (string Value, (string Id, string DerivationsId, string Chain, string Token, string Address) Column) Networks) Table)

    KeyExternallyOwnedAccounts = (
        Value: "eoa20261025191950.db", Table: (
            // for mnemonics
            // the Id is the unique Id in its table used for foreign referenece
            // UId is the unique Id that links it to the master.db
            // Root contains the encrypted seed phrase
            Roots: (
                Value: "Roots",
                Column: (
                    Id: "Id",
                    UId: "Date",
                    Root: "Root"
                )
            ),
            // one seed can have many derivations
            // the Id is the unique Id in its table used for foreign referenece
            // RootsId is the foreign Id of the Root it is associated with
            // Passphrase is the phrase that created the seed
            // Seed is the calculation of the associated 
            Seeds: (
                Value: "Seeds",
                Column: (
                    Id: "Id",
                    RootsId: "RootsId",
                    Alias: "Alias",
                    PassPhrase: "PassPhrase",
                    Seed: "Seed"
                )
            ),
            // multiple derivations from one seed
            // the Id is the unique Id in its table used for foreign referenece
            // SeedsId is the foreign Id of the Seed it is associated with
            // HDIndex is the index used for HD Derivation
            Derivations: (
                Value: "Derivations",
                Column: (
                    Id: "Id",
                    SeedsId: "SeedsId",
                    HDIndex: "HDIndex"
                )
            ),
            // network interface
            // the Id is the unique Id in its table used for foreign referenece
            // DerivationsId is the foreign Id of the Derivation it is associated with
            Networks: (
                Value: "Networks",
                Column: (
                    Id: "Id",
                    DerivationsId: "DerivationsId",
                    Chain: "Chain",
                    Token: "Token",
                    Address: "Address"
                )
            )

        )
    );

    // used to dynamically establish SQL connection
    private readonly Dictionary<string, Dictionary<string, List<string>>> MasterKeyDefinition = new()
    {
        {
            __KeyTest,  new Dictionary<string, List<string>>
            {
                {
                    "Table1", new List<string>
                    {
                        "Text TEXT NOT NULL",
                        "Number INTEGER NOT NULL"
                    }
                }
            }
        },
        {
            KeyMaster.Value, new Dictionary<string, List<string>>
            {
                {
                    KeyMaster.Table.Methods.Value, new List<string>
                    {
                        $"{KeyMaster.Table.Methods.Column.UId} TEXT NOT NULL",
                        $"{KeyMaster.Table.Methods.Column.Alias} TEXT NOT NULL",
                        $"{KeyMaster.Table.Methods.Column.Method} TEXT NOT NULL"
                    }
                }
            }
        },
        {
            KeyExternallyOwnedAccounts.Value, new Dictionary<string, List<string>>
            {
                {
                    KeyExternallyOwnedAccounts.Table.Roots.Value, new List<string>
                    {
                        $"{KeyExternallyOwnedAccounts.Table.Roots.Column.Id} INTEGER PRIMARY KEY AUTOINCREMENT",
                        $"{KeyExternallyOwnedAccounts.Table.Roots.Column.UId} TEXT NOT NULL",
                        $"{KeyExternallyOwnedAccounts.Table.Roots.Column.Root} BLOB NOT NULL"
                    }
                },
                {
                    KeyExternallyOwnedAccounts.Table.Seeds.Value, new List<string>
                    {
                        $"{KeyExternallyOwnedAccounts.Table.Seeds.Column.Id} INTEGER PRIMARY KEY AUTOINCREMENT",
                        $"{KeyExternallyOwnedAccounts.Table.Seeds.Column.RootsId} INTEGER NOT NULL",
                        $"{KeyExternallyOwnedAccounts.Table.Seeds.Column.Alias} TEXT NOT NULL",
                        $"{KeyExternallyOwnedAccounts.Table.Seeds.Column.PassPhrase} BLOB NOT NULL",
                        $"{KeyExternallyOwnedAccounts.Table.Seeds.Column.Seed} BLOB NOT NULL",
                                 
                        $"""
                        FOREIGN KEY ({KeyExternallyOwnedAccounts.Table.Seeds.Column.RootsId})
                        REFERENCES {KeyExternallyOwnedAccounts.Table.Roots.Value}({KeyExternallyOwnedAccounts.Table.Roots.Column.Id})
                        ON DELETE CASCADE
                        """
                    }
                },
                {
                    KeyExternallyOwnedAccounts.Table.Derivations.Value, new List<string>
                    {
                        $"{KeyExternallyOwnedAccounts.Table.Derivations.Column.Id} INTEGER PRIMARY KEY AUTOINCREMENT",
                        $"{KeyExternallyOwnedAccounts.Table.Derivations.Column.SeedsId} INTEGER NOT NULL",
                        $"{KeyExternallyOwnedAccounts.Table.Derivations.Column.HDIndex} INTEGER NOT NULL",
                        
                        $"""
                        FOREIGN KEY ({KeyExternallyOwnedAccounts.Table.Derivations.Column.SeedsId})
                        REFERENCES {KeyExternallyOwnedAccounts.Table.Seeds.Value}({KeyExternallyOwnedAccounts.Table.Seeds.Column.Id})
                        ON DELETE CASCADE
                        """
                    }
                },
                {
                    KeyExternallyOwnedAccounts.Table.Networks.Value, new List<string>
                    {
                        $"{KeyExternallyOwnedAccounts.Table.Networks.Column.Id} INTEGER PRIMARY KEY AUTOINCREMENT",
                        $"{KeyExternallyOwnedAccounts.Table.Networks.Column.DerivationsId} INTEGER",
                        $"{KeyExternallyOwnedAccounts.Table.Networks.Column.Chain} TEXT NOT NULL",
                        $"{KeyExternallyOwnedAccounts.Table.Networks.Column.Token} TEXT NOT NULL", 
                        $"{KeyExternallyOwnedAccounts.Table.Networks.Column.Address} TEXT NOT NULL",
                        
                        $"""
                        FOREIGN KEY ({KeyExternallyOwnedAccounts.Table.Networks.Column.DerivationsId})
                        REFERENCES {KeyExternallyOwnedAccounts.Table.Derivations.Value}({KeyExternallyOwnedAccounts.Table.Derivations.Column.Id})
                        ON DELETE CASCADE
                        """
                    }
                }
            }
        }
    };
    
    // list of established SQL connections
    private Dictionary<string, SqliteConnection> ConnectionList = new();


    /**
     * ! {code-behind : general}
    **/


    // use to find Table definitions
    private bool FindDefinitionInMasterKeyList(string Key, string Table)
    {
        try
        {
            if (! MasterKeyDefinition.ContainsKey(Key)) throw new KeyNotFoundException(Key);
            if (! MasterKeyDefinition[Key].ContainsKey(Table)) throw new KeyNotFoundException(Table);

            return true; 
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }

        return false;
    }

    private SqliteConnection? GetAConnection(string Key)
    {
        // check if it is registered the master list
        if (! Instance.MasterKeyDefinition.ContainsKey(Key))
        {
            throw new KeyNotFoundException(Key);
        }
        // check if it not yet connected
        if (! Instance.ConnectionList.ContainsKey(Key))
        {
            LogService.Debug($"{Key} not yet connected");
            var connect = DatabaseHelper.Make(
                Key,
                Instance.MasterKeyDefinition[Key]
            );
            if (connect is null) throw new Exception($"failed to load {Key}");
            Instance.ConnectionList[Key] = connect;
        }
        return Instance.ConnectionList[Key];
    }

    private void CloseAConnection(string dbname)
    {

        if (ConnectionList.ContainsKey(dbname))
        {
            ConnectionList[dbname].Close();
            ConnectionList.Remove(dbname);
        }
    }

    // delete the database
    public void Delete(string dbname)
    {
        // ensure the connection is closed
        CloseAConnection(dbname);

        // construct the path
        string path = Path.Combine(AssetService.Instance.PATH_DATABASES!, dbname);

        // delete the database
        if (File.Exists(path)) {
            File.Delete(path);
            LogService.Debug($"{dbname} successfully deleted");
        } else
        {
            LogService.Debug($"{dbname} does not exist");
        }
    }

    public List<InSQLType[]>? Execute (string Key, string Text)
    {
        try
        {
            SqliteConnection? Connect = GetAConnection(Key);
            if (Connect is null) throw new Exception("connection not established");

            using var Command = Connect.CreateCommand();
            Command.CommandText = Text;

            Command.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.StackTrace} | {e.Message}");
        }

        return null;
    }

    private void CloseAllConnections()
    {
        // close all connections
        foreach (var item in ConnectionList)
        {
            item.Value.Close();
        }
    }
    

    /**
     * ! {code-behind : tables}
    **/


    // create table
    public void Create(string Key, string Table)
    {
        try
        {
            SqliteConnection? Connect = GetAConnection(Key);
            if (Connect is null) throw new Exception("connection not established");

            using var cmd = Connect.CreateCommand();

            LogService.Debug($"Attempting to create Table > {Table} < in DB({Key})");

            cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {Table}
            ({string.Join(", ", MasterKeyDefinition[Key][Table])})
            STRICT
            """;

            cmd.ExecuteNonQuery(); 
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }
    }
    
    // select table
    public void Select(string Key, string Table)
    {
        try
        {
            SqliteConnection? Connect = GetAConnection(Key);
            if (Connect is null) throw new Exception("connection not established");

            using var cmd = Connect.CreateCommand();

            cmd.CommandText = $"""
            PRAGMA table_info({Table})
            """;

            using var reader = cmd.ExecuteReader();
            LogService.Debug("Table Info start ...");
            while (reader.Read())
            {
                LogService.Debug((reader.GetString(1), reader.GetString(2)));
            }
            LogService.Debug("... Table Info stop");
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }
    }

    // delete table in the database
    public void Delete(string Key, string Table)
    {
        try
        {
            SqliteConnection? Connect = GetAConnection(Key);
            if (Connect is null) throw new Exception("connection not established");

            using var cmd = Connect.CreateCommand();

            cmd.CommandText = $"""
            DROP TABLE IF EXISTS {Table}
            """;

            cmd.ExecuteNonQuery();

            LogService.Debug($"{Table} deleted in {Key}");
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }
    }

    // TODO: combine with select
    public Dictionary<string, string>? Columns(string Key, string Table)
    {
        try
        {
            var result = new Dictionary<string, string>();

            SqliteConnection? Connect = GetAConnection(Key);
            if (Connect is null) throw new Exception("connection not established");

            using var cmd = Connect.CreateCommand();

            cmd.CommandText = $"""
            PRAGMA table_info({Table})
            """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result[ reader.GetString(1) ] = reader.GetString(2);
            }

            return result;
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }

        return null;
    }

    /**
     * ! {code-behind : builder}
    **/

    public class Builder
    {
        private enum Mode
        {
            CREATE,
            SELECT,
            UPDATE,
            DELETE
        };
        private Mode? _Mode
        {
            set
            {
                if (! _Mode.HasValue)
                {
                    field = value;
                }
            }
            get => field;
        }

        private string KEY;
        
        private string? TABLE;

        private Dictionary<string, string> Columns;

        private SqliteCommand Command;

        private string[]? Text;

        List<string> SetColumns;

        private Dictionary<string, (string Form, InSQLType Value)>? SetParameters = new();
        
        private Dictionary<string, (string Form, InSQLType Value)>? WhereParameters = new();

        public Builder (string Key, string Table)
        {
            KEY = Key;
            TABLE = Table;
            Columns = Instance.Columns(Key, Table)!;

            // create its command
            SqliteConnection? Connect = Instance.GetAConnection(KEY);
            if (Connect is null) throw new Exception("connection not established");

            Command = Connect.CreateCommand();

            SetColumns = new();
        }

        ~Builder ()
        {
            try
            {
                Command.Dispose();
            }
            catch (Exception e)
            {
                LogService.Debug(e.Message);
            }
        }

        public Builder Create ()
        {
            _Mode = Mode.CREATE;
            Text =
            [
                $"INSERT INTO {TABLE} ()",
                $"VALUES ()"
            ];
            return this;
        }

        public Builder Select ()
        {
            _Mode = Mode.SELECT;
            Text =
            [
                $"SELECT ))",
                $"FROM {TABLE}",
                $""
            ];
            return this;
        }

        public Builder Update ()
        {
            Text =
            [
                $"UPDATE {TABLE}",
                $"SET ))",
                $"WHERE ))"
            ];
            _Mode = Mode.UPDATE;
            return this;
        }

        public Builder Delete ()
        {
            _Mode = Mode.DELETE;
            Text =
            [
                $"DELETE",
                $"FROM {TABLE}",
                $"WHERE ))"
            ];
            return this;
        }

        public Builder Set (string Col)
        {
            if (_Mode is null) throw new Exception("no Set on null");
            if (_Mode is not Mode.SELECT) throw new Exception("special Set");

            // append to construction
            if (Text is null) throw new Exception("null command");

            SetAppend(Col, null, true);

            return this;
        }

        public Builder Set (string Col, int? Value)
        {
            if (_Mode is null) throw new Exception("no Set on null");
            if (_Mode is Mode.DELETE) throw new Exception("no Set on Delete");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "INTEGER") throw new Exception($"attempt to set {Columns[Col]} as int for {Col} in {KEY}:{TABLE}");

            SetAppend(Col, Value);

            return this;
        }

        public Builder Set (string Col, string? Value)
        {
            if (_Mode is null) throw new Exception("no Set on null");
            if (_Mode is Mode.DELETE) throw new Exception("no Set on Delete");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "TEXT") throw new Exception($"attempt to set {Columns[Col]} as string for {Col} in {KEY}:{TABLE}");

            SetAppend(Col, Value);

            return this;
        }

        public Builder Set (string Col, double? Value)
        {
            if (_Mode is null) throw new Exception("no Set on null");
            if (_Mode is Mode.DELETE) throw new Exception("no Set on Delete");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "REAL") throw new Exception($"attempt to set {Columns[Col]} as double for {Col} in {KEY}:{TABLE}");

            SetAppend(Col, Value);

            return this;
        }

        public Builder Set (string Col, byte[]? Value)
        {
            if (_Mode is null) throw new Exception("no Set on null");
            if (_Mode is Mode.DELETE) throw new Exception("no Set on Delete");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "BLOB") throw new Exception($"attempt to set {Columns[Col]} as byte[] for {Col} in {KEY}:{TABLE}");

            SetAppend(Col, Value);

            return this;
        }

        
        private void SetAppend(string Col, object? Value, bool noParam = false)
        {
            SetColumns.Add(Col);

            // append to parameters
            string raw = $"#Set{Col}";

            if (! noParam) {
                var Parameter =  Command.CreateParameter();
                Parameter.ParameterName = raw;
                Parameter.Value = Value;
                Command.Parameters.Add(Parameter);
            }

            // append to construction
            if (Text is null) throw new Exception("null command");
            switch (_Mode)
            {
                case Mode.CREATE: {
                    Text[0] = Text[0].Replace(
                        ")",
                        $"{(Text[0].Contains("()") ? "" : ", ")}{Col})"
                    );

                    Text[1] = Text[1].Replace(
                        ")",
                        $"{(Text[1].Contains("()") ? "" : ", ")}{raw})"
                    );
                    break;
                } 

                case Mode.SELECT: {
                    if (Text[0].Contains("))"))
                    {
                        Text[0] = Text[0].Replace("))", Col);
                    }
                    else
                    {
                        Text[0] = $"{Text[0]}, {Col}";
                    }
                    break;
                } 

                case Mode.UPDATE: {
                    if (Text[1].Contains("))"))
                    {
                        Text[1] = Text[1].Replace("))", $"{Col}={raw}");
                    }
                    else
                    {
                        Text[1] = $"{Text[1]}, {Col}={raw}";
                    }
                    break;
                }

                default: {
                    throw new Exception("unknown type");
                }
            }
        }

        public Builder Where (string Col, int? Value)
        {
            if (_Mode is null) throw new Exception("no Where on null");
            if (_Mode is Mode.CREATE) throw new Exception("no Where on Create");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "INTEGER") throw new Exception($"attempt to set {Columns[Col]} as int for {Col} in {KEY}:{TABLE}");

            WhereAppend(Col, Value);

            return this;
        }

        public Builder Where (string Col, string? Value)
        {
            if (_Mode is null) throw new Exception("no Where on null");
            if (_Mode is Mode.CREATE) throw new Exception("no Where on Create");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "TEXT") throw new Exception($"attempt to set {Columns[Col]} as string for {Col} in {KEY}:{TABLE}");

            WhereAppend(Col, Value);

            return this;
        }

        public Builder Where (string Col, double? Value)
        {
            if (_Mode is null) throw new Exception("no Where on null");
            if (_Mode is Mode.CREATE) throw new Exception("no Where on Create");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "REAL") throw new Exception($"attempt to set {Columns[Col]} as double for {Col} in {KEY}:{TABLE}");

            WhereAppend(Col, Value);

            return this;
        }

        public Builder Where (string Col, byte[]? Value)
        {
            if (_Mode is null) throw new Exception("no Where on null");
            if (_Mode is Mode.CREATE) throw new Exception("no Where on Create");

            // verify
            if (!Columns.ContainsKey(Col)) throw new Exception($"no {Col} in {KEY}:{TABLE}");
            if (Columns[Col] != "BLOB") throw new Exception($"attempt to set {Columns[Col]} as byte[] for {Col} in {KEY}:{TABLE}");

            WhereAppend(Col, Value);

            return this;
        }

        private void WhereAppend(string Col, object? Value)
        {
            // append to parameters
            string expr = $"#Whr{Col}";

            var Parameter =  Command.CreateParameter();
            Parameter.ParameterName = expr;
            Parameter.Value = Value;
            Command.Parameters.Add(Parameter);

            // convert to equated form
            expr = $"{Col}={expr}";

            // append to construction
            if (Text is null) throw new Exception("null command");

            switch (_Mode)
            {
                case Mode.SELECT: {
                    if (string.IsNullOrWhiteSpace(Text[2]))
                    {
                        Text[2] = $"WHERE {expr}";
                    }
                    else
                    {
                        Text[2] = $"{Text[2]} AND {expr}";
                    }
                    break;
                } 

                case Mode.UPDATE:
                case Mode.DELETE: {
                    if (Text[2].Contains("))"))
                    {
                        Text[2] = Text[2].Replace("))", $"{expr}");
                    }
                    else
                    {
                        Text[2] = $"{Text[2]} AND {expr}";
                    }
                    break;
                }

                default: {
                    throw new Exception("unknown type");
                }
            }
        }


        public List<InSQLType[]>? Execute ()
        {
            Command.CommandText = string.Join(" ", Text!);

            List<InSQLType[]>? result = null;
            
            if (_Mode is Mode.SELECT)
            {
                result = new();
                
                using var Reader = Command.ExecuteReader();

                while (Reader.Read())
                {
                    // create a temporary object that will be appended to the `result`
                    var Tmp = new InSQLType [SetColumns.Count];

                    for (int i = 0; i < Tmp.Length; i++)
                    {
                        if (Reader.IsDBNull(i))
                        {
                            continue;
                        }
                        switch ( Columns[SetColumns[i]] )
                        {
                            case "INTEGER":
                                Tmp[i] = Reader.GetInt64(i);
                                break;
                            case "TEXT":
                                Tmp[i] = Reader.GetString(i);
                                break;
                            case "REAL":
                                Tmp[i] = Reader.GetDouble(i);
                                break;
                            case "BLOB":
                                Tmp[i] = (byte[])Reader.GetValue(i);
                                break;
                            default:
                                
                                break;
                        }
                    }

                    result.Add(Tmp);
                }
            }
            else
            {
                Command.ExecuteNonQuery();
                Instance.NotifyChanged?.Invoke(
                    Instance, new OperationEventArgs(
                        KEY,
                        TABLE
                    )
                );
            }

            return result;
        }
    }



    /**
     * ! {code-behind : typing}
    **/


    public readonly struct InSQLType
    {
        // slots 
        private readonly long? _INTEGER;
        private readonly double? _REAL;
        private readonly string? _TEXT;
        private readonly byte[]? _BLOB;

        // constructors
        public InSQLType(long value) => _INTEGER = value;
        public InSQLType(double value) => _REAL = value;
        public InSQLType(string value) => _TEXT = value;
        public InSQLType(byte[] value) => _BLOB = value;
        public InSQLType(object value) => _BLOB = (byte[])value;

        public Type Type =>
            _INTEGER is not null ? typeof(long):
            _REAL is not null ? typeof(double):
            _TEXT is not null ? typeof(string):
            _BLOB is not null ? typeof(byte[]):
            typeof(DBNull);

        public static implicit operator InSQLType(int value) => new(value);
        public static implicit operator InSQLType(long value) => new(value);
        public static implicit operator InSQLType(byte[] value) => new(value);
        public static implicit operator InSQLType(double value) => new(value);
        public static implicit operator InSQLType(string value) => new(value);

        public static explicit operator int(InSQLType value) => (int)value._INTEGER!;
        public static explicit operator long(InSQLType value) => (long)value._INTEGER!;
        public static explicit operator byte[](InSQLType value) => (byte[])value._BLOB!;
        public static explicit operator double(InSQLType value) => (double)value._REAL!;
        public static explicit operator string(InSQLType value) => (string)value._TEXT!;

        public override string ToString()
        {
            if (_INTEGER is not null) return $"{_INTEGER}";
            if (_REAL is not null) return $"{_REAL}";
            if (_TEXT is not null) return $"{_TEXT}";
            if (_BLOB is not null) return $"`{_BLOB.Length} bytes`";
            
            return "";
        }

        public static void show(InSQLType[] Value)
        {
            string value = "[";
            for (int i = 0; i < Value.Length; i++)
            {
                value = $"{value}{Value[i]}";
                if (i < Value.Length - 1)
                {
                    value = $"{value}, ";
                }
            }
            value = $"{value}]";

            LogService.Debug(value);
        }
    }

    /**
     * ! {code-behind : testing}
    **/


    public void __Test_CRUD()
    {
        
        string __Table1 = "Table1";

        Delete(__KeyTest, "Table1");
        Create(__KeyTest, "Table1");

        // // show table information
        // Select(__TestDB, "Table1");

        // insert three items into the database
        new Builder(__KeyTest, __Table1)
            .Create()
            .Set("Text", "NameA")
            .Set("Number", 4837)
            .Execute();

        new Builder(__KeyTest, __Table1)
            .Create()
            .Set("Text", "NameB")
            .Set("Number", 4333)
            .Execute();

        new Builder(__KeyTest, __Table1)
            .Create()
            .Set("Text", "NameC")
            .Set("Number", 4587)
            .Execute();

        // select to show them
        var select = new Builder(__KeyTest, __Table1)
            .Select()
            .Set("Text")
            .Set("Number")
            .Execute();
        if (select is null)
        {
            LogService.Debug("failed to retrieve from database");
        }
        else
        {
            LogService.Debug("Content start ...");
            foreach (var item in select)
            {
                InSQLType.show(item);
            }
            LogService.Debug("... Content stop");
        }

        // select to show them, where
        select = new Builder(__KeyTest, __Table1)
            .Select()
            .Set("Text")
            .Set("Number")
            .Where("Text", "NameA")
            .Execute();
        if (select is null)
        {
            LogService.Debug("failed to retrieve from database");
        }
        else
        {
            LogService.Debug("Content start ...");
            foreach (var item in select)
            {
                InSQLType.show(item);
            }
            LogService.Debug("... Content stop");
        }

        // update two elements
        new Builder(__KeyTest, __Table1)
            .Update()
            .Set("Number", 3053)
            .Where("Text", "NameB")
            .Execute();
        new Builder(__KeyTest, __Table1)
            .Update()
            .Set("Text", "NameX")
            .Set("Number", 7340)
            .Where("Text", "NameC")
            .Execute();
        select = new Builder(__KeyTest, __Table1)
            .Select()
            .Set("Text")
            .Set("Number")
            .Execute();
        if (select is null)
        {
            LogService.Debug("failed to retrieve from database");
        }
        else
        {
            LogService.Debug("Content start ...");
            foreach (var item in select)
            {
                InSQLType.show(item);
            }
            LogService.Debug("... Content stop");
        }
        

        // delete one element
        new Builder(__KeyTest, __Table1)
            .Delete()
            .Where("Text", "NameA")
            .Execute();
        select = new Builder(__KeyTest, __Table1)
            .Select()
            .Set("Text")
            .Set("Number")
            .Execute();
        if (select is null)
        {
            LogService.Debug("failed to retrieve from database");
        }
        else
        {
            LogService.Debug("Content start ...");
            foreach (var item in select)
            {
                InSQLType.show(item);
            }
            LogService.Debug("... Content stop");
        }
    }


    /**
     * ! [event handlers]
     * ! events that the service invokes
    **/

    /**
     * when an operation changes the Database
     * an event is emitted with the name Key and Table
     * the key corresponds to the file
    **/
    public class OperationEventArgs: EventArgs
    {   
        /* Switchable ContentViewModelBase for the Main View Model  */
        
        public string Key;
        public string? Table;

        /* constructor */

        public OperationEventArgs (string key, string? table=null)
        {
            Key = key;
            Table = table;
        }
    }

    public event EventHandler<OperationEventArgs>? NotifyChanged;



    /**
     * ! [records]
     * ! where they are meant to be
    **/    
}



file static class DatabaseHelper
{
    public static SqliteConnection? Make(string Name, Dictionary<string, List<string>> Tables)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new Exception("invalid name");
            }
            if (! (Name.EndsWith(".db") || Name.EndsWith(".sqlite")) )
            {
                throw new Exception($"database extension restricted to .db | .sqlite > {Name}");
            }

            LogService.Debug($"Establishing Sqlite connection > {Name}");

            // create the connection
            var connect = new SqliteConnection (
                $"Data Source={Path.Combine(AssetService.Instance.PATH_DATABASES!, Name)}; Foreign Keys=True"
            );

            LogService.Debug($"Opening Sqlite connection > {Name}");
            connect.Open();

            // Turn foreign key enforcement ON
            LogService.Debug($"Enforcing foreign keys on Sqlite connection > {Name}");
            {
                using var cmd = connect.CreateCommand();
                
                cmd.CommandText = $"""
                PRAGMA foreign_keys = ON;
                """;
                
                cmd.ExecuteNonQuery();
            }

            // create the tables
            foreach (var table in Tables)
            {
                using var cmd = connect.CreateCommand();

                LogService.Debug($"Attempting to create Table > {table.Key} < in DB({Name})");

                cmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {table.Key}
                ({string.Join(", ", table.Value)})
                STRICT
                """;

                cmd.ExecuteNonQuery();
            }

            return connect;
        }
        catch (Exception e)
        {
            LogService.Debug(e.Message);
        }

        return null;
    }
}

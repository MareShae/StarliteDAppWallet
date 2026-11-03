using System.Runtime.CompilerServices;
using StarliteDAppWallet;

public class LogService {
    
    public static void Debug(bool value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(char value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(char[]? value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(decimal value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(float value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(int value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(uint value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(long value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(ulong value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(object? value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }

    public static void Debug(string? value, string id="", [CallerFilePath] string? file = null, [CallerLineNumber] int? line = null)
    {
        System.Diagnostics.Debug.WriteLine($"""[{id}:{App.ProjectName}:DEBUG][{file}:{line}]{value}""");
    }
}
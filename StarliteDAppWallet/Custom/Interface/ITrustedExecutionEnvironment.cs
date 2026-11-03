

using System;
using System.Text;


/**
 * a singleton class for storing and retrieving sensitive information
 * 
 * [resources]
 * + ledger.com/blog/software-wallets
 *
 * security:
 * 
 * Keychain/Keystore to leverage device HSM
 *
 * password provided by user
 * + PBKDF2 for encryption key derivation
 * + AES-GCM to encrypt using key
 * 
 * [glossary]
 * ! [singleton]
 * ! [...strutor]
 * ! [code-behind]
 * ! [event handlers]
 * ! [records]
**/
public class ITrustedExecutionEnvironment
{

    /**
     * ! [singleton]
     * ! only one instance permitted
    **/ 
    
    public static ITrustedExecutionEnvironment Instance {

        protected set {
            field = value;
        }

        get {
            field = (field != null)? field : new ITrustedExecutionEnvironment();
            return field;
        }

    }

    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    protected ITrustedExecutionEnvironment()
    {}

    // destructor

    ~ITrustedExecutionEnvironment() {}



    /**
     * ! [code-behind]
    **/ 

    public virtual byte[]? Encrypt(byte[]? decrypted)
    {
        LogService.Debug("... not externally implemented");

        throw new NotImplementedException("Encrypt");
    } 

    public byte[]? Encrypt(string? decrypted)
    {
        try
        {
            if (decrypted is null) throw new Exception("null input");

            return Encrypt(
                Encoding.UTF8.GetBytes(decrypted)
            );
        }
        catch (Exception e)
        {
            LogService.Debug(e.ToString());
        }

        return null;
    }

    public virtual string? Decrypt(byte[]? encrypted)
    {
        LogService.Debug("... not externally implemented");

        throw new NotImplementedException("Decrypt");
    }

    public virtual void __test()
    {
        LogService.Debug("... not externally implemented");

        throw new NotImplementedException("Test");
    }
}
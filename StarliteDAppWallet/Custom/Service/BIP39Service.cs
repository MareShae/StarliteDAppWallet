
using System;
using System.IO;
using System.Text;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;


/**
 * a singleton class for bip39
 * https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki
 * 
 * [glossary]
 * ! [singleton]
 * ! [...strutor]
 * ! [code-behind]
 * ! [event handlers]
 * ! [records]
**/
class BIP39Service
{
    /**
     * ! [singleton]
     * ! only one instance permitted
    **/ 

    private readonly static string LOG_ID = "BIP0039";
    
    public static BIP39Service Instance {

        private set {
            field = value;
        }

        get {
            field = (field != null)? field : new BIP39Service();
            return field;
        }

    }


    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    private BIP39Service() {}

    // destructor

    ~BIP39Service() {}



    /**
     * ! [code-behind]
    **/ 

    static int MNEMONIC_BIN_WORD_SIZE = 11;

    /**
        Summary:
            generate seed phrase based on entrophy
            
        Parameters:
            ENT:
                The initial entropy length between 128..256 with increments of 32

                It produces seed phrases between 12..24 with increments of 3
    **/
    public async Task<string?> MnemonicGenerate(int ENT)
    {
        try
        {
            LogService.Debug($"Generating mnemonic", LOG_ID);
            /** assertions **/

            // calling AssetService should auto-download the necessary file
            // if it fails, this fails as well
            // it is asserted here, rather than the end to not do unnecessary work
            if (! File.Exists(AssetService.Instance.PATH_BIP0039_ENGLISH))
            {
                throw new Exception ($"{AssetService.Instance.PATH_BIP0039_ENGLISH}  not found/downloaded");
            }

            /** valid ENT is 32 * {4..8} for 12 - 24 words **/
            LogService.Debug($"Validating entrophy size", LOG_ID);

            // adjust ENT if it is between 4..8
            ENT = 32 * (int)Math.Round((double)ENT / 32);

            // check that it is valid
            if (ENT < 128 || ENT > 256) throw new Exception($"unallowed ENT size {ENT}");

            /** 1. generate entrophy **/

            // the more entrophy, the more security with increased length
            
            LogService.Debug($"Generating entrophy", LOG_ID);
            byte[] entrophy = RandomNumberGenerator.GetBytes(ENT / 8);
            LogService.Debug($"Generated entrophy of size {ENT}", LOG_ID);

            // the checksum length is taken from the first (ENT/32) bits of its SHA256 hash
            
            byte[] checksum = SHA256.HashData(entrophy);
            LogService.Debug($"Calculating checksum", LOG_ID);

            // the mnemonic string

            string mnemonic = "";
            int MnemonicTargetLength = ENT + (ENT / 32);
            
            LogService.Debug($"Convert entropy to binary", LOG_ID);
            foreach (byte byt in entrophy)
            {
                // convert from bytes to binary with padding
                string tmp = Convert.ToString(byt, 2).PadLeft(8, '0');
                mnemonic = $"{mnemonic}{tmp}";
            }
            
            // only extract the first (ENT / 32)-bits of the checksum
            foreach (byte byt in checksum)
            {
                // break if Target Length secured
                if (mnemonic.Length >= MnemonicTargetLength) break;
                // convert from bytes to binary with padding
                string tmp = Convert.ToString(byt, 2).PadLeft(8, '0');
                int tmpLen = tmp.Length;
                // get the cut required to make the mnemonic length be the MnemonicTargetLength
                if (mnemonic.Length + tmp.Length > MnemonicTargetLength)
                {
                    tmpLen = mnemonic.Length + tmp.Length - MnemonicTargetLength;
                }
                // slice the checksum to match
                mnemonic = $"{mnemonic}{tmp.Substring(0, tmpLen)}";
            }
            
            if (mnemonic.Length != MnemonicTargetLength) throw new Exception("invalid mnemonic length");

            /** 2. wordlist **/

            // split into groups of MNEMONIC_WORD_SIZE

            LogService.Debug($"Convert binary to decimal", LOG_ID);
            
            int[] mnemonicIdx = new int[mnemonic.Length / MNEMONIC_BIN_WORD_SIZE];

            if (mnemonic.Length != MNEMONIC_BIN_WORD_SIZE * mnemonicIdx.Length) {
                throw new Exception ($"invalid mnemonic index length > {mnemonic.Length}");
            }

            for (int i = 0; i < mnemonicIdx.Length; i++)
            {
                var word = mnemonic.Substring(i * MNEMONIC_BIN_WORD_SIZE, MNEMONIC_BIN_WORD_SIZE);

                mnemonicIdx[i] = Convert.ToInt32(word, 2);
            }

            // mnemonic words
            // the binary representation of each word is between 0..2047, 2^11

            LogService.Debug($"Secure english words for mnemonic counterparts");

            (bool status, string? detail, var words) = AssetService.Instance.GetLinesFromFile(
                AssetService.Instance.PATH_BIP0039_ENGLISH!,
                mnemonicIdx
            );

            if (! status) throw new Exception($"Failed to get line from file: {detail}");
            if (words is null) throw new Exception("Mnemomic was not converted to seed phrase");
            if (words.Length != mnemonicIdx.Length) throw new Exception($"Mnemonic words result mismatch");

            // convert to human-readable language
            return string.Join(' ', words);
        }
        catch (Exception e)
        {
            LogService.Debug($"{e.Message}");
        }

        return null;
    }

    public byte[]? MnemonicToSeedSync(string mnemonic, string passphrase="")
    {
        /**
         * Source: https://bitcoin.org/bip/39/#from-mnemonic-to-seed
         *
         * to create a binary seed from the mnemonic,
         * we use the PBKDF2 function
         * with a mnemonic sentence (in UTF-8 NFKD) used as the password
         * and the string "mnemonic" + passphrase (again in UTF-8 NFKD) used as the salt.
         * the iteration count is set to 2048
         * and HMAC-SHA512 is used as the pseudo-random function.
         * the length of the derived key is 512 bits (=64 bytes).
        **/

        try
        {
            // normalize to UTF-8 NFKD
            var password = Encoding.UTF8.GetBytes(
                mnemonic.Normalize(NormalizationForm.FormKD)
            );

            // normalize to UTF-8 NFKD
            var salt = Encoding.UTF8.GetBytes(
                ($"mnemonic{passphrase}").Normalize(NormalizationForm.FormKD)
            );

            return Rfc2898DeriveBytes.Pbkdf2 (password, salt, 2048, HashAlgorithmName.SHA512, 64);
        }
        catch (Exception e)
        {
            LogService.Debug(e.ToString());
        }
        
        return null;
    }
}
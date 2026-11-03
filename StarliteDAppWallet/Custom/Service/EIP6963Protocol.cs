

/**
 * a singleton class for Eth Improvement Proposals (EIP) - 6963
 * https://eips.ethereum.org/EIPS/eip-6963
 * 
 * [glossary]
 * ! [singleton]
 * ! [...strutor]
 * ! [code-behind]
 * ! [event handlers]
 * ! [records]
**/
class EIP6963Protocol
{
    /**
     * ! [singleton]
     * ! only one instance permitted
    **/ 
    
    public static EIP6963Protocol Instance {

        private set {
            field = value;
        }

        get {
            field = (field is not null)? field : new EIP6963Protocol();
            return field;
        }

    }


    /**
     * ! [...strutor] 
     *
    **/

    /** constructor **/

    private EIP6963Protocol() {}

    // destructor

    ~EIP6963Protocol() {}



    /**
     * ! [code-behind]
    **/ 

    public readonly string Provider = """
    interface EIP1193Provider {}
    """;

    public readonly string ProviderInfo = """
    interface EIP6963ProviderInfo {
        uuid: string;
        name: string;
        icon: string;
        rdns: string;
    }
    """;


    // composition interface to announce a Wallet Provider and related metadata about the same
    public readonly string ProviderDetail = """
    interface EIP6963ProviderDetail {
        info: EIP6963ProviderInfo;
        provider: EIP1193Provider;
    }
    """;

    public readonly string AnnounceProviderEvent = """
    interface EIP6963AnnounceProviderEvent extends CustomEvent {
        type: "eip6963:announceProvider";
        detail: EIP6963ProviderDetail;
    }
    """;
    
    public readonly string RequestProviderEvent = """
    interface EIP6963RequestProviderEvent extends CustomEvent {
        type: "eip6963:requestProvider";
    }
    """;

    public readonly string window_dispatchEvent = """

    """;

    public readonly string window_addEventListener = """

    """;
}
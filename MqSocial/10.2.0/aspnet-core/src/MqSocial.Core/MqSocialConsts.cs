using MqSocial.Debugging;

namespace MqSocial;

public class MqSocialConsts
{
    public const string LocalizationSourceName = "MqSocial";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "f7ecb9141ac746638b2d85230c55960e";
}

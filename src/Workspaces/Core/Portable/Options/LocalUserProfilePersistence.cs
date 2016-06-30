namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option should be persisted into the user's local registry hive.
    /// </summary>
    internal sealed class LocalUserProfilePersistence : OptionPersistence
    {
        public string KeyName { get; }

        public LocalUserProfilePersistence(string keyName)
        {
            KeyName = keyName;
        }
    }
}

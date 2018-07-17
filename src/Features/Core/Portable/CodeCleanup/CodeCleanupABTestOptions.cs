using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal static class CodeCleanupABTestOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\CodeCleanup\";

        public static readonly Option<bool> ParticipatedInExperiment = new Option<bool>(nameof(CodeCleanupABTestOptions), nameof(ParticipatedInExperiment),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ParticipatedInExperiment)));
    }
}

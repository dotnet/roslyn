using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal class AddImportOptions
    {
        public const string FeatureName = "AddImport";

        public static Option<bool> SuggestForTypesInReferenceAssemblies =
            new Option<bool>(FeatureName, nameof(SuggestForTypesInReferenceAssemblies), defaultValue: false);

        public static Option<bool> SuggestForTypesInNuGetPackages =
            new Option<bool>(FeatureName, nameof(SuggestForTypesInNuGetPackages), defaultValue: false);
    }
}

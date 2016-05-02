using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal class AddImportOptions
    {
        public const string FeatureName = "AddImport";

        public static PerLanguageOption<bool> OfferForTypesFromNetReferenceAssemblies =
            new PerLanguageOption<bool>(FeatureName, nameof(OfferForTypesFromNetReferenceAssemblies), defaultValue: false);

        public static PerLanguageOption<bool> OfferForTypesFromNugetOrgPackages =
            new PerLanguageOption<bool>(FeatureName, nameof(OfferForTypesFromNugetOrgPackages), defaultValue: false);
    }
}

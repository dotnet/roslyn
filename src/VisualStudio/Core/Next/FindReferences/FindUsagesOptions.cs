using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal static class FindUsagesOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\FindUsages\";

        /// <summary>
        /// Used to store the user's explicit 'grouping priority' for the 'Definition' column.
        /// We store this because we'll disable this grouping sometimes (i.e. for GoToImplementation),
        /// and we want to restore the value back to its original state when the user does the
        /// next FindReferences call.
        /// </summary>
        [ExportOption]
        public static readonly Option<int> DefinitionGroupingPriority = new Option<int>(
            nameof(FindUsagesOptions), nameof(DefinitionGroupingPriority), defaultValue: -1,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(DefinitionGroupingPriority)));
    }
}

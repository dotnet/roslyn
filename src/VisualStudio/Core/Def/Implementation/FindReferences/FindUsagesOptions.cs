// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

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
        public static readonly Option<int> DefinitionGroupingPriority = new Option<int>(
            nameof(FindUsagesOptions), nameof(DefinitionGroupingPriority), defaultValue: -1,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(DefinitionGroupingPriority)));
    }

    [ExportOptionProvider, Shared]
    internal class FindUsagesOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public FindUsagesOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            FindUsagesOptions.DefinitionGroupingPriority);
    }

}

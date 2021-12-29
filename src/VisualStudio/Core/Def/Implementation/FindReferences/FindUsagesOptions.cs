﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class FindUsagesOptions : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindUsagesOptions()
        {
        }

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            DefinitionGroupingPriority);

        private const string LocalRegistryPath = @"Roslyn\Internal\FindUsages\";

        /// <summary>
        /// Used to store the user's explicit 'grouping priority' for the 'Definition' column.
        /// We store this because we'll disable this grouping sometimes (i.e. for GoToImplementation),
        /// and we want to restore the value back to its original state when the user does the
        /// next FindReferences call.
        /// </summary>
        public static readonly Option<int> DefinitionGroupingPriority = new(
            nameof(FindUsagesOptions), nameof(DefinitionGroupingPriority), defaultValue: -1,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(DefinitionGroupingPriority)));
    }
}

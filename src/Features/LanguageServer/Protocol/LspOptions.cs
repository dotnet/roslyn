// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class LspOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Lsp\";

        /// <summary>
        /// This sets the max list size we will return in response to a completion request.
        /// If there are more than this many items, we will set the isIncomplete flag on the returned completion list.
        /// </summary>
        public static readonly Option2<int> MaxCompletionListSize = new(nameof(LspOptions), nameof(MaxCompletionListSize), defaultValue: 1000,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(MaxCompletionListSize)));
    }

    [ExportOptionProvider, Shared]
    internal class LspOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LspOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(LspOptions.MaxCompletionListSize);
    }
}

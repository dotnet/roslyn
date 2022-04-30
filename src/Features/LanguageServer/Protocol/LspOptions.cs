// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportGlobalOptionProvider, Shared]
    internal sealed class LspOptions : IOptionProvider
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Lsp\";
        private const string FeatureName = "LspOptions";

        /// <summary>
        /// This sets the max list size we will return in response to a completion request.
        /// If there are more than this many items, we will set the isIncomplete flag on the returned completion list.
        /// </summary>
        public static readonly Option2<int> MaxCompletionListSize = new(FeatureName, nameof(MaxCompletionListSize), defaultValue: 1000,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(MaxCompletionListSize)));

        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> LspEditorFeatureFlag = new(FeatureName, nameof(LspEditorFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.LSP.Editor"));

        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> LspSemanticTokensFeatureFlag = new(FeatureName, nameof(LspSemanticTokensFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.LSP.SemanticTokens"));

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            MaxCompletionListSize,
            LspEditorFeatureFlag);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LspOptions()
        {
        }
    }
}

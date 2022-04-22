// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Enables legacy APIs to access global options from workspace.
    /// </summary>
    [ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared]
    internal sealed class LegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly CodeActionOptionsStorage.Provider _provider;

        private static readonly Option2<bool> s_generateOverridesOption = new(
            "GenerateOverridesOptions", "SelectAll", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation($"TextEditor.Specific.GenerateOverridesOptions.SelectAll"));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyGlobalOptionsWorkspaceService(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
            _provider = _globalOptions.CreateProvider();
        }

        public bool GenerateOverrides
        {
            get => _globalOptions.GetOption(s_generateOverridesOption);
            set => _globalOptions.SetGlobalOption(new OptionKey(s_generateOverridesOption), value);
        }

        public bool RazorUseTabs
            => GlobalOptions.GetOption(RazorLineFormattingOptionsStorage.UseTabs);

        public int RazorTabSize
            => GlobalOptions.GetOption(RazorLineFormattingOptionsStorage.TabSize);

        /// TODO: remove. https://github.com/dotnet/roslyn/issues/57283
        public bool InlineHintsOptionsDisplayAllOverride
        {
            get => _globalOptions.GetOption(InlineHintsGlobalStateOption.DisplayAllOverride);
            set => _globalOptions.SetGlobalOption(new OptionKey(InlineHintsGlobalStateOption.DisplayAllOverride), value);
        }

        public CleanCodeGenerationOptionsProvider CleanCodeGenerationOptionsProvider
            => _provider;

        public AutoFormattingOptions GetAutoFormattingOptions(HostLanguageServices languageServices)
            => _globalOptions.GetAutoFormattingOptions(languageServices.Language);
    }
}

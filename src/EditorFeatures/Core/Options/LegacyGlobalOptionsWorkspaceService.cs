// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
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

        private static readonly PerLanguageOption2<bool> s_generateOperators = new(
            "GenerateEqualsAndGetHashCodeFromMembersOptions",
            "GenerateOperators", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation(
                "TextEditor.%LANGUAGE%.Specific.GenerateEqualsAndGetHashCodeFromMembersOptions.GenerateOperators"));

        private static readonly PerLanguageOption2<bool> s_implementIEquatable = new(
            "GenerateEqualsAndGetHashCodeFromMembersOptions",
            "ImplementIEquatable", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation(
                "TextEditor.%LANGUAGE%.Specific.GenerateEqualsAndGetHashCodeFromMembersOptions.ImplementIEquatable"));

        private static readonly PerLanguageOption2<bool> s_addNullChecks = new(
            "GenerateConstructorFromMembersOptions",
            "AddNullChecks", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation(
                $"TextEditor.%LANGUAGE%.Specific.GenerateConstructorFromMembersOptions.AddNullChecks"));

        internal static readonly PerLanguageOption2<bool> AddNullChecksToConstructorsGeneratedFromMembers = new(
            "GenerateConstructorFromMembersOptions",
            "AddNullChecks", defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.GenerateConstructorFromMembersOptions.AddNullChecks"));

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
            => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.UseTabs);

        public int RazorTabSize
            => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.TabSize);

        /// TODO: remove. https://github.com/dotnet/roslyn/issues/57283
        public bool InlineHintsOptionsDisplayAllOverride
        {
            get => _globalOptions.GetOption(InlineHintsGlobalStateOption.DisplayAllOverride);
            set => _globalOptions.SetGlobalOption(new OptionKey(InlineHintsGlobalStateOption.DisplayAllOverride), value);
        }

        public CleanCodeGenerationOptionsProvider CleanCodeGenerationOptionsProvider
            => _provider;

        public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language)
            => _globalOptions.GetOption(s_implementIEquatable, language);

        public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value)
            => _globalOptions.SetGlobalOption(new OptionKey(s_generateOperators, language), value);

        public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language)
            => _globalOptions.GetOption(s_implementIEquatable, language);

        public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value)
            => _globalOptions.SetGlobalOption(new OptionKey(s_implementIEquatable, language), value);

        public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language)
            => _globalOptions.GetOption(s_addNullChecks, language);

        public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value)
            => _globalOptions.SetGlobalOption(new OptionKey(s_addNullChecks, language), value);
    }
}

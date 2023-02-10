// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
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

        private static readonly Option2<bool> s_generateOverridesOption = new(
            "GenerateOverridesOptions_SelectAll", defaultValue: true);

        private static readonly PerLanguageOption2<bool> s_generateOperators = new(
            "GenerateEqualsAndGetHashCodeFromMembersOptions_GenerateOperators",
            defaultValue: false);

        private static readonly PerLanguageOption2<bool> s_implementIEquatable = new(
            "GenerateEqualsAndGetHashCodeFromMembersOptions_ImplementIEquatable",
            defaultValue: false);

        private static readonly PerLanguageOption2<bool> s_addNullChecks = new(
            "GenerateConstructorFromMembersOptions_AddNullChecks",
            defaultValue: false);

        internal static readonly PerLanguageOption2<bool> AddNullChecksToConstructorsGeneratedFromMembers = new(
            "GenerateConstructorFromMembersOptions_AddNullChecks",
            defaultValue: false);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyGlobalOptionsWorkspaceService(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public bool GenerateOverrides
        {
            get => _globalOptions.GetOption(s_generateOverridesOption);
            set => _globalOptions.SetGlobalOption(s_generateOverridesOption, value);
        }

        public bool RazorUseTabs
            => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.UseTabs);

        public int RazorTabSize
            => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.TabSize);

        /// TODO: remove. https://github.com/dotnet/roslyn/issues/57283
        public bool InlineHintsOptionsDisplayAllOverride
        {
            get => _globalOptions.GetOption(InlineHintsGlobalStateOption.DisplayAllOverride);
            set => _globalOptions.SetGlobalOption(InlineHintsGlobalStateOption.DisplayAllOverride, value);
        }

        public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language)
            => _globalOptions.GetOption(s_implementIEquatable, language);

        public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value)
            => _globalOptions.SetGlobalOption(s_generateOperators, language, value);

        public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language)
            => _globalOptions.GetOption(s_implementIEquatable, language);

        public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value)
            => _globalOptions.SetGlobalOption(s_implementIEquatable, language, value);

        public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language)
            => _globalOptions.GetOption(s_addNullChecks, language);

        public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value)
            => _globalOptions.SetGlobalOption(s_addNullChecks, language, value);
    }
}

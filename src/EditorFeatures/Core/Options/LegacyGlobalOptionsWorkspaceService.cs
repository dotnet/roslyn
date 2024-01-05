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
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class LegacyGlobalOptionsWorkspaceService(IGlobalOptionService globalOptions) : ILegacyGlobalOptionsWorkspaceService
    {
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        private static readonly Option2<bool> s_generateOverridesOption = new(
            "dotnet_generate_overrides_for_all_members", defaultValue: true);

        private static readonly PerLanguageOption2<bool> s_generateOperators = new(
            "dotnet_generate_equality_operators",
            defaultValue: false);

        private static readonly PerLanguageOption2<bool> s_implementIEquatable = new(
            "dotnet_generate_iequatable_implementation",
            defaultValue: false);

        internal static readonly PerLanguageOption2<bool> s_addNullChecks = new(
            "dotnet_generate_constructor_parameter_null_checks",
            defaultValue: false);

        public bool GenerateOverrides
        {
            get => _globalOptions.GetOption(s_generateOverridesOption);
            set => _globalOptions.SetGlobalOption(s_generateOverridesOption, value);
        }

        public bool RazorUseTabs
            => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.UseTabs);

        public int RazorTabSize
            => _globalOptions.GetOption(RazorLineFormattingOptionsStorage.TabSize);

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

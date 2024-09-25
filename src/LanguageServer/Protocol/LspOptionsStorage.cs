// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class LspOptionsStorage
    {
        /// <summary>
        /// This sets the max list size we will return in response to a completion request.
        /// If there are more than this many items, we will set the isIncomplete flag on the returned completion list.
        /// If set to negative value, we will always return the full list.
        /// </summary>
        public static readonly Option2<int> MaxCompletionListSize = new("dotnet_lsp_max_completion_list_size", defaultValue: 1000);

        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> LspEditorFeatureFlag = new("dotnet_enable_lsp_editor", defaultValue: false);

        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> LspSemanticTokensFeatureFlag = new("dotnet_enable_lsp_semantic_tokens", defaultValue: false);

        /// <summary>
        /// This flag is turned on when the C# devkit is installed.
        /// This can cause certain LSP features to behave differently, for example we avoid returning test code lenses when devkit is running.
        /// </summary>
        /// <remarks>
        /// This flag is not user visible.
        /// </remarks>
        public static readonly Option2<bool> LspUsingDevkitFeatures = new("dotnet_lsp_using_devkit", defaultValue: false);

        private static readonly OptionGroup s_codeLensOptionGroup = new(name: "code_lens", description: "");

        private static readonly OptionGroup s_autoInsertOptionGroup = new(name: "auto_insert", description: "");

        /// <summary>
        /// Flag indicating whether or not references should be returned in LSP codelens.
        /// </summary>
        public static readonly PerLanguageOption2<bool> LspEnableReferencesCodeLens = new("dotnet_enable_references_code_lens", defaultValue: true, group: s_codeLensOptionGroup);

        /// <summary>
        /// Flag indicating whether or not test and debug code lens items should be returned.
        /// </summary>
        public static readonly PerLanguageOption2<bool> LspEnableTestsCodeLens = new("dotnet_enable_tests_code_lens", defaultValue: true, group: s_codeLensOptionGroup);

        /// <summary>
        /// Flag indicating whether or not auto-insert should be abled by default in LSP.
        /// </summary>
        public static readonly PerLanguageOption2<bool> LspEnableAutoInsert = new("dotnet_enable_auto_insert", defaultValue: true, group: s_autoInsertOptionGroup);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class LspOptions
    {
        /// <summary>
        /// This sets the max list size we will return in response to a completion request.
        /// If there are more than this many items, we will set the isIncomplete flag on the returned completion list.
        /// </summary>
        public static readonly Option2<int> MaxCompletionListSize = new("dotnet_language_server_protocol_max_completion_list_size", defaultValue: 1000);

        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> LspEditorFeatureFlag = new("dotnet_language_server_protocol_editor_enabled", defaultValue: false);

        // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
        public static readonly Option2<bool> LspSemanticTokensFeatureFlag = new("dotnet_language_server_protocol_semantic_tokens_enabled", defaultValue: false);
    }
}

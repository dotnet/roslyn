// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSMac.API;

internal class VSMacLspOptions
{
    /// <summary>
    /// External access option to allow VSMac to turn on the LSP editor feature flag.
    /// </summary>
    public static readonly Option2<bool> VSMacLspEditorOption = LspOptions.LspEditorFeatureFlag;

    /// <summary>
    /// External access option to allow VSMac to turn on the semantic tokens feature flag.
    /// </summary>
    public static readonly Option2<bool> VSMacLspSemanticTokensOption = LspOptions.LspSemanticTokensFeatureFlag;
}

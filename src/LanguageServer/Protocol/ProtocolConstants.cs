// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ProtocolConstants
{
    public static ImmutableArray<string> RoslynLspLanguages = [LanguageNames.CSharp, LanguageNames.VisualBasic, LanguageNames.FSharp];

    public const string RoslynLspLanguagesContract = "RoslynLspLanguages";

    public const string TypeScriptLanguageContract = "TypeScriptLspLanguage";

    /// <summary>
    /// LSP contract for services that apply to servers of every LSP contract.  Only valid on LSP service exports; no
    /// server may use it as its own contract.
    /// </summary>
    public const string AllLspContracts = "*";
}

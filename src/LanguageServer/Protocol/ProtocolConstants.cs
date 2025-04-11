// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class ProtocolConstants
{
    public const string RazorCSharp = "RazorCSharp";

    public static ImmutableArray<string> RoslynLspLanguages = [LanguageNames.CSharp, LanguageNames.VisualBasic, LanguageNames.FSharp];

    public const string RoslynLspLanguagesContract = "RoslynLspLanguages";

    public const string TypeScriptLanguageContract = "TypeScriptLspLanguage";

    public const string RazorCohostContract = "RazorLanguageServer";
}

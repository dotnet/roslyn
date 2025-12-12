// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Enables legacy APIs to access global options from workspace.
/// Not available OOP. Only use in client code and when IGlobalOptionService can't be MEF imported.
/// </summary>
internal interface ILegacyGlobalOptionsWorkspaceService : IWorkspaceService
{
    bool RazorUseTabs { get; }
    int RazorTabSize { get; }

    bool GenerateOverrides { get; set; }

    bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language);
    void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value);

    bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language);
    void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value);

    bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language);
    void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value);

    SyntaxFormattingOptions GetSyntaxFormattingOptions(LanguageServices languageServices);
}

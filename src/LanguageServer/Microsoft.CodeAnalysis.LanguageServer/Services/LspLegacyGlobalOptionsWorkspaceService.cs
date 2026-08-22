// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

/// <summary>
/// Headless implementation of <see cref="ILegacyGlobalOptionsWorkspaceService"/> for the LSP server.
/// The real (VS) implementation lives in EditorFeatures, which does not ship with the language
/// server. Several "from members" refactorings (generate constructor, generate equals/GetHashCode,
/// generate overrides) require this service to be present, otherwise their dialog code actions are
/// never produced.
/// </summary>
[ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspLegacyGlobalOptionsWorkspaceService() : ILegacyGlobalOptionsWorkspaceService
{
    public bool RazorUseTabs
        => LineFormattingOptions.Default.UseTabs;

    public int RazorTabSize
        => LineFormattingOptions.Default.TabSize;

    public bool GenerateOverrides
    {
        get => true;
        set { }
    }

    public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language)
        => false;

    public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value)
    {
    }

    public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language)
        => false;

    public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value)
    {
    }

    public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language)
        => false;

    public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value)
    {
    }

    public SyntaxFormattingOptions GetSyntaxFormattingOptions(LanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingService>().DefaultOptions;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;

/// <summary>
/// Enables legacy APIs to access global options from workspace.
/// </summary>
[ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared]
internal sealed class OmnisharpLegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OmnisharpLegacyGlobalOptionsWorkspaceService()
    {
    }

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
        => SyntaxFormattingOptions.CommonDefaults;
}

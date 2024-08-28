// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseExplicitTypeDiagnosticAnalyzer : CSharpTypeStyleDiagnosticAnalyzerBase
{
    private static readonly LocalizableString s_Title =
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_explicit_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

    private static readonly LocalizableString s_Message =
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_explicit_type_instead_of_var), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

    protected override CSharpTypeStyleHelper Helper => CSharpUseExplicitTypeHelper.Instance;

    public CSharpUseExplicitTypeDiagnosticAnalyzer()
        : base(diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
               enforceOnBuild: EnforceOnBuildValues.UseExplicitType,
               title: s_Title,
               message: s_Message)
    {
    }
}

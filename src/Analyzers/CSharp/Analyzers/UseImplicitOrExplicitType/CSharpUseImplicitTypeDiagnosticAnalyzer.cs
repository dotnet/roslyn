// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseImplicitTypeDiagnosticAnalyzer : CSharpTypeStyleDiagnosticAnalyzerBase
    {
        private static readonly LocalizableString s_Title =
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_implicit_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly LocalizableString s_Message =
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.use_var_instead_of_explicit_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        protected override CSharpTypeStyleHelper Helper => CSharpUseImplicitTypeHelper.Instance;

        public CSharpUseImplicitTypeDiagnosticAnalyzer()
            : base(diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                   enforceOnBuild: EnforceOnBuildValues.UseImplicitType,
                   title: s_Title,
                   message: s_Message)
        {
        }
    }
}

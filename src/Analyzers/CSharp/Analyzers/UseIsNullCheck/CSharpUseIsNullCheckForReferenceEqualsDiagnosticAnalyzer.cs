// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseIsNullCheck;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer : AbstractUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer<SyntaxKind>
    {
        public CSharpUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer()
            : base(CSharpAnalyzersResources.Use_is_null_check)
        {
        }

        protected override bool IsLanguageVersionSupported(Compilation compilation)
            => compilation.LanguageVersion() >= LanguageVersion.CSharp7;

        protected override bool IsUnconstrainedGenericSupported(Compilation compilation)
            => compilation.LanguageVersion() >= LanguageVersion.CSharp8;

        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;
    }
}

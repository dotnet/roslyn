// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
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

        protected override bool IsLanguageVersionSupported(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7;

        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;
    }
}

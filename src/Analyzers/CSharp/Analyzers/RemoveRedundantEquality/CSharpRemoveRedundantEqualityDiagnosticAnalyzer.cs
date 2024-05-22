// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveRedundantEquality;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantEquality;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveRedundantEqualityDiagnosticAnalyzer
    : AbstractRemoveRedundantEqualityDiagnosticAnalyzer
{
    public CSharpRemoveRedundantEqualityDiagnosticAnalyzer() : base(CSharpSyntaxFacts.Instance)
    {
    }
}

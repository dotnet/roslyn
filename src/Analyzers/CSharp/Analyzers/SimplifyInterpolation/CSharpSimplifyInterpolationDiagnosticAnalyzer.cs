// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Analyzers.SimplifyInterpolation;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SimplifyInterpolation;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpSimplifyInterpolationDiagnosticAnalyzer : AbstractSimplifyInterpolationDiagnosticAnalyzer<
    InterpolationSyntax, ExpressionSyntax>
{
    protected override IVirtualCharService GetVirtualCharService()
        => CSharpVirtualCharService.Instance;

    protected override ISyntaxFacts GetSyntaxFacts()
        => CSharpSyntaxFacts.Instance;

    protected override AbstractSimplifyInterpolationHelpers GetHelpers() => CSharpSimplifyInterpolationHelpers.Instance;
}

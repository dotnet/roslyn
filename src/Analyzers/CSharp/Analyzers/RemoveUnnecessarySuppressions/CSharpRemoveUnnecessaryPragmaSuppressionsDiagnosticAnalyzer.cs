// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions
{
#if !CODE_STYLE // Not exported in CodeStyle layer: https://github.com/dotnet/roslyn/issues/47942
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
#endif
    internal sealed class CSharpRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer
        : AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer
    {
        protected override string CompilerErrorCodePrefix => "CS";
        protected override int CompilerErrorCodeDigitCount => 4;
        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;
        protected override ISemanticFacts SemanticFacts => CSharpSemanticFacts.Instance;
        protected override (Assembly assembly, string typeName) GetCompilerDiagnosticAnalyzerInfo()
            => (typeof(SyntaxKind).Assembly, CompilerDiagnosticAnalyzerNames.CSharpCompilerAnalyzerTypeName);
        protected override bool ContainsPragmaDirective(SyntaxNode root)
            => root.ContainsDirective(SyntaxKind.PragmaWarningDirectiveTrivia);
    }
}

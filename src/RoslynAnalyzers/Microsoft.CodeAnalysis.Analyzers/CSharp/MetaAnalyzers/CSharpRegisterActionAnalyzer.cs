// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpRegisterActionAnalyzer : RegisterActionAnalyzer<InvocationExpressionSyntax, ArgumentSyntax, SyntaxKind>
    {
        internal const string CSharpSyntaxKindName = @"Microsoft.CodeAnalysis.CSharp.SyntaxKind";
        internal const string BasicSyntaxKindName = @"Microsoft.CodeAnalysis.VisualBasic.SyntaxKind";

        protected override RegisterActionCodeBlockAnalyzer GetCodeBlockAnalyzer(
            Compilation compilation,
            INamedTypeSymbol analysisContext,
            INamedTypeSymbol compilationStartAnalysisContext,
            INamedTypeSymbol codeBlockStartAnalysisContext,
            INamedTypeSymbol operationBlockStartAnalysisContext,
            INamedTypeSymbol symbolKind)
        {
            INamedTypeSymbol? csharpSyntaxKind = compilation.GetOrCreateTypeByMetadataName(CSharpSyntaxKindName);
            INamedTypeSymbol? basicSyntaxKind = compilation.GetOrCreateTypeByMetadataName(BasicSyntaxKindName);
            return new CSharpRegisterActionCodeBlockAnalyzer(csharpSyntaxKind, basicSyntaxKind, analysisContext, compilationStartAnalysisContext,
                codeBlockStartAnalysisContext, operationBlockStartAnalysisContext, symbolKind);
        }

        private sealed class CSharpRegisterActionCodeBlockAnalyzer : RegisterActionCodeBlockAnalyzer
        {
            private readonly ITypeSymbol? _csharpSyntaxKind, _basicSyntaxKind;

            public CSharpRegisterActionCodeBlockAnalyzer(
                INamedTypeSymbol? csharpSyntaxKind,
                INamedTypeSymbol? basicSyntaxKind,
                INamedTypeSymbol analysisContext,
                INamedTypeSymbol compilationStartAnalysisContext,
                INamedTypeSymbol codeBlockStartAnalysisContext,
                INamedTypeSymbol operationBlockStartAnalysisContext,
                INamedTypeSymbol symbolKind)
                : base(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, operationBlockStartAnalysisContext, symbolKind)
            {
                _csharpSyntaxKind = csharpSyntaxKind;
                _basicSyntaxKind = basicSyntaxKind;
            }

            protected override SyntaxKind InvocationExpressionKind => SyntaxKind.InvocationExpression;
            protected override SyntaxKind ArgumentSyntaxKind => SyntaxKind.Argument;
            protected override SyntaxKind ParameterSyntaxKind => SyntaxKind.Parameter;

            protected override IEnumerable<SyntaxNode>? GetArgumentExpressions(InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList != null)
                {
                    return invocation.ArgumentList.Arguments.Select(a => a.Expression);
                }

                return null;
            }

            protected override SyntaxNode GetArgumentExpression(ArgumentSyntax argument)
            {
                return argument.Expression;
            }

            protected override SyntaxNode GetInvocationExpression(InvocationExpressionSyntax invocation)
            {
                return invocation.Expression;
            }

            protected override SyntaxNode? GetInvocationReceiver(InvocationExpressionSyntax invocation)
            {
                return (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
            }

            protected override bool IsSyntaxKind(ITypeSymbol type)
                => SymbolEqualityComparer.Default.Equals(type, _csharpSyntaxKind)
                    || SymbolEqualityComparer.Default.Equals(type, _basicSyntaxKind);
        }
    }
}

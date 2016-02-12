// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpRegisterActionAnalyzer : RegisterActionAnalyzer<ClassDeclarationSyntax, InvocationExpressionSyntax, ArgumentSyntax, SyntaxKind>
    {
        internal static readonly string CSharpSyntaxKindName = typeof(SyntaxKind).FullName;
        internal static readonly string BasicSyntaxKindName = @"Microsoft.CodeAnalysis.VisualBasic.SyntaxKind";

        protected override RegisterActionCodeBlockAnalyzer GetCodeBlockAnalyzer(Compilation compilation, INamedTypeSymbol analysisContext, INamedTypeSymbol compilationStartAnalysisContext, INamedTypeSymbol codeBlockStartAnalysisContext, INamedTypeSymbol symbolKind, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            INamedTypeSymbol csharpSyntaxKind = compilation.GetTypeByMetadataName(CSharpSyntaxKindName);
            INamedTypeSymbol basicSyntaxKind = compilation.GetTypeByMetadataName(BasicSyntaxKindName);
            return new CSharpRegisterActionCodeBlockAnalyzer(csharpSyntaxKind, basicSyntaxKind, analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class CSharpRegisterActionCodeBlockAnalyzer : RegisterActionCodeBlockAnalyzer
        {
            private readonly ITypeSymbol _csharpSyntaxKind, _basicSyntaxKind;

            public CSharpRegisterActionCodeBlockAnalyzer(
                INamedTypeSymbol csharpSyntaxKind,
                INamedTypeSymbol basicSyntaxKind,
                INamedTypeSymbol analysisContext,
                INamedTypeSymbol compilationStartAnalysisContext,
                INamedTypeSymbol codeBlockStartAnalysisContext,
                INamedTypeSymbol symbolKind,
                INamedTypeSymbol diagnosticAnalyzer,
                INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                _csharpSyntaxKind = csharpSyntaxKind;
                _basicSyntaxKind = basicSyntaxKind;
            }

            protected override SyntaxKind InvocationExpressionKind => SyntaxKind.InvocationExpression;
            protected override SyntaxKind ArgumentSyntaxKind => SyntaxKind.Argument;
            protected override SyntaxKind ParameterSyntaxKind => SyntaxKind.Parameter;

            protected override IEnumerable<SyntaxNode> GetArgumentExpressions(InvocationExpressionSyntax invocation)
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

            protected override SyntaxNode GetInvocationReceiver(InvocationExpressionSyntax invocation)
            {
                return (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
            }

            protected override bool IsSyntaxKind(ITypeSymbol type)
            {
                return (_csharpSyntaxKind != null && type.Equals(_csharpSyntaxKind)) ||
                    (_basicSyntaxKind != null && type.Equals(_basicSyntaxKind));
            }
        }
    }
}

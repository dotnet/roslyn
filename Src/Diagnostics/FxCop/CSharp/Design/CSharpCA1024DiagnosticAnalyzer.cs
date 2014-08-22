// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1024: Use properties where appropriate
    /// 
    /// Cause:
    /// A public or protected method has a name that starts with Get, takes no parameters, and returns a value that is not an array.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA1024DiagnosticAnalyzer : CA1024DiagnosticAnalyzer
    {
        protected override CA1024CodeBlockEndedAnalyzer GetCodeBlockEndedAnalyzer()
        {
            return new CodeBlockEndedAnalyzer();
        }

        private class CodeBlockEndedAnalyzer : CA1024CodeBlockEndedAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
        {
            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.InvocationExpression);

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            protected override Location GetDiagnosticLocation(SyntaxNode node)
            {
                switch (node.CSharpKind())
                {
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)node).Identifier.GetLocation();

                    case SyntaxKind.Block:
                        return GetDiagnosticLocation(node.Parent);

                    default:
                        return Location.None;
                }
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AnalyzerPowerPack.CSharp.Design
{
    /// <summary>
    /// CA1024: Use properties where appropriate
    /// 
    /// Cause:
    /// A public or protected method has a name that starts with Get, takes no parameters, and returns a value that is not an array.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA1024DiagnosticAnalyzer : CA1024DiagnosticAnalyzer<SyntaxKind>
    {
        protected override CA1024CodeBlockEndedAnalyzer GetCodeBlockEndedAnalyzer()
        {
            return new CodeBlockEndedAnalyzer();
        }

        private class CodeBlockEndedAnalyzer : CA1024CodeBlockEndedAnalyzer
        {
            public override SyntaxKind SyntaxKindOfInterest
            {
                get { return SyntaxKind.InvocationExpression; }
            }

            protected override Location GetDiagnosticLocation(SyntaxNode node)
            {
                switch (node.Kind())
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

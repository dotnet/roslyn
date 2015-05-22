// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AnalyzerPowerPack.CSharp.Design
{
    /// <summary>
    /// CA1033: Interface methods should be callable by child types
    /// <para>
    /// Consider a base type that explicitly implements a public interface method.
    /// A type that derives from the base type can access the inherited interface method only through a reference to the current instance ('this' in C#) that is cast to the interface.
    /// If the derived type re-implements (explicitly) the inherited interface method, the base implementation can no longer be accessed.
    /// The call through the current instance reference will invoke the derived implementation; this causes recursion and an eventual stack overflow.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This rule does not report a violation for an explicit implementation of IDisposable.Dispose when an externally visible Close() or System.IDisposable.Dispose(Boolean) method is provided.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpInterfaceMethodsShouldBeCallableByChildTypesAnalyzer :
        InterfaceMethodsShouldBeCallableByChildTypesAnalyzer<InvocationExpressionSyntax>
    {
        protected override bool ShouldExcludeCodeBlock(SyntaxNode codeBlock)
        {
            BlockSyntax body;
            if (codeBlock is BaseMethodDeclarationSyntax)
            {
                body = ((BaseMethodDeclarationSyntax)codeBlock).Body;
            }
            else if (codeBlock is AccessorDeclarationSyntax)
            {
                body = ((AccessorDeclarationSyntax)codeBlock).Body;
            }
            else if (codeBlock.Kind() == SyntaxKind.Block)
            {
                body = (BlockSyntax)codeBlock;
            }
            else
            {
                return false;
            }

            // Empty body OR body that just throws.
            return body.Statements.Count == 0 ||
                body.Statements.Count == 1 && body.Statements[0] is ThrowStatementSyntax;
        }
    }
}

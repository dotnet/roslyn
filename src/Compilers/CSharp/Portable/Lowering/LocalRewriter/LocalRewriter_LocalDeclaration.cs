// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            return RewriteLocalDeclaration(node.Syntax, node.LocalSymbol, VisitExpression(node.InitializerOpt), node.WasCompilerGenerated, node.HasErrors);
        }

        private BoundStatement RewriteLocalDeclaration(CSharpSyntaxNode syntax, LocalSymbol localSymbol, BoundExpression rewrittenInitializer, bool wasCompilerGenerated = false, bool hasErrors = false)
        {
            // A declaration of a local variable without an initializer has no associated IL.
            // Simply remove the declaration from the bound tree. The local symbol will
            // remain in the bound block, so codegen will make a stack frame location for it.
            if (rewrittenInitializer == null)
            {
                return null;
            }

            // A declaration of a local constant also does nothing, even though there is
            // an assignment. The value will be emitted directly where it is used. The 
            // local symbol remains in the bound block, but codegen will skip making a 
            // stack frame location for it. (We still need a symbol for it to stay 
            // around because we'll be generating debug info for it.)
            if (localSymbol.IsConst)
            {
                if (!localSymbol.Type.IsReferenceType && localSymbol.ConstantValue == null)
                {
                    // This can occur in error scenarios (e.g. bad imported metadata)
                    hasErrors = true;
                }
                else
                {
                    return null;
                }
            }

            // lowered local declaration node is associated with declaration (not whole statement)
            // this is done to make sure that debugger stepping is same as before
            var localDeclaration = syntax as LocalDeclarationStatementSyntax;
            if (localDeclaration != null)
            {
                syntax = localDeclaration.Declaration.Variables[0];
            }

            BoundStatement rewrittenLocalDeclaration = new BoundExpressionStatement(
                syntax,
                new BoundAssignmentOperator(
                    syntax,
                    new BoundLocal(
                        syntax,
                        localSymbol,
                        null,
                        localSymbol.Type
                    ),
                    rewrittenInitializer,
                    localSymbol.Type,
                    localSymbol.RefKind),
                hasErrors);

            return AddLocalDeclarationSequencePointIfNecessary(syntax, localSymbol, rewrittenLocalDeclaration, wasCompilerGenerated);
        }

        private BoundStatement AddLocalDeclarationSequencePointIfNecessary(CSharpSyntaxNode syntax, LocalSymbol localSymbol, BoundStatement rewrittenLocalDeclaration, bool wasCompilerGenerated = false)
        {
            // Add sequence points, if necessary.
            if (this.GenerateDebugInfo && !wasCompilerGenerated && !localSymbol.IsConst && syntax.Kind() == SyntaxKind.VariableDeclarator)
            {
                Debug.Assert(syntax.SyntaxTree != null);
                rewrittenLocalDeclaration = AddSequencePoint((VariableDeclaratorSyntax)syntax, rewrittenLocalDeclaration);
            }

            return rewrittenLocalDeclaration;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            return RewriteLocalDeclaration(node, node.Syntax, node.LocalSymbol, VisitExpression(node.InitializerOpt), node.HasErrors);
        }

        private BoundStatement? RewriteLocalDeclaration(BoundLocalDeclaration? originalOpt, SyntaxNode syntax, LocalSymbol localSymbol, BoundExpression? rewrittenInitializer, bool hasErrors = false)
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
                _factory.AssignmentExpression(
                    syntax,
                    new BoundLocal(
                        syntax,
                        localSymbol,
                        null,
                        localSymbol.Type
                    ),
                    rewrittenInitializer,
                    localSymbol.IsRef),
                hasErrors);

            return InstrumentLocalDeclarationIfNecessary(originalOpt, localSymbol, rewrittenLocalDeclaration);
        }

        private BoundStatement InstrumentLocalDeclarationIfNecessary(BoundLocalDeclaration? originalOpt, LocalSymbol localSymbol, BoundStatement rewrittenLocalDeclaration)
        {
            // Add sequence points, if necessary.
            if (this.Instrument && originalOpt?.WasCompilerGenerated == false && !localSymbol.IsConst &&
                (originalOpt.Syntax.Kind() == SyntaxKind.VariableDeclarator ||
                    (originalOpt.Syntax.Kind() == SyntaxKind.LocalDeclarationStatement &&
                        ((LocalDeclarationStatementSyntax)originalOpt.Syntax).Declaration.Variables.Count == 1)))
            {
                rewrittenLocalDeclaration = Instrumenter.InstrumentUserDefinedLocalInitialization(originalOpt, rewrittenLocalDeclaration);
            }

            return rewrittenLocalDeclaration;
        }

        public sealed override BoundNode VisitOutVariablePendingInference(OutVariablePendingInference node)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class UsedAssembliesRecorder : BoundTreeWalkerWithStackGuard
    {
        private readonly CSharpCompilation _compilation;

        public static void RecordUsedAssemblies(CSharpCompilation compilation, BoundNode node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node != null);

            try
            {
                var visitor = new UsedAssembliesRecorder(compilation);
                visitor.Visit(node);
            }
            catch (CancelledByStackGuardException ex)
            {
                // PROTOTYPE(UsedAssemblyReferences): This error might not be cached, but its presence might affect cached full set of used assemblies. 
                //                                    We would report all assemblies as used, even though no one will ever see this error and under
                //                                    different environment state the pass could succeed causing us to return different set of used assemblies
                //                                    with no apparent reason for the difference from the consumer's point of view. 
                //                                    We will have the same problem with any BoundTreeWalker invoked under umbrella of GetUsedAssemblyReferences API.
                //                                    Including flow analysis, lowering, etc.
                ex.AddAnError(diagnostics);
            }
        }

        private UsedAssembliesRecorder(CSharpCompilation compilation)
        {
            _compilation = compilation;
        }

        private void AddAssembliesUsedBySymbolReference(BoundExpression receiverOpt, Symbol symbol)
        {
            if (symbol.IsStatic && receiverOpt?.Kind != BoundKind.TypeExpression)
            {
                _compilation.AddAssembliesUsedByTypeReference(symbol.ContainingType);
            }
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.FieldSymbol);
            return base.VisitFieldAccess(node);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.Method);
            return base.VisitCall(node);
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            if (node.Argument is BoundNamespaceExpression nsExpr)
            {
                Debug.Assert(!nsExpr.NamespaceSymbol.IsGlobalNamespace);
                _compilation.AddAssembliesUsedByNamespaceReference(nsExpr.NamespaceSymbol);
            }

            return base.VisitNameOfOperator(node);
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            // It is very common for bound trees to be left-heavy binary operators, eg,
            // a + b + c + d + ...
            // To avoid blowing the stack, do not recurse down the left hand side.

            // In order to avoid blowing the stack, we end up visiting right children
            // before left children; this should not be a problem

            BoundBinaryOperator current = node;
            while (true)
            {
                Visit(current.Right);
                if (current.Left.Kind == BoundKind.BinaryOperator)
                {
                    current = (BoundBinaryOperator)current.Left;
                }
                else
                {
                    Visit(current.Left);
                    break;
                }
            }

            return null;
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            // In order to avoid blowing the stack, we end up visiting right children
            // before left children; this should not be a problem

            BoundUserDefinedConditionalLogicalOperator current = node;
            while (true)
            {
                Visit(current.Right);
                if (current.Left.Kind == BoundKind.UserDefinedConditionalLogicalOperator)
                {
                    current = (BoundUserDefinedConditionalLogicalOperator)current.Left;
                }
                else
                {
                    Visit(current.Left);
                    break;
                }
            }

            return null;
        }
    }
}

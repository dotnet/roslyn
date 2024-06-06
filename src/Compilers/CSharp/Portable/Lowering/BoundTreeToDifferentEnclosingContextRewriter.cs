// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// a bound node rewriter that rewrites types properly (which in some cases the automatically-generated
    /// base class does not).  This is used in the lambda rewriter, the iterator rewriter, and the async rewriter.
    /// </summary>
    internal abstract class BoundTreeToDifferentEnclosingContextRewriter : BoundTreeRewriterWithStackGuard
    {
        // A mapping from every local variable to its replacement local variable.  Local variables are replaced when
        // their types change due to being inside of a generic method.  Otherwise we reuse the original local (even
        // though its containing method is not correct because the code is moved into another method)
        private readonly Dictionary<LocalSymbol, LocalSymbol> localMap = new Dictionary<LocalSymbol, LocalSymbol>();

        // A mapping for types in the original method to types in its replacement.  This is mainly necessary
        // when the original method was generic, as type parameters in the original method are mapping into
        // type parameters of the resulting class.
        protected abstract TypeMap TypeMap { get; }

        protected abstract MethodSymbol CurrentMethod { get; }

        public override BoundNode DefaultVisit(BoundNode node)
        {
            Debug.Fail($"Override the visitor for {node.Kind}");
            return base.DefaultVisit(node);
        }

        protected void RewriteLocals(ImmutableArray<LocalSymbol> locals, ArrayBuilder<LocalSymbol> newLocals)
        {
            foreach (var local in locals)
            {
                if (TryRewriteLocal(local, out LocalSymbol? newLocal))
                {
                    newLocals.Add(newLocal);
                }
            }
        }

        protected virtual bool TryRewriteLocal(LocalSymbol local, [NotNullWhen(true)] out LocalSymbol? newLocal)
        {
            if (localMap.TryGetValue(local, out newLocal))
            {
                return true;
            }

            var newType = VisitType(local.Type);
            if (TypeSymbol.Equals(newType, local.Type, TypeCompareKind.ConsiderEverything2))
            {
                newLocal = local;
            }
            else
            {
                newLocal = new TypeSubstitutedLocalSymbol(local, TypeWithAnnotations.Create(newType), CurrentMethod);
                localMap.Add(local, newLocal);
            }

            return true;
        }

        protected sealed override ImmutableArray<LocalSymbol> VisitLocals(ImmutableArray<LocalSymbol> locals)
        {
            if (locals.IsEmpty) return locals;
            var newLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            RewriteLocals(locals, newLocals);
            return newLocals.ToImmutableAndFree();
        }

        protected sealed override LocalSymbol VisitLocalSymbol(LocalSymbol local)
        {
            if (!TryRewriteLocal(local, out var newLocal))
            {
                throw ExceptionUtilities.UnexpectedValue(local);
            }

            return newLocal;
        }

        protected bool TryGetRewrittenLocal(LocalSymbol local, [NotNullWhen(true)] out LocalSymbol? localToUse)
        {
            return localMap.TryGetValue(local, out localToUse);
        }

        public override BoundNode VisitBlock(BoundBlock node)
            => VisitBlock(node, removeInstrumentation: false);

        protected BoundBlock VisitBlock(BoundBlock node, bool removeInstrumentation)
        {
            // Note: Instrumentation variable is intentionally not rewritten. It should never be lifted.

            var newLocals = this.VisitLocals(node.Locals);
            var newLocalFunctions = this.VisitDeclaredLocalFunctions(node.LocalFunctions);
            var newStatements = VisitList(node.Statements);
            var newInstrumentation = removeInstrumentation ? null : (BoundBlockInstrumentation?)Visit(node.Instrumentation);
            return node.Update(newLocals, newLocalFunctions, node.HasUnsafeModifier, newInstrumentation, newStatements);
        }

        [return: NotNullIfNotNull(nameof(type))]
        public sealed override TypeSymbol? VisitType(TypeSymbol? type)
        {
            return TypeMap.SubstituteType(type).Type;
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            // Local rewriter should have already rewritten interpolated strings into their final form of calls and gotos
            Debug.Assert(node.InterpolatedStringHandlerData is null);

            return node.Update(
                node.OperatorKind,
                node.ConstantValueOpt,
                VisitMethodSymbol(node.Method),
                VisitType(node.ConstrainedToType),
                node.ResultKind,
                (BoundExpression)Visit(node.Left),
                (BoundExpression)Visit(node.Right),
                VisitType(node.Type));
        }

        public override BoundNode? VisitConversion(BoundConversion node)
        {
            var conversion = node.Conversion;

            if (conversion.Method is not null)
            {
                conversion = conversion.SetConversionMethod(VisitMethodSymbol(conversion.Method));
            }

            return node.Update(
                (BoundExpression)Visit(node.Operand),
                conversion,
                node.IsBaseConversion,
                node.Checked,
                node.ExplicitCastInCode,
                node.ConstantValueOpt,
                node.ConversionGroupOpt,
                VisitType(node.Type));
        }

        [return: NotNullIfNotNull(nameof(method))]
        protected override MethodSymbol? VisitMethodSymbol(MethodSymbol? method)
        {
            if (method is null)
            {
                return null;
            }

            if (method.ContainingType.IsAnonymousType)
            {
                //  Method of an anonymous type
                var newType = (NamedTypeSymbol)TypeMap.SubstituteType(method.ContainingType).AsTypeSymbolOnly();
                if (ReferenceEquals(newType, method.ContainingType))
                {
                    //  Anonymous type symbol was not rewritten
                    return method;
                }

                //  get a new method by name
                foreach (var member in newType.GetMembers(method.Name))
                {
                    if (member.Kind == SymbolKind.Method)
                    {
                        return (MethodSymbol)member;
                    }
                }

                throw ExceptionUtilities.Unreachable();
            }
            else
            {
                //  Method of a regular type
                return ((MethodSymbol)method.OriginalDefinition)
                    .AsMember((NamedTypeSymbol)TypeMap.SubstituteType(method.ContainingType).AsTypeSymbolOnly())
                    .ConstructIfGeneric(TypeMap.SubstituteTypes(method.TypeArgumentsWithAnnotations));
            }
        }
    }
}

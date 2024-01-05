// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitTupleLiteral(BoundTupleLiteral node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitConvertedTupleLiteral(BoundConvertedTupleLiteral node)
        {
            return VisitTupleExpression(node);
        }

        private BoundNode VisitTupleExpression(BoundTupleExpression node)
        {
            ImmutableArray<BoundExpression> rewrittenArguments = VisitList(node.Arguments);
            return RewriteTupleCreationExpression(node, rewrittenArguments);
        }

        /// <summary>
        /// Converts the expression for creating a tuple instance into an expression creating a ValueTuple (if short) or nested ValueTuples (if longer).
        ///
        /// For instance, for a long tuple we'll generate:
        /// creationExpression(ctor=largestCtor, args=firstArgs+(nested creationExpression for remainder, with smaller ctor and next few args))
        /// </summary>
        private BoundExpression RewriteTupleCreationExpression(BoundTupleExpression node, ImmutableArray<BoundExpression> rewrittenArguments)
        {
            Debug.Assert(node.Type is { });
            return MakeTupleCreationExpression(node.Syntax, (NamedTypeSymbol)node.Type, rewrittenArguments);
        }

        private BoundExpression MakeTupleCreationExpression(SyntaxNode syntax, NamedTypeSymbol type, ImmutableArray<BoundExpression> rewrittenArguments)
        {
            Debug.Assert(type.IsTupleType);

            ArrayBuilder<NamedTypeSymbol> underlyingTupleTypeChain = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            NamedTypeSymbol.GetUnderlyingTypeChain(type, underlyingTupleTypeChain);

            try
            {
                // make a creation expression for the smallest type
                NamedTypeSymbol smallestType = underlyingTupleTypeChain.Pop();
                ImmutableArray<BoundExpression> smallestCtorArguments = ImmutableArray.Create(rewrittenArguments,
                                                                                              underlyingTupleTypeChain.Count * (NamedTypeSymbol.ValueTupleRestPosition - 1),
                                                                                              smallestType.Arity);
                var smallestCtor = (MethodSymbol?)NamedTypeSymbol.GetWellKnownMemberInType(smallestType.OriginalDefinition,
                                                                                            NamedTypeSymbol.GetTupleCtor(smallestType.Arity),
                                                                                            _diagnostics,
                                                                                            syntax);
                if (smallestCtor is null)
                {
                    return _factory.BadExpression(type);
                }

                MethodSymbol smallestConstructor = smallestCtor.AsMember(smallestType);
                BoundObjectCreationExpression currentCreation = new BoundObjectCreationExpression(syntax, smallestConstructor, smallestCtorArguments);

                Binder.CheckRequiredMembersInObjectInitializer(smallestConstructor, initializers: ImmutableArray<BoundExpression>.Empty, syntax, _diagnostics);

                if (underlyingTupleTypeChain.Count > 0)
                {
                    NamedTypeSymbol tuple8Type = underlyingTupleTypeChain.Peek();
                    var tuple8Ctor = (MethodSymbol?)NamedTypeSymbol.GetWellKnownMemberInType(tuple8Type.OriginalDefinition,
                                                                                            NamedTypeSymbol.GetTupleCtor(NamedTypeSymbol.ValueTupleRestPosition),
                                                                                            _diagnostics,
                                                                                            syntax);
                    if (tuple8Ctor is null)
                    {
                        return _factory.BadExpression(type);
                    }

                    Binder.CheckRequiredMembersInObjectInitializer(tuple8Ctor, initializers: ImmutableArray<BoundExpression>.Empty, syntax, _diagnostics);

                    // make successively larger creation expressions containing the previous one
                    do
                    {
                        ImmutableArray<BoundExpression> ctorArguments = ImmutableArray.Create(rewrittenArguments,
                                                                                              (underlyingTupleTypeChain.Count - 1) * (NamedTypeSymbol.ValueTupleRestPosition - 1),
                                                                                              NamedTypeSymbol.ValueTupleRestPosition - 1)
                                                                                      .Add(currentCreation);

                        MethodSymbol constructor = tuple8Ctor.AsMember(underlyingTupleTypeChain.Pop());
                        currentCreation = new BoundObjectCreationExpression(syntax, constructor, ctorArguments);
                    }
                    while (underlyingTupleTypeChain.Count > 0);
                }

                currentCreation = currentCreation.Update(
                    currentCreation.Constructor,
                    currentCreation.Arguments,
                    currentCreation.ArgumentNamesOpt,
                    currentCreation.ArgumentRefKindsOpt,
                    currentCreation.Expanded,
                    currentCreation.ArgsToParamsOpt,
                    currentCreation.DefaultArguments,
                    currentCreation.ConstantValueOpt,
                    currentCreation.InitializerExpressionOpt,
                    type);

                return currentCreation;
            }
            finally
            {
                underlyingTupleTypeChain.Free();
            }
        }

    }
}

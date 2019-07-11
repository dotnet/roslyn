// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            throw ExceptionUtilities.Unreachable;
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
            return MakeTupleCreationExpression(node.Syntax, (NamedTypeSymbol)node.Type, rewrittenArguments);
        }

        private BoundExpression MakeTupleCreationExpression(SyntaxNode syntax, NamedTypeSymbol type, ImmutableArray<BoundExpression> rewrittenArguments)
        {
            NamedTypeSymbol underlyingTupleType = type.TupleUnderlyingType ?? type;
            Debug.Assert(underlyingTupleType.IsTupleCompatible());

            ArrayBuilder<NamedTypeSymbol> underlyingTupleTypeChain = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            TupleTypeSymbol.GetUnderlyingTypeChain(underlyingTupleType, underlyingTupleTypeChain);

            try
            {
                // make a creation expression for the smallest type
                NamedTypeSymbol smallestType = underlyingTupleTypeChain.Pop();
                ImmutableArray<BoundExpression> smallestCtorArguments = ImmutableArray.Create(rewrittenArguments,
                                                                                              underlyingTupleTypeChain.Count * (TupleTypeSymbol.RestPosition - 1),
                                                                                              smallestType.Arity);
                var smallestCtor = (MethodSymbol)TupleTypeSymbol.GetWellKnownMemberInType(smallestType.OriginalDefinition,
                                                                                            TupleTypeSymbol.GetTupleCtor(smallestType.Arity),
                                                                                            _diagnostics,
                                                                                            syntax);
                if ((object)smallestCtor == null)
                {
                    return _factory.BadExpression(type);
                }

                MethodSymbol smallestConstructor = smallestCtor.AsMember(smallestType);
                BoundObjectCreationExpression currentCreation = new BoundObjectCreationExpression(syntax, smallestConstructor, null, smallestCtorArguments);

                if (underlyingTupleTypeChain.Count > 0)
                {
                    NamedTypeSymbol tuple8Type = underlyingTupleTypeChain.Peek();
                    var tuple8Ctor = (MethodSymbol)TupleTypeSymbol.GetWellKnownMemberInType(tuple8Type.OriginalDefinition,
                                                                                            TupleTypeSymbol.GetTupleCtor(TupleTypeSymbol.RestPosition),
                                                                                            _diagnostics,
                                                                                            syntax);
                    if ((object)tuple8Ctor == null)
                    {
                        return _factory.BadExpression(type);
                    }

                    // make successively larger creation expressions containing the previous one
                    do
                    {
                        ImmutableArray<BoundExpression> ctorArguments = ImmutableArray.Create(rewrittenArguments,
                                                                                              (underlyingTupleTypeChain.Count - 1) * (TupleTypeSymbol.RestPosition - 1),
                                                                                              TupleTypeSymbol.RestPosition - 1)
                                                                                      .Add(currentCreation);

                        MethodSymbol constructor = tuple8Ctor.AsMember(underlyingTupleTypeChain.Pop());
                        currentCreation = new BoundObjectCreationExpression(syntax, constructor, null, ctorArguments);
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
                    currentCreation.ConstantValue,
                    currentCreation.InitializerExpressionOpt,
                    currentCreation.BinderOpt,
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

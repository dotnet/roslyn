﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitTupleLiteral(BoundTupleLiteral node)
        {
            ImmutableArray<BoundExpression> rewrittenArguments = VisitList(node.Arguments);
            return RewriteTupleCreationExpression(node, node.DeclaredOrTargetTupleTypeOpt, rewrittenArguments);
        }

        /// <summary>
        /// Converts the expression for creating a tuple instance into an expression creating a ValueTuple (if short) or nested ValueTuples (if longer).
        ///
        /// For instance, for a long tuple we'll generate:
        /// creationExpression(ctor=largestCtor, args=firstArgs+(nested creationExpression for remainder, with smaller ctor and next few args))
        /// </summary>
        private BoundNode RewriteTupleCreationExpression(BoundTupleLiteral node, TypeSymbol tupleType, ImmutableArray<BoundExpression> rewrittenArguments)
        {
            Debug.Assert((object)tupleType != null);
            Debug.Assert(tupleType.IsTupleType);
            NamedTypeSymbol underlyingTupleType = tupleType.TupleUnderlyingType;

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
                                                                                            node.Syntax);
                if ((object)smallestCtor == null)
                {
                    return new BoundBadExpression(node.Syntax, LookupResultKind.NotCreatable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), underlyingTupleType);
                }

                MethodSymbol smallestConstructor = smallestCtor.AsMember(smallestType);
                BoundObjectCreationExpression currentCreation = new BoundObjectCreationExpression(node.Syntax, smallestConstructor, smallestCtorArguments);

                if (underlyingTupleTypeChain.Count > 0)
                {
                    NamedTypeSymbol tuple8Type = underlyingTupleTypeChain.Peek();
                    var tuple8Ctor = (MethodSymbol)TupleTypeSymbol.GetWellKnownMemberInType(tuple8Type.OriginalDefinition,
                                                                                            TupleTypeSymbol.GetTupleCtor(TupleTypeSymbol.RestPosition),
                                                                                            _diagnostics,
                                                                                            node.Syntax);
                    if ((object)tuple8Ctor == null)
                    {
                        return new BoundBadExpression(node.Syntax, LookupResultKind.NotCreatable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), underlyingTupleType);
                    }

                    // make successively larger creation expressions containing the previous one
                    do
                    {
                        ImmutableArray<BoundExpression> ctorArguments = ImmutableArray.Create(rewrittenArguments,
                                                                                              (underlyingTupleTypeChain.Count - 1) * (TupleTypeSymbol.RestPosition - 1),
                                                                                              TupleTypeSymbol.RestPosition - 1)
                                                                                      .Add(currentCreation);

                        MethodSymbol constructor = tuple8Ctor.AsMember(underlyingTupleTypeChain.Pop());
                        currentCreation = new BoundObjectCreationExpression(node.Syntax, constructor, ctorArguments);
                    }
                    while (underlyingTupleTypeChain.Count > 0);
                }

                return currentCreation;
            }
            finally
            {
                underlyingTupleTypeChain.Free();
            }
        }
    }
}

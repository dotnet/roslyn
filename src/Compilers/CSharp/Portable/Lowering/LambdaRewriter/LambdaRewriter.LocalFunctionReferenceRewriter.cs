// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LambdaRewriter : MethodToClassRewriter
    {
        /// <summary>
        /// This pass is expected to run on partially lowered methods
        /// previously containing one or more local functions. At this
        /// point all local functions should have been rewritten into
        /// proper closure classes and have frames and proxies generated
        /// for them.
        /// 
        /// The only thing left is to visit all "references" to local functions
        /// and rewrite them to be references to the rewritten form. 
        /// </summary>
        private sealed class LocalFunctionReferenceRewriter : BoundTreeRewriterWithStackGuard
        {
            private readonly LambdaRewriter _lambdaRewriter;

            public LocalFunctionReferenceRewriter(LambdaRewriter lambdaRewriter)
            {
                _lambdaRewriter = lambdaRewriter;
            }

            public override BoundNode VisitCall(BoundCall node)
            {
                if (node.Method.MethodKind == MethodKind.LocalFunction)
                {
                    BoundExpression receiver;
                    MethodSymbol method;
                    var arguments = node.Arguments;
                    _lambdaRewriter.RemapLocalFunction(node.Syntax, node.Method, out receiver, out method, ref arguments);
                    node = node.Update(receiver, method, arguments);
                }

                return base.VisitCall(node);
            }

            public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
            {
                if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
                {
                    BoundExpression receiver;
                    MethodSymbol method;
                    var arguments = default(ImmutableArray<BoundExpression>);
                    _lambdaRewriter.RemapLocalFunction(
                        node.Syntax, node.MethodOpt, out receiver, out method, ref arguments);

                    return new BoundDelegateCreationExpression(
                        node.Syntax, receiver, method, isExtensionMethod: false, type: node.Type);
                }

                return base.VisitDelegateCreationExpression(node);
            }

            public override BoundNode VisitConversion(BoundConversion conversion)
            {
                if (conversion.ConversionKind == ConversionKind.MethodGroup &&
                    conversion.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
                {
                    BoundExpression receiver;
                    MethodSymbol method;
                    var arguments = default(ImmutableArray<BoundExpression>);
                    _lambdaRewriter.RemapLocalFunction(
                        conversion.Syntax, conversion.SymbolOpt, out receiver, out method, ref arguments);

                    return new BoundDelegateCreationExpression(
                        conversion.Syntax, receiver, method, isExtensionMethod: false, type: conversion.Type);
                }
                return base.VisitConversion(conversion);
            }
        }


        private void RemapLocalFunction(
            SyntaxNode syntax,
            MethodSymbol symbol,
            out BoundExpression receiver,
            out MethodSymbol method,
            ref ImmutableArray<BoundExpression> parameters,
            ImmutableArray<TypeSymbol> typeArguments = default(ImmutableArray<TypeSymbol>))
        {
            Debug.Assert(symbol.MethodKind == MethodKind.LocalFunction);

            if ((object)symbol != symbol.ConstructedFrom)
            {
                RemapLocalFunction(syntax,
                                   symbol.ConstructedFrom,
                                   out receiver,
                                   out method,
                                   ref parameters,
                                   TypeMap.SubstituteTypes(symbol.TypeArguments)
                                          .SelectAsArray(t => t.Type));
                return;
            }

            var mappedLocalFunction = _localFunctionMap[(LocalFunctionSymbol)symbol];

            var lambda = mappedLocalFunction.Symbol;
            var frameCount = lambda.ExtraSynthesizedParameterCount;
            if (frameCount != 0)
            {
                Debug.Assert(!parameters.IsDefault);
                var builder = ArrayBuilder<BoundExpression>.GetInstance();
                builder.AddRange(parameters);
                var start = lambda.ParameterCount - frameCount;
                for (int i = start; i < lambda.ParameterCount; i++)
                {
                    // will always be a LambdaFrame, it's always a closure class
                    var frameType = (NamedTypeSymbol)lambda.Parameters[i].Type.OriginalDefinition;

                    Debug.Assert(frameType is LambdaFrame);

                    if (frameType.IsGenericType)
                    {
                        var typeParameters = ((LambdaFrame)frameType).ConstructedFromTypeParameters;
                        var subst = this.TypeMap.SubstituteTypeParameters(typeParameters);
                        frameType = frameType.Construct(subst);
                    }
                    var frame = FrameOfType(syntax, frameType);
                    builder.Add(frame);
                }
                parameters = builder.ToImmutableAndFree();
            }

            method = lambda;
            NamedTypeSymbol constructedFrame;
            RemapLambdaOrLocalFunction(syntax,
                                       symbol,
                                       typeArguments,
                                       mappedLocalFunction.ClosureKind,
                                       ref method,
                                       out receiver,
                                       out constructedFrame);
        }
    }
}

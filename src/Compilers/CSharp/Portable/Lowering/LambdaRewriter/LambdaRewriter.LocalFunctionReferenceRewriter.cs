// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
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

            public override BoundNode Visit(BoundNode node)
            {
                var partiallyLowered = node as PartiallyLoweredLocalFunctionReference;
                if (partiallyLowered != null)
                {
                    var underlying = partiallyLowered.UnderlyingNode;
                    Debug.Assert(underlying.Kind == BoundKind.Call ||
                                 underlying.Kind == BoundKind.DelegateCreationExpression ||
                                 underlying.Kind == BoundKind.Conversion);
                    var oldProxies = _lambdaRewriter.proxies;
                    _lambdaRewriter.proxies = partiallyLowered.Proxies;

                    var result = base.Visit(underlying);

                    _lambdaRewriter.proxies = oldProxies;

                    return result;
                }
                return base.Visit(node);
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

        /// <summary>
        /// Visit all references to local functions (calls, delegate
        /// conversions, delegate creations) and rewrite them to point
        /// to the rewritten local function method instead of the original. 
        /// </summary>
        public BoundStatement RewriteLocalFunctionReferences(BoundStatement loweredBody)
        {
            var rewriter = new LocalFunctionReferenceRewriter(this);

            Debug.Assert(_currentMethod == _topLevelMethod);

            // Visit the body first since the state is already set
            // for the top-level method
            var newBody = (BoundStatement)rewriter.Visit(loweredBody);

            // Visit all the rewritten methods as well
            var synthesizedMethods = _synthesizedMethods;
            if (synthesizedMethods != null)
            {
                var newMethods = ArrayBuilder<TypeCompilationState.MethodWithBody>.GetInstance(
                    synthesizedMethods.Count);

                foreach (var oldMethod in synthesizedMethods)
                {
                    var synthesizedLambda = oldMethod.Method as SynthesizedLambdaMethod;
                    if (synthesizedLambda == null)
                    {
                        // The only methods synthesized by the rewriter should
                        // be lowered closures and frame constructors
                        Debug.Assert(oldMethod.Method.MethodKind == MethodKind.Constructor ||
                                     oldMethod.Method.MethodKind == MethodKind.StaticConstructor);
                        newMethods.Add(oldMethod);
                        continue;
                    }

                    _currentMethod = synthesizedLambda;
                    var closureKind = synthesizedLambda.ClosureKind;
                    if (closureKind == ClosureKind.Static || closureKind == ClosureKind.Singleton)
                    {
                        // no link from a static lambda to its container
                        _innermostFramePointer = _currentFrameThis = null;
                    }
                    else
                    {
                        _currentFrameThis = synthesizedLambda.ThisParameter;
                        _innermostFramePointer = null;
                        _framePointers.TryGetValue(synthesizedLambda.ContainingType, out _innermostFramePointer);
                    }

                    _currentTypeParameters = synthesizedLambda.ContainingType
                        ?.TypeParameters.Concat(synthesizedLambda.TypeParameters)
                        ?? synthesizedLambda.TypeParameters;
                    _currentLambdaBodyTypeMap = synthesizedLambda.TypeMap;

                    var rewrittenBody = (BoundStatement)rewriter.Visit(oldMethod.Body);

                    var newMethod = new TypeCompilationState.MethodWithBody(
                        synthesizedLambda, rewrittenBody, oldMethod.ImportChainOpt);
                    newMethods.Add(newMethod);
                }

                _synthesizedMethods = newMethods;
                synthesizedMethods.Free();
            }

            return newBody;
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

                    if (frameType.Arity > 0)
                    {
                        var typeParameters = ((LambdaFrame)frameType).ConstructedFromTypeParameters;
                        Debug.Assert(typeParameters.Length == frameType.Arity);
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

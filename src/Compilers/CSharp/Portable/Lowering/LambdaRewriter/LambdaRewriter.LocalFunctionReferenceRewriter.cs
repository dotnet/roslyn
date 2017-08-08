// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
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
                    _lambdaRewriter.RemapLocalFunction(
                        node.Syntax,
                        node.Method,
                        out receiver,
                        out method,
                        ref arguments);
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
                        node.Syntax,
                        node.MethodOpt,
                        out receiver,
                        out method,
                        ref arguments);

                    return new BoundDelegateCreationExpression(
                        node.Syntax, receiver, method, isExtensionMethod: false, type: node.Type);
                }

                return base.VisitDelegateCreationExpression(node);
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

                    var containerAsFrame = synthesizedLambda.ContainingType as ClosureEnvironment;

                    // Includes type parameters from the containing type iff
                    // the containing type is a frame. If it is a frame then
                    // the type parameters are captured, meaning that the
                    // type parameters should be included.
                    // If it is not a frame then the local function is being
                    // directly lowered into the method's containing type and
                    // the parameters should never be substituted.
                    _currentTypeParameters = containerAsFrame?.TypeParameters.Concat(synthesizedLambda.TypeParameters)
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

        /// <summary>
        /// Rewrites a reference to an unlowered local function to the newly
        /// lowered local function.
        /// </summary>
        private void RemapLocalFunction(
            SyntaxNode syntax,
            MethodSymbol localFunc,
            out BoundExpression receiver,
            out MethodSymbol method,
            ref ImmutableArray<BoundExpression> parameters)
        {
            Debug.Assert(localFunc.MethodKind == MethodKind.LocalFunction);

            var mappedLocalFunction = _localFunctionMap[(LocalFunctionSymbol)localFunc.OriginalDefinition];
            var loweredSymbol = mappedLocalFunction.Symbol;

            // If the local function captured variables then they will be stored
            // in frames and the frames need to be passed as extra parameters.
            var frameCount = loweredSymbol.ExtraSynthesizedParameterCount;
            if (frameCount != 0)
            {
                Debug.Assert(!parameters.IsDefault);

                // Build a new list of parameters to pass to the local function
                // call that includes any necessary capture frames
                var parametersBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                parametersBuilder.AddRange(parameters);

                var start = loweredSymbol.ParameterCount - frameCount;
                for (int i = start; i < loweredSymbol.ParameterCount; i++)
                {
                    // will always be a LambdaFrame, it's always a capture frame
                    var frameType = (NamedTypeSymbol)loweredSymbol.Parameters[i].Type.OriginalDefinition;

                    Debug.Assert(frameType is ClosureEnvironment);

                    if (frameType.Arity > 0)
                    {
                        var typeParameters = ((ClosureEnvironment)frameType).ConstructedFromTypeParameters;
                        Debug.Assert(typeParameters.Length == frameType.Arity);
                        var subst = this.TypeMap.SubstituteTypeParameters(typeParameters);
                        frameType = frameType.Construct(subst);
                    }

                    var frame = FrameOfType(syntax, frameType);
                    parametersBuilder.Add(frame);
                }
                parameters = parametersBuilder.ToImmutableAndFree();
            }

            method = loweredSymbol;
            NamedTypeSymbol constructedFrame;

            RemapLambdaOrLocalFunction(syntax,
                                       localFunc,
                                       SubstituteTypeArguments(localFunc.TypeArguments),
                                       mappedLocalFunction.ClosureKind,
                                       ref method,
                                       out receiver,
                                       out constructedFrame);
        }

        /// <summary>
        /// Substitutes references from old type arguments to new type arguments
        /// in the lowered methods.
        /// </summary>
        /// <example>
        /// Consider the following method:
        ///     void M() {
        ///         void L&lt;T&gt;(T t) => Console.Write(t);
        ///         L("A");
        ///     }
        ///     
        /// In this example, L&lt;T&gt; is a local function that will be
        /// lowered into its own method and the type parameter T will be
        /// alpha renamed to something else (let's call it T'). In this case,
        /// all references to the original type parameter T in L must be
        /// rewritten to the renamed parameter, T'.
        /// </example>
        private ImmutableArray<TypeSymbol> SubstituteTypeArguments(ImmutableArray<TypeSymbol> typeArguments)
        {
            Debug.Assert(!typeArguments.IsDefault);

            if (typeArguments.IsEmpty)
            {
                return typeArguments;
            }

            // We must perform this process repeatedly as local
            // functions may nest inside one another and capture type
            // parameters from the enclosing local functions. Each
            // iteration of nesting will cause alpha-renaming of the captured
            // parameters, meaning that we must replace until there are no
            // more alpha-rename mappings.
            //
            // The method symbol references are different from all other
            // substituted types in this context because the method symbol in
            // local function references is not rewritten until all local
            // functions have already been lowered. Everything else is rewritten
            // by the visitors as the definition is lowered. This means that
            // only one substition happens per lowering, but we need to do
            // N substitutions all at once, where N is the number of lowerings.

            var builder = ArrayBuilder<TypeSymbol>.GetInstance();
            foreach (var typeArg in typeArguments)
            {
                TypeSymbol oldTypeArg;
                TypeSymbol newTypeArg = typeArg;
                do
                {
                    oldTypeArg = newTypeArg;
                    newTypeArg = this.TypeMap.SubstituteType(typeArg).Type;
                }
                while (oldTypeArg != newTypeArg);

                Debug.Assert((object)oldTypeArg == newTypeArg);

                builder.Add(newTypeArg);
            }

            return builder.ToImmutableAndFree();
        }
    }
}

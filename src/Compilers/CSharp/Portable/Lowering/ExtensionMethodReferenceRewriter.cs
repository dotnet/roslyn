// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Replaces references to extension methods with references to their implementation methods
    /// </summary>
    internal sealed class ExtensionMethodReferenceRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private ExtensionMethodReferenceRewriter()
        {
        }

        public static BoundStatement Rewrite(BoundStatement statement)
        {
            var rewriter = new ExtensionMethodReferenceRewriter();
            return (BoundStatement)rewriter.Visit(statement);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            return VisitCall(this, node);
        }

        public static BoundNode VisitCall(BoundTreeRewriter rewriter, BoundCall node)
        {
            Debug.Assert(node != null);

            BoundExpression rewrittenCall;

            if (LocalRewriter.TryGetReceiver(node, out BoundCall? receiver1))
            {
                // Handle long call chain of both instance and extension method invocations.
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);
                node = receiver1;

                while (LocalRewriter.TryGetReceiver(node, out BoundCall? receiver2))
                {
                    calls.Push(node);
                    node = receiver2;
                }

                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = (BoundExpression?)rewriter.Visit(node.ReceiverOpt);

                do
                {
                    rewrittenCall = visitArgumentsAndFinishRewrite(rewriter, node, rewrittenReceiver);
                    rewrittenReceiver = rewrittenCall;
                }
                while (calls.TryPop(out node!));

                calls.Free();
            }
            else
            {
                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = (BoundExpression?)rewriter.Visit(node.ReceiverOpt);
                rewrittenCall = visitArgumentsAndFinishRewrite(rewriter, node, rewrittenReceiver);
            }

            return rewrittenCall;

            static BoundExpression visitArgumentsAndFinishRewrite(BoundTreeRewriter rewriter, BoundCall node, BoundExpression? rewrittenReceiver)
            {
                return updateCall(
                    node,
                    VisitMethodSymbolWithExtensionRewrite(rewriter, node.Method),
                    rewriter.VisitSymbols(node.OriginalMethodsOpt),
                    rewrittenReceiver,
                    rewriter.VisitList(node.Arguments),
                    node.ArgumentRefKindsOpt,
                    node.InvokedAsExtensionMethod,
                    rewriter.VisitType(node.Type));
            }

            static BoundExpression updateCall(
                BoundCall boundCall,
                MethodSymbol method,
                ImmutableArray<MethodSymbol> originalMethodsOpt,
                BoundExpression? receiverOpt,
                ImmutableArray<BoundExpression> arguments,
                ImmutableArray<RefKind> argumentRefKinds,
                bool invokedAsExtensionMethod,
                TypeSymbol type)
            {
                if (receiverOpt is not null && arguments.Length == method.ParameterCount - 1)
                {
                    Debug.Assert(boundCall.Method.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod() == (object)method.OriginalDefinition);
                    Debug.Assert(!boundCall.Method.IsStatic);

                    var receiverRefKind = method.Parameters[0].RefKind;

                    if (argumentRefKinds.IsDefault)
                    {
                        if (receiverRefKind != RefKind.None)
                        {
                            var builder = ArrayBuilder<RefKind>.GetInstance(method.ParameterCount, RefKind.None);
                            builder[0] = argumentRefKindFromReceiverRefKind(receiverRefKind);
                            argumentRefKinds = builder.ToImmutableAndFree();
                        }
                    }
                    else
                    {
                        argumentRefKinds = argumentRefKinds.Insert(0, argumentRefKindFromReceiverRefKind(receiverRefKind)); // PROTOTYPE: Test this code path
                    }

                    invokedAsExtensionMethod = true;

                    // PROTOTYPE: We probably need to convert the receiver to the parameter's type here

                    arguments = arguments.Insert(0, receiverOpt);
                    receiverOpt = null;
                }

                return boundCall.Update(
                    receiverOpt,
                    boundCall.InitialBindingReceiverIsSubjectToCloning,
                    method,
                    arguments,
                    default,
                    argumentRefKinds,
                    boundCall.IsDelegateCall,
                    boundCall.Expanded,
                    invokedAsExtensionMethod,
                    default,
                    default,
                    boundCall.ResultKind,
                    originalMethodsOpt,
                    type);

                static RefKind argumentRefKindFromReceiverRefKind(RefKind receiverRefKind)
                {
                    return SyntheticBoundNodeFactory.ArgumentRefKindFromParameterRefKind(receiverRefKind, useStrictArgumentRefKinds: false);
                }
            }
        }

        [return: NotNullIfNotNull(nameof(method))]
        private static MethodSymbol? VisitMethodSymbolWithExtensionRewrite(BoundTreeRewriter rewriter, MethodSymbol? method)
        {
            if (method is { OriginalDefinition.ContainingSymbol: NamedTypeSymbol { IsExtension: true } declaringTypeDefinition } &&
                method.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod() is MethodSymbol implementationMethod)
            {
                method = implementationMethod.AsMember(method.ContainingSymbol.ContainingType).
                    ConstructIfGeneric(method.ContainingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Concat(method.TypeArgumentsWithAnnotations));
            }

            return rewriter.VisitMethodSymbol(method);
        }

        [return: NotNullIfNotNull(nameof(method))]
        public override MethodSymbol? VisitMethodSymbol(MethodSymbol? method)
        {
            Debug.Assert(method is not { OriginalDefinition.ContainingSymbol: NamedTypeSymbol { IsExtension: true } } ||
                         method.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod() is null);
            return base.VisitMethodSymbol(method);
        }

        public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            return VisitDelegateCreationExpression(this, node);
        }

        public static BoundNode VisitDelegateCreationExpression(BoundTreeRewriter rewriter, BoundDelegateCreationExpression node)
        {
            return updateDelegateCreation(node, VisitMethodSymbolWithExtensionRewrite(rewriter, node.MethodOpt), (BoundExpression)rewriter.Visit(node.Argument), rewriter.VisitType(node.Type));

            static BoundNode updateDelegateCreation(BoundDelegateCreationExpression node, MethodSymbol? methodOpt, BoundExpression argument, TypeSymbol type)
            {
                bool isExtensionMethod = node.IsExtensionMethod;

                if (!isExtensionMethod && argument is not BoundTypeExpression && methodOpt?.IsStatic == true)
                {
                    Debug.Assert(node.MethodOpt!.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod() == (object)methodOpt.OriginalDefinition);
                    isExtensionMethod = true;
                }

                return node.Update(argument, methodOpt, isExtensionMethod, node.WasTargetTyped, type);
            }
        }

        public override BoundNode VisitFunctionPointerLoad(BoundFunctionPointerLoad node)
        {
            return VisitFunctionPointerLoad(this, node);
        }

        public static BoundNode VisitFunctionPointerLoad(BoundTreeRewriter rewriter, BoundFunctionPointerLoad node)
        {
            MethodSymbol targetMethod = VisitMethodSymbolWithExtensionRewrite(rewriter, node.TargetMethod);
            TypeSymbol? constrainedToTypeOpt = rewriter.VisitType(node.ConstrainedToTypeOpt);
            TypeSymbol? type = rewriter.VisitType(node.Type);
            return node.Update(targetMethod, constrainedToTypeOpt, type);
        }
    }
}

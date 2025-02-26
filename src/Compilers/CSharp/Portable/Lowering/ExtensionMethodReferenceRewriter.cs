// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
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
                    rewriter.VisitMethodSymbol(node.Method),
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
                if (method.OriginalDefinition.ContainingSymbol is NamedTypeSymbol { IsExtension: true } declaringTypeDefinition &&
                    method.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod() is MethodSymbol implementationMethod)
                {
                    method = implementationMethod.AsMember(method.ContainingSymbol.ContainingType).
                        ConstructIfGeneric(method.ContainingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Concat(method.TypeArgumentsWithAnnotations));

                    if (receiverOpt is not null && arguments.Length == method.ParameterCount - 1)
                    {
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

                        Debug.Assert(receiverOpt.Type!.Equals(method.Parameters[0].Type, TypeCompareKind.ConsiderEverything));

                        arguments = arguments.Insert(0, receiverOpt);
                        receiverOpt = null;
                    }
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

        // PROTOTYPE: public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
    }
}

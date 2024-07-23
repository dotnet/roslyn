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
    /// Replaces references to instance extension methods with references to their static/metadata form, passing what used to be
    /// a receiver as the first argument.
    /// </summary>
    internal class InstanceExtensionMethodReferenceRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private MethodSymbol _containingMethod;

        private InstanceExtensionMethodReferenceRewriter(MethodSymbol containingMethod)
        {
            _containingMethod = containingMethod;
        }

        public static BoundStatement Rewrite(MethodSymbol method, BoundStatement statement)
        {
            var rewriter = new InstanceExtensionMethodReferenceRewriter(method);
            return (BoundStatement)rewriter.Visit(statement);
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var oldContainingMethod = _containingMethod;
            _containingMethod = node.Symbol;

            var result = base.VisitLambda(node);

            _containingMethod = oldContainingMethod;
            return result;
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var oldContainingMethod = _containingMethod;
            _containingMethod = node.Symbol;

            var result = base.VisitLocalFunctionStatement(node);

            _containingMethod = oldContainingMethod;
            return result;
        }

        public override BoundNode VisitCall(BoundCall node)
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
                BoundExpression? rewrittenReceiver = (BoundExpression?)this.Visit(node.ReceiverOpt);

                do
                {
                    rewrittenCall = visitArgumentsAndFinishRewrite(node, rewrittenReceiver);
                    rewrittenReceiver = rewrittenCall;
                }
                while (calls.TryPop(out node!));

                calls.Free();
            }
            else
            {
                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = (BoundExpression?)this.Visit(node.ReceiverOpt);
                rewrittenCall = visitArgumentsAndFinishRewrite(node, rewrittenReceiver);
            }

            return rewrittenCall;

            BoundExpression visitArgumentsAndFinishRewrite(BoundCall node, BoundExpression? rewrittenReceiver)
            {
                return UpdateCall(
                    _containingMethod,
                    node,
                    this.VisitMethodSymbol(node.Method),
                    this.VisitSymbols<MethodSymbol>(node.OriginalMethodsOpt),
                    rewrittenReceiver,
                    this.VisitList(node.Arguments),
                    node.ArgumentRefKindsOpt,
                    node.InvokedAsExtensionMethod,
                    this.VisitType(node.Type));
            }
        }

        internal static BoundExpression UpdateCall(
            MethodSymbol containingMethod,
            BoundCall boundCall,
            MethodSymbol method,
            ImmutableArray<MethodSymbol> originalMethodsOpt,
            BoundExpression? receiverOpt,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> argumentRefKinds,
            bool invokedAsExtensionMethod,
            TypeSymbol type)
        {
            var temps = ImmutableArray<LocalSymbol>.Empty;

            if (receiverOpt is not null && method.OriginalDefinition.ContainingSymbol is NamedTypeSymbol declaringTypeDefinition &&
                declaringTypeDefinition.TryGetCorrespondingStaticMetadataExtensionMember(method.OriginalDefinition) is MethodSymbol metadataMethod)
            {
                Debug.Assert(receiverOpt.Type is not null);

                method = metadataMethod.AsMember(method.ContainingType).ConstructIfGeneric(method.TypeArgumentsWithAnnotations);

                var thisRefKind = method.Parameters[0].RefKind;

                if (argumentRefKinds.IsDefault)
                {
                    if (thisRefKind != RefKind.None)
                    {
                        argumentRefKinds = SyntheticBoundNodeFactory.ArgumentRefKindsFromParameterRefKinds(method, useStrictArgumentRefKinds: true);
                    }
                }
                else
                {
                    argumentRefKinds = argumentRefKinds.Insert(0, SyntheticBoundNodeFactory.ArgumentRefKindFromParameterRefKind(thisRefKind, useStrictArgumentRefKinds: true));
                }

                invokedAsExtensionMethod = true;

                // PROTOTYPE(roles): We probably need to convert the receiver to the parameter's type here

                if (thisRefKind != RefKind.None)
                {
                    Debug.Assert(thisRefKind == RefKind.Ref);

                    if ((receiverOpt.Type.IsReferenceType &&
                         receiverOpt is not BoundLocal { LocalSymbol.SynthesizedKind: SynthesizedLocalKind.LoweringTemp, LocalSymbol.RefKind: RefKind.None }) || // If receiver is already captured by value, user's code shouldn't be able to change its value
                        !Binder.HasHome(receiverOpt,
                                        Binder.AddressKind.Writeable,
                                        containingMethod,
                                        peVerifyCompatEnabled: true,
                                        stackLocalsOpt: null))
                    {
                        // PROTOTYPE(roles): If the following assert fails (for example this could happen if we start supporting 'readonly' extension members),
                        //                   we will create a local of a wrong type below. 
                        Debug.Assert(receiverOpt is not BoundThisReference || containingMethod.ContainingType.GetExtendedTypeNoUseSiteDiagnostics(null) is null);

                        // We have an rValue, but the parameter is a 'ref'. Capture it in a local to keep CodeGenerator happy.
                        receiverOpt = SyntheticBoundNodeFactory.StoreToTemp(containingMethod, receiverOpt, containingMethod.DeclaringCompilation.IsPeVerifyCompatEnabled, out BoundAssignmentOperator assignmentToTemp);
                        temps = temps.Add(((BoundLocal)receiverOpt).LocalSymbol);
                        receiverOpt = new BoundSequence(receiverOpt.Syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create<BoundExpression>(assignmentToTemp), receiverOpt, receiverOpt.Type!);
                    }
                }

                arguments = arguments.Insert(0, receiverOpt);
                receiverOpt = null;
            }

            var result = boundCall.Update(
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

            if (!temps.IsDefaultOrEmpty)
            {
                return new BoundSequence(
                    boundCall.Syntax,
                    locals: temps,
                    sideEffects: ImmutableArray<BoundExpression>.Empty,
                    value: result,
                    type: result.Type);
            }

            return result;
        }

        public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            return UpdateDelegateCreation(node, this.VisitMethodSymbol(node.MethodOpt), (BoundExpression)this.Visit(node.Argument), node.IsExtensionMethod, this.VisitType(node.Type));
        }

        internal static BoundNode UpdateDelegateCreation(BoundDelegateCreationExpression node, MethodSymbol? methodOpt, BoundExpression argument, bool isExtensionMethod, TypeSymbol type)
        {
            if (methodOpt?.OriginalDefinition.ContainingSymbol is NamedTypeSymbol declaringTypeDefinition &&
                declaringTypeDefinition.TryGetCorrespondingStaticMetadataExtensionMember(methodOpt.OriginalDefinition) is MethodSymbol metadataMethod)
            {
                methodOpt = metadataMethod.AsMember(methodOpt.ContainingType).ConstructIfGeneric(methodOpt.TypeArgumentsWithAnnotations);

                // PROTOTYPE(roles): We probably also need to convert the receiver to the parameter's type here

                isExtensionMethod = true;
            }

            return node.Update(argument, methodOpt, isExtensionMethod, node.WasTargetTyped, type);
        }
    }
}

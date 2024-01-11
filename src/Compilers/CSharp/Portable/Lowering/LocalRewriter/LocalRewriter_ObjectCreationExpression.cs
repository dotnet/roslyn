// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            // There are no target types for dynamic object creation scenarios, so there should be no implicit handler conversions
            AssertNoImplicitInterpolatedStringHandlerConversions(node.Arguments);
            var loweredArguments = VisitList(node.Arguments);
            var constructorInvocation = _dynamicFactory.MakeDynamicConstructorInvocation(node.Syntax, node.Type, loweredArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt).ToExpression();

            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return constructorInvocation;
            }

            return MakeExpressionWithInitializer(node.Syntax, constructorInvocation, node.InitializerExpressionOpt, node.Type);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(node != null);

            // Rewrite the arguments.
            // NOTE: We may need additional argument rewriting such as
            //       re-ordering arguments based on argsToParamsOpt map, etc.
            // NOTE: This is done later by MakeArguments, for now we just lower each argument.
            BoundExpression? receiverDiscard = null;

            ImmutableArray<RefKind> argumentRefKindsOpt = node.ArgumentRefKindsOpt;
            ArrayBuilder<LocalSymbol>? tempsBuilder = null;
            ImmutableArray<BoundExpression> rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(
                ref receiverDiscard,
                captureReceiverMode: ReceiverCaptureMode.Default,
                node.Arguments,
                node.Constructor,
                node.ArgsToParamsOpt,
                argumentRefKindsOpt,
                storesOpt: null,
                ref tempsBuilder);

            Debug.Assert(receiverDiscard is null);

            // We have already lowered each argument, but we may need some additional rewriting for the arguments,
            // such as re-ordering arguments based on argsToParamsOpt map, etc.
            rewrittenArguments = MakeArguments(
                rewrittenArguments,
                node.Constructor,
                node.Expanded,
                node.ArgsToParamsOpt,
                ref argumentRefKindsOpt,
                ref tempsBuilder);

            BoundExpression rewrittenObjectCreation;
            var temps = tempsBuilder.ToImmutableAndFree();

            if (_inExpressionLambda)
            {
                if (!temps.IsDefaultOrEmpty)
                {
                    throw ExceptionUtilities.UnexpectedValue(temps.Length);
                }

                rewrittenObjectCreation = node.UpdateArgumentsAndInitializer(rewrittenArguments, argumentRefKindsOpt, MakeObjectCreationInitializerForExpressionTree(node.InitializerExpressionOpt), changeTypeOpt: node.Constructor.ContainingType);

                if (node.Type.IsInterfaceType())
                {
                    Debug.Assert(TypeSymbol.Equals(rewrittenObjectCreation.Type, ((NamedTypeSymbol)node.Type).ComImportCoClass, TypeCompareKind.ConsiderEverything2));
                    rewrittenObjectCreation = MakeConversionNode(rewrittenObjectCreation, node.Type, false, false);
                }

                return rewrittenObjectCreation;
            }

            rewrittenObjectCreation = node.UpdateArgumentsAndInitializer(rewrittenArguments, argumentRefKindsOpt, newInitializerExpression: null, changeTypeOpt: node.Constructor.ContainingType);

            // replace "new S()" with a default struct ctor with "default(S)"
            if (node.Constructor.IsDefaultValueTypeConstructor())
            {
                rewrittenObjectCreation = new BoundDefaultExpression(rewrittenObjectCreation.Syntax, rewrittenObjectCreation.Type!);
            }

            if (!temps.IsDefaultOrEmpty)
            {
                rewrittenObjectCreation = new BoundSequence(
                    node.Syntax,
                    temps,
                    ImmutableArray<BoundExpression>.Empty,
                    rewrittenObjectCreation,
                    node.Type);
            }

            if (node.Type.IsInterfaceType())
            {
                Debug.Assert(TypeSymbol.Equals(rewrittenObjectCreation.Type, ((NamedTypeSymbol)node.Type).ComImportCoClass, TypeCompareKind.ConsiderEverything2));
                rewrittenObjectCreation = MakeConversionNode(rewrittenObjectCreation, node.Type, false, false);
            }

            if (Instrument)
            {
                rewrittenObjectCreation = Instrumenter.InstrumentObjectCreationExpression(node, rewrittenObjectCreation);
            }

            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return rewrittenObjectCreation;
            }

            return MakeExpressionWithInitializer(node.Syntax, rewrittenObjectCreation, node.InitializerExpressionOpt, node.Type);
        }

        public override BoundNode VisitWithExpression(BoundWithExpression withExpr)
        {
            TypeSymbol type = withExpr.Type;
            BoundExpression receiver = withExpr.Receiver;
            Debug.Assert(receiver.Type!.Equals(type, TypeCompareKind.ConsiderEverything));

            // for a with expression of the form
            //
            //      receiver with { P1 = e1, P2 = e2 } // P3 is copied implicitly
            //
            // if the receiver is a struct, duplicate the value, then set the given struct properties:
            //
            //     var tmp = receiver;
            //     tmp.P1 = e1;
            //     tmp.P2 = e2;
            //     tmp
            //
            // if the receiver is an anonymous type, then invoke its constructor:
            //
            //     new Type(e1, e2, receiver.P3);
            //
            // otherwise the receiver is a record class and we want to lower it to a call to its `Clone` method, then
            // set the given record properties. i.e.
            //
            //      var tmp = (ReceiverType)receiver.Clone();
            //      tmp.P1 = e1;
            //      tmp.P2 = e2;
            //      tmp

            BoundExpression rewrittenReceiver = VisitExpression(receiver);

            if (type.IsAnonymousType)
            {
                var anonymousType = (AnonymousTypeManager.AnonymousTypePublicSymbol)type;
                var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();
                var temps = ArrayBuilder<LocalSymbol>.GetInstance();
                BoundLocal oldValue = _factory.StoreToTemp(rewrittenReceiver, out BoundAssignmentOperator boundAssignmentToTemp);
                temps.Add(oldValue.LocalSymbol);
                sideEffects.Add(boundAssignmentToTemp);

                BoundExpression value = _factory.New(anonymousType, getAnonymousTypeValues(withExpr, oldValue, anonymousType, sideEffects, temps));

                return new BoundSequence(withExpr.Syntax, temps.ToImmutableAndFree(), sideEffects.ToImmutableAndFree(), value, type);
            }

            BoundExpression expression;
            if (type.IsValueType)
            {
                expression = rewrittenReceiver;
            }
            else
            {
                Debug.Assert(withExpr.CloneMethod is not null);
                Debug.Assert(withExpr.CloneMethod.ParameterCount == 0);

                expression = _factory.Convert(
                    type,
                    _factory.Call(
                        rewrittenReceiver,
                        withExpr.CloneMethod));
            }

            return MakeExpressionWithInitializer(
                withExpr.Syntax,
                expression,
                withExpr.InitializerExpression,
                type);

            ImmutableArray<BoundExpression> getAnonymousTypeValues(BoundWithExpression withExpr, BoundExpression oldValue, AnonymousTypeManager.AnonymousTypePublicSymbol anonymousType,
                ArrayBuilder<BoundExpression> sideEffects, ArrayBuilder<LocalSymbol> temps)
            {
                // map: [propertyIndex] -> valueTemp
                var valueTemps = ArrayBuilder<BoundExpression?>.GetInstance(anonymousType.Properties.Length, fillWithValue: null);

                foreach (BoundExpression initializer in withExpr.InitializerExpression.Initializers)
                {
                    var assignment = (BoundAssignmentOperator)initializer;
                    var left = (BoundObjectInitializerMember)assignment.Left;
                    Debug.Assert(left.MemberSymbol is not null);

                    // We evaluate the values provided in source first
                    var rewrittenRight = VisitExpression(assignment.Right);
                    BoundLocal valueTemp = _factory.StoreToTemp(rewrittenRight, out BoundAssignmentOperator boundAssignmentToTemp);
                    temps.Add(valueTemp.LocalSymbol);
                    sideEffects.Add(boundAssignmentToTemp);

                    var property = left.MemberSymbol;
                    Debug.Assert(property.MemberIndexOpt!.Value >= 0 && property.MemberIndexOpt.Value < anonymousType.Properties.Length);
                    valueTemps[property.MemberIndexOpt.Value] = valueTemp;
                }

                var builder = ArrayBuilder<BoundExpression>.GetInstance(anonymousType.Properties.Length);
                foreach (var property in anonymousType.Properties)
                {
                    if (valueTemps[property.MemberIndexOpt!.Value] is BoundExpression initializerValue)
                    {
                        builder.Add(initializerValue);
                    }
                    else
                    {
                        // The values that are implicitly copied over will get evaluated afterwards, in the order they are needed
                        builder.Add(_factory.Property(oldValue, property));
                    }
                }

                valueTemps.Free();
                return builder.ToImmutableAndFree();
            }
        }

        [return: NotNullIfNotNull(nameof(initializerExpressionOpt))]
        private BoundObjectInitializerExpressionBase? MakeObjectCreationInitializerForExpressionTree(BoundObjectInitializerExpressionBase? initializerExpressionOpt)
        {
            if (initializerExpressionOpt != null && !initializerExpressionOpt.HasErrors)
            {
                // We may need to MakeArguments for collection initializer add method call if the method has a param array parameter.
                var rewrittenInitializers = MakeObjectOrCollectionInitializersForExpressionTree(initializerExpressionOpt);
                return UpdateInitializers(initializerExpressionOpt, rewrittenInitializers);
            }

            return null;
        }

        // Shared helper for VisitWithExpression, MakeObjectCreationWithInitializer and MakeNewT
        private BoundExpression MakeExpressionWithInitializer(
            SyntaxNode syntax,
            BoundExpression rewrittenExpression,
            BoundExpression initializerExpression,
            TypeSymbol type)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(initializerExpression != null && !initializerExpression.HasErrors);

            // Create a temp and assign it with the object creation expression.
            BoundAssignmentOperator boundAssignmentToTemp;
            BoundLocal value = _factory.StoreToTemp(rewrittenExpression, out boundAssignmentToTemp, isKnownToReferToTempIfReferenceType: true);

            // Rewrite object/collection initializer expressions
            ArrayBuilder<BoundExpression>? dynamicSiteInitializers = null;
            ArrayBuilder<LocalSymbol>? temps = null;
            ArrayBuilder<BoundExpression>? loweredInitializers = ArrayBuilder<BoundExpression>.GetInstance();

            AddObjectOrCollectionInitializers(ref dynamicSiteInitializers, ref temps, loweredInitializers, value, initializerExpression);

            int dynamicSiteCount = dynamicSiteInitializers?.Count ?? 0;
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(1 + dynamicSiteCount + loweredInitializers.Count);
            sideEffects.Add(boundAssignmentToTemp);

            if (dynamicSiteCount > 0)
            {
                sideEffects.AddRange(dynamicSiteInitializers!);
                dynamicSiteInitializers!.Free();
            }

            sideEffects.AddRange(loweredInitializers);
            loweredInitializers.Free();

            ImmutableArray<LocalSymbol> locals;
            if (temps == null)
            {
                locals = ImmutableArray.Create(value.LocalSymbol);
            }
            else
            {
                temps.Insert(0, value.LocalSymbol);
                locals = temps.ToImmutableAndFree();
            }

            return new BoundSequence(
                syntax,
                locals,
                sideEffects.ToImmutableAndFree(),
                value,
                type);
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            if (_inExpressionLambda)
            {
                return node.Update(MakeObjectCreationInitializerForExpressionTree(node.InitializerExpressionOpt), node.WasTargetTyped, node.Type);
            }

            var rewrittenNewT = MakeNewT(node.Syntax, (TypeParameterSymbol)node.Type);
            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return rewrittenNewT;
            }

            return MakeExpressionWithInitializer(node.Syntax, rewrittenNewT, node.InitializerExpressionOpt, rewrittenNewT.Type!);
        }

        private BoundExpression MakeNewT(SyntaxNode syntax, TypeParameterSymbol typeParameter)
        {
            // "new T()" is rewritten as: "Activator.CreateInstance<T>()".

            // NOTE: DIFFERENCE FROM DEV12
            // Dev12 tried to statically optimize this and would emit default(T) if T happens to be struct
            // However semantics of "new" in C# requires that parameterless constructor be called
            // if struct defines one.
            // Since we cannot know if T has a parameterless constructor statically, 
            // we must call Activator.CreateInstance unconditionally.
            MethodSymbol method;

            if (!this.TryGetWellKnownTypeMember(syntax, WellKnownMember.System_Activator__CreateInstance_T, out method))
            {
                return new BoundDefaultExpression(syntax, type: typeParameter, hasErrors: true);
            }

            Debug.Assert((object)method != null);
            method = method.Construct(ImmutableArray.Create<TypeSymbol>(typeParameter));

            var createInstanceCall = new BoundCall(
                syntax,
                receiverOpt: null,
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                method,
                ImmutableArray<BoundExpression>.Empty,
                default(ImmutableArray<string?>),
                default(ImmutableArray<RefKind>),
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                defaultArguments: default(BitVector),
                resultKind: LookupResultKind.Viable,
                type: typeParameter);

            return createInstanceCall;
        }

        public override BoundNode VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            // For the NoPIA feature, we need to gather the GUID from the coclass, and 
            // generate the following:
            //
            // (IPiaType)System.Activator.CreateInstance(System.Runtime.InteropServices.Marshal.GetTypeFromCLSID(new Guid(GUID)))
            //
            // If System.Runtime.InteropServices.Marshal.GetTypeFromCLSID is not available (older framework),
            // System.Type.GetTypeFromCLSID() is used to get the type for the CLSID:
            //
            // (IPiaType)System.Activator.CreateInstance(System.Type.GetTypeFromCLSID(new Guid(GUID)))

            SyntaxNode oldSyntax = _factory.Syntax;
            _factory.Syntax = node.Syntax;

            var ctor = _factory.WellKnownMethod(WellKnownMember.System_Guid__ctor);
            BoundExpression newGuid;

            if (ctor is { })
            {
                Debug.Assert(node.GuidString is { });
                newGuid = _factory.New(ctor, _factory.Literal(node.GuidString));
            }
            else
            {
                newGuid = new BoundBadExpression(node.Syntax, LookupResultKind.NotCreatable, ImmutableArray<Symbol?>.Empty, ImmutableArray<BoundExpression>.Empty, ErrorTypeSymbol.UnknownResultType);
            }

            var getTypeFromCLSID = _factory.WellKnownMethod(WellKnownMember.System_Runtime_InteropServices_Marshal__GetTypeFromCLSID, isOptional: true);

            if (getTypeFromCLSID is null)
            {
                getTypeFromCLSID = _factory.WellKnownMethod(WellKnownMember.System_Type__GetTypeFromCLSID);
            }

            BoundExpression callGetTypeFromCLSID;

            if (getTypeFromCLSID is { })
            {
                callGetTypeFromCLSID = _factory.Call(null, getTypeFromCLSID, newGuid);
            }
            else
            {
                callGetTypeFromCLSID = new BoundBadExpression(node.Syntax, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol?>.Empty, ImmutableArray<BoundExpression>.Empty, ErrorTypeSymbol.UnknownResultType);
            }

            var createInstance = _factory.WellKnownMethod(WellKnownMember.System_Activator__CreateInstance);
            BoundExpression rewrittenObjectCreation;

            if ((object)createInstance != null)
            {
                rewrittenObjectCreation = _factory.Convert(node.Type, _factory.Call(null, createInstance, callGetTypeFromCLSID));
            }
            else
            {
                rewrittenObjectCreation = new BoundBadExpression(node.Syntax, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol?>.Empty, ImmutableArray<BoundExpression>.Empty, node.Type);
            }

            _factory.Syntax = oldSyntax;

            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return rewrittenObjectCreation;
            }

            return MakeExpressionWithInitializer(node.Syntax, rewrittenObjectCreation, node.InitializerExpressionOpt, node.Type);
        }
    }
}

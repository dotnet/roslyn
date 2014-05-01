// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            var loweredArguments = VisitList(node.Arguments);
            return dynamicFactory.MakeDynamicConstructorInvocation(node.Syntax, node.Type, loweredArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt).ToExpression();
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(node != null);

            // Rewrite the arguments.
            // NOTE: We may need additional argument rewriting such as generating a params array,
            //       re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            // NOTE: This is done later by MakeArguments, for now we just lower each argument.
            var rewrittenArguments = VisitList(node.Arguments);

            // We have already lowered each argument, but we may need some additional rewriting for the arguments,
            // such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            ImmutableArray<LocalSymbol> temps;
            ImmutableArray<RefKind> argumentRefKindsOpt = node.ArgumentRefKindsOpt;
            rewrittenArguments = MakeArguments(node.Syntax, rewrittenArguments, node.Constructor, node.Constructor, node.Expanded, node.ArgsToParamsOpt, ref argumentRefKindsOpt, out temps);

            BoundExpression rewrittenObjectCreation;

            if (inExpressionLambda)
            {
                if (!temps.IsDefaultOrEmpty) throw ExceptionUtilities.UnexpectedValue(temps.Length);

                // Rewrite the optional initializer expression
                BoundExpression rewrittenInitializerExpressionOpt = null;

                if (node.InitializerExpressionOpt != null && !node.InitializerExpressionOpt.HasErrors)
                {
                    // We may need to MakeArguments for collection initializer add method call if the method has a param array parameter.
                    var rewrittenInitializers = MakeObjectOrCollectionInitializersForExpressionTree(node.InitializerExpressionOpt);
                    rewrittenInitializerExpressionOpt = UpdateInitializers(node.InitializerExpressionOpt, rewrittenInitializers);
                }

                rewrittenObjectCreation = node.UpdateArgumentsAndInitializer(rewrittenArguments, rewrittenInitializerExpressionOpt, changeTypeOpt: node.Constructor.ContainingType);

                if (node.Type.IsInterfaceType())
                {
                    Debug.Assert(rewrittenObjectCreation.Type == ((NamedTypeSymbol)node.Type).ComImportCoClass);
                    rewrittenObjectCreation = MakeConversion(rewrittenObjectCreation, node.Type, false, false);
                }

                return rewrittenObjectCreation;
            }

            rewrittenObjectCreation = node.UpdateArgumentsAndInitializer(rewrittenArguments, newInitializerExpression: null, changeTypeOpt: node.Constructor.ContainingType);

            // replace "new S()" with a default struct ctor with "default(S)"
            if (node.Constructor.IsParameterlessValueTypeConstructor(requireSynthesized: true))
            {
                rewrittenObjectCreation = new BoundDefaultOperator(rewrittenObjectCreation.Syntax, rewrittenObjectCreation.Type);
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
                Debug.Assert(rewrittenObjectCreation.Type == ((NamedTypeSymbol)node.Type).ComImportCoClass);
                rewrittenObjectCreation = MakeConversion(rewrittenObjectCreation, node.Type, false, false);
            }

            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return rewrittenObjectCreation;
            }

            return MakeObjectCreationWithInitializer(node.Syntax, rewrittenObjectCreation, node.InitializerExpressionOpt, node.Type);
        }

        // Shared helper for MakeObjectCreationWithInitializer and MakeNewT
        private BoundExpression MakeObjectCreationWithInitializer(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenObjectCreation,
            BoundExpression initializerExpression,
            TypeSymbol type)
        {
            Debug.Assert(!inExpressionLambda);
            Debug.Assert(initializerExpression != null && !initializerExpression.HasErrors);

            // Create a temp and assign it with the object creation expression.
            BoundAssignmentOperator boundAssignmentToTemp;
            BoundLocal boundTemp = this.factory.StoreToTemp(rewrittenObjectCreation, out boundAssignmentToTemp);

            // Rewrite object/collection initializer expressions
            ArrayBuilder<BoundExpression> dynamicSiteInitializers = null;
            ArrayBuilder<BoundExpression> loweredInitializers = ArrayBuilder<BoundExpression>.GetInstance();

            AddObjectOrCollectionInitializers(ref dynamicSiteInitializers, loweredInitializers, boundTemp, initializerExpression);

            int dynamicSiteCount = (dynamicSiteInitializers != null) ? dynamicSiteInitializers.Count : 0;

            var sideEffects = new BoundExpression[1 + dynamicSiteCount + loweredInitializers.Count];
            sideEffects[0] = boundAssignmentToTemp;

            if (dynamicSiteCount > 0)
            {
                dynamicSiteInitializers.CopyTo(sideEffects, 1);
                dynamicSiteInitializers.Free();
            }

            loweredInitializers.CopyTo(sideEffects, 1 + dynamicSiteCount);
            loweredInitializers.Free();

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: sideEffects.AsImmutableOrNull(),
                value: boundTemp,
                type: type);
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            if (inExpressionLambda)
            {
                return node;
            }

            var rewrittenNewT = MakeNewT(node.Syntax, (TypeParameterSymbol)node.Type);
            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return rewrittenNewT;
            }

            return MakeObjectCreationWithInitializer(node.Syntax, rewrittenNewT, node.InitializerExpressionOpt, rewrittenNewT.Type);
        }

        private BoundExpression MakeNewT(CSharpSyntaxNode syntax, TypeParameterSymbol typeParameter)
        {
            // How "new T()" is rewritten depends on whether T is known to be a value
            // type, a reference type, or neither (see OperatorRewriter::VisitNEWTYVAR).

            if (typeParameter.IsValueType)
            {
                // "new T()" rewritten as: "default(T)".
                return new BoundDefaultOperator(syntax, type: typeParameter);
            }

            // For types not known to be value types, "new T()" requires
            // Activator.CreateInstance<T>().

            MethodSymbol method;

            if (!this.TryGetWellKnownTypeMember(syntax, WellKnownMember.System_Activator__CreateInstance_T, out method))
            {
                return new BoundDefaultOperator(syntax, null, type: typeParameter, hasErrors: true);
            }

            Debug.Assert((object)method != null);
            method = method.Construct(ImmutableArray.Create<TypeSymbol>(typeParameter));

            var createInstanceCall = new BoundCall(
                syntax,
                null,
                method,
                ImmutableArray<BoundExpression>.Empty,
                default(ImmutableArray<string>),
                default(ImmutableArray<RefKind>),
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: default(ImmutableArray<int>),
                resultKind: LookupResultKind.Viable,
                type: typeParameter);

            if (typeParameter.IsReferenceType)
            {
                // "new T()" is rewritten as: "Activator.CreateInstance<T>()".
                return createInstanceCall;
            }
            else
            {
                // "new T()" is rewritten as: "(null == (object)default(T)) ? Activator.CreateInstance<T>() : default(T)".
                var defaultT = new BoundDefaultOperator(syntax, type: typeParameter);
                return new BoundConditionalOperator(
                    syntax,
                    MakeNullCheck(
                        syntax: syntax,
                        rewrittenExpr: MakeConversion(
                            syntax: syntax,
                            rewrittenOperand: defaultT,
                            conversionKind: ConversionKind.Boxing,
                            rewrittenType: this.compilation.GetSpecialType(SpecialType.System_Object),
                            @checked: false),
                        operatorKind: BinaryOperatorKind.Equal),
                    createInstanceCall,
                    defaultT,
                    constantValueOpt: null,
                    type: typeParameter);
            }
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

            CSharpSyntaxNode oldSyntax = factory.Syntax;
            factory.Syntax = node.Syntax;


            var ctor = factory.WellKnownMethod(WellKnownMember.System_Guid__ctor);
            BoundExpression newGuid;

            if ((object)ctor != null)
            {
                newGuid = factory.New(ctor, factory.Literal(node.GuidString));
            }
            else
            {
                newGuid = new BoundBadExpression(node.Syntax, LookupResultKind.NotCreatable, ImmutableArray<Symbol>.Empty, ImmutableArray<BoundNode>.Empty, ErrorTypeSymbol.UnknownResultType);
            }

            var getTypeFromCLSID = factory.WellKnownMethod(WellKnownMember.System_Runtime_InteropServices_Marshal__GetTypeFromCLSID, isOptional: true);

            if ((object)getTypeFromCLSID == null)
            {
                getTypeFromCLSID = factory.WellKnownMethod(WellKnownMember.System_Type__GetTypeFromCLSID);
            }

            BoundExpression callGetTypeFromCLSID;

            if ((object)getTypeFromCLSID != null)
            {
                callGetTypeFromCLSID = factory.Call(null, getTypeFromCLSID, newGuid);
            }
            else
            {
                callGetTypeFromCLSID = new BoundBadExpression(node.Syntax, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol>.Empty, ImmutableArray<BoundNode>.Empty, ErrorTypeSymbol.UnknownResultType);
            }

            var createInstance = factory.WellKnownMethod(WellKnownMember.System_Activator__CreateInstance);
            BoundExpression rewrittenObjectCreation;

            if ((object)createInstance != null)
            {
                rewrittenObjectCreation = factory.Convert(node.Type, factory.Call(null, createInstance, callGetTypeFromCLSID));
            }
            else
            {
                rewrittenObjectCreation = new BoundBadExpression(node.Syntax, LookupResultKind.OverloadResolutionFailure, ImmutableArray<Symbol>.Empty, ImmutableArray<BoundNode>.Empty, node.Type);
            }

            factory.Syntax = oldSyntax;

            if (node.InitializerExpressionOpt == null || node.InitializerExpressionOpt.HasErrors)
            {
                return rewrittenObjectCreation;
            }

            return MakeObjectCreationWithInitializer(node.Syntax, rewrittenObjectCreation, node.InitializerExpressionOpt, node.Type);
        }
    }
}

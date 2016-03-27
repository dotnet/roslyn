// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// a bound node rewriter that rewrites types properly (which in some cases the automatically-generated
    /// base class does not).  This is used in the lambda rewriter, the iterator rewriter, and the async rewriter.
    /// </summary>
    internal abstract partial class MethodToClassRewriter : BoundTreeRewriterWithStackGuard
    {
        // For each captured variable, information about its replacement.  May be populated lazily (that is, not all
        // upfront) by subclasses.  Specifically, the async rewriter produces captured symbols for temps, including
        // ref locals, lazily.
        protected readonly Dictionary<Symbol, CapturedSymbolReplacement> proxies = new Dictionary<Symbol, CapturedSymbolReplacement>();

        // A mapping from every local variable to its replacement local variable.  Local variables are replaced when
        // their types change due to being inside of a generic method.  Otherwise we reuse the original local (even
        // though its containing method is not correct because the code is moved into another method)
        protected readonly Dictionary<LocalSymbol, LocalSymbol> localMap = new Dictionary<LocalSymbol, LocalSymbol>();

        // A mapping for types in the original method to types in its replacement.  This is mainly necessary
        // when the original method was generic, as type parameters in the original method are mapping into
        // type parameters of the resulting class.
        protected abstract TypeMap TypeMap { get; }

        // Subclasses override this method to fetch a frame pointer.
        protected abstract BoundExpression FramePointer(CSharpSyntaxNode syntax, NamedTypeSymbol frameClass);

        protected abstract MethodSymbol CurrentMethod { get; }

        // Containing type for any synthesized members.
        protected abstract NamedTypeSymbol ContainingType { get; }

        /// <summary> A not-null collection of synthesized methods generated for the current source type. </summary>
        protected readonly TypeCompilationState CompilationState;

        protected readonly DiagnosticBag Diagnostics;
        protected readonly VariableSlotAllocator slotAllocatorOpt;

        protected MethodToClassRewriter(VariableSlotAllocator slotAllocatorOpt, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.CompilationState = compilationState;
            this.Diagnostics = diagnostics;
            this.slotAllocatorOpt = slotAllocatorOpt;
        }

        /// <summary>
        /// Returns true if the specified local/parameter needs to be hoisted to a field.
        /// Variable may be hoisted even if it is not captured, to improve debugging experience.
        /// </summary>
        protected abstract bool NeedsProxy(Symbol localOrParameter);

        protected void RewriteLocals(ImmutableArray<LocalSymbol> locals, ArrayBuilder<LocalSymbol> newLocals)
        {
            foreach (var local in locals)
            {
                LocalSymbol newLocal;
                if (TryRewriteLocal(local, out newLocal))
                {
                    newLocals.Add(newLocal);
                }
            }
        }

        protected bool TryRewriteLocal(LocalSymbol local, out LocalSymbol newLocal)
        {
            if (NeedsProxy(local))
            {
                // no longer a local symbol
                newLocal = null;
                return false;
            }

            if (localMap.TryGetValue(local, out newLocal))
            {
                return true;
            }

            var newType = VisitType(local.Type.TypeSymbol);
            if (newType == local.Type.TypeSymbol)
            {
                newLocal = local;
            }
            else
            {
                newLocal = new TypeSubstitutedLocalSymbol(local, TypeSymbolWithAnnotations.Create(newType), CurrentMethod);
                localMap.Add(local, newLocal);
            }

            return true;
        }

        private ImmutableArray<LocalSymbol> RewriteLocals(ImmutableArray<LocalSymbol> locals)
        {
            if (locals.IsEmpty) return locals;
            var newLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            RewriteLocals(locals, newLocals);
            return newLocals.ToImmutableAndFree();
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            if ((object)node.LocalOpt != null)
            {
                // Yield/await aren't supported in catch block atm, but we need to rewrite the type 
                // of the variable owned by the catch block. Note that this variable might be a closure frame reference.
                LocalSymbol newLocal;
                TryRewriteLocal(node.LocalOpt, out newLocal);

                return node.Update(
                    newLocal,
                    (BoundExpression)this.Visit(node.ExceptionSourceOpt),
                    this.VisitType(node.ExceptionTypeOpt),
                    (BoundExpression)this.Visit(node.ExceptionFilterOpt),
                    (BoundBlock)this.Visit(node.Body),
                    node.IsSynthesizedAsyncCatchAll);
            }

            return base.VisitCatchBlock(node);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            var newLocals = RewriteLocals(node.Locals);
            var newLocalFunctions = node.LocalFunctions;
            var newStatements = VisitList(node.Statements);
            return node.Update(newLocals, newLocalFunctions, newStatements);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            var newLocals = RewriteLocals(node.Locals);
            var newSideEffects = VisitList<BoundExpression>(node.SideEffects);
            var newValue = (BoundExpression)this.Visit(node.Value);
            var newType = this.VisitType(node.Type);
            return node.Update(newLocals, newSideEffects, newValue, newType);
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            var preambleOpt = (BoundStatement)this.Visit(node.LoweredPreambleOpt);
            var newInnerLocals = RewriteLocals(node.InnerLocals);
            BoundExpression boundExpression = (BoundExpression)this.Visit(node.Expression);
            ImmutableArray<BoundSwitchSection> switchSections = (ImmutableArray<BoundSwitchSection>)this.VisitList(node.SwitchSections);
            return node.Update(preambleOpt, boundExpression, node.ConstantTargetOpt, newInnerLocals, node.InnerLocalFunctions, switchSections, node.BreakLabel, node.StringEquality);
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            var newOuterLocals = RewriteLocals(node.OuterLocals);
            BoundStatement initializer = (BoundStatement)this.Visit(node.Initializer);
            BoundExpression condition = (BoundExpression)this.Visit(node.Condition);
            BoundStatement increment = (BoundStatement)this.Visit(node.Increment);
            BoundStatement body = (BoundStatement)this.Visit(node.Body);
            return node.Update(newOuterLocals, initializer, condition, increment, body, node.BreakLabel, node.ContinueLabel);
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            var newLocals = RewriteLocals(node.Locals);
            BoundMultipleLocalDeclarations declarationsOpt = (BoundMultipleLocalDeclarations)this.Visit(node.DeclarationsOpt);
            BoundExpression expressionOpt = (BoundExpression)this.Visit(node.ExpressionOpt);
            BoundStatement body = (BoundStatement)this.Visit(node.Body);
            Conversion disposableConversion = RewriteConversion(node.IDisposableConversion);
            return node.Update(newLocals, declarationsOpt, expressionOpt, disposableConversion, body);
        }

        private Conversion RewriteConversion(Conversion conversion)
        {
            switch (conversion.Kind)
            {
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.MethodGroup:
                    return new Conversion(conversion.Kind, VisitMethodSymbol(conversion.Method), conversion.IsExtensionMethod);
                default:
                    return conversion;
            }
        }

        public sealed override TypeSymbol VisitType(TypeSymbol type)
        {
            return TypeMap.SubstituteType(type)?.TypeSymbol;
        }

        public override BoundNode VisitMethodInfo(BoundMethodInfo node)
        {
            var rewrittenMethod = VisitMethodSymbol(node.Method);
            // No need to rewrite the node's type, because it is always System.Reflection.MethodInfo
            return node.Update(rewrittenMethod, node.GetMethodFromHandle, node.Type);
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var rewrittenPropertySymbol = VisitPropertySymbol(node.PropertySymbol);
            var rewrittenReceiver = (BoundExpression)Visit(node.ReceiverOpt);
            return node.Update(rewrittenReceiver, rewrittenPropertySymbol, node.ResultKind, VisitType(node.Type));
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            var rewrittenMethodSymbol = VisitMethodSymbol(node.Method);
            var rewrittenReceiver = (BoundExpression)this.Visit(node.ReceiverOpt);
            var rewrittenArguments = (ImmutableArray<BoundExpression>)this.VisitList(node.Arguments);
            var rewrittenType = this.VisitType(node.Type);

            Debug.Assert(rewrittenMethodSymbol.IsMetadataVirtual() == node.Method.IsMetadataVirtual());

            // If the original receiver was a base access and it was rewritten, 
            // change the method to point to the wrapper method
            if (BaseReferenceInReceiverWasRewritten(node.ReceiverOpt, rewrittenReceiver) && node.Method.IsMetadataVirtual())
            {
                rewrittenMethodSymbol = GetMethodWrapperForBaseNonVirtualCall(rewrittenMethodSymbol, node.Syntax);
            }

            return node.Update(
                rewrittenReceiver,
                rewrittenMethodSymbol,
                rewrittenArguments,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                node.IsDelegateCall,
                node.Expanded,
                node.InvokedAsExtensionMethod,
                node.ArgsToParamsOpt,
                node.ResultKind,
                rewrittenType);
        }

        private MethodSymbol GetMethodWrapperForBaseNonVirtualCall(MethodSymbol methodBeingCalled, CSharpSyntaxNode syntax)
        {
            var newMethod = GetOrCreateBaseFunctionWrapper(methodBeingCalled, syntax);
            if (!newMethod.IsGenericMethod)
            {
                return newMethod;
            }

            //  for generic methods we need to construct the method to be actually called
            Debug.Assert(methodBeingCalled.IsGenericMethod);
            var typeArgs = methodBeingCalled.TypeArguments;
            Debug.Assert(typeArgs.Length == newMethod.Arity);

            var visitedTypeArgs = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance(typeArgs.Length);
            foreach (var typeArg in typeArgs)
            {
                visitedTypeArgs.Add(typeArg.Update(VisitType(typeArg.TypeSymbol), typeArg.CustomModifiers));
            }

            return newMethod.Construct(visitedTypeArgs.ToImmutableAndFree());
        }

        private MethodSymbol GetOrCreateBaseFunctionWrapper(MethodSymbol methodBeingWrapped, CSharpSyntaxNode syntax)
        {
            methodBeingWrapped = methodBeingWrapped.ConstructedFrom;

            MethodSymbol wrapper = this.CompilationState.GetMethodWrapper(methodBeingWrapped);
            if ((object)wrapper != null)
            {
                return wrapper;
            }

            var containingType = this.ContainingType;

            //  create a method symbol
            string methodName = GeneratedNames.MakeBaseMethodWrapperName(this.CompilationState.NextWrapperMethodIndex);
            wrapper = new BaseMethodWrapperSymbol(containingType, methodBeingWrapped, syntax, methodName);

            //  add the method to module
            if (this.CompilationState.Emitting)
            {
                this.CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(containingType, wrapper);
            }

            Debug.Assert(wrapper.SynthesizesLoweredBoundBody);
            wrapper.GenerateMethodBody(this.CompilationState, this.Diagnostics);
            return wrapper;
        }

        private bool TryReplaceWithProxy(Symbol parameterOrLocal, CSharpSyntaxNode syntax, out BoundNode replacement)
        {
            CapturedSymbolReplacement proxy;
            if (proxies.TryGetValue(parameterOrLocal, out proxy))
            {
                replacement = proxy.Replacement(syntax, frameType => FramePointer(syntax, frameType));
                return true;
            }

            replacement = null;
            return false;
        }

        public sealed override BoundNode VisitParameter(BoundParameter node)
        {
            BoundNode replacement;
            if (TryReplaceWithProxy(node.ParameterSymbol, node.Syntax, out replacement))
            {
                return replacement;
            }

            // Non-captured and expression tree lambda parameters don't have a proxy.
            return VisitUnhoistedParameter(node);
        }

        protected virtual BoundNode VisitUnhoistedParameter(BoundParameter node)
        {
            return base.VisitParameter(node);
        }

        public sealed override BoundNode VisitLocal(BoundLocal node)
        {
            BoundNode replacement;
            if (TryReplaceWithProxy(node.LocalSymbol, node.Syntax, out replacement))
            {
                return replacement;
            }

            // if a local needs a proxy it should have been allocated by its declaration node.
            Debug.Assert(!NeedsProxy(node.LocalSymbol));

            return VisitUnhoistedLocal(node);
        }

        private BoundNode VisitUnhoistedLocal(BoundLocal node)
        {
            LocalSymbol replacementLocal;
            if (this.localMap.TryGetValue(node.LocalSymbol, out replacementLocal))
            {
                return new BoundLocal(node.Syntax, replacementLocal, node.ConstantValueOpt, replacementLocal.Type.TypeSymbol, node.HasErrors);
            }

            return base.VisitLocal(node);
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            BoundExpression expression = (BoundExpression)this.Visit(node.Expression);
            TypeSymbol type = this.VisitType(node.Type);
            return node.Update(expression, VisitMethodSymbol(node.GetAwaiter), VisitPropertySymbol(node.IsCompleted), VisitMethodSymbol(node.GetResult), type);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            BoundExpression originalLeft = node.Left;

            if (originalLeft.Kind != BoundKind.Local)
            {
                return base.VisitAssignmentOperator(node);
            }

            var leftLocal = (BoundLocal)originalLeft;

            BoundExpression originalRight = node.Right;

            if (leftLocal.LocalSymbol.RefKind != RefKind.None &&
                node.RefKind != RefKind.None &&
                NeedsProxy(leftLocal.LocalSymbol))
            {
                Debug.Assert(!proxies.ContainsKey(leftLocal.LocalSymbol));
                Debug.Assert(!IsStackAlloc(originalRight));
                //spilling ref local variables
                throw ExceptionUtilities.Unreachable;
            }

            if (NeedsProxy(leftLocal.LocalSymbol) && !proxies.ContainsKey(leftLocal.LocalSymbol))
            {
                Debug.Assert(leftLocal.LocalSymbol.DeclarationKind == LocalDeclarationKind.None);
                // spilling temp variables
                throw ExceptionUtilities.Unreachable;
            }

            BoundExpression rewrittenLeft = (BoundExpression)this.Visit(leftLocal);
            BoundExpression rewrittenRight = (BoundExpression)this.Visit(originalRight);
            TypeSymbol rewrittenType = VisitType(node.Type);

            // Check if we're assigning the result of stackalloc to a hoisted local.
            // If we are, we need to store the result in a temp local and then assign
            // the value of the local to the field corresponding to the hoisted local.
            // If the receiver of the field is on the stack when the stackalloc happens,
            // popping it will free the memory (?) or otherwise cause verification issues.
            // DevDiv Bugs 59454
            if (rewrittenLeft.Kind != BoundKind.Local && IsStackAlloc(originalRight))
            {
                // From ILGENREC::genAssign:
                // DevDiv Bugs 59454: Handle hoisted local initialized with a stackalloc
                // NOTE: Need to check for cast of stackalloc on RHS.
                // If LHS isLocal, then genAddr is a noop so regular case works fine.

                SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(this.CurrentMethod, rewrittenLeft.Syntax, this.CompilationState, this.Diagnostics);
                BoundAssignmentOperator tempAssignment;
                BoundLocal tempLocal = factory.StoreToTemp(rewrittenRight, out tempAssignment);

                Debug.Assert(node.RefKind == RefKind.None);
                BoundAssignmentOperator rewrittenAssignment = node.Update(rewrittenLeft, tempLocal, node.RefKind, rewrittenType);

                return new BoundSequence(
                    node.Syntax,
                    ImmutableArray.Create<LocalSymbol>(tempLocal.LocalSymbol),
                    ImmutableArray.Create<BoundExpression>(tempAssignment),
                    rewrittenAssignment,
                    rewrittenType);
            }

            return node.Update(rewrittenLeft, rewrittenRight, node.RefKind, rewrittenType);
        }

        private static bool IsStackAlloc(BoundExpression expr)
        {
            return
                expr.Kind == BoundKind.StackAllocArrayCreation ||
                expr.Kind == BoundKind.Conversion && ((BoundConversion)expr).Operand.Kind == BoundKind.StackAllocArrayCreation;
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            var rewrittenField = ((FieldSymbol)node.Field.OriginalDefinition)
                .AsMember((NamedTypeSymbol)this.VisitType(node.Field.ContainingType));
            return node.Update(rewrittenField, node.GetFieldFromHandle, node.Type);
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            BoundExpression receiverOpt = (BoundExpression)this.Visit(node.ReceiverOpt);
            TypeSymbol type = this.VisitType(node.Type);
            var fieldSymbol = ((FieldSymbol)node.FieldSymbol.OriginalDefinition)
                .AsMember((NamedTypeSymbol)this.VisitType(node.FieldSymbol.ContainingType));
            return node.Update(receiverOpt, fieldSymbol, node.ConstantValueOpt, node.ResultKind, type);
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            var rewritten = (BoundObjectCreationExpression)base.VisitObjectCreationExpression(node);
            if (rewritten.Type != node.Type && (object)node.Constructor != null)
            {
                MethodSymbol ctor = VisitMethodSymbol(node.Constructor);
                rewritten = rewritten.Update(
                    ctor,
                    rewritten.Arguments,
                    rewritten.ArgumentNamesOpt,
                    rewritten.ArgumentRefKindsOpt,
                    rewritten.Expanded,
                    rewritten.ArgsToParamsOpt,
                    rewritten.ConstantValueOpt,
                    rewritten.InitializerExpressionOpt,
                    rewritten.Type);
            }

            return rewritten;
        }

        public override BoundNode VisitConversion(BoundConversion conversion)
        {
            if (conversion.ConversionKind == ConversionKind.MethodGroup && (object)conversion.SymbolOpt != null)
            {
                return RewriteMethodGroupConversion(conversion);
            }
            else
            {
                return base.VisitConversion(conversion);
            }
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            BoundExpression originalArgument = node.Argument;
            BoundExpression rewrittenArgument = (BoundExpression)this.Visit(originalArgument);
            MethodSymbol method = node.MethodOpt;
            if (originalArgument.Kind == BoundKind.MethodGroup && rewrittenArgument.Kind == BoundKind.MethodGroup)
            {
                //  if the original argument was a method group AND the receiver was BoundKind.BaseReference
                //  and the visited argument is still a method group with receiver overridden, change the 
                //  method to point to the wrapper method
                var originalReceiver = ((BoundMethodGroup)originalArgument).ReceiverOpt;
                var newReceiver = ((BoundMethodGroup)rewrittenArgument).ReceiverOpt;
                if (BaseReferenceInReceiverWasRewritten(originalReceiver, newReceiver) && method.IsMetadataVirtual())
                {
                    method = GetMethodWrapperForBaseNonVirtualCall(method, originalArgument.Syntax);
                    // NOTE: we substitute the method in rewritten bound delegate 
                    //       creation node, but leave the method group unchanged
                }
            }
            method = VisitMethodSymbol(method);
            TypeSymbol type = this.VisitType(node.Type);
            return node.Update(rewrittenArgument, method, node.IsExtensionMethod, type);
        }

        protected MethodSymbol VisitMethodSymbol(MethodSymbol method)
        {
            if ((object)method == null)
            {
                return null;
            }

            if (!method.ContainingType.IsAnonymousType)
            {
                //  Method of a regular type
                return ((MethodSymbol)method.OriginalDefinition)
                    .AsMember((NamedTypeSymbol)TypeMap.SubstituteType(method.ContainingType).AsTypeSymbolOnly())
                    .ConstructIfGeneric(TypeMap.SubstituteTypes(method.TypeArguments));
            }

            //  Method of an anonymous type
            var newType = (NamedTypeSymbol)TypeMap.SubstituteType(method.ContainingType).AsTypeSymbolOnly();
            if (ReferenceEquals(newType, method.ContainingType))
            {
                //  Anonymous type symbol was not rewritten
                return method;
            }

            //  get a new method by name
            foreach (var member in newType.GetMembers(method.Name))
            {
                if (member.Kind == SymbolKind.Method)
                {
                    return (MethodSymbol)member;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private PropertySymbol VisitPropertySymbol(PropertySymbol property)
        {
            if ((object)property == null)
            {
                return null;
            }

            if (!property.ContainingType.IsAnonymousType)
            {
                //  Property of a regular type
                return ((PropertySymbol)property.OriginalDefinition)
                    .AsMember((NamedTypeSymbol)TypeMap.SubstituteType(property.ContainingType).AsTypeSymbolOnly());
            }

            //  Method of an anonymous type
            var newType = (NamedTypeSymbol)TypeMap.SubstituteType(property.ContainingType).AsTypeSymbolOnly();
            if (ReferenceEquals(newType, property.ContainingType))
            {
                //  Anonymous type symbol was not rewritten
                return property;
            }

            //  get a new property by name
            foreach (var member in newType.GetMembers(property.Name))
            {
                if (member.Kind == SymbolKind.Property)
                {
                    return (PropertySymbol)member;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private FieldSymbol VisitFieldSymbol(FieldSymbol field)
        {
            //  Property of a regular type
            return ((FieldSymbol)field.OriginalDefinition)
                .AsMember((NamedTypeSymbol)TypeMap.SubstituteType(field.ContainingType).AsTypeSymbolOnly());
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            ImmutableArray<BoundExpression> arguments = (ImmutableArray<BoundExpression>)this.VisitList(node.Arguments);
            TypeSymbol type = this.VisitType(node.Type);

            var member = node.MemberSymbol;

            switch (member.Kind)
            {
                case SymbolKind.Field:
                    member = VisitFieldSymbol((FieldSymbol)member);
                    break;
                case SymbolKind.Property:
                    member = VisitPropertySymbol((PropertySymbol)member);
                    break;
            }

            return node.Update(member, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.ResultKind, type);
        }

        private static bool BaseReferenceInReceiverWasRewritten(BoundExpression originalReceiver, BoundExpression rewrittenReceiver)
        {
            return originalReceiver != null && originalReceiver.Kind == BoundKind.BaseReference &&
                   rewrittenReceiver != null && rewrittenReceiver.Kind != BoundKind.BaseReference;
        }

        private BoundNode RewriteMethodGroupConversion(BoundConversion conversion)
        {
            // in a method group conversion, we may need to rewrite the selected method
            BoundMethodGroup operand = (BoundMethodGroup)conversion.Operand;
            BoundExpression originalReceiverOpt = operand.ReceiverOpt;
            BoundExpression receiverOpt;

            if (originalReceiverOpt == null)
            {
                receiverOpt = null;
            }
            else if (!conversion.IsExtensionMethod && conversion.SymbolOpt.IsStatic)
            {
                receiverOpt = new BoundTypeExpression(originalReceiverOpt.Syntax, null, VisitType(originalReceiverOpt.Type));
            }
            else
            {
                receiverOpt = (BoundExpression)Visit(originalReceiverOpt);
            }

            TypeSymbol type = this.VisitType(conversion.Type);

            MethodSymbol method = conversion.SymbolOpt;

            //  if the original receiver was a base access and is was rewritten, 
            //  change the method to point to the wrapper method
            if (BaseReferenceInReceiverWasRewritten(originalReceiverOpt, receiverOpt) && method.IsMetadataVirtual())
            {
                method = GetMethodWrapperForBaseNonVirtualCall(method, conversion.Syntax);
            }

            method = VisitMethodSymbol(method);
            operand = operand.Update(
                TypeMap.SubstituteTypesWithoutModifiers(operand.TypeArgumentsOpt),
                method.Name,
                operand.Methods,
                operand.LookupSymbolOpt,
                operand.LookupError,
                operand.Flags,
                receiverOpt,
                operand.ResultKind);

            return conversion.Update(
                operand,
                conversion.ConversionKind,
                resultKind: conversion.ResultKind,
                isBaseConversion: conversion.IsBaseConversion,
                symbolOpt: method,
                @checked: conversion.Checked,
                explicitCastInCode: conversion.ExplicitCastInCode,
                isExtensionMethod: conversion.IsExtensionMethod,
                isArrayIndex: conversion.IsArrayIndex,
                constantValueOpt: conversion.ConstantValueOpt,
                type: type);
        }

        /// <summary>
        /// A wrapper method that is created for non-virtually calling a base-class 
        /// virtual method from other classes (like those created for lambdas...).
        /// </summary>
        private sealed partial class BaseMethodWrapperSymbol : SynthesizedMethodBaseSymbol
        {
            internal BaseMethodWrapperSymbol(NamedTypeSymbol containingType, MethodSymbol methodBeingWrapped, CSharpSyntaxNode syntax, string name)
                : base(containingType, methodBeingWrapped, syntax.SyntaxTree.GetReference(syntax), null, syntax.GetLocation(), name, DeclarationModifiers.Private)
            {
                Debug.Assert(containingType.ContainingModule is SourceModuleSymbol);
                Debug.Assert(ReferenceEquals(methodBeingWrapped, methodBeingWrapped.ConstructedFrom));
                Debug.Assert(!methodBeingWrapped.IsStatic);

                TypeMap typeMap = null;
                ImmutableArray<TypeParameterSymbol> typeParameters;

                var substitutedType = methodBeingWrapped.ContainingType as SubstitutedNamedTypeSymbol;
                typeMap = ((object)substitutedType == null ? TypeMap.Empty : substitutedType.TypeSubstitution);

                if (!methodBeingWrapped.IsGenericMethod)
                {
                    typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                }
                else
                {
                    typeMap = typeMap.WithAlphaRename(methodBeingWrapped, this, out typeParameters);
                }

                AssignTypeMapAndTypeParameters(typeMap, typeParameters);
            }

            internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(compilationState, ref attributes);

                AddSynthesizedAttribute(ref attributes, this.DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
            }
        }
    }
}

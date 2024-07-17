// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract partial class MethodToClassRewriter : BoundTreeToDifferentEnclosingContextRewriter
    {
        // For each captured variable, information about its replacement.  May be populated lazily (that is, not all
        // upfront) by subclasses.  Specifically, the async rewriter produces captured symbols for temps, including
        // ref locals, lazily.
        // The lambda rewriter also saves/restores the proxies across passes, since local function
        // reference rewriting is done in a separate pass but still requires the frame proxies
        // created in the first pass.
        protected Dictionary<Symbol, CapturedSymbolReplacement> proxies = new Dictionary<Symbol, CapturedSymbolReplacement>();

        // Subclasses override this method to fetch a frame pointer.
        protected abstract BoundExpression FramePointer(SyntaxNode syntax, NamedTypeSymbol frameClass);

        // Containing type for any synthesized members.
        protected abstract NamedTypeSymbol ContainingType { get; }

        /// <summary> A not-null collection of synthesized methods generated for the current source type. </summary>
        protected readonly TypeCompilationState CompilationState;

        protected readonly BindingDiagnosticBag Diagnostics;
        protected readonly VariableSlotAllocator? slotAllocator;

        private readonly Dictionary<BoundValuePlaceholderBase, BoundExpression> _placeholderMap;

        protected MethodToClassRewriter(VariableSlotAllocator? slotAllocator, TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(diagnostics.DiagnosticBag != null);

            this.CompilationState = compilationState;
            this.Diagnostics = diagnostics;
            this.slotAllocator = slotAllocator;
            this._placeholderMap = new Dictionary<BoundValuePlaceholderBase, BoundExpression>();
        }

        /// <summary>
        /// Returns true if the specified local/parameter needs to be hoisted to a field.
        /// Variable may be hoisted even if it is not captured, to improve debugging experience.
        /// </summary>
        protected abstract bool NeedsProxy(Symbol localOrParameter);

        protected sealed override bool TryRewriteLocal(LocalSymbol local, [NotNullWhen(true)] out LocalSymbol? newLocal)
        {
            if (NeedsProxy(local))
            {
                // no longer a local symbol
                newLocal = null;
                return false;
            }

            return base.TryRewriteLocal(local, out newLocal);
        }

        public abstract override BoundNode VisitScope(BoundScope node);

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var rewrittenPropertySymbol = VisitPropertySymbol(node.PropertySymbol);
            var rewrittenReceiver = (BoundExpression?)Visit(node.ReceiverOpt);
            return node.Update(rewrittenReceiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, rewrittenPropertySymbol, node.ResultKind, VisitType(node.Type));
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            var rewrittenMethodSymbol = VisitMethodSymbol(node.Method);
            var rewrittenReceiver = (BoundExpression?)this.Visit(node.ReceiverOpt);
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
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                rewrittenMethodSymbol,
                rewrittenArguments,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                node.IsDelegateCall,
                node.Expanded,
                node.InvokedAsExtensionMethod,
                node.ArgsToParamsOpt,
                node.DefaultArguments,
                node.ResultKind,
                rewrittenType);
        }

        private MethodSymbol GetMethodWrapperForBaseNonVirtualCall(MethodSymbol methodBeingCalled, SyntaxNode syntax)
        {
            var newMethod = GetOrCreateBaseFunctionWrapper(methodBeingCalled, syntax);
            if (!newMethod.IsGenericMethod)
            {
                return newMethod;
            }

            //  for generic methods we need to construct the method to be actually called
            Debug.Assert(methodBeingCalled.IsGenericMethod);
            var typeArgs = methodBeingCalled.TypeArgumentsWithAnnotations;
            Debug.Assert(typeArgs.Length == newMethod.Arity);

            var visitedTypeArgs = ArrayBuilder<TypeWithAnnotations>.GetInstance(typeArgs.Length);
            foreach (var typeArg in typeArgs)
            {
                visitedTypeArgs.Add(typeArg.WithTypeAndModifiers(VisitType(typeArg.Type), typeArg.CustomModifiers));
            }

            return newMethod.Construct(visitedTypeArgs.ToImmutableAndFree());
        }

        private MethodSymbol GetOrCreateBaseFunctionWrapper(MethodSymbol methodBeingWrapped, SyntaxNode syntax)
        {
            methodBeingWrapped = methodBeingWrapped.ConstructedFrom;

            MethodSymbol? wrapper = this.CompilationState.GetMethodWrapper(methodBeingWrapped);
            if (wrapper is not null)
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
                this.CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(containingType, wrapper.GetCciAdapter());
            }

            Debug.Assert(wrapper.SynthesizesLoweredBoundBody);
            wrapper.GenerateMethodBody(this.CompilationState, this.Diagnostics);
            return wrapper;
        }

        private bool TryReplaceWithProxy(Symbol parameterOrLocal, SyntaxNode syntax, [NotNullWhen(true)] out BoundNode? replacement)
        {
            if (proxies.TryGetValue(parameterOrLocal, out CapturedSymbolReplacement? proxy))
            {
                replacement = proxy.Replacement(
                    syntax,
                    static (frameType, arg) => arg.self.FramePointer(arg.syntax, frameType),
                    (syntax, self: this));

                return true;
            }

            replacement = null;
            return false;
        }

        public sealed override BoundNode VisitParameter(BoundParameter node)
        {
            if (TryReplaceWithProxy(node.ParameterSymbol, node.Syntax, out BoundNode? replacement))
            {
                return replacement;
            }

            // Non-captured and expression tree lambda parameters don't have a proxy.
            return VisitUnhoistedParameter(node);
        }

        protected virtual BoundNode VisitUnhoistedParameter(BoundParameter node)
        {
            return base.VisitParameter(node)!;
        }

        public sealed override BoundNode VisitLocal(BoundLocal node)
        {
            if (TryReplaceWithProxy(node.LocalSymbol, node.Syntax, out BoundNode? replacement))
            {
                return replacement;
            }

            // if a local needs a proxy it should have been allocated by its declaration node.
            Debug.Assert(!NeedsProxy(node.LocalSymbol));

            return base.VisitLocal(node)!;
        }

        public override BoundNode? VisitLocalId(BoundLocalId node)
            => TryGetHoistedField(node.Local, out var fieldSymbol) ?
                node.Update(node.Local, fieldSymbol, node.Type) :
                base.VisitLocalId(node);

        public override BoundNode? VisitParameterId(BoundParameterId node)
            => TryGetHoistedField(node.Parameter, out var fieldSymbol) ?
                node.Update(node.Parameter, fieldSymbol, node.Type) :
                base.VisitParameterId(node);

        private bool TryGetHoistedField(Symbol variable, [NotNullWhen(true)] out FieldSymbol? field)
        {
            if (proxies.TryGetValue(variable, out CapturedSymbolReplacement? proxy))
            {
                field = proxy switch
                {
                    CapturedToStateMachineFieldReplacement stateMachineProxy => (FieldSymbol)stateMachineProxy.HoistedField,
                    CapturedToFrameSymbolReplacement closureProxy => closureProxy.HoistedField,
                    _ => throw ExceptionUtilities.UnexpectedValue(proxy)
                };

                return true;
            }

            field = null;
            return false;
        }

        public override BoundNode VisitAwaitableInfo(BoundAwaitableInfo node)
        {
            var awaitablePlaceholder = node.AwaitableInstancePlaceholder;
            if (awaitablePlaceholder is null)
            {
                return node;
            }

            var rewrittenPlaceholder = awaitablePlaceholder.Update(VisitType(awaitablePlaceholder.Type));
            _placeholderMap.Add(awaitablePlaceholder, rewrittenPlaceholder);

            var getAwaiter = (BoundExpression?)this.Visit(node.GetAwaiter);
            var isCompleted = VisitPropertySymbol(node.IsCompleted);
            var getResult = VisitMethodSymbol(node.GetResult);

            _placeholderMap.Remove(awaitablePlaceholder);

            return node.Update(rewrittenPlaceholder, node.IsDynamic, getAwaiter, isCompleted, getResult);
        }

        public override BoundNode VisitAwaitableValuePlaceholder(BoundAwaitableValuePlaceholder node)
        {
            return _placeholderMap[node];
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            BoundExpression originalLeft = node.Left;

            if (originalLeft.Kind != BoundKind.Local)
            {
                return base.VisitAssignmentOperator(node)!;
            }

            var leftLocal = (BoundLocal)originalLeft;

            BoundExpression originalRight = node.Right;

            if (leftLocal.LocalSymbol.RefKind != RefKind.None &&
                node.IsRef &&
                NeedsProxy(leftLocal.LocalSymbol))
            {
                Debug.Assert(!proxies.ContainsKey(leftLocal.LocalSymbol));
                Debug.Assert(originalRight.Kind != BoundKind.ConvertedStackAllocExpression);
                //spilling ref local variables
                throw ExceptionUtilities.Unreachable();
            }

            if (NeedsProxy(leftLocal.LocalSymbol) && !proxies.ContainsKey(leftLocal.LocalSymbol))
            {
                Debug.Assert(leftLocal.LocalSymbol.DeclarationKind == LocalDeclarationKind.None);
                // spilling temp variables
                throw ExceptionUtilities.Unreachable();
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
            if (rewrittenLeft.Kind != BoundKind.Local && originalRight.Kind == BoundKind.ConvertedStackAllocExpression)
            {
                // From ILGENREC::genAssign:
                // DevDiv Bugs 59454: Handle hoisted local initialized with a stackalloc
                // NOTE: Need to check for cast of stackalloc on RHS.
                // If LHS isLocal, then genAddr is a noop so regular case works fine.

                SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(this.CurrentMethod, rewrittenLeft.Syntax, this.CompilationState, this.Diagnostics);
                BoundAssignmentOperator tempAssignment;
                BoundLocal tempLocal = factory.StoreToTemp(rewrittenRight, out tempAssignment);

                Debug.Assert(!node.IsRef);
                BoundAssignmentOperator rewrittenAssignment = node.Update(rewrittenLeft, tempLocal, node.IsRef, rewrittenType);

                return new BoundSequence(
                    node.Syntax,
                    ImmutableArray.Create<LocalSymbol>(tempLocal.LocalSymbol),
                    ImmutableArray.Create<BoundExpression>(tempAssignment),
                    rewrittenAssignment,
                    rewrittenType);
            }

            return node.Update(rewrittenLeft, rewrittenRight, node.IsRef, rewrittenType);
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            var rewrittenField = ((FieldSymbol)node.Field.OriginalDefinition)
                .AsMember((NamedTypeSymbol)this.VisitType(node.Field.ContainingType));
            return node.Update(rewrittenField, node.GetFieldFromHandle, node.Type);
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var receiverOpt = (BoundExpression?)this.Visit(node.ReceiverOpt);
            TypeSymbol type = this.VisitType(node.Type);
            var fieldSymbol = ((FieldSymbol)node.FieldSymbol.OriginalDefinition)
                .AsMember((NamedTypeSymbol)this.VisitType(node.FieldSymbol.ContainingType));
            return node.Update(receiverOpt, fieldSymbol, node.ConstantValueOpt, node.ResultKind, type);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            BoundExpression originalArgument = node.Argument;
            BoundExpression rewrittenArgument = (BoundExpression)this.Visit(originalArgument);
            MethodSymbol? method = node.MethodOpt;

            // if the original receiver was BoundKind.BaseReference (i.e. from a method group)
            // and the receiver is overridden, change the method to point to a wrapper method
            if (BaseReferenceInReceiverWasRewritten(originalArgument, rewrittenArgument) && method!.IsMetadataVirtual())
            {
                method = GetMethodWrapperForBaseNonVirtualCall(method, originalArgument.Syntax);
            }
            method = VisitMethodSymbol(method);
            TypeSymbol type = this.VisitType(node.Type);
            return node.Update(rewrittenArgument, method, node.IsExtensionMethod, node.WasTargetTyped, type);
        }

        [return: NotNullIfNotNull(nameof(property))]
        private new PropertySymbol? VisitPropertySymbol(PropertySymbol? property)
        {
            if (property is null)
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

            throw ExceptionUtilities.Unreachable();
        }

        private new FieldSymbol VisitFieldSymbol(FieldSymbol field)
        {
            //  Property of a regular type
            return ((FieldSymbol)field.OriginalDefinition)
                .AsMember((NamedTypeSymbol)TypeMap.SubstituteType(field.ContainingType).AsTypeSymbolOnly());
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            ImmutableArray<BoundExpression> arguments = (ImmutableArray<BoundExpression>)this.VisitList(node.Arguments);
            TypeSymbol type = this.VisitType(node.Type);
            TypeSymbol receiverType = this.VisitType(node.ReceiverType);

            var member = node.MemberSymbol;
            Debug.Assert(member is not null);

            switch (member.Kind)
            {
                case SymbolKind.Field:
                    member = VisitFieldSymbol((FieldSymbol)member);
                    break;
                case SymbolKind.Property:
                    member = VisitPropertySymbol((PropertySymbol)member);
                    break;
            }

            return node.Update(member, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.DefaultArguments, node.ResultKind, node.AccessorKind, receiverType, type);
        }

        private static bool BaseReferenceInReceiverWasRewritten([NotNullWhen(true)] BoundExpression? originalReceiver, [NotNullWhen(true)] BoundExpression? rewrittenReceiver)
        {
            return originalReceiver is { Kind: BoundKind.BaseReference } &&
                   rewrittenReceiver is { Kind: not BoundKind.BaseReference };
        }

        /// <summary>
        /// A wrapper method that is created for non-virtually calling a base-class 
        /// virtual method from other classes (like those created for lambdas...).
        /// </summary>
        private sealed partial class BaseMethodWrapperSymbol : SynthesizedMethodBaseSymbol
        {
            internal BaseMethodWrapperSymbol(NamedTypeSymbol containingType, MethodSymbol methodBeingWrapped, SyntaxNode syntax, string name)
                : base(containingType, methodBeingWrapped, syntax.SyntaxTree.GetReference(syntax), syntax.GetLocation(), name, DeclarationModifiers.Private,
                      isIterator: false)
            {
                Debug.Assert(containingType.ContainingModule is SourceModuleSymbol);
                Debug.Assert(ReferenceEquals(methodBeingWrapped, methodBeingWrapped.ConstructedFrom));
                Debug.Assert(!methodBeingWrapped.IsStatic);

                TypeMap? typeMap = methodBeingWrapped.ContainingType is SubstitutedNamedTypeSymbol substitutedType ? substitutedType.TypeSubstitution : TypeMap.Empty;

                ImmutableArray<TypeParameterSymbol> typeParameters;
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

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

                AddSynthesizedAttribute(ref attributes, this.DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
            }
        }
    }
}

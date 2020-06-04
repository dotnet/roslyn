﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceMemberMethodSymbol : SourceMethodSymbolWithAttributes, IAttributeTargetSymbol
    {
        // The flags type is used to compact many different bits of information.
        protected struct Flags
        {
            // We currently pack everything into a 32 bit int with the following layout:
            //
            // |                |vvv|yy|s|r|q|z|wwwww|
            // 
            // w = method kind.  5 bits.
            // z = isExtensionMethod. 1 bit.
            // q = isMetadataVirtualIgnoringInterfaceChanges. 1 bit.
            // r = isMetadataVirtual. 1 bit. (At least as true as isMetadataVirtualIgnoringInterfaceChanges.)
            // s = isMetadataVirtualLocked. 1 bit.
            // y = ReturnsVoid. 2 bits.
            // v = NullableContext. 3 bits.
            private int _flags;

            private const int MethodKindOffset = 0;
            private const int MethodKindSize = 5;

            private const int IsExtensionMethodOffset = MethodKindOffset + MethodKindSize;
            private const int IsExtensionMethodSize = 1;

            private const int IsMetadataVirtualIgnoringInterfaceChangesOffset = IsExtensionMethodOffset + IsExtensionMethodSize;
            private const int IsMetadataVirtualIgnoringInterfaceChangesSize = 1;

            private const int IsMetadataVirtualOffset = IsMetadataVirtualIgnoringInterfaceChangesOffset + IsMetadataVirtualIgnoringInterfaceChangesSize;
            private const int IsMetadataVirtualSize = 1;

            private const int IsMetadataVirtualLockedOffset = IsMetadataVirtualOffset + IsMetadataVirtualSize;
            private const int IsMetadataVirtualLockedSize = 1;

            private const int ReturnsVoidOffset = IsMetadataVirtualLockedOffset + IsMetadataVirtualLockedSize;
            private const int ReturnsVoidSize = 2;

            private const int NullableContextOffset = ReturnsVoidOffset + ReturnsVoidSize;
            private const int NullableContextSize = 3;

            private const int MethodKindMask = (1 << MethodKindSize) - 1;

            private const int IsExtensionMethodBit = 1 << IsExtensionMethodOffset;
            private const int IsMetadataVirtualIgnoringInterfaceChangesBit = 1 << IsMetadataVirtualIgnoringInterfaceChangesOffset;
            private const int IsMetadataVirtualBit = 1 << IsMetadataVirtualIgnoringInterfaceChangesOffset;
            private const int IsMetadataVirtualLockedBit = 1 << IsMetadataVirtualLockedOffset;

            private const int ReturnsVoidBit = 1 << ReturnsVoidOffset;
            private const int ReturnsVoidIsSetBit = 1 << ReturnsVoidOffset + 1;

            private const int NullableContextMask = (1 << NullableContextSize) - 1;

            public bool TryGetReturnsVoid(out bool value)
            {
                int bits = _flags;
                value = (bits & ReturnsVoidBit) != 0;
                return (bits & ReturnsVoidIsSetBit) != 0;
            }

            public void SetReturnsVoid(bool value)
            {
                ThreadSafeFlagOperations.Set(ref _flags, (int)(ReturnsVoidIsSetBit | (value ? ReturnsVoidBit : 0)));
            }

            public MethodKind MethodKind
            {
                get { return (MethodKind)((_flags >> MethodKindOffset) & MethodKindMask); }
            }

            public bool IsExtensionMethod
            {
                get { return (_flags & IsExtensionMethodBit) != 0; }
            }

            public bool IsMetadataVirtualLocked
            {
                get { return (_flags & IsMetadataVirtualLockedBit) != 0; }
            }

#if DEBUG
            static Flags()
            {
                // Verify masks are sufficient for values.
                Debug.Assert(EnumUtilities.ContainsAllValues<MethodKind>(MethodKindMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<NullableContextKind>(NullableContextMask));
            }
#endif

            private static bool ModifiersRequireMetadataVirtual(DeclarationModifiers modifiers)
            {
                return (modifiers & (DeclarationModifiers.Abstract | DeclarationModifiers.Virtual | DeclarationModifiers.Override)) != 0;
            }

            public Flags(
                MethodKind methodKind,
                DeclarationModifiers declarationModifiers,
                bool returnsVoid,
                bool isExtensionMethod,
                bool isMetadataVirtualIgnoringModifiers = false)
            {
                bool isMetadataVirtual = isMetadataVirtualIgnoringModifiers || ModifiersRequireMetadataVirtual(declarationModifiers);

                int methodKindInt = ((int)methodKind & MethodKindMask) << MethodKindOffset;
                int isExtensionMethodInt = isExtensionMethod ? IsExtensionMethodBit : 0;
                int isMetadataVirtualIgnoringInterfaceImplementationChangesInt = isMetadataVirtual ? IsMetadataVirtualIgnoringInterfaceChangesBit : 0;
                int isMetadataVirtualInt = isMetadataVirtual ? IsMetadataVirtualBit : 0;

                _flags = methodKindInt
                    | isExtensionMethodInt
                    | isMetadataVirtualIgnoringInterfaceImplementationChangesInt
                    | isMetadataVirtualInt
                    | (returnsVoid ? ReturnsVoidBit : 0)
                    | ReturnsVoidIsSetBit;
            }

            public bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
            {
                // This flag is immutable, so there's no reason to set a lock bit, as we do below.
                if (ignoreInterfaceImplementationChanges)
                {
                    return (_flags & IsMetadataVirtualIgnoringInterfaceChangesBit) != 0;
                }

                if (!IsMetadataVirtualLocked)
                {
                    ThreadSafeFlagOperations.Set(ref _flags, IsMetadataVirtualLockedBit);
                }

                return (_flags & IsMetadataVirtualBit) != 0;
            }

            public void EnsureMetadataVirtual()
            {
                // ACASEY: This assert is here to check that we're not mutating the value of IsMetadataVirtual after
                // someone has consumed it.  The best practice is to not access IsMetadataVirtual before ForceComplete
                // has been called on all SourceNamedTypeSymbols.  If it is necessary to do so, then you can pass
                // ignoreInterfaceImplementationChanges: true, but you must be conscious that seeing "false" may not
                // reflect the final, emitted modifier.
                Debug.Assert(!IsMetadataVirtualLocked);
                if ((_flags & IsMetadataVirtualBit) == 0)
                {
                    ThreadSafeFlagOperations.Set(ref _flags, IsMetadataVirtualBit);
                }
            }

            public bool TryGetNullableContext(out byte? value)
            {
                return ((NullableContextKind)((_flags >> NullableContextOffset) & NullableContextMask)).TryGetByte(out value);
            }

            public bool SetNullableContext(byte? value)
            {
                return ThreadSafeFlagOperations.Set(ref _flags, (((int)value.ToNullableContextFlags() & NullableContextMask) << NullableContextOffset));
            }
        }

        protected SymbolCompletionState state;

        protected DeclarationModifiers DeclarationModifiers;
        protected Flags flags;

        private readonly NamedTypeSymbol _containingType;
        private ParameterSymbol _lazyThisParameter;
        private TypeWithAnnotations.Boxed _lazyIteratorElementType;

        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;

        protected ImmutableArray<Location> locations;
        protected string lazyDocComment;
        protected string lazyExpandedDocComment;

        //null if has never been computed. Initial binding diagnostics
        //are stashed here in service of API usage patterns
        //where method body diagnostics are requested multiple times.
        private ImmutableArray<Diagnostic> _cachedDiagnostics;
        internal ImmutableArray<Diagnostic> Diagnostics
        {
            get { return _cachedDiagnostics; }
        }

        internal ImmutableArray<Diagnostic> SetDiagnostics(ImmutableArray<Diagnostic> newSet, out bool diagsWritten)
        {
            //return the diagnostics that were actually saved in the event that there were two threads racing. 
            diagsWritten = ImmutableInterlocked.InterlockedInitialize(ref _cachedDiagnostics, newSet);
            return _cachedDiagnostics;
        }

        protected SourceMemberMethodSymbol(NamedTypeSymbol containingType, SyntaxReference syntaxReferenceOpt, Location location, bool isIterator)
            : this(containingType, syntaxReferenceOpt, ImmutableArray.Create(location), isIterator)
        {
        }

        protected SourceMemberMethodSymbol(
            NamedTypeSymbol containingType,
            SyntaxReference syntaxReferenceOpt,
            ImmutableArray<Location> locations,
            bool isIterator)
            : base(syntaxReferenceOpt)
        {
            Debug.Assert((object)containingType != null);
            Debug.Assert(!locations.IsEmpty);

            _containingType = containingType;
            this.locations = locations;

            if (isIterator)
            {
                _lazyIteratorElementType = TypeWithAnnotations.Boxed.Sentinel;
            }
        }

        protected void CheckEffectiveAccessibility(TypeWithAnnotations returnType, ImmutableArray<ParameterSymbol> parameters, DiagnosticBag diagnostics)
        {
            if (this.DeclaredAccessibility <= Accessibility.Private || MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                return;
            }

            ErrorCode code = (this.MethodKind == MethodKind.Conversion || this.MethodKind == MethodKind.UserDefinedOperator) ?
                ErrorCode.ERR_BadVisOpReturn :
                ErrorCode.ERR_BadVisReturnType;

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (!this.IsNoMoreVisibleThan(returnType, ref useSiteDiagnostics))
            {
                // Inconsistent accessibility: return type '{1}' is less accessible than method '{0}'
                diagnostics.Add(code, Locations[0], this, returnType.Type);
            }

            code = (this.MethodKind == MethodKind.Conversion || this.MethodKind == MethodKind.UserDefinedOperator) ?
                ErrorCode.ERR_BadVisOpParam :
                ErrorCode.ERR_BadVisParamType;

            foreach (var parameter in parameters)
            {
                if (!parameter.TypeWithAnnotations.IsAtLeastAsVisibleAs(this, ref useSiteDiagnostics))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than method '{0}'
                    diagnostics.Add(code, Locations[0], this, parameter.Type);
                }
            }

            diagnostics.Add(Locations[0], useSiteDiagnostics);
        }

        protected void MakeFlags(
            MethodKind methodKind,
            DeclarationModifiers declarationModifiers,
            bool returnsVoid,
            bool isExtensionMethod,
            bool isMetadataVirtualIgnoringModifiers = false)
        {
            DeclarationModifiers = declarationModifiers;
            this.flags = new Flags(methodKind, declarationModifiers, returnsVoid, isExtensionMethod, isMetadataVirtualIgnoringModifiers);
        }

        protected void SetReturnsVoid(bool returnsVoid)
        {
            this.flags.SetReturnsVoid(returnsVoid);
        }

        /// <remarks>
        /// Implementers should assume that a lock has been taken on MethodChecksLockObject.
        /// In particular, it should not (generally) be necessary to use CompareExchange to
        /// protect assignments to fields.
        /// </remarks>
        protected abstract void MethodChecks(DiagnosticBag diagnostics);

        /// <summary>
        /// We can usually lock on the syntax reference of this method, but it turns
        /// out that some synthesized methods (e.g. field-like event accessors) also
        /// need to do method checks.  This property allows such methods to supply
        /// their own lock objects, so that we don't have to add a new field to every
        /// SourceMethodSymbol.
        /// </summary>
        protected virtual object MethodChecksLockObject
        {
            get { return this.syntaxReferenceOpt; }
        }

        protected void LazyMethodChecks()
        {
            if (!state.HasComplete(CompletionPart.FinishMethodChecks))
            {
                // TODO: if this lock ever encloses a potential call to Debugger.NotifyOfCrossThreadDependency,
                // then we should call DebuggerUtilities.CallBeforeAcquiringLock() (see method comment for more
                // details).

                object lockObject = MethodChecksLockObject;
                Debug.Assert(lockObject != null);
                lock (lockObject)
                {
                    if (state.NotePartComplete(CompletionPart.StartMethodChecks))
                    {
                        // By setting StartMethodChecks, we've committed to doing the checks and setting
                        // FinishMethodChecks.  So there is no cancellation supported between one and the other.
                        var diagnostics = DiagnosticBag.GetInstance();
                        try
                        {
                            MethodChecks(diagnostics);
                            AddDeclarationDiagnostics(diagnostics);
                        }
                        finally
                        {
                            state.NotePartComplete(CompletionPart.FinishMethodChecks);
                            diagnostics.Free();
                        }
                    }
                    else
                    {
                        // Either (1) this thread is in the process of completing the method,
                        // or (2) some other thread has beat us to the punch and completed the method.
                        // We can distinguish the two cases here by checking for the FinishMethodChecks
                        // part to be complete, which would only occur if another thread completed this
                        // method.
                        //
                        // The other case, in which this thread is in the process of completing the method,
                        // requires that we return here even though the checks are not complete.  That's because
                        // methods are processed by first populating the return type and parameters by binding
                        // the syntax from source.  Those values are visible to the same thread for the purpose
                        // of computing which methods are implemented and overridden.  But then those values
                        // may be rewritten (by the same thread) to copy down custom modifiers.  In order to
                        // allow the same thread to see the return type and parameters from the syntax (though
                        // they do not yet take on their final values), we return here.

                        // Due to the fact that LazyMethodChecks is potentially reentrant, we must use a 
                        // reentrant lock to avoid deadlock and cannot assert that at this point method checks
                        // have completed (state.HasComplete(CompletionPart.FinishMethodChecks)).
                    }
                }
            }
        }

        protected virtual void LazyAsyncMethodChecks(CancellationToken cancellationToken)
        {
            state.NotePartComplete(CompletionPart.StartAsyncMethodChecks);
            state.NotePartComplete(CompletionPart.FinishAsyncMethodChecks);
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        #region Flags

        public override bool ReturnsVoid
        {
            get
            {
                flags.TryGetReturnsVoid(out bool value);
                return value;
            }
        }

        public sealed override MethodKind MethodKind
        {
            get
            {
                return this.flags.MethodKind;
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return this.flags.IsExtensionMethod;
            }
        }

        // TODO (tomat): sealed
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            if (IsExplicitInterfaceImplementation && _containingType.IsInterface)
            {
                // All implementations of methods from base interfaces should omit the newslot bit to ensure no new vtable slot is allocated.
                return false;
            }

            // If C# and the runtime don't agree on the overridden method,
            // then we will mark the method as newslot and specify the
            // override explicitly (see GetExplicitImplementationOverrides
            // in NamedTypeSymbolAdapter.cs).
            return this.IsOverride ?
                this.RequiresExplicitOverride() :
                this.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        // TODO (tomat): sealed?
        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return this.flags.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal void EnsureMetadataVirtual()
        {
            this.flags.EnsureMetadataVirtual();
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(this.DeclarationModifiers);
            }
        }

        internal bool HasExternModifier
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Extern) != 0;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return HasExternModifier;
            }
        }

        public sealed override bool IsSealed
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Sealed) != 0;
            }
        }

        public sealed override bool IsAbstract
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Abstract) != 0;
            }
        }

        public sealed override bool IsOverride
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Override) != 0;
            }
        }

        internal bool IsPartial
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Partial) != 0;
            }
        }

        public sealed override bool IsVirtual
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Virtual) != 0;
            }
        }

        internal bool IsNew
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.New) != 0;
            }
        }

        public sealed override bool IsStatic
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Static) != 0;
            }
        }

        internal bool IsUnsafe
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Unsafe) != 0;
            }
        }

        public sealed override bool IsAsync
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Async) != 0;
            }
        }

        internal override bool IsDeclaredReadOnly
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.ReadOnly) != 0;
            }
        }

        internal override bool IsInitOnly => false;

        internal sealed override Cci.CallingConvention CallingConvention
        {
            get
            {
                var cc = IsVararg ? Cci.CallingConvention.ExtraArguments : Cci.CallingConvention.Default;

                if (IsGenericMethod)
                {
                    cc |= Cci.CallingConvention.Generic;
                }

                if (!IsStatic)
                {
                    cc |= Cci.CallingConvention.HasThis;
                }

                return cc;
            }
        }

        #endregion

        #region Syntax

        internal (BlockSyntax blockBody, ArrowExpressionClauseSyntax arrowBody) Bodies
        {
            get
            {
                switch (SyntaxNode)
                {
                    case BaseMethodDeclarationSyntax method:
                        return (method.Body, method.ExpressionBody);

                    case AccessorDeclarationSyntax accessor:
                        return (accessor.Body, accessor.ExpressionBody);

                    case ArrowExpressionClauseSyntax arrowExpression:
                        Debug.Assert(arrowExpression.Parent.Kind() == SyntaxKind.PropertyDeclaration ||
                                     arrowExpression.Parent.Kind() == SyntaxKind.IndexerDeclaration ||
                                     this is SynthesizedClosureMethod);
                        return (null, arrowExpression);

                    case BlockSyntax block:
                        Debug.Assert(this is SynthesizedClosureMethod);
                        return (block, null);

                    default:
                        return (null, null);
                }
            }
        }

        private Binder TryGetInMethodBinder(BinderFactory binderFactoryOpt = null)
        {
            CSharpSyntaxNode contextNode = GetInMethodSyntaxNode();
            if (contextNode == null)
            {
                return null;
            }

            Binder result = (binderFactoryOpt ?? this.DeclaringCompilation.GetBinderFactory(contextNode.SyntaxTree)).GetBinder(contextNode);
#if DEBUG
            Binder current = result;
            do
            {
                if (current is InMethodBinder)
                {
                    break;
                }

                current = current.Next;
            }
            while (current != null);

            Debug.Assert(current is InMethodBinder);
#endif
            return result;
        }

        internal virtual ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            Binder inMethod = TryGetInMethodBinder(binderFactoryOpt);
            return inMethod == null ? null : new ExecutableCodeBinder(SyntaxNode, this, inMethod.WithAdditionalFlags(ignoreAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None));
        }

        /// <summary>
        /// Overridden by <see cref="SourceOrdinaryMethodSymbol"/>, 
        /// which might return locations of partial methods.
        /// </summary>
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.locations;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref this.lazyExpandedDocComment : ref this.lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        #endregion

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get
            {
                return GetTypeParametersAsTypeArguments();
            }
        }

        public sealed override int Arity
        {
            get
            {
                return TypeParameters.Length;
            }
        }

        internal sealed override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            thisParameter = _lazyThisParameter;
            if ((object)thisParameter != null || IsStatic)
            {
                return true;
            }

            Interlocked.CompareExchange(ref _lazyThisParameter, new ThisParameterSymbol(this), null);
            thisParameter = _lazyThisParameter;
            return true;
        }

        internal override TypeWithAnnotations IteratorElementTypeWithAnnotations
        {
            get
            {
                return _lazyIteratorElementType?.Value ?? default;
            }
            set
            {
                Debug.Assert(_lazyIteratorElementType == TypeWithAnnotations.Boxed.Sentinel || TypeSymbol.Equals(_lazyIteratorElementType.Value.Type, value.Type, TypeCompareKind.ConsiderEverything2));
                Interlocked.CompareExchange(ref _lazyIteratorElementType, new TypeWithAnnotations.Boxed(value), TypeWithAnnotations.Boxed.Sentinel);
            }
        }

        internal override bool IsIterator => _lazyIteratorElementType is object;

        //overridden appropriately in SourceMemberMethodSymbol
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        internal sealed override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                this.LazyMethodChecks();
                if (_lazyOverriddenOrHiddenMembers == null)
                {
                    Interlocked.CompareExchange(ref _lazyOverriddenOrHiddenMembers, this.MakeOverriddenOrHiddenMembers(), null);
                }

                return _lazyOverriddenOrHiddenMembers;
            }
        }

        internal sealed override bool RequiresCompletion
        {
            get { return true; }
        }

        internal sealed override bool HasComplete(CompletionPart part)
        {
            return state.HasComplete(part);
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.ReturnTypeAttributes:
                        this.GetReturnTypeAttributes();
                        break;

                    case CompletionPart.Type:
                        var unusedType = this.ReturnTypeWithAnnotations;
                        state.NotePartComplete(CompletionPart.Type);
                        break;

                    case CompletionPart.Parameters:
                        foreach (var parameter in this.Parameters)
                        {
                            parameter.ForceComplete(locationOpt, cancellationToken);
                        }

                        state.NotePartComplete(CompletionPart.Parameters);
                        break;

                    case CompletionPart.TypeParameters:
                        foreach (var typeParameter in this.TypeParameters)
                        {
                            typeParameter.ForceComplete(locationOpt, cancellationToken);
                        }

                        state.NotePartComplete(CompletionPart.TypeParameters);
                        break;

                    case CompletionPart.StartAsyncMethodChecks:
                    case CompletionPart.FinishAsyncMethodChecks:
                        LazyAsyncMethodChecks(cancellationToken);
                        break;

                    case CompletionPart.StartMethodChecks:
                    case CompletionPart.FinishMethodChecks:
                        LazyMethodChecks();
                        goto done;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        state.NotePartComplete(CompletionPart.All & ~CompletionPart.MethodSymbolAll);
                        break;
                }

                state.SpinWaitComplete(incompletePart, cancellationToken);
            }

done:
// Don't return until we've seen all of the CompletionParts. This ensures all
// diagnostics have been reported (not necessarily on this thread).
            CompletionPart allParts = CompletionPart.MethodSymbolAll;
            state.SpinWaitComplete(allParts, cancellationToken);
        }

        protected sealed override void NoteAttributesComplete(bool forReturnType)
        {
            var part = forReturnType ? CompletionPart.ReturnTypeAttributes : CompletionPart.Attributes;
            state.NotePartComplete(part);
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            var compilation = this.DeclaringCompilation;
            var location = locations[0];

            if (IsDeclaredReadOnly && !ContainingType.IsReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (compilation.ShouldEmitNullableAttributes(this) &&
                ShouldEmitNullableContextValue(out _))
            {
                compilation.EnsureNullableContextAttributeExists(diagnostics, location, modifyCompilation: true);
            }
        }

        // Consider moving this state to SourceMethodSymbol to emit NullableContextAttributes
        // on lambdas and local functions (see https://github.com/dotnet/roslyn/issues/36736).
        internal override byte? GetLocalNullableContextValue()
        {
            byte? value;
            if (!flags.TryGetNullableContext(out value))
            {
                value = ComputeNullableContextValue();
                flags.SetNullableContext(value);
            }
            return value;
        }

        private byte? ComputeNullableContextValue()
        {
            var compilation = DeclaringCompilation;
            if (!compilation.ShouldEmitNullableAttributes(this))
            {
                return null;
            }

            var builder = new MostCommonNullableValueBuilder();
            foreach (var typeParameter in TypeParameters)
            {
                typeParameter.GetCommonNullableValues(compilation, ref builder);
            }
            builder.AddValue(ReturnTypeWithAnnotations);
            foreach (var parameter in Parameters)
            {
                parameter.GetCommonNullableValues(compilation, ref builder);
            }
            return builder.MostCommonValue;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (IsDeclaredReadOnly && !ContainingType.IsReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }

            var compilation = this.DeclaringCompilation;

            if (compilation.ShouldEmitNullableAttributes(this) &&
                ShouldEmitNullableContextValue(out byte nullableContextValue))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableContextAttribute(this, nullableContextValue));
            }

            bool isAsync = this.IsAsync;
            bool isIterator = this.IsIterator;

            if (!isAsync && !isIterator)
            {
                return;
            }

            // The async state machine type is not synthesized until the async method body is rewritten. If we are
            // only emitting metadata the method body will not have been rewritten, and the async state machine
            // type will not have been created. In this case, omit the attribute.
            if (moduleBuilder.CompilationState.TryGetStateMachineType(this, out NamedTypeSymbol stateMachineType))
            {
                var arg = new TypedConstant(compilation.GetWellKnownType(WellKnownType.System_Type),
                    TypedConstantKind.Type, stateMachineType.GetUnboundGenericTypeOrSelf());

                if (isAsync && isIterator)
                {
                    AddSynthesizedAttribute(ref attributes,
                        compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute__ctor,
                            ImmutableArray.Create(arg)));
                }
                else if (isAsync)
                {
                    AddSynthesizedAttribute(ref attributes,
                        compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor,
                            ImmutableArray.Create(arg)));
                }
                else if (isIterator)
                {
                    AddSynthesizedAttribute(ref attributes,
                        compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor,
                            ImmutableArray.Create(arg)));
                }
            }

            if (isAsync && !isIterator)
            {
                // Regular async (not async-iterator) kick-off method calls MoveNext, which contains user code.
                // This means we need to emit DebuggerStepThroughAttribute in order
                // to have correct stepping behavior during debugging.
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerStepThroughAttribute());
            }
        }

        /// <summary>
        /// Checks to see if a body is legal given the current modifiers.
        /// If it is not, a diagnostic is added with the current type.
        /// </summary>
        protected void CheckModifiersForBody(SyntaxNode declarationSyntax, Location location, DiagnosticBag diagnostics)
        {
            if (IsExtern && !IsAbstract)
            {
                diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
            }
            else if (IsAbstract && !IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractHasBody, location, this);
            }
            // Do not report error for IsAbstract && IsExtern. Dev10 reports CS0180 only
            // in that case ("member cannot be both extern and abstract").
        }

        protected void CheckFeatureAvailabilityAndRuntimeSupport(SyntaxNode declarationSyntax, Location location, bool hasBody, DiagnosticBag diagnostics)
        {
            if (_containingType.IsInterface)
            {
                if (hasBody || IsExplicitInterfaceImplementation)
                {
                    Binder.CheckFeatureAvailability(declarationSyntax, MessageID.IDS_DefaultInterfaceImplementation, diagnostics, location);
                }

                if ((hasBody || IsExplicitInterfaceImplementation || IsExtern) && !ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, location);
                }
            }
        }

        /// <summary>
        /// Returns true if the method body is an expression, as expressed
        /// by the <see cref="ArrowExpressionClauseSyntax"/> syntax. False
        /// otherwise.
        /// </summary>
        /// <remarks>
        /// If the method has both block body and an expression body
        /// present, this is not treated as expression-bodied.
        /// </remarks>
        internal abstract bool IsExpressionBodied { get; }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            Debug.Assert(this.SyntaxNode.SyntaxTree == localTree);

            (BlockSyntax blockBody, ArrowExpressionClauseSyntax expressionBody) = Bodies;
            CSharpSyntaxNode bodySyntax = null;

            // All locals are declared within the body of the method.
            if (blockBody?.Span.Contains(localPosition) == true)
            {
                bodySyntax = blockBody;
            }
            else if (expressionBody?.Span.Contains(localPosition) == true)
            {
                bodySyntax = expressionBody;
            }
            else
            {
                // Method without body doesn't declare locals.
                Debug.Assert(bodySyntax != null);
                return -1;
            }

            return localPosition - bodySyntax.SpanStart;
        }
    }
}

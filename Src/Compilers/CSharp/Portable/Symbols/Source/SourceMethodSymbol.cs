// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceMethodSymbol : MethodSymbol, IAttributeTargetSymbol
    {
#if DEBUG
        static SourceMethodSymbol()
        {
            // Verify a few things about the values we combine into flags.  This way, if they ever
            // change, this will get hit and you will know you have to update this type as well.

            // 1) Verify that the range of method kinds doesn't fall outside the bounds of the
            // method kind mask.
            var methodKinds = EnumExtensions.GetValues<MethodKind>();
            var maxMethodKind = (int)methodKinds.Aggregate((m1, m2) => m1 | m2);
            Debug.Assert((maxMethodKind & (MethodKindMask >> MethodKindOffset)) == maxMethodKind);

            // 1) Verify that the range of declaration modifiers doesn't fall outside the bounds of
            // the declaration modifier mask.
            var declarationModifiers = EnumExtensions.GetValues<DeclarationModifiers>();
            var maxDeclarationModifier = (int)declarationModifiers.Aggregate((d1, d2) => d1 | d2);
            Debug.Assert((maxDeclarationModifier & (DeclarationModifiersMask >> DeclarationModifiersOffset)) == maxDeclarationModifier);
        }
#endif
        protected SymbolCompletionState state;

        // The flags int is used to compact many different bits of information efficiently into a 32
        // bit int.  The layout is currently:
        //
        // |   |s|r|q|z|y|xxxxxxxxxxxxxxxxxxxxx|wwwww|
        // 
        // w = method kind.  5 bits.
        //
        // x = modifiers.  21 bits.
        //
        // y = returnsVoid. 1 bit.
        //
        // z = isExtensionMethod. 1 bit.
        //
        // q = isMetadataVirtualIgnoringInterfaceChanges. 1 bit.
        //
        // r = isMetadataVirtual. 1 bit.  (At least as true as isMetadataVirtualIgnoringInterfaceChanges.)
        //
        // s = isMetadataVirtualLocked. 1 bit.
        //
        // 2 bits remain for future purposes.

        private const int MethodKindOffset = 0;
        private const int DeclarationModifiersOffset = 5;
        private const int ReturnsVoidOffset = 26;
        private const int IsExtensionMethodOffset = 27;
        private const int IsMetadataVirtualIgnoringInterfaceChangesOffset = 28;
        private const int IsMetadataVirtualOffset = 29;
        private const int IsMetadataVirtualLockedOffset = 30;

        private const int MethodKindMask = 0x1F << MethodKindOffset;
        private const int DeclarationModifiersMask = 0x1FFFFF << DeclarationModifiersOffset;
        private const int ReturnsVoidMask = 0x1 << ReturnsVoidOffset;
        private const int IsExtensionMethodMask = 0x1 << IsExtensionMethodOffset;
        private const int IsMetadataVirtualIgnoringInterfaceChangesMask = 0x1 << IsMetadataVirtualIgnoringInterfaceChangesOffset;
        private const int IsMetadataVirtualMask = 0x1 << IsMetadataVirtualOffset;
        private const int IsMetadataVirtualLockedMask = 0x1 << IsMetadataVirtualLockedOffset;

        protected int flags;

        private readonly NamedTypeSymbol containingType;
        private ParameterSymbol lazyThisParameter;
        private TypeSymbol iteratorElementType;

        private CustomAttributesBag<CSharpAttributeData> lazyCustomAttributesBag;
        private CustomAttributesBag<CSharpAttributeData> lazyReturnTypeCustomAttributesBag;

        private OverriddenOrHiddenMembersResult lazyOverriddenOrHiddenMembers;

        // syntax may not be available (some symbols have just signature, others body, or no syntax at all)
        protected readonly SyntaxReference syntaxReference;
        protected readonly SyntaxReference blockSyntaxReference;
        protected ImmutableArray<Location> locations;
        protected string lazyDocComment;

        //null if has never been computed. Initial binding diagnostics
        //are stashed here in service of API usage patterns
        //where method body diagnostics are requested multiple times.
        private ImmutableArray<Diagnostic> cachedDiagnostics;
        internal ImmutableArray<Diagnostic> Diagnostics
        {
            get { return cachedDiagnostics; }
        }

        internal ImmutableArray<Diagnostic> SetDiagnostics(ImmutableArray<Diagnostic> newSet, out bool diagsWritten)
        {
            //return the diagnostics that were actually saved in the event that there were two threads racing. 
            diagsWritten = ImmutableInterlocked.InterlockedInitialize(ref cachedDiagnostics, newSet);
            return cachedDiagnostics;
        }

        protected SourceMethodSymbol(NamedTypeSymbol containingType, SyntaxReference syntaxReference, SyntaxReference blockSyntaxReference, Location location)
            : this(containingType, syntaxReference, blockSyntaxReference, ImmutableArray.Create(location))
        {
        }

        protected SourceMethodSymbol(NamedTypeSymbol containingType, SyntaxReference syntaxReference, SyntaxReference blockSyntaxReference, ImmutableArray<Location> locations)
        {
            Debug.Assert((object)containingType != null);
            Debug.Assert(!locations.IsEmpty);

            this.containingType = containingType;
            this.syntaxReference = syntaxReference;
            this.blockSyntaxReference = blockSyntaxReference;
            this.locations = locations;
        }

        protected void CheckEffectiveAccessibility(TypeSymbol returnType, ImmutableArray<ParameterSymbol> parameters, DiagnosticBag diagnostics)
        {
            if (this.DeclaredAccessibility <= Accessibility.Private)
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
                diagnostics.Add(code, Locations[0], this, returnType);
            }

            code = (this.MethodKind == MethodKind.Conversion || this.MethodKind == MethodKind.UserDefinedOperator) ?
                ErrorCode.ERR_BadVisOpParam :
                ErrorCode.ERR_BadVisParamType;

            foreach (var parameter in parameters)
            {
                if (!parameter.Type.IsAtLeastAsVisibleAs(this, ref useSiteDiagnostics))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than method '{0}'
                    diagnostics.Add(code, Locations[0], this, parameter.Type);
                }
            }

            diagnostics.Add(Locations[0], useSiteDiagnostics);
        }

        private static bool ModifiersRequireMetadataVirtual(DeclarationModifiers modifiers)
        {
            return (modifiers & (DeclarationModifiers.Abstract | DeclarationModifiers.Virtual | DeclarationModifiers.Override)) != 0;
        }

        protected static int MakeFlags(
            MethodKind methodKind,
            DeclarationModifiers declarationModifiers,
            bool returnsVoid,
            bool isExtensionMethod,
            bool isMetadataVirtualIgnoringModifiers = false)
        {
            Debug.Assert((declarationModifiers & DeclarationModifiers.PrimaryCtor) == 0 || methodKind == MethodKind.Constructor);
            bool isMetadataVirtual = isMetadataVirtualIgnoringModifiers || ModifiersRequireMetadataVirtual(declarationModifiers);

            int methodKindInt = ((int)methodKind << MethodKindOffset) & MethodKindMask;
            int declarationModifiersInt = ((int)declarationModifiers << DeclarationModifiersOffset) & DeclarationModifiersMask;
            int returnsVoidInt = returnsVoid ? ReturnsVoidMask : 0;
            int isExtensionMethodInt = isExtensionMethod ? IsExtensionMethodMask : 0;
            int isMetadataVirtualIgnoringInterfaceImplementationChangesInt = isMetadataVirtual ? IsMetadataVirtualIgnoringInterfaceChangesMask : 0;
            int isMetadataVirtualInt = isMetadataVirtual ? IsMetadataVirtualMask : 0;
            int isMetadataVirtualLockedInt = 0;

            return methodKindInt | declarationModifiersInt | returnsVoidInt | isExtensionMethodInt | isMetadataVirtualIgnoringInterfaceImplementationChangesInt | isMetadataVirtualInt | isMetadataVirtualLockedInt;
        }

        protected static int MakeReturnsVoidFlags(bool returnsVoid)
        {
            return returnsVoid ? ReturnsVoidMask : 0;
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
            get { return this.syntaxReference; }
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
                            AddSemanticDiagnostics(diagnostics);
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

                        // Our normal pattern would be to wait for FinishMethodChecks to be set, but profiling
                        // showed too many threads spinning at this location, so we switched to a lock.  Also,
                        // the comment above seems to indicate that we might deadlock if we followed this pattern.
                        // state.SpinWaitComplete(CompletionPart.FinishMethodChecks, CancellationToken.None)
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
                return this.containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
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
                return (this.flags & ReturnsVoidMask) != 0;
            }
        }

        public sealed override MethodKind MethodKind
        {
            get
            {
                return (MethodKind)((this.flags & MethodKindMask) >> MethodKindOffset);
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return (this.flags & IsExtensionMethodMask) != 0;
            }
        }

        private bool IsMetadataVirtualLocked
        {
            get
            {
                return (this.flags & IsMetadataVirtualLockedMask) != 0;
            }
        }

        // TODO (tomat): sealed
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
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
            // This flag is immutable, so there's no reason to set a lock bit, as we do below.
            if (ignoreInterfaceImplementationChanges)
            {
                return (this.flags & IsMetadataVirtualIgnoringInterfaceChangesMask) != 0;
            }

            if (!IsMetadataVirtualLocked)
            {
                ThreadSafeFlagOperations.Set(ref this.flags, IsMetadataVirtualLockedMask);
            }

            return (this.flags & IsMetadataVirtualMask) != 0;
        }

        internal void EnsureMetadataVirtual()
        {
            // ACASEY: This assert is here to check that we're not mutating the value of IsMetadataVirtual after
            // someone has consumed it.  The best practice is to not access IsMetadataVirtual before ForceComplete
            // has been called on all SourceNamedTypeSymbols.  If it is necessary to do so, then you can pass
            // ignoreInterfaceImplementationChanges: true, but you must be conscious that seeing "false" may not
            // reflect the final, emitted modifier.
            Debug.Assert(!IsMetadataVirtualLocked);
            if ((this.flags & IsMetadataVirtualMask) == 0)
            {
                ThreadSafeFlagOperations.Set(ref this.flags, IsMetadataVirtualMask);
            }
        }

        /// <summary>
        /// This method indicates whether or not the runtime will regard the method
        /// as final (as indicated by the presence of the "final" modifier in the
        /// signature).
        /// NOTE: The method is supposed to be called ONLY from emitter.
        /// </summary>
        internal override bool IsMetadataFinal()
        {
            return ((Cci.IMethodDefinition)this).IsSealed;
        }

        protected DeclarationModifiers DeclarationModifiers
        {
            get
            {
                return (DeclarationModifiers)((this.flags & DeclarationModifiersMask) >> DeclarationModifiersOffset);
            }
        }

        // TODO (tomat): sealed?
        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(this.DeclarationModifiers);
            }
        }

        public sealed override bool IsExtern
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Extern) != 0;
            }
        }

        /// <summary>
        /// Is this a declaration of a Primary constructor?
        /// Note, more than one constructor in a type can return true for this property.
        /// This happens for an error situation when more than one partial declaration declares Primary
        /// Constructor. We will create a constructor symbol for each of them and all of them will have
        /// IsPrimaryCtor == true. However, The first one that we create will be the "main" Primary Constructor,
        /// SourceMemberContainerTypeSymbol.PrimaryCtor property returns it.
        /// </summary>
        public bool IsPrimaryCtor
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.PrimaryCtor) != 0;
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

        internal BlockSyntax BlockSyntax
        {
            get
            {
                return (this.blockSyntaxReference == null) ? null : this.blockSyntaxReference.GetSyntax() as BlockSyntax;
            }
        }

        internal SyntaxReference SyntaxRef
        {
            get
            {
                return this.syntaxReference;
            }
        }

        internal CSharpSyntaxNode SyntaxNode
        {
            get
            {
                return this.syntaxReference == null ? null : (CSharpSyntaxNode)this.syntaxReference.GetSyntax();
            }
        }

        internal SyntaxTree SyntaxTree
        {
            get
            {
                return this.syntaxReference == null ? null : this.syntaxReference.SyntaxTree;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return new LexicalSortKey(locations[0], this.DeclaringCompilation);
        }

        /// <summary>
        /// Overridden by <see cref="SourceMemberMethodSymbol"/>, 
        /// which might return locations of partial methods.
        /// </summary>
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.locations;
            }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                CSharpSyntaxNode node = this.SyntaxNode;
                return (node == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create<SyntaxReference>(node.GetReference());
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        #endregion

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public sealed override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
            }
        }

        public sealed override int Arity
        {
            get
            {
                return TypeParameters.Length;
            }
        }

        internal sealed override ParameterSymbol ThisParameter
        {
            get
            {
                var thisParam = lazyThisParameter;
                if ((object)thisParam != null || IsStatic)
                {
                    return thisParam;
                }

                    Interlocked.CompareExchange(ref lazyThisParameter, new ThisParameterSymbol(this), null);
                return lazyThisParameter;
            }
        }

        internal override TypeSymbol IteratorElementType
        {
            get
            {
                return iteratorElementType;
            }
            set
            {
                Debug.Assert((object)iteratorElementType == null || iteratorElementType == value);
                Interlocked.CompareExchange(ref iteratorElementType, value, null);
            }
        }

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
                if (this.lazyOverriddenOrHiddenMembers == null)
                {
                    Interlocked.CompareExchange(ref this.lazyOverriddenOrHiddenMembers, this.MakeOverriddenOrHiddenMembers(), null);
                }

                return this.lazyOverriddenOrHiddenMembers;
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
                        var unusedType = this.ReturnType;
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

        #region Attributes

        /// <summary>
        /// Symbol to copy bound attributes from, or null if the attributes are not shared among multiple source method symbols.
        /// </summary>
        /// <remarks>
        /// Used for example for event accessors. The "remove" method delegates attribute binding to the "add" method. 
        /// The bound attribute data are then applied to both accessors.
        /// </remarks>
        protected virtual SourceMethodSymbol BoundAttributesSource
        {
            get
            {
                return null;
            }
        }

        protected virtual IAttributeTargetSymbol AttributeOwner
        {
            get { return this; }
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this.AttributeOwner; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Method; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get
            {
                switch (MethodKind)
                {
                    case MethodKind.Constructor:
                    case MethodKind.Destructor:
                    case MethodKind.StaticConstructor:
                        return AttributeLocation.Method;

                    case MethodKind.PropertySet:
                    case MethodKind.EventRemove:
                    case MethodKind.EventAdd:
                        return AttributeLocation.Method | AttributeLocation.Return | AttributeLocation.Parameter;

                    default:
                        return AttributeLocation.Method | AttributeLocation.Return;
                }
            }
        }

        /// <summary>
        /// Gets the syntax list of custom attributes that declares atributes for this method symbol.
        /// </summary>
        internal virtual OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
        }

        /// <summary>
        /// Gets the syntax list of custom attributes that declares atributes for return type of this method.
        /// </summary>
        internal virtual OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
        {
            // Usually the same list as other attributes applied on the method, but e.g.
            // constructors and destructors do not allow return-type attributes, so this is empty.
            return GetAttributeDeclarations();
        }

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonMethodEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = this.lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonMethodEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonMethodWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = this.lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonMethodWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns information retrieved from custom attributes on return type in source, or null if the symbol is not source symbol or there are none.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonReturnTypeWellKnownAttributeData GetDecodedReturnTypeWellKnownAttributeData()
        {
            var attributesBag = this.lazyReturnTypeCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetReturnTypeAttributesBag();
            }

            return (CommonReturnTypeWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            var bag = this.lazyCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            return GetAttributesBag(ref lazyCustomAttributesBag, forReturnType: false);
        }

        /// <summary>
        /// Returns a bag of custom attributes applied on the method return value and data decoded from well-known attributes. Returns null if there are no attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetReturnTypeAttributesBag()
        {
            var bag = this.lazyReturnTypeCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            return GetAttributesBag(ref lazyReturnTypeCustomAttributesBag, forReturnType: true);
        }

        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag(ref CustomAttributesBag<CSharpAttributeData> lazyCustomAttributesBag, bool forReturnType)
        {
            SourceMethodSymbol copyFrom = this.BoundAttributesSource;

            // prevent infinite recursion:
            Debug.Assert(!ReferenceEquals(copyFrom, this));

            bool bagCreatedOnThisThread;
            if ((object)copyFrom != null)
            {
                var attributesBag = forReturnType ? copyFrom.GetReturnTypeAttributesBag() : copyFrom.GetAttributesBag();
                bagCreatedOnThisThread = Interlocked.CompareExchange(ref lazyCustomAttributesBag, attributesBag, null) == null;
            }
            else if (forReturnType)
            {
                bagCreatedOnThisThread = LoadAndValidateAttributes(this.GetReturnTypeAttributeDeclarations(), ref lazyCustomAttributesBag, symbolPart: AttributeLocation.Return);
            }
            else
            {
                bagCreatedOnThisThread = LoadAndValidateAttributes(this.GetAttributeDeclarations(), ref lazyCustomAttributesBag);
            }

            var part = forReturnType ? CompletionPart.ReturnTypeAttributes : CompletionPart.Attributes;
            state.NotePartComplete(part);
            return lazyCustomAttributesBag;
        }

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag().Attributes;
        }

        /// <summary>
        /// Gets the attributes applied on the return value of this method symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public sealed override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return this.GetReturnTypeAttributesBag().Attributes;
        }

        internal override void AddSynthesizedReturnTypeAttributes(ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedReturnTypeAttributes(ref attributes);

            if (this.ReturnType.ContainsDynamic())
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.ReturnType, this.ReturnTypeCustomModifiers.Length));
            }
        }

        internal override CSharpAttributeData EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None || arguments.SymbolPart == AttributeLocation.Return);

            bool hasAnyDiagnostics;

            if (arguments.SymbolPart == AttributeLocation.None)
            {
                if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute))
                {
                    var boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                    if (!boundAttribute.HasErrors)
                    {
                        string name = boundAttribute.GetConstructorArgument<string>(0, SpecialType.System_String);
                        arguments.GetOrCreateData<CommonMethodEarlyWellKnownAttributeData>().AddConditionalSymbol(name);
                        if (!hasAnyDiagnostics)
                        {
                            return boundAttribute;
                        }
                    }

                    return null;
                }
                else 
                {
                    CSharpAttributeData boundAttribute;
                    ObsoleteAttributeData obsoleteData;

                    if (EarlyDecodeDeprecatedOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
                    {
                        if (obsoleteData != null)
                        {
                            arguments.GetOrCreateData<CommonMethodEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                        }

                            return boundAttribute;
                        }
                    }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                var sourceContainer = ContainingType as SourceMemberContainerTypeSymbol;
                if ((object)sourceContainer == null || !sourceContainer.AnyMemberHasAttributes)
                {
                    return null;
                }

                var lazyCustomAttributesBag = this.lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    var data = (CommonMethodEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
                    return data != null ? data.ObsoleteAttributeData : null;
                }

                var reference = this.syntaxReference;
                if (reference == null)
                {
                    // no references -> no attributes
                    return null;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            CommonMethodEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
            return data != null ? data.ConditionalSymbols : ImmutableArray<string>.Empty;
        }

        internal override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(!arguments.Attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None || arguments.SymbolPart == AttributeLocation.Return);

            if (arguments.SymbolPart == AttributeLocation.None)
            {
                DecodeWellKnownAttributeAppliedToMethod(ref arguments);
            }
            else
            {
                DecodeWellKnownAttributeAppliedToReturnValue(ref arguments);
            }
        }

        private void DecodeWellKnownAttributeAppliedToMethod(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);

            if (attribute.IsTargetAttribute(this, AttributeDescription.PreserveSigAttribute))
            {
                arguments.GetOrCreateData<CommonMethodWellKnownAttributeData>().SetPreserveSignature(arguments.Index);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MethodImplAttribute))
            {
                AttributeData.DecodeMethodImplAttribute<CommonMethodWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>(ref arguments, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DllImportAttribute))
            {
                DecodeDllImportAttribute(ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<CommonMethodWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ConditionalAttribute))
            {
                ValidateConditionalAttribute(attribute, arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SuppressUnmanagedCodeSecurityAttribute))
            {
                arguments.GetOrCreateData<CommonMethodWellKnownAttributeData>().HasSuppressUnmanagedCodeSecurityAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicSecurityMethodAttribute))
            {
                arguments.GetOrCreateData<CommonMethodWellKnownAttributeData>().HasDynamicSecurityMethodAttribute = true;
            }
            else if (VerifyObsoleteAttributeAppliedToMethod(ref arguments, AttributeDescription.ObsoleteAttribute))
            {
            }
            else if (VerifyObsoleteAttributeAppliedToMethod(ref arguments, AttributeDescription.DeprecatedAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CaseSensitiveExtensionAttribute))
            {
                // [Extension] attribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitExtension, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SecurityCriticalAttribute)
                || attribute.IsTargetAttribute(this, AttributeDescription.SecuritySafeCriticalAttribute))
            {
                if (IsAsync)
                {
                    arguments.Diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, arguments.AttributeSyntaxOpt.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                }
            }
            else
            {
                var compilation = this.DeclaringCompilation;
                if (attribute.IsSecurityAttribute(compilation))
                {
                    attribute.DecodeSecurityAttribute<CommonMethodWellKnownAttributeData>(this, compilation, ref arguments);
                }
            }
        }

        private bool VerifyObsoleteAttributeAppliedToMethod(
            ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments,
            AttributeDescription description)
        {
            if (arguments.Attribute.IsTargetAttribute(this, description))
        {
            if (this.IsAccessor())
            {
                // CS1667: Attribute '{0}' is not valid on property or event accessors. It is only valid on '{1}' declarations.
                    AttributeUsageInfo attributeUsage = arguments.Attribute.AttributeClass.GetAttributeUsageInfo();
                    arguments.Diagnostics.Add(ErrorCode.ERR_AttributeNotOnAccessor, arguments.AttributeSyntaxOpt.Name.Location, description.FullName, attributeUsage.GetValidTargetsString());
                }

                return true;
            }

            return false;
        }

        private void ValidateConditionalAttribute(CSharpAttributeData attribute, AttributeSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsConditional);

            if (this.IsAccessor())
            {
                // CS1667: Attribute '{0}' is not valid on property or event accessors. It is only valid on '{1}' declarations.
                AttributeUsageInfo attributeUsage = attribute.AttributeClass.GetAttributeUsageInfo();
                diagnostics.Add(ErrorCode.ERR_AttributeNotOnAccessor, node.Name.Location, node.GetErrorDisplayName(), attributeUsage.GetValidTargetsString());
            }
            else if (this.ContainingType.IsInterfaceType())
            {
                // CS0582: The Conditional attribute is not valid on interface members
                diagnostics.Add(ErrorCode.ERR_ConditionalOnInterfaceMethod, node.Location);
            }
            else if (this.IsOverride)
            {
                // CS0243: The Conditional attribute is not valid on '{0}' because it is an override method
                diagnostics.Add(ErrorCode.ERR_ConditionalOnOverride, node.Location, this);
            }
            else if (!this.CanBeReferencedByName || this.MethodKind == MethodKind.Destructor)
            {
                // CS0577: The Conditional attribute is not valid on '{0}' because it is a constructor, destructor, operator, or explicit interface implementation
                diagnostics.Add(ErrorCode.ERR_ConditionalOnSpecialMethod, node.Location, this);
            }
            else if (!this.ReturnsVoid)
            {
                // CS0578: The Conditional attribute is not valid on '{0}' because its return type is not void
                diagnostics.Add(ErrorCode.ERR_ConditionalMustReturnVoid, node.Location, this);
            }
            else if (this.HasAnyOutParameter())
            {
                // CS0685: Conditional member '{0}' cannot have an out parameter
                diagnostics.Add(ErrorCode.ERR_ConditionalWithOutParam, node.Location, this);
            }
            else
            {
                string name = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);

                if (name == null || !SyntaxFacts.IsValidIdentifier(name))
                {
                    // CS0633: The argument to the '{0}' attribute must be a valid identifier
                    CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, node);
                    diagnostics.Add(ErrorCode.ERR_BadArgumentToAttribute, attributeArgumentSyntax.Location, node.GetErrorDisplayName());
                }
            }
        }

        private bool HasAnyOutParameter()
        {
            foreach (var param in this.Parameters)
            {
                if (param.RefKind == RefKind.Out)
                {
                    return true;
                }
            }

            return false;
        }

        private void DecodeWellKnownAttributeAppliedToReturnValue(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);

            if (attribute.IsTargetAttribute(this, AttributeDescription.MarshalAsAttribute))
            {
                // MarshalAs applied to the return value:
                MarshalAsAttributeDecoder<CommonReturnTypeWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.ReturnValue, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
        }

        private void DecodeDllImportAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            bool hasErrors = false;

            if (!this.IsExtern || !this.IsStatic)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_DllImportOnInvalidMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            if (this.IsGenericMethod || (object)containingType != null && containingType.IsGenericType)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_DllImportOnGenericMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            string moduleName = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);
            if (!MetadataHelpers.IsValidMetadataIdentifier(moduleName))
            {
                // Dev10 reports CS0647: "Error emitting attribute ..."
                CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, arguments.AttributeSyntaxOpt);
                arguments.Diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntax.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                hasErrors = true;
                moduleName = null;
            }

            // Default value of charset is inherited from the module (only if specified).
            // This might be different from ContainingType.DefaultMarshallingCharSet. If the charset is not specified on module
            // ContainingType.DefaultMarshallingCharSet would be Ansi (the class is emitted with "Ansi" charset metadata flag) 
            // while the charset in P/Invoke metadata should be "None".
            CharSet charSet = this.GetEffectiveDefaultMarshallingCharSet() ?? Cci.Constants.CharSet_None;

            string importName = null;
            bool preserveSig = true;
            CallingConvention callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi;
            bool setLastError = false;
            bool exactSpelling = false;  // C#: ExactSpelling=false for any charset
            bool? bestFitMapping = null;
            bool? throwOnUnmappable = null;

            int position = 1;
            foreach (var namedArg in attribute.CommonNamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "EntryPoint":
                        importName = namedArg.Value.Value as string;
                        if (!MetadataHelpers.IsValidMetadataIdentifier(importName))
                        {
                            // Dev10 reports CS0647: "Error emitting attribute ..."
                            arguments.Diagnostics.Add(ErrorCode.ERR_InvalidNamedArgument, arguments.AttributeSyntaxOpt.ArgumentList.Arguments[position].Location, namedArg.Key);
                            hasErrors = true;
                            importName = null;
                        }

                        break;

                    case "CharSet":
                        // invalid values will be ignored
                        charSet = namedArg.Value.DecodeValue<CharSet>(SpecialType.System_Enum);
                        break;

                    case "SetLastError":
                        // invalid values will be ignored
                        setLastError = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "ExactSpelling":
                        // invalid values will be ignored
                        exactSpelling = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "PreserveSig":
                        preserveSig = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "CallingConvention":
                        // invalid values will be ignored
                        callingConvention = namedArg.Value.DecodeValue<CallingConvention>(SpecialType.System_Enum);
                        break;

                    case "BestFitMapping":
                        bestFitMapping = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "ThrowOnUnmappableChar":
                        throwOnUnmappable = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;
                }

                position++;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<CommonMethodWellKnownAttributeData>().SetDllImport(
                    arguments.Index,
                    moduleName,
                    importName,
                    DllImportData.MakeFlags(
                        exactSpelling,
                        charSet,
                        setLastError,
                        callingConvention,
                        bestFitMapping,
                        throwOnUnmappable),
                    preserveSig);
            }
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(symbolPart == AttributeLocation.None || symbolPart == AttributeLocation.Return);

            if (symbolPart != AttributeLocation.Return)
            {
                Debug.Assert(this.lazyCustomAttributesBag != null);
                Debug.Assert(this.lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);

                if (this.containingType.IsComImport && this.containingType.TypeKind == TypeKind.Class)
                {
                    switch (this.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            if (!this.IsImplicitlyDeclared)
                            {
                                // CS0669: A class with the ComImport attribute cannot have a user-defined constructor
                                diagnostics.Add(ErrorCode.ERR_ComImportWithUserCtor, this.Locations[0]);
                            }

                            break;

                        default:
                            if (!this.IsAbstract && !this.IsExtern)
                            {
                                // CS0423: Since '{1}' has the ComImport attribute, '{0}' must be extern or abstract
                                diagnostics.Add(ErrorCode.ERR_ComImportWithImpl, this.Locations[0], this, this.containingType);
                            }

                            break;
                    }
                }
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        public sealed override bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return base.HasRuntimeSpecialName || IsVtableGapInterfaceMethod();
            }
        }

        private bool IsVtableGapInterfaceMethod()
        {
            return this.ContainingType.IsInterface &&
                   ModuleExtensions.GetVTableGapSize(this.MetadataName) > 0;
        }

        internal sealed override bool HasSpecialName
        {
            get
            {
                switch (this.MethodKind)
                {
                    case MethodKind.Constructor:
                    case MethodKind.StaticConstructor:
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                    case MethodKind.UserDefinedOperator:
                    case MethodKind.Conversion:
                        return true;
                }

                if (IsVtableGapInterfaceMethod())
                {
                    return true;
                }

                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        internal sealed override bool RequiresSecurityObject
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasDynamicSecurityMethodAttribute;
            }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data != null && data.HasDeclarativeSecurity;
            }
        }

        internal sealed override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            var attributesBag = this.GetAttributesBag();
            var wellKnownData = (CommonMethodWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
            if (wellKnownData != null)
            {
                SecurityWellKnownAttributeData securityData = wellKnownData.SecurityInformation;
                if (securityData != null)
                {
                    return securityData.GetSecurityAttributes(attributesBag.Attributes);
                }
            }

            return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
        }

        public sealed override DllImportData GetDllImportData()
        {
            var data = this.GetDecodedWellKnownAttributeData();
            return data != null ? data.DllImportPlatformInvokeData : null;
        }

        internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                var data = this.GetDecodedReturnTypeWellKnownAttributeData();
                return data != null ? data.MarshallingInformation : null;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                var result = (data != null) ? data.MethodImplAttributes : default(System.Reflection.MethodImplAttributes);

                if (this.ContainingType.IsComImport && this.MethodKind == MethodKind.Constructor)
                {
                    // Synthesized constructor of ComImport types is marked as Runtime implemented and InternalCall
                    result |= (System.Reflection.MethodImplAttributes.Runtime | System.Reflection.MethodImplAttributes.InternalCall);
                }

                return result;
            }
        }

        #endregion

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            if (this.IsAsync)
            {
                var compilation = this.DeclaringCompilation;

                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerStepThroughAttribute__ctor));

                // The async state machine type is not synthesized until the async method body is rewritten. If we are
                // only emitting metadata the method body will not have been rewritten, and the async state machine
                // type will not have been created. In this case, omit the AsyncStateMachineAttribute.
                NamedTypeSymbol asyncStateMachineType;
                if (compilationState.TryGetStateMachineType(this, out asyncStateMachineType))
                {
                    AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(
                       WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor,
                       ImmutableArray.Create(
                           new TypedConstant(
                               compilation.GetWellKnownType(WellKnownType.System_Type),
                               TypedConstantKind.Type,
                               asyncStateMachineType.ConstructUnboundGenericType()))));   
                }
            }
        }
    }
}

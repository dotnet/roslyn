// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a named type symbol whose members are declared in source.
    /// </summary>
    internal abstract partial class SourceMemberContainerTypeSymbol : NamedTypeSymbol
    {
        // The flags type is used to compact many different bits of information efficiently.
        private struct Flags
        {
            // We current pack everything into two 32-bit ints; layouts for each are given below.

            // First int:
            //
            // |  |d|yy|xxxxxxxxxxxxxxxxxxxxx|wwwwww|
            //
            // w = special type.  6 bits.
            // x = modifiers.  21 bits.
            // y = IsManagedType.  2 bits.
            // d = FieldDefinitionsNoted. 1 bit
            private const int SpecialTypeOffset = 0;
            private const int DeclarationModifiersOffset = 6;
            private const int IsManagedTypeOffset = 26;

            private const int SpecialTypeMask = 0x3F;
            private const int DeclarationModifiersMask = 0x1FFFFF;
            private const int IsManagedTypeMask = 0x3;

            private const int FieldDefinitionsNotedBit = 1 << 28;

            private int _flags;

            // More flags.
            //
            // |                           |zzzz|f|
            //
            // f = FlattenedMembersIsSorted.  1 bit.
            // z = TypeKind. 4 bits.
            private const int TypeKindOffset = 1;

            private const int TypeKindMask = 0xF;

            private const int FlattenedMembersIsSortedBit = 1 << 0;

            private int _flags2;

            public SpecialType SpecialType
            {
                get { return (SpecialType)((_flags >> SpecialTypeOffset) & SpecialTypeMask); }
            }

            public DeclarationModifiers DeclarationModifiers
            {
                get { return (DeclarationModifiers)((_flags >> DeclarationModifiersOffset) & DeclarationModifiersMask); }
            }

            public ThreeState IsManagedType
            {
                get { return (ThreeState)((_flags >> IsManagedTypeOffset) & IsManagedTypeMask); }
            }

            public bool FieldDefinitionsNoted
            {
                get { return (_flags & FieldDefinitionsNotedBit) != 0; }
            }

            // True if "lazyMembersFlattened" is sorted.
            public bool FlattenedMembersIsSorted
            {
                get { return (_flags2 & FlattenedMembersIsSortedBit) != 0; }
            }

            public TypeKind TypeKind
            {
                get { return (TypeKind)((_flags2 >> TypeKindOffset) & TypeKindMask); }
            }


#if DEBUG
            static Flags()
            {
                // Verify a few things about the values we combine into flags.  This way, if they ever
                // change, this will get hit and you will know you have to update this type as well.

                // 1) Verify that the range of special types doesn't fall outside the bounds of the
                // special type mask.
                var specialTypes = EnumExtensions.GetValues<SpecialType>();
                var maxSpecialType = (int)specialTypes.Aggregate((s1, s2) => s1 | s2);
                Debug.Assert((maxSpecialType & SpecialTypeMask) == maxSpecialType);

                // 1) Verify that the range of declaration modifiers doesn't fall outside the bounds of
                // the declaration modifier mask.
                var declarationModifiers = EnumExtensions.GetValues<DeclarationModifiers>();
                var maxDeclarationModifier = (int)declarationModifiers.Aggregate((d1, d2) => d1 | d2);
                Debug.Assert((maxDeclarationModifier & DeclarationModifiersMask) == maxDeclarationModifier);
            }
#endif

            public Flags(SpecialType specialType, DeclarationModifiers declarationModifiers, TypeKind typeKind)
            {
                int specialTypeInt = ((int)specialType & SpecialTypeMask) << SpecialTypeOffset;
                int declarationModifiersInt = ((int)declarationModifiers & DeclarationModifiersMask) << DeclarationModifiersOffset;
                int typeKindInt = ((int)typeKind & TypeKindMask) << TypeKindOffset;

                _flags = specialTypeInt | declarationModifiersInt;
                _flags2 = typeKindInt;
            }

            public void SetFieldDefinitionsNoted()
            {
                ThreadSafeFlagOperations.Set(ref _flags, FieldDefinitionsNotedBit);
            }

            public void SetFlattenedMembersIsSorted()
            {
                ThreadSafeFlagOperations.Set(ref _flags2, (FlattenedMembersIsSortedBit));
            }

            private static bool BitsAreUnsetOrSame(int bits, int mask)
            {
                return (bits & mask) == 0 || (bits & mask) == mask;
            }

            public void SetIsManagedType(bool isManagedType)
            {
                int bitsToSet = ((int)isManagedType.ToThreeState() & IsManagedTypeMask) << IsManagedTypeOffset;
                Debug.Assert(BitsAreUnsetOrSame(_flags, bitsToSet));
                ThreadSafeFlagOperations.Set(ref _flags, bitsToSet);
            }
        }

        protected SymbolCompletionState state;

        private Flags _flags;

        private readonly NamespaceOrTypeSymbol _containingSymbol;
        protected readonly MergedTypeDeclaration declaration;

        private MembersAndInitializers _lazyMembersAndInitializers;
        private Dictionary<string, ImmutableArray<Symbol>> _lazyMembersDictionary;
        private Dictionary<string, ImmutableArray<Symbol>> _lazyEarlyAttributeDecodingMembersDictionary;

        private static readonly Dictionary<string, ImmutableArray<NamedTypeSymbol>> s_emptyTypeMembers = new Dictionary<string, ImmutableArray<NamedTypeSymbol>>();
        private Dictionary<string, ImmutableArray<NamedTypeSymbol>> _lazyTypeMembers;
        private ImmutableArray<Symbol> _lazyMembersFlattened;
        private ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> _lazySynthesizedExplicitImplementations;
        private int _lazyKnownCircularStruct;
        private LexicalSortKey _lazyLexicalSortKey = LexicalSortKey.NotInitialized;

        private ThreeState _lazyContainsExtensionMethods;
        private ThreeState _lazyAnyMemberHasAttributes;

        #region Construction

        internal SourceMemberContainerTypeSymbol(
            NamespaceOrTypeSymbol containingSymbol,
            MergedTypeDeclaration declaration,
            DiagnosticBag diagnostics)
        {
            _containingSymbol = containingSymbol;
            this.declaration = declaration;

            TypeKind typeKind = declaration.Kind.ToTypeKind();
            var modifiers = MakeModifiers(typeKind, diagnostics);

            int access = (int)(modifiers & DeclarationModifiers.AccessibilityMask);
            if ((access & (access - 1)) != 0)
            {   // more than one access modifier
                if ((modifiers & DeclarationModifiers.Partial) != 0)
                    diagnostics.Add(ErrorCode.ERR_PartialModifierConflict, Locations[0], this);
                access = access & ~(access - 1); // narrow down to one access modifier
                modifiers &= ~DeclarationModifiers.AccessibilityMask; // remove them all
                modifiers |= (DeclarationModifiers)access; // except the one
            }

            var specialType = access == (int)DeclarationModifiers.Public
                ? MakeSpecialType()
                : SpecialType.None;

            _flags = new Flags(specialType, modifiers, typeKind);

            var containingType = this.ContainingType;
            if ((object)containingType != null && containingType.IsSealed && this.DeclaredAccessibility.HasProtected())
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), Locations[0], this);
            }

            state.NotePartComplete(CompletionPart.TypeArguments); // type arguments need not be computed separately
        }

        private SpecialType MakeSpecialType()
        {
            // check if this is one of the COR library types
            if (ContainingSymbol.Kind == SymbolKind.Namespace &&
                ContainingSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes)
            {
                //for a namespace, the emitted name is a dot-separated list of containing namespaces
                var emittedName = ContainingSymbol.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
                emittedName = MetadataHelpers.BuildQualifiedName(emittedName, MetadataName);

                return SpecialTypes.GetTypeFromMetadataName(emittedName);
            }
            else
            {
                return SpecialType.None;
            }
        }

        private DeclarationModifiers MakeModifiers(TypeKind typeKind, DiagnosticBag diagnostics)
        {
            var defaultAccess = this.ContainingSymbol is NamespaceSymbol
                ? DeclarationModifiers.Internal
                : DeclarationModifiers.Private;

            var allowedModifiers = DeclarationModifiers.AccessibilityMask | DeclarationModifiers.Partial;

            if (ContainingSymbol is TypeSymbol)
            {
                if (ContainingType.IsInterface)
                {
                    allowedModifiers |= DeclarationModifiers.All;
                }
                else
                {
                    allowedModifiers |= DeclarationModifiers.New;
                }
            }

            switch (typeKind)
            {
                case TypeKind.Class:
                case TypeKind.Submission:
                    // static, sealed, and abstract allowed if a class
                    allowedModifiers |= DeclarationModifiers.Static | DeclarationModifiers.Sealed | DeclarationModifiers.Abstract | DeclarationModifiers.Unsafe;
                    break;
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Delegate:
                    allowedModifiers |= DeclarationModifiers.Unsafe;
                    break;
            }

            bool modifierErrors;
            var mods = MakeAndCheckTypeModifiers(
                defaultAccess,
                allowedModifiers,
                this,
                diagnostics,
                out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            if (!modifierErrors &&
                (mods & DeclarationModifiers.Abstract) != 0 &&
                (mods & (DeclarationModifiers.Sealed | DeclarationModifiers.Static)) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractSealedStatic, Locations[0], this);
            }

            if (!modifierErrors &&
                (mods & (DeclarationModifiers.Sealed | DeclarationModifiers.Static)) == (DeclarationModifiers.Sealed | DeclarationModifiers.Static))
            {
                diagnostics.Add(ErrorCode.ERR_SealedStaticClass, Locations[0], this);
            }

            switch (typeKind)
            {
                case TypeKind.Interface:
                    mods |= DeclarationModifiers.Abstract;
                    break;
                case TypeKind.Struct:
                case TypeKind.Enum:
                    mods |= DeclarationModifiers.Sealed;
                    break;
                case TypeKind.Delegate:
                    mods |= DeclarationModifiers.Sealed;
                    break;
            }

            return mods;
        }

        private DeclarationModifiers MakeAndCheckTypeModifiers(
            DeclarationModifiers defaultAccess,
            DeclarationModifiers allowedModifiers,
            SourceMemberContainerTypeSymbol self,
            DiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            modifierErrors = false;

            var result = DeclarationModifiers.Unset;
            var partCount = declaration.Declarations.Length;
            var missingPartial = false;

            for (var i = 0; i < partCount; i++)
            {
                var mods = declaration.Declarations[i].Modifiers;

                if (partCount > 1 && (mods & DeclarationModifiers.Partial) == 0)
                {
                    missingPartial = true;
                }

                if (result == DeclarationModifiers.Unset)
                {
                    result = mods;
                    continue;
                }

                result |= mods;
            }

            result = ModifierUtils.CheckModifiers(result, allowedModifiers, self.Locations[0], diagnostics, out modifierErrors);

            if ((result & DeclarationModifiers.AccessibilityMask) == 0)
            {
                result |= defaultAccess;
            }

            if (missingPartial)
            {
                if ((result & DeclarationModifiers.Partial) == 0)
                {
                    // duplicate definitions
                    switch (self.ContainingSymbol.Kind)
                    {
                        case SymbolKind.Namespace:
                            for (var i = 1; i < partCount; i++)
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateNameInNS, declaration.Declarations[i].NameLocation, self.Name, self.ContainingSymbol);
                                modifierErrors = true;
                            }
                            break;

                        case SymbolKind.NamedType:
                            for (var i = 1; i < partCount; i++)
                            {
                                if (ContainingType.Locations.Length == 1 || ContainingType.IsPartial())
                                    diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, declaration.Declarations[i].NameLocation, self.ContainingSymbol, self.Name);
                                modifierErrors = true;
                            }
                            break;
                    }
                }
                else
                {
                    for (var i = 0; i < partCount; i++)
                    {
                        var singleDeclaration = declaration.Declarations[i];
                        var mods = singleDeclaration.Modifiers;
                        if ((mods & DeclarationModifiers.Partial) == 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_MissingPartial, singleDeclaration.NameLocation, self.Name);
                            modifierErrors = true;
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Completion

        internal sealed override bool RequiresCompletion
        {
            get { return true; }
        }

        internal sealed override bool HasComplete(CompletionPart part)
        {
            return state.HasComplete(part);
        }

        protected abstract void CheckBase(DiagnosticBag diagnostics);
        protected abstract void CheckInterfaces(DiagnosticBag diagnostics);

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                // NOTE: cases that depend on GetMembers[ByName] should call RequireCompletionPartMembers.
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.StartBaseType:
                    case CompletionPart.FinishBaseType:
                        if (state.NotePartComplete(CompletionPart.StartBaseType))
                        {
                            var diagnostics = DiagnosticBag.GetInstance();
                            CheckBase(diagnostics);
                            AddDeclarationDiagnostics(diagnostics);
                            state.NotePartComplete(CompletionPart.FinishBaseType);
                            diagnostics.Free();
                        }
                        break;

                    case CompletionPart.StartInterfaces:
                    case CompletionPart.FinishInterfaces:
                        if (state.NotePartComplete(CompletionPart.StartInterfaces))
                        {
                            var diagnostics = DiagnosticBag.GetInstance();
                            CheckInterfaces(diagnostics);
                            AddDeclarationDiagnostics(diagnostics);
                            state.NotePartComplete(CompletionPart.FinishInterfaces);
                            diagnostics.Free();
                        }
                        break;

                    case CompletionPart.EnumUnderlyingType:
                        var discarded = this.EnumUnderlyingType;
                        break;

                    case CompletionPart.TypeArguments:
                        {
                            var tmp = this.TypeArgumentsNoUseSiteDiagnostics; // force type arguments
                        }
                        break;

                    case CompletionPart.TypeParameters:
                        // force type parameters
                        foreach (var typeParameter in this.TypeParameters)
                        {
                            typeParameter.ForceComplete(locationOpt, cancellationToken);
                        }

                        state.NotePartComplete(CompletionPart.TypeParameters);
                        break;

                    case CompletionPart.Members:
                        this.GetMembersByName();
                        break;

                    case CompletionPart.TypeMembers:
                        this.GetTypeMembersUnordered();
                        break;

                    case CompletionPart.SynthesizedExplicitImplementations:
                        this.GetSynthesizedExplicitImplementations(cancellationToken); //force interface and base class errors to be checked
                        break;

                    case CompletionPart.StartMemberChecks:
                    case CompletionPart.FinishMemberChecks:
                        if (state.NotePartComplete(CompletionPart.StartMemberChecks))
                        {
                            var diagnostics = DiagnosticBag.GetInstance();
                            AfterMembersChecks(diagnostics);
                            AddDeclarationDiagnostics(diagnostics);
                            var thisThreadCompleted = state.NotePartComplete(CompletionPart.FinishMemberChecks);
                            Debug.Assert(thisThreadCompleted);
                            diagnostics.Free();
                        }
                        break;

                    case CompletionPart.MembersCompleted:
                        {
                            ImmutableArray<Symbol> members = this.GetMembersUnordered();

                            bool allCompleted = true;

                            if (locationOpt == null)
                            {
                                foreach (var member in members)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    member.ForceComplete(locationOpt, cancellationToken);
                                }
                            }
                            else
                            {
                                foreach (var member in members)
                                {
                                    ForceCompleteMemberByLocation(locationOpt, member, cancellationToken);
                                    allCompleted = allCompleted && member.HasComplete(CompletionPart.All);
                                }
                            }

                            if (!allCompleted)
                            {
                                // We did not complete all members so we won't have enough information for
                                // the PointedAtManagedTypeChecks, so just kick out now.
                                var allParts = CompletionPart.NamedTypeSymbolWithLocationAll;
                                state.SpinWaitComplete(allParts, cancellationToken);
                                return;
                            }

                            EnsureFieldDefinitionsNoted();

                            // We've completed all members, so we're ready for the PointedAtManagedTypeChecks;
                            // proceed to the next iteration.
                            if (state.NotePartComplete(CompletionPart.MembersCompleted))
                            {
                                DeclaringCompilation.SymbolDeclaredEvent(this);
                            }
                            break;
                        }

                    case CompletionPart.None:
                        return;

                    default:
                        // This assert will trigger if we forgot to handle any of the completion parts
                        Debug.Assert((incompletePart & CompletionPart.NamedTypeSymbolAll) == 0);
                        // any other values are completion parts intended for other kinds of symbols
                        state.NotePartComplete(CompletionPart.All & ~CompletionPart.NamedTypeSymbolAll);
                        break;
                }

                state.SpinWaitComplete(incompletePart, cancellationToken);
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal void EnsureFieldDefinitionsNoted()
        {
            if (_flags.FieldDefinitionsNoted)
            {
                return;
            }

            NoteFieldDefinitions();
        }

        private void NoteFieldDefinitions()
        {
            // we must note all fields once therefore we need to lock
            lock (this.GetMembersAndInitializers())
            {
                if (!_flags.FieldDefinitionsNoted)
                {
                    if (!this.IsAbstract)
                    {
                        var assembly = (SourceAssemblySymbol)ContainingAssembly;

                        Accessibility containerEffectiveAccessibility = EffectiveAccessibility();

                        foreach (var member in _lazyMembersAndInitializers.NonTypeNonIndexerMembers)
                        {
                            FieldSymbol field;
                            if (!member.IsFieldOrFieldLikeEvent(out field) || field.IsConst || field.IsFixed)
                            {
                                continue;
                            }

                            Accessibility fieldDeclaredAccessibility = field.DeclaredAccessibility;
                            if (fieldDeclaredAccessibility == Accessibility.Private)
                            {
                                // mark private fields as tentatively unassigned and unread unless we discover otherwise.
                                assembly.NoteFieldDefinition(field, isInternal: false, isUnread: true);
                            }
                            else if (containerEffectiveAccessibility == Accessibility.Private)
                            {
                                // mark effectively private fields as tentatively unassigned unless we discover otherwise.
                                assembly.NoteFieldDefinition(field, isInternal: false, isUnread: false);
                            }
                            else if (fieldDeclaredAccessibility == Accessibility.Internal || containerEffectiveAccessibility == Accessibility.Internal)
                            {
                                // mark effectively internal fields as tentatively unassigned unless we discover otherwise.
                                // NOTE: These fields will be reported as unassigned only if internals are not visible from this assembly.
                                // See property SourceAssemblySymbol.UnusedFieldWarnings.
                                assembly.NoteFieldDefinition(field, isInternal: true, isUnread: false);
                            }
                        }
                    }
                    _flags.SetFieldDefinitionsNoted();
                }
            }
        }

        #endregion

        #region Containers

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingSymbol as NamedTypeSymbol;
            }
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        #endregion

        #region Flags Encoded Properties

        public override SpecialType SpecialType
        {
            get
            {
                return _flags.SpecialType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return _flags.TypeKind;
            }
        }

        internal MergedTypeDeclaration MergedDeclaration
        {
            get
            {
                return this.declaration;
            }
        }

        internal sealed override bool IsInterface
        {
            get
            {
                // TypeKind is computed eagerly, so this is cheap.
                return this.TypeKind == TypeKind.Interface;
            }
        }

        internal override bool IsManagedType
        {
            get
            {
                var isManagedType = _flags.IsManagedType;
                if (!isManagedType.HasValue())
                {
                    bool value = base.IsManagedType;
                    _flags.SetIsManagedType(value);
                    return value;
                }
                return isManagedType.Value();
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (_flags.DeclarationModifiers & DeclarationModifiers.Static) != 0;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return (_flags.DeclarationModifiers & DeclarationModifiers.Sealed) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return (_flags.DeclarationModifiers & DeclarationModifiers.Abstract) != 0;
            }
        }

        internal bool IsPartial
        {
            get
            {
                return (_flags.DeclarationModifiers & DeclarationModifiers.Partial) != 0;
            }
        }

        internal bool IsNew
        {
            get
            {
                return (_flags.DeclarationModifiers & DeclarationModifiers.New) != 0;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(_flags.DeclarationModifiers);
            }
        }

        /// <summary>
        /// Compute the "effective accessibility" of the current class for the purpose of warnings about unused fields.
        /// </summary>
        private Accessibility EffectiveAccessibility()
        {
            var result = DeclaredAccessibility;
            if (result == Accessibility.Private) return Accessibility.Private;
            for (Symbol container = this.ContainingType; (object)container != null; container = container.ContainingType)
            {
                switch (container.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        return Accessibility.Private;
                    case Accessibility.Internal:
                        result = Accessibility.Internal;
                        continue;
                }
            }

            return result;
        }

        #endregion

        #region Syntax

        public override bool IsScriptClass
        {
            get
            {
                var kind = this.declaration.Declarations[0].Kind;
                return kind == DeclarationKind.Script || kind == DeclarationKind.Submission;
            }
        }

        public override bool IsImplicitClass
        {
            get
            {
                return this.declaration.Declarations[0].Kind == DeclarationKind.ImplicitClass;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return IsImplicitClass || IsScriptClass;
            }
        }

        public override int Arity
        {
            get
            {
                return declaration.Arity;
            }
        }

        public override string Name
        {
            get
            {
                return declaration.Name;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return Arity > 0;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            if (!_lazyLexicalSortKey.IsInitialized)
            {
                _lazyLexicalSortKey.SetFrom(declaration.GetLexicalSortKey(this.DeclaringCompilation));
            }
            return _lazyLexicalSortKey;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return declaration.NameLocations.Cast<SourceLocation, Location>();
            }
        }

        public ImmutableArray<SyntaxReference> SyntaxReferences
        {
            get
            {
                return this.declaration.SyntaxReferences;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                // PERF: Declaring references are cached for compilations with event queue.
                return this.DeclaringCompilation?.EventQueue != null ? GetCachedDeclaringReferences() : this.SyntaxReferences;
            }
        }

        private ImmutableArray<SyntaxReference> GetCachedDeclaringReferences()
        {
            ImmutableArray<SyntaxReference> declaringReferences;
            if (!Diagnostics.AnalyzerDriver.TryGetCachedDeclaringReferences(this, this.DeclaringCompilation, out declaringReferences))
            {
                declaringReferences = this.SyntaxReferences;
                Diagnostics.AnalyzerDriver.CacheDeclaringReferences(this, this.DeclaringCompilation, declaringReferences);
            }

            return declaringReferences;
        }

        #endregion

        #region Members

        /// <summary>
        /// Encapsulates information about the non-type members of a (i.e. this) type.
        ///   1) For non-initializers, symbols are created and stored in a list.
        ///   2) For fields and properties, the symbols are stored in (1) and their initializers are
        ///      stored with other initialized fields and properties from the same syntax tree with
        ///      the same static-ness.
        ///   3) For indexers, syntax (weak) references are stored for later binding.
        /// </summary>
        /// <remarks>
        /// CONSIDER: most types won't have indexers, so we could move the indexer list
        /// into a subclass to spare most instances the space required for the field.
        /// </remarks>
        private sealed class MembersAndInitializers
        {
            internal readonly ImmutableArray<Symbol> NonTypeNonIndexerMembers;
            internal readonly ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> StaticInitializers;
            internal readonly ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> InstanceInitializers;
            internal readonly ImmutableArray<SyntaxReference> IndexerDeclarations;
            internal readonly int StaticInitializersSyntaxLength;
            internal readonly int InstanceInitializersSyntaxLength;

            public MembersAndInitializers(
                ImmutableArray<Symbol> nonTypeNonIndexerMembers,
                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> staticInitializers,
                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> instanceInitializers,
                ImmutableArray<SyntaxReference> indexerDeclarations,
                int staticInitializersSyntaxLength,
                int instanceInitializersSyntaxLength)
            {
                Debug.Assert(!nonTypeNonIndexerMembers.IsDefault);
                Debug.Assert(!staticInitializers.IsDefault);
                Debug.Assert(!instanceInitializers.IsDefault);
                Debug.Assert(!indexerDeclarations.IsDefault);

                Debug.Assert(!nonTypeNonIndexerMembers.Any(s => s is TypeSymbol));
                Debug.Assert(!nonTypeNonIndexerMembers.Any(s => s.IsIndexer()));
                Debug.Assert(!nonTypeNonIndexerMembers.Any(s => s.IsAccessor() && ((MethodSymbol)s).AssociatedSymbol.IsIndexer()));

                Debug.Assert(staticInitializersSyntaxLength == staticInitializers.Sum(s => s.Sum(i => (i.FieldOpt == null || !i.FieldOpt.IsMetadataConstant) ? i.Syntax.Span.Length : 0)));
                Debug.Assert(instanceInitializersSyntaxLength == instanceInitializers.Sum(s => s.Sum(i => i.Syntax.Span.Length)));

                this.NonTypeNonIndexerMembers = nonTypeNonIndexerMembers;
                this.StaticInitializers = staticInitializers;
                this.InstanceInitializers = instanceInitializers;
                this.IndexerDeclarations = indexerDeclarations;
                this.StaticInitializersSyntaxLength = staticInitializersSyntaxLength;
                this.InstanceInitializersSyntaxLength = instanceInitializersSyntaxLength;
            }
        }

        internal ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> StaticInitializers
        {
            get { return GetMembersAndInitializers().StaticInitializers; }
        }

        internal ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> InstanceInitializers
        {
            get { return GetMembersAndInitializers().InstanceInitializers; }
        }

        internal int CalculateSyntaxOffsetInSynthesizedConstructor(int position, SyntaxTree tree, bool isStatic)
        {
            if (IsScriptClass && !isStatic)
            {
                int aggregateLength = 0;

                foreach (var declaration in this.declaration.Declarations)
                {
                    var syntaxRef = declaration.SyntaxReference;
                    if (tree == syntaxRef.SyntaxTree)
                    {
                        return aggregateLength + position;
                    }

                    aggregateLength += syntaxRef.Span.Length;
                }

                throw ExceptionUtilities.Unreachable;
            }

            int syntaxOffset;
            if (TryCalculateSyntaxOffsetOfPositionInInitializer(position, tree, isStatic, ctorInitializerLength: 0, syntaxOffset: out syntaxOffset))
            {
                return syntaxOffset;
            }

            // an implicit constructor has no body and no initializer, so the variable has to be declared in a member initializer
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Calculates a syntax offset of a syntax position that is contained in a property or field initializer (if it is in fact contained in one).
        /// </summary>
        internal bool TryCalculateSyntaxOffsetOfPositionInInitializer(int position, SyntaxTree tree, bool isStatic, int ctorInitializerLength, out int syntaxOffset)
        {
            Debug.Assert(ctorInitializerLength >= 0);

            var membersAndInitializers = GetMembersAndInitializers();
            var allInitializers = isStatic ? membersAndInitializers.StaticInitializers : membersAndInitializers.InstanceInitializers;

            var siblingInitializers = GetInitializersInSourceTree(tree, allInitializers);
            int index = IndexOfInitializerContainingPosition(siblingInitializers, position);
            if (index < 0)
            {
                syntaxOffset = 0;
                return false;
            }

            //                                 |<-----------distanceFromCtorBody----------->|
            // [      initializer 0    ][ initializer 1 ][ initializer 2 ][ctor initializer][ctor body]
            // |<--preceding init len-->|      ^
            //                             position 

            int initializersLength = isStatic ? membersAndInitializers.StaticInitializersSyntaxLength : membersAndInitializers.InstanceInitializersSyntaxLength;
            int distanceFromInitializerStart = position - siblingInitializers[index].Syntax.Span.Start;

            int distanceFromCtorBody =
                initializersLength + ctorInitializerLength -
                (siblingInitializers[index].PrecedingInitializersLength + distanceFromInitializerStart);

            Debug.Assert(distanceFromCtorBody > 0);

            // syntax offset 0 is at the start of the ctor body:
            syntaxOffset = -distanceFromCtorBody;
            return true;
        }

        private static ImmutableArray<FieldOrPropertyInitializer> GetInitializersInSourceTree(SyntaxTree tree, ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers)
        {
            var builder = ArrayBuilder<FieldOrPropertyInitializer>.GetInstance();
            foreach (var siblingInitializers in initializers)
            {
                Debug.Assert(!siblingInitializers.IsEmpty);

                if (siblingInitializers[0].Syntax.SyntaxTree == tree)
                {
                    builder.AddRange(siblingInitializers);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private static int IndexOfInitializerContainingPosition(ImmutableArray<FieldOrPropertyInitializer> initializers, int position)
        {
            // Search for the start of the span (the spans are non-overlapping and sorted)
            int index = initializers.BinarySearch(position, (initializer, pos) => initializer.Syntax.Span.Start.CompareTo(pos));

            // Binary search returns non-negative result if the position is exactly the start of some span.
            if (index >= 0)
            {
                return index;
            }

            // Otherwise, ~index is the closest span whose start is greater than the position.
            // => Check if the preceding initializer span contains the position.
            int precedingInitializerIndex = ~index - 1;
            if (precedingInitializerIndex >= 0 && initializers[precedingInitializerIndex].Syntax.Span.Contains(position))
            {
                return precedingInitializerIndex;
            }

            return -1;
        }

        public override IEnumerable<string> MemberNames
        {
            get { return this.declaration.MemberNames; }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return GetTypeMembersDictionary().Flatten();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return GetTypeMembersDictionary().Flatten(LexicalOrderSymbolComparer.Instance);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            ImmutableArray<NamedTypeSymbol> members;
            if (GetTypeMembersDictionary().TryGetValue(name, out members))
            {
                return members;
            }

            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray(t => t.Arity == arity);
        }

        private Dictionary<string, ImmutableArray<NamedTypeSymbol>> GetTypeMembersDictionary()
        {
            if (_lazyTypeMembers == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref _lazyTypeMembers, MakeTypeMembers(diagnostics), null) == null)
                {
                    AddDeclarationDiagnostics(diagnostics);

                    state.NotePartComplete(CompletionPart.TypeMembers);
                }

                diagnostics.Free();
            }

            return _lazyTypeMembers;
        }

        private Dictionary<string, ImmutableArray<NamedTypeSymbol>> MakeTypeMembers(DiagnosticBag diagnostics)
        {
            var symbols = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var conflictDict = new Dictionary<ValueTuple<string, int>, SourceNamedTypeSymbol>();
            try
            {
                foreach (var childDeclaration in declaration.Children)
                {
                    var t = new SourceNamedTypeSymbol(this, childDeclaration, diagnostics);
                    this.CheckMemberNameDistinctFromType(t, diagnostics);

                    var key = new ValueTuple<string, int>(t.Name, t.Arity);
                    SourceNamedTypeSymbol other;
                    if (conflictDict.TryGetValue(key, out other))
                    {
                        if (Locations.Length == 1 || IsPartial)
                        {
                            if (t.IsPartial && other.IsPartial)
                            {
                                diagnostics.Add(ErrorCode.ERR_PartialTypeKindConflict, t.Locations[0], t);
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, t.Locations[0], this, t.Name);
                            }
                        }
                    }
                    else
                    {
                        conflictDict.Add(key, t);
                    }

                    symbols.Add(t);
                }

                if (IsInterface)
                {
                    foreach (var t in symbols)
                    {
                        diagnostics.Add(ErrorCode.ERR_InterfacesCannotContainTypes, t.Locations[0], t);
                    }
                }

                Debug.Assert(s_emptyTypeMembers.Count == 0);
                return symbols.Count > 0 ? symbols.ToDictionary(s => s.Name) : s_emptyTypeMembers;
            }
            finally
            {
                symbols.Free();
            }
        }

        private void CheckMemberNameDistinctFromType(Symbol member, DiagnosticBag diagnostics)
        {
            switch (this.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                    if (member.Name == this.Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_MemberNameSameAsType, member.Locations[0], this.Name);
                    }
                    break;
            }
        }

        internal override ImmutableArray<Symbol> GetMembersUnordered()
        {
            var result = _lazyMembersFlattened;

            if (result.IsDefault)
            {
                result = GetMembersByName().Flatten(null);  // do not sort.
                ImmutableInterlocked.InterlockedInitialize(ref _lazyMembersFlattened, result);
                result = _lazyMembersFlattened;
            }

#if DEBUG
            // In DEBUG, swap first and last elements so that use of Unordered in a place it isn't warranted is caught
            // more obviously.
            return result.DeOrder();
#else
            return result;
#endif
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            if (_flags.FlattenedMembersIsSorted)
            {
                return _lazyMembersFlattened;
            }
            else
            {
                var allMembers = this.GetMembersUnordered();

                if (allMembers.Length > 1)
                {
                    // The array isn't sorted. Sort it and remember that we sorted it.
                    allMembers = allMembers.Sort(LexicalOrderSymbolComparer.Instance);
                    ImmutableInterlocked.InterlockedExchange(ref _lazyMembersFlattened, allMembers);
                }

                _flags.SetFlattenedMembersIsSorted();
                return allMembers;
            }
        }

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            ImmutableArray<Symbol> members;
            if (GetMembersByName().TryGetValue(name, out members))
            {
                return members;
            }

            return ImmutableArray<Symbol>.Empty;
        }

        internal override ImmutableArray<Symbol> GetSimpleNonTypeMembers(string name)
        {
            if (_lazyMembersDictionary != null || MemberNames.Contains(name))
            {
                return GetMembers(name);
            }

            return ImmutableArray<Symbol>.Empty;
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            if (this.TypeKind == TypeKind.Enum)
            {
                // For consistency with Dev10, emit value__ field first.
                var valueField = ((SourceNamedTypeSymbol)this).EnumValueField;
                Debug.Assert((object)valueField != null);
                yield return valueField;
            }

            foreach (var m in this.GetMembers())
            {
                switch (m.Kind)
                {
                    case SymbolKind.Field:
                        yield return (FieldSymbol)m;
                        break;
                    case SymbolKind.Event:
                        FieldSymbol associatedField = ((EventSymbol)m).AssociatedField;
                        if ((object)associatedField != null)
                        {
                            yield return associatedField;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// During early attribute decoding, we consider a safe subset of all members that will not
        /// cause cyclic dependencies.  Get all such members for this symbol.
        /// 
        /// In particular, this method will return nested types and fields (other than auto-property
        /// backing fields).
        /// </summary>
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return GetEarlyAttributeDecodingMembersDictionary().Flatten();
        }

        /// <summary>
        /// During early attribute decoding, we consider a safe subset of all members that will not
        /// cause cyclic dependencies.  Get all such members for this symbol that have a particular name.
        /// 
        /// In particular, this method will return nested types and fields (other than auto-property
        /// backing fields).
        /// </summary>
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            ImmutableArray<Symbol> result;
            return GetEarlyAttributeDecodingMembersDictionary().TryGetValue(name, out result) ? result : ImmutableArray<Symbol>.Empty;
        }

        private Dictionary<string, ImmutableArray<Symbol>> GetEarlyAttributeDecodingMembersDictionary()
        {
            if (_lazyEarlyAttributeDecodingMembersDictionary == null)
            {
                var membersAndInitializers = GetMembersAndInitializers(); //NOTE: separately cached

                // NOTE: members were added in a single pass over the syntax, so they're already
                // in lexical order.

                // TODO: Can we move ToDictionary() off ArrayBuilder<T> so that we don't need a temp here?
                var temp = ArrayBuilder<Symbol>.GetInstance();
                temp.AddRange(membersAndInitializers.NonTypeNonIndexerMembers);
                var membersByName = temp.ToDictionary(s => s.Name);
                temp.Free();

                AddNestedTypesToDictionary(membersByName, GetTypeMembersDictionary());

                Interlocked.CompareExchange(ref _lazyEarlyAttributeDecodingMembersDictionary, membersByName, null);
            }

            return _lazyEarlyAttributeDecodingMembersDictionary;
        }

        // NOTE: this method should do as little work as possible
        //       we often need to get members just to do a lookup.
        //       All additional checks and diagnostics may be not
        //       needed yet or at all.
        private MembersAndInitializers GetMembersAndInitializers()
        {
            var membersAndInitializers = _lazyMembersAndInitializers;
            if (membersAndInitializers != null)
            {
                return membersAndInitializers;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            membersAndInitializers = BuildMembersAndInitializers(diagnostics);

            var alreadyKnown = Interlocked.CompareExchange(ref _lazyMembersAndInitializers, membersAndInitializers, null);
            if (alreadyKnown != null)
            {
                diagnostics.Free();
                return alreadyKnown;
            }

            AddDeclarationDiagnostics(diagnostics);
            diagnostics.Free();

            return membersAndInitializers;
        }

        protected Dictionary<string, ImmutableArray<Symbol>> GetMembersByName()
        {
            if (this.state.HasComplete(CompletionPart.Members))
            {
                return _lazyMembersDictionary;
            }

            return GetMembersByNameSlow();
        }

        private Dictionary<string, ImmutableArray<Symbol>> GetMembersByNameSlow()
        {
            if (_lazyMembersDictionary == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var membersDictionary = MakeAllMembers(diagnostics);
                if (Interlocked.CompareExchange(ref _lazyMembersDictionary, membersDictionary, null) == null)
                {
                    MergePartialMethods(_lazyMembersDictionary, diagnostics);
                    AddDeclarationDiagnostics(diagnostics);
                    state.NotePartComplete(CompletionPart.Members);
                }

                diagnostics.Free();
            }

            state.SpinWaitComplete(CompletionPart.Members, default(CancellationToken));
            return _lazyMembersDictionary;
        }

        protected void AfterMembersChecks(DiagnosticBag diagnostics)
        {
            if (IsInterface)
            {
                CheckInterfaceMembers(this.GetMembersAndInitializers().NonTypeNonIndexerMembers, diagnostics);
            }

            CheckMemberNamesDistinctFromType(diagnostics);
            CheckMemberNameConflicts(diagnostics);
            CheckSpecialMemberErrors(diagnostics);
            CheckTypeParameterNameConflicts(diagnostics);
            CheckAccessorNameConflicts(diagnostics);

            bool unused = KnownCircularStruct;

            CheckSequentialOnPartialType(diagnostics);
            CheckForProtectedInStaticClass(diagnostics);
            CheckForUnmatchedOperators(diagnostics);
        }

        private void CheckMemberNamesDistinctFromType(DiagnosticBag diagnostics)
        {
            foreach (var member in GetMembersAndInitializers().NonTypeNonIndexerMembers)
            {
                CheckMemberNameDistinctFromType(member, diagnostics);
            }
        }

        private void CheckMemberNameConflicts(DiagnosticBag diagnostics)
        {
            Dictionary<string, ImmutableArray<Symbol>> membersByName = GetMembersByName();

            // Collisions involving indexers are handled specially.
            CheckIndexerNameConflicts(diagnostics, membersByName);

            // key and value will be the same object in these dictionaries.
            var methodsBySignature = new Dictionary<SourceMethodSymbol, SourceMethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);
            var conversionsAsMethods = new Dictionary<SourceMethodSymbol, SourceMethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);
            var conversionsAsConversions = new Dictionary<SourceUserDefinedConversionSymbol, SourceUserDefinedConversionSymbol>(ConversionSignatureComparer.Comparer);

            // SPEC: The signature of an operator must differ from the signatures of all other
            // SPEC: operators declared in the same class.

            // DELIBERATE SPEC VIOLATION:
            // The specification does not state that a user-defined conversion reserves the names
            // op_Implicit or op_Explicit, but nevertheless the native compiler does so; an attempt
            // to define a field or a conflicting method with the metadata name of a user-defined
            // conversion is an error.  We preserve this reasonable behavior.
            //
            // Similarly, we treat "public static C operator +(C, C)" as colliding with
            // "public static C op_Addition(C, C)". Fortunately, this behavior simply
            // falls out of treating user-defined operators as ordinary methods; we do
            // not need any special handling in this method.
            //
            // However, we must have special handling for conversions because conversions
            // use a completely different rule for detecting collisions between two 
            // conversions: conversion signatures consist only of the source and target
            // types of the conversions, and not the kind of the conversion (implicit or explicit),
            // the name of the method, and so on.
            //
            // Therefore we must detect the following kinds of member name conflicts:
            //
            // 1. a method, conversion or field has the same name as a (different) field (* see note below) 
            // 2. a method has the same method signature as another method or conversion
            // 3. a conversion has the same conversion signature as another conversion.
            //
            // However, we must *not* detect "a conversion has the same *method* signature 
            // as another conversion" because conversions are allowed to overload on 
            // return type but methods are not.
            //
            // (*) NOTE: Throughout the rest of this method I will use "field" as a shorthand for
            // "non-method, non-conversion, non-type member", rather than spelling out 
            // "field, property or event...")

            //

            foreach (var name in membersByName.Keys)
            {
                Symbol lastSym = GetTypeMembers(name).FirstOrDefault();
                methodsBySignature.Clear();
                conversionsAsMethods.Clear();
                // Conversion collisions do not consider the name of the conversion,
                // so do not clear that dictionary.
                foreach (var symbol in membersByName[name])
                {
                    if (symbol.Kind == SymbolKind.NamedType ||
                        symbol.IsAccessor() ||
                        symbol.IsIndexer())
                    {
                        continue;
                    }

                    // We detect the first category of conflict by running down the list of members
                    // of the same name, and producing an error when we discover any of the following
                    // "bad transitions".
                    //
                    // * a method or conversion that comes after any field (not necessarily directly)
                    // * a field directly following a field
                    // * a field directly following a method or conversion
                    //
                    // Furthermore: we do not wish to detect collisions between nested types in 
                    // this code; that is tested elsewhere. However, we do wish to detect a collision
                    // between a nested type and a field, method or conversion. Therefore we
                    // initialize our "bad transition" detector with a type of the given name,
                    // if there is one. That way we also detect the transitions of "method following
                    // type", and so on.
                    //
                    // The "lastSym" local below is used to detect these transitions. Its value is
                    // one of the following:
                    //
                    // * a nested type of the given name, or
                    // * the first method of the given name, or
                    // * the most recently processed field of the given name.
                    //
                    // If either the current symbol or the "last symbol" are not methods then
                    // there must be a collision:
                    // 
                    // * if the current symbol is not a method and the last symbol is, then 
                    //   there is a field directly following a method of the same name
                    // * if the current symbol is a method and the last symbol is not, then
                    //   there is a method directly or indirectly following a field of the same name,
                    //   or a method of the same name as a nested type.
                    // * if neither are methods then either we have a field directly
                    //   following a field of the same name, or a field and a nested type of the same name.
                    //

                    if ((object)lastSym != null)
                    {
                        if (symbol.Kind != SymbolKind.Method || lastSym.Kind != SymbolKind.Method)
                        {
                            if (symbol.Kind != SymbolKind.Field || !symbol.IsImplicitlyDeclared || !(symbol is SynthesizedBackingFieldSymbol)) // don't report duplicate errors on backing fields
                            {
                                // The type '{0}' already contains a definition for '{1}'
                                if (Locations.Length == 1 || IsPartial)
                                    diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, symbol.Locations[0], this, symbol.Name);
                            }

                            if (lastSym.Kind == SymbolKind.Method)
                            {
                                lastSym = symbol;
                            }
                        }
                    }
                    else
                    {
                        lastSym = symbol;
                    }

                    // That takes care of the first category of conflict; we detect the
                    // second and third categories as follows:

                    var conversion = symbol as SourceUserDefinedConversionSymbol;
                    var method = symbol as SourceMethodSymbol;
                    if ((object)conversion != null)
                    {
                        // Does this conversion collide *as a conversion* with any previously-seen
                        // conversion?

                        SourceUserDefinedConversionSymbol previousConversion;
                        if (conversionsAsConversions.TryGetValue(conversion, out previousConversion))
                        {
                            // CS0557: Duplicate user-defined conversion in type 'C'
                            diagnostics.Add(ErrorCode.ERR_DuplicateConversionInClass, conversion.Locations[0], this);
                        }
                        else
                        {
                            // We haven't seen this conversion before; make a note of it in case we
                            // see it again later, either as a conversion or as a method.
                            conversionsAsConversions[conversion] = conversion;

                            // The other set might already contain a conversion which would collide
                            // *as a method* with the current conversion.
                            if (!conversionsAsMethods.ContainsKey(conversion))
                            {
                                conversionsAsMethods[conversion] = conversion;
                            }
                        }

                        // Does this conversion collide *as a method* with any previously-seen
                        // non-conversion method?

                        SourceMethodSymbol previousMethod;
                        if (methodsBySignature.TryGetValue(conversion, out previousMethod))
                        {
                            ReportMethodSignatureCollision(diagnostics, conversion, previousMethod);
                        }
                        // Do not add the conversion to the set of previously-seen methods; that set
                        // is only non-conversion methods.

                    }
                    else if ((object)method != null)
                    {
                        // Does this method collide *as a method* with any previously-seen 
                        // conversion?

                        SourceMethodSymbol previousConversion;
                        if (conversionsAsMethods.TryGetValue(method, out previousConversion))
                        {
                            ReportMethodSignatureCollision(diagnostics, method, previousConversion);
                        }
                        // Do not add the method to the set of previously-seen conversions.

                        // Does this method collide *as a method* with any previously-seen
                        // non-conversion method?

                        SourceMethodSymbol previousMethod;
                        if (methodsBySignature.TryGetValue(method, out previousMethod))
                        {
                            ReportMethodSignatureCollision(diagnostics, method, previousMethod);
                        }
                        else
                        {
                            // We haven't seen this method before. Make a note of it in case
                            // we see a colliding method later.
                            methodsBySignature[method] = method;
                        }
                    }
                }
            }
        }

        // Report a name conflict; the error is reported on the location of method1.
        // UNDONE: Consider adding a secondary location pointing to the second method.
        private void ReportMethodSignatureCollision(DiagnosticBag diagnostics, SourceMethodSymbol method1, SourceMethodSymbol method2)
        {
            // Partial methods are allowed to collide by signature.
            if (method1.IsPartial && method2.IsPartial)
            {
                return;
            }

            if (DifferByOutOrRef(method1, method2))
            {
                // '{0}' cannot define overloaded methods that differ only on ref and out
                ErrorCode errorCode = method1.MethodKind == MethodKind.Constructor ?
                    ErrorCode.ERR_OverloadRefOutCtor :
                    ErrorCode.ERR_OverloadRefOut;
                diagnostics.Add(errorCode, method1.Locations[0], this);
            }
            else if (method1.MethodKind == MethodKind.Destructor && method2.MethodKind == MethodKind.Destructor)
            {
                // Special case: if there are two destructors, use the destructor syntax instead of "Finalize"
                // Type '{1}' already defines a member called '{0}' with the same parameter types
                diagnostics.Add(ErrorCode.ERR_MemberAlreadyExists, method1.Locations[0], "~" + this.Name, this);
            }
            else
            {
                // Type '{1}' already defines a member called '{0}' with the same parameter types
                diagnostics.Add(ErrorCode.ERR_MemberAlreadyExists, method1.Locations[0], method1.Name, this);
            }
        }

        private void CheckIndexerNameConflicts(DiagnosticBag diagnostics, Dictionary<string, ImmutableArray<Symbol>> membersByName)
        {
            PooledHashSet<string> typeParameterNames = null;
            if (this.Arity > 0)
            {
                typeParameterNames = PooledHashSet<string>.GetInstance();
                foreach (TypeParameterSymbol typeParameter in this.TypeParameters)
                {
                    typeParameterNames.Add(typeParameter.Name);
                }
            }


            var indexersBySignature = new Dictionary<PropertySymbol, PropertySymbol>(MemberSignatureComparer.DuplicateSourceComparer);

            // Note: Can't assume that all indexers are called WellKnownMemberNames.Indexer because 
            // they may be explicit interface implementations.
            foreach (string name in membersByName.Keys)
            {
                string lastIndexerName = null;
                indexersBySignature.Clear();
                foreach (var symbol in membersByName[name])
                {
                    if (symbol.IsIndexer())
                    {
                        PropertySymbol indexer = (PropertySymbol)symbol;
                        CheckIndexerSignatureCollisions(
                            indexer,
                            diagnostics,
                            membersByName,
                            indexersBySignature,
                            ref lastIndexerName);

                        // Also check for collisions with type parameters, which aren't in the member map.
                        // NOTE: Accessors have normal names and are handled in CheckTypeParameterNameConflicts.
                        if (typeParameterNames != null)
                        {
                            string indexerName = indexer.MetadataName;
                            if (typeParameterNames.Contains(indexerName))
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, indexer.Locations[0], this, indexerName);
                                continue;
                            }
                        }
                    }
                }
            }

            typeParameterNames?.Free();
        }

        private void CheckIndexerSignatureCollisions(
            PropertySymbol indexer,
            DiagnosticBag diagnostics,
            Dictionary<string, ImmutableArray<Symbol>> membersByName,
            Dictionary<PropertySymbol, PropertySymbol> indexersBySignature,
            ref string lastIndexerName)
        {
            if (!indexer.IsExplicitInterfaceImplementation) //explicit implementation names are not checked
            {
                string indexerName = indexer.MetadataName;

                if (lastIndexerName != null && lastIndexerName != indexerName)
                {
                    // NOTE: dev10 checks indexer names by comparing each to the previous.
                    // For example, if indexers are declared with names A, B, A, B, then there
                    // will be three errors - one for each time the name is different from the
                    // previous one.  If, on the other hand, the names are A, A, B, B, then
                    // there will only be one error because only one indexer has a different
                    // name from the previous one.

                    diagnostics.Add(ErrorCode.ERR_InconsistentIndexerNames, indexer.Locations[0]);
                }

                lastIndexerName = indexerName;

                if (Locations.Length == 1 || IsPartial)
                {
                    if (membersByName.ContainsKey(indexerName))
                    {
                        // The name of the indexer is reserved - it can only be used by other indexers.
                        Debug.Assert(!membersByName[indexerName].Any(SymbolExtensions.IsIndexer));
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, indexer.Locations[0], this, indexerName);
                    }
                }
            }

            PropertySymbol prevIndexerBySignature;
            if (indexersBySignature.TryGetValue(indexer, out prevIndexerBySignature))
            {
                // Type '{1}' already defines a member called '{0}' with the same parameter types
                // NOTE: Dev10 prints "this" as the name of the indexer.
                diagnostics.Add(ErrorCode.ERR_MemberAlreadyExists, indexer.Locations[0], SyntaxFacts.GetText(SyntaxKind.ThisKeyword), this);
            }
            else
            {
                indexersBySignature[indexer] = indexer;
            }
        }

        private void CheckSpecialMemberErrors(DiagnosticBag diagnostics)
        {
            var conversions = new TypeConversions(this.ContainingAssembly.CorLibrary);
            foreach (var member in this.GetMembersUnordered())
            {
                member.AfterAddingTypeMembersChecks(conversions, diagnostics);
            }
        }

        private void CheckTypeParameterNameConflicts(DiagnosticBag diagnostics)
        {
            if (this.TypeKind == TypeKind.Delegate)
            {
                // Delegates do not have conflicts between their type parameter
                // names and their methods; it is legal (though odd) to say
                // delegate void D<Invoke>(Invoke x);

                return;
            }

            if (Locations.Length == 1 || IsPartial)
            {
                foreach (var tp in TypeParameters)
                {
                    foreach (var dup in GetMembers(tp.Name))
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, dup.Locations[0], this, tp.Name);
                    }
                }
            }
        }

        private void CheckAccessorNameConflicts(DiagnosticBag diagnostics)
        {
            // Report errors where property and event accessors
            // conflict with other members of the same name.
            foreach (Symbol symbol in this.GetMembersUnordered())
            {
                if (symbol.IsExplicitInterfaceImplementation())
                {
                    // If there's a name conflict it will show up as a more specific
                    // interface implementation error.
                    continue;
                }
                switch (symbol.Kind)
                {
                    case SymbolKind.Property:
                        {
                            var propertySymbol = (PropertySymbol)symbol;
                            this.CheckForMemberConflictWithPropertyAccessor(propertySymbol, getNotSet: true, diagnostics: diagnostics);
                            this.CheckForMemberConflictWithPropertyAccessor(propertySymbol, getNotSet: false, diagnostics: diagnostics);
                            break;
                        }
                    case SymbolKind.Event:
                        {
                            var eventSymbol = (EventSymbol)symbol;
                            this.CheckForMemberConflictWithEventAccessor(eventSymbol, isAdder: true, diagnostics: diagnostics);
                            this.CheckForMemberConflictWithEventAccessor(eventSymbol, isAdder: false, diagnostics: diagnostics);
                            break;
                        }
                }
            }
        }

        internal override bool KnownCircularStruct
        {
            get
            {
                if (_lazyKnownCircularStruct == (int)ThreeState.Unknown)
                {
                    if (TypeKind != TypeKind.Struct)
                    {
                        Interlocked.CompareExchange(ref _lazyKnownCircularStruct, (int)ThreeState.False, (int)ThreeState.Unknown);
                    }
                    else
                    {
                        var diagnostics = DiagnosticBag.GetInstance();
                        var value = (int)CheckStructCircularity(diagnostics).ToThreeState();

                        if (Interlocked.CompareExchange(ref _lazyKnownCircularStruct, value, (int)ThreeState.Unknown) == (int)ThreeState.Unknown)
                        {
                            AddDeclarationDiagnostics(diagnostics);
                        }

                        Debug.Assert(value == _lazyKnownCircularStruct);
                        diagnostics.Free();
                    }
                }

                return _lazyKnownCircularStruct == (int)ThreeState.True;
            }
        }

        private bool CheckStructCircularity(DiagnosticBag diagnostics)
        {
            Debug.Assert(TypeKind == TypeKind.Struct);

            CheckFiniteFlatteningGraph(diagnostics);
            return HasStructCircularity(diagnostics);
        }

        private bool HasStructCircularity(DiagnosticBag diagnostics)
        {
            foreach (var valuesByName in GetMembersByName().Values)
            {
                foreach (var member in valuesByName)
                {
                    if (member.Kind != SymbolKind.Field)
                    {
                        // NOTE: don't have to check field-like events, because they can't have struct types.
                        continue;
                    }
                    var field = (FieldSymbol)member;
                    if (field.IsStatic)
                    {
                        continue;
                    }
                    var type = field.Type.TypeSymbol;
                    if (((object)type != null) &&
                        (type.TypeKind == TypeKind.Struct) &&
                        BaseTypeAnalysis.StructDependsOn(type, this) &&
                        !type.IsPrimitiveRecursiveStruct()) // allow System.Int32 to contain a field of its own type
                    {
                        // If this is a backing field, report the error on the associated property.
                        var symbol = field.AssociatedSymbol ?? field;

                        if (symbol.Kind == SymbolKind.Parameter)
                        {
                            // We should stick to members for this error.
                            symbol = field;
                        }

                        // Struct member '{0}' of type '{1}' causes a cycle in the struct layout
                        diagnostics.Add(ErrorCode.ERR_StructLayoutCycle, symbol.Locations[0], symbol, type);
                        return true;
                    }
                }
            }
            return false;
        }

        private void CheckForProtectedInStaticClass(DiagnosticBag diagnostics)
        {
            if (!IsStatic)
            {
                return;
            }

            // no protected members allowed
            foreach (var valuesByName in GetMembersByName().Values)
            {
                foreach (var member in valuesByName)
                {
                    if (member is TypeSymbol)
                    {
                        // Duplicate Dev10's failure to diagnose this error.
                        continue;
                    }

                    if (member.DeclaredAccessibility == Accessibility.Protected || member.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                    {
                        if (member.Kind != SymbolKind.Method || ((MethodSymbol)member).MethodKind != MethodKind.Destructor)
                        {
                            diagnostics.Add(ErrorCode.ERR_ProtectedInStatic, member.Locations[0], member);
                        }
                    }
                }
            }
        }

        private void CheckForUnmatchedOperators(DiagnosticBag diagnostics)
        {
            // SPEC: The true and false unary operators require pairwise declaration.
            // SPEC: A compile-time error occurs if a class or struct declares one 
            // SPEC: of these operators without also declaring the other.
            //
            // SPEC DEFICIENCY: The line of the specification quoted above should say
            // the same thing as the lines below: that the formal parameters of the
            // paired true/false operators must match exactly. You can't do 
            // op true(S) and op false(S?) for example.

            // SPEC: Certain binary operators require pairwise declaration. For every
            // SPEC: declaration of either operator of a pair, there must be a matching
            // SPEC: declaration of the other operator of the pair. Two operator 
            // SPEC: declarations match when they have the same return type and the same
            // SPEC: type for each parameter. The following operators require pairwise
            // SPEC: declaration: == and !=, > and <, >= and <=.

            CheckForUnmatchedOperator(diagnostics, WellKnownMemberNames.TrueOperatorName, WellKnownMemberNames.FalseOperatorName);
            CheckForUnmatchedOperator(diagnostics, WellKnownMemberNames.EqualityOperatorName, WellKnownMemberNames.InequalityOperatorName);
            CheckForUnmatchedOperator(diagnostics, WellKnownMemberNames.LessThanOperatorName, WellKnownMemberNames.GreaterThanOperatorName);
            CheckForUnmatchedOperator(diagnostics, WellKnownMemberNames.LessThanOrEqualOperatorName, WellKnownMemberNames.GreaterThanOrEqualOperatorName);

            // We also produce a warning if == / != is overridden without also overriding
            // Equals and GetHashCode, or if Equals is overridden without GetHashCode.

            CheckForEqualityAndGetHashCode(diagnostics);
        }

        private void CheckForUnmatchedOperator(DiagnosticBag diagnostics, string operatorName1, string operatorName2)
        {
            var ops1 = this.GetOperators(operatorName1);
            var ops2 = this.GetOperators(operatorName2);
            CheckForUnmatchedOperator(diagnostics, ops1, ops2, operatorName2);
            CheckForUnmatchedOperator(diagnostics, ops2, ops1, operatorName1);
        }

        private static void CheckForUnmatchedOperator(
            DiagnosticBag diagnostics,
            ImmutableArray<MethodSymbol> ops1,
            ImmutableArray<MethodSymbol> ops2,
            string operatorName2)
        {
            foreach (var op1 in ops1)
            {
                bool foundMatch = false;
                foreach (var op2 in ops2)
                {
                    foundMatch = DoOperatorsPair(op1, op2);
                    if (foundMatch)
                    {
                        break;
                    }
                }

                if (!foundMatch)
                {
                    // CS0216: The operator 'C.operator true(C)' requires a matching operator 'false' to also be defined
                    diagnostics.Add(ErrorCode.ERR_OperatorNeedsMatch, op1.Locations[0], op1,
                        SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(operatorName2)));
                }
            }
        }

        private static bool DoOperatorsPair(MethodSymbol op1, MethodSymbol op2)
        {
            if (op1.ParameterCount != op2.ParameterCount)
            {
                return false;
            }

            for (int p = 0; p < op1.ParameterCount; ++p)
            {
                if (!op1.ParameterTypes[p].Equals(op2.ParameterTypes[p], TypeSymbolEqualityOptions.SameType))
                {
                    return false;
                }
            }

            if (!op1.ReturnType.TypeSymbol.Equals(op2.ReturnType.TypeSymbol, TypeSymbolEqualityOptions.SameType))
            {
                return false;
            }

            return true;
        }

        private void CheckForEqualityAndGetHashCode(DiagnosticBag diagnostics)
        {
            if (this.IsInterfaceType())
            {
                // Interfaces are allowed to define Equals without GetHashCode if they want.
                return;
            }

            bool hasOp = this.GetOperators(WellKnownMemberNames.EqualityOperatorName).Any() ||
                this.GetOperators(WellKnownMemberNames.InequalityOperatorName).Any();
            bool overridesEquals = this.TypeOverridesObjectMethod("Equals");

            if (hasOp || overridesEquals)
            {
                bool overridesGHC = this.TypeOverridesObjectMethod("GetHashCode");
                if (overridesEquals && !overridesGHC)
                {
                    // CS0659: 'C' overrides Object.Equals(object o) but does not override Object.GetHashCode()
                    diagnostics.Add(ErrorCode.WRN_EqualsWithoutGetHashCode, this.Locations[0], this);
                }

                if (hasOp && !overridesEquals)
                {
                    // CS0660: 'C' defines operator == or operator != but does not override Object.Equals(object o)
                    diagnostics.Add(ErrorCode.WRN_EqualityOpWithoutEquals, this.Locations[0], this);
                }

                if (hasOp && !overridesGHC)
                {
                    // CS0661: 'C' defines operator == or operator != but does not override Object.GetHashCode()
                    diagnostics.Add(ErrorCode.WRN_EqualityOpWithoutGetHashCode, this.Locations[0], this);
                }
            }
        }

        private bool TypeOverridesObjectMethod(string name)
        {
            foreach (var method in this.GetMembers(name).OfType<MethodSymbol>())
            {
                if (method.IsOverride && method.GetConstructedLeastOverriddenMethod(this).ContainingType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckFiniteFlatteningGraph(DiagnosticBag diagnostics)
        {
            Debug.Assert(ReferenceEquals(this, this.OriginalDefinition));
            if (AllTypeArgumentCount() == 0) return;
            var instanceMap = new Dictionary<NamedTypeSymbol, NamedTypeSymbol>(ReferenceEqualityComparer.Instance);
            instanceMap.Add(this, this);
            foreach (var m in this.GetMembersUnordered())
            {
                var f = m as FieldSymbol;
                if ((object)f == null || !f.IsStatic || f.Type.TypeKind != TypeKind.Struct) continue;
                var type = (NamedTypeSymbol)f.Type.TypeSymbol;
                if (InfiniteFlatteningGraph(this, type, instanceMap))
                {
                    // Struct member '{0}' of type '{1}' causes a cycle in the struct layout
                    diagnostics.Add(ErrorCode.ERR_StructLayoutCycle, f.Locations[0], f, type);
                    //this.KnownCircularStruct = true;
                    return;
                }
            }
        }

        private static bool InfiniteFlatteningGraph(SourceMemberContainerTypeSymbol top, NamedTypeSymbol t, Dictionary<NamedTypeSymbol, NamedTypeSymbol> instanceMap)
        {
            if (!t.ContainsTypeParameter()) return false;
            NamedTypeSymbol oldInstance;
            var tOriginal = t.OriginalDefinition;
            if (instanceMap.TryGetValue(tOriginal, out oldInstance))
            {
                // short circuit when we find a cycle, but only return true when the cycle contains the top struct
                return (oldInstance != t) && ReferenceEquals(tOriginal, top);
            }
            else
            {
                instanceMap.Add(tOriginal, t);
                try
                {
                    foreach (var m in t.GetMembersUnordered())
                    {
                        var f = m as FieldSymbol;
                        if ((object)f == null || !f.IsStatic || f.Type.TypeKind != TypeKind.Struct) continue;
                        var type = (NamedTypeSymbol)f.Type.TypeSymbol;
                        if (InfiniteFlatteningGraph(top, type, instanceMap)) return true;
                    }
                    return false;
                }
                finally
                {
                    instanceMap.Remove(tOriginal);
                }
            }
        }

        private void CheckSequentialOnPartialType(DiagnosticBag diagnostics)
        {
            if (!IsPartial || this.Layout.Kind != LayoutKind.Sequential)
            {
                return;
            }

            SyntaxReference whereFoundField = null;
            if (this.SyntaxReferences.Length <= 1)
            {
                return;
            }

            foreach (var syntaxRef in this.SyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax() as TypeDeclarationSyntax;
                if (syntax == null)
                {
                    continue;
                }

                foreach (var m in syntax.Members)
                {
                    if (HasInstanceData(m))
                    {
                        if (whereFoundField != null && whereFoundField != syntaxRef)
                        {
                            diagnostics.Add(ErrorCode.WRN_SequentialOnPartialClass, Locations[0], this);
                            return;
                        }

                        whereFoundField = syntaxRef;
                    }
                }
            }
        }

        private static bool HasInstanceData(MemberDeclarationSyntax m)
        {
            switch (m.Kind())
            {
                case SyntaxKind.FieldDeclaration:
                    var fieldDecl = (FieldDeclarationSyntax)m;
                    return
                        !ContainsModifier(fieldDecl.Modifiers, SyntaxKind.StaticKeyword) &&
                        !ContainsModifier(fieldDecl.Modifiers, SyntaxKind.ConstKeyword);
                case SyntaxKind.PropertyDeclaration:
                    // auto-property
                    var propertyDecl = (PropertyDeclarationSyntax)m;
                    return
                        !ContainsModifier(propertyDecl.Modifiers, SyntaxKind.StaticKeyword) &&
                        !ContainsModifier(propertyDecl.Modifiers, SyntaxKind.AbstractKeyword) &&
                        !ContainsModifier(propertyDecl.Modifiers, SyntaxKind.ExternKeyword) &&
                        propertyDecl.AccessorList != null &&
                        All(propertyDecl.AccessorList.Accessors, a => a.Body == null);
                case SyntaxKind.EventFieldDeclaration:
                    // field-like event declaration
                    var eventFieldDecl = (EventFieldDeclarationSyntax)m;
                    return
                        !ContainsModifier(eventFieldDecl.Modifiers, SyntaxKind.StaticKeyword) &&
                        !ContainsModifier(eventFieldDecl.Modifiers, SyntaxKind.AbstractKeyword) &&
                        !ContainsModifier(eventFieldDecl.Modifiers, SyntaxKind.ExternKeyword);
                default:
                    return false;
            }
        }

        private static bool All<T>(SyntaxList<T> list, Func<T, bool> predicate) where T : CSharpSyntaxNode
        {
            foreach (var t in list) { if (predicate(t)) return true; };
            return false;
        }

        private static bool ContainsModifier(SyntaxTokenList modifiers, SyntaxKind modifier)
        {
            foreach (var m in modifiers) { if (m.IsKind(modifier)) return true; };
            return false;
        }

        private Dictionary<string, ImmutableArray<Symbol>> MakeAllMembers(DiagnosticBag diagnostics)
        {
            var membersAndInitializers = GetMembersAndInitializers();

            // Most types don't have indexers.  If this is one of those types,
            // just reuse the dictionary we build for early attribute decoding.
            if (membersAndInitializers.IndexerDeclarations.Length == 0)
            {
                return GetEarlyAttributeDecodingMembersDictionary();
            }

            // Add indexers (plus their accessors)
            var indexerMembers = ArrayBuilder<Symbol>.GetInstance();
            Binder binder = null;
            SyntaxTree currentTree = null;
            foreach (var decl in membersAndInitializers.IndexerDeclarations)
            {
                var syntax = (IndexerDeclarationSyntax)decl.GetSyntax();

                if (binder == null || currentTree != decl.SyntaxTree)
                {
                    currentTree = decl.SyntaxTree;
                    BinderFactory binderFactory = this.DeclaringCompilation.GetBinderFactory(currentTree);
                    binder = binderFactory.GetBinder(syntax);
                }

                var indexer = SourcePropertySymbol.Create(this, binder, syntax, diagnostics);
                CheckMemberNameDistinctFromType(indexer, diagnostics);

                indexerMembers.Add(indexer);
                AddAccessorIfAvailable(indexerMembers, indexer.GetMethod, diagnostics, checkName: true);
                AddAccessorIfAvailable(indexerMembers, indexer.SetMethod, diagnostics, checkName: true);
            }

            var membersByName = MergeIndexersAndNonIndexers(membersAndInitializers.NonTypeNonIndexerMembers, indexerMembers);
            indexerMembers.Free();

            // Merge types into the member dictionary
            AddNestedTypesToDictionary(membersByName, GetTypeMembersDictionary());

            return membersByName;
        }

        /// <summary>
        /// Merge (already ordered) non-type, non-indexer members with (already ordered) indexer members.
        /// </summary>
        private Dictionary<string, ImmutableArray<Symbol>> MergeIndexersAndNonIndexers(ImmutableArray<Symbol> nonIndexerMembers, ArrayBuilder<Symbol> indexerMembers)
        {
            int nonIndexerCount = nonIndexerMembers.Length;
            int indexerCount = indexerMembers.Count;

            var merged = ArrayBuilder<Symbol>.GetInstance(nonIndexerCount + indexerCount);

            int nonIndexerPos = 0;
            int indexerPos = 0;

            while (nonIndexerPos < nonIndexerCount && indexerPos < indexerCount)
            {
                var nonIndexer = nonIndexerMembers[nonIndexerPos];
                var indexer = indexerMembers[indexerPos];
                if (LexicalOrderSymbolComparer.Instance.Compare(nonIndexer, indexer) < 0)
                {
                    merged.Add(nonIndexer);
                    nonIndexerPos++;
                }
                else
                {
                    merged.Add(indexer);
                    indexerPos++;
                }
            }

            for (; nonIndexerPos < nonIndexerCount; nonIndexerPos++)
            {
                merged.Add(nonIndexerMembers[nonIndexerPos]);
            }

            for (; indexerPos < indexerCount; indexerPos++)
            {
                merged.Add(indexerMembers[indexerPos]);
            }

            var membersByName = merged.ToDictionary(s => s.Name);
            merged.Free();

            return membersByName;
        }

        private static void AddNestedTypesToDictionary(Dictionary<string, ImmutableArray<Symbol>> membersByName, Dictionary<string, ImmutableArray<NamedTypeSymbol>> typesByName)
        {
            foreach (var pair in typesByName)
            {
                string name = pair.Key;
                ImmutableArray<NamedTypeSymbol> types = pair.Value;
                ImmutableArray<Symbol> typesAsSymbols = StaticCast<Symbol>.From(types);

                ImmutableArray<Symbol> membersForName;
                if (membersByName.TryGetValue(name, out membersForName))
                {
                    membersByName[name] = membersForName.Concat(typesAsSymbols);
                }
                else
                {
                    membersByName.Add(name, typesAsSymbols);
                }
            }
        }

        private class MembersAndInitializersBuilder
        {
            public readonly ArrayBuilder<Symbol> NonTypeNonIndexerMembers = ArrayBuilder<Symbol>.GetInstance();
            public readonly ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>> StaticInitializers = ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>>.GetInstance();
            public readonly ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>> InstanceInitializers = ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>>.GetInstance();
            public readonly ArrayBuilder<SyntaxReference> IndexerDeclarations = ArrayBuilder<SyntaxReference>.GetInstance();

            public int StaticSyntaxLength;
            public int InstanceSyntaxLength;

            public MembersAndInitializers ToReadOnlyAndFree()
            {
                return new MembersAndInitializers(
                    NonTypeNonIndexerMembers.ToImmutableAndFree(),
                    StaticInitializers.ToImmutableAndFree(),
                    InstanceInitializers.ToImmutableAndFree(),
                    IndexerDeclarations.ToImmutableAndFree(),
                    StaticSyntaxLength,
                    InstanceSyntaxLength);
            }

            public void Free()
            {
                NonTypeNonIndexerMembers.Free();
                StaticInitializers.Free();
                InstanceInitializers.Free();
                IndexerDeclarations.Free();
            }
        }

        private MembersAndInitializers BuildMembersAndInitializers(DiagnosticBag diagnostics)
        {
            var builder = new MembersAndInitializersBuilder();
            AddDeclaredNontypeMembers(builder, diagnostics);

            switch (TypeKind)
            {
                case TypeKind.Struct:
                    CheckForStructBadInitializers(builder, diagnostics);
                    CheckForStructDefaultConstructors(builder.NonTypeNonIndexerMembers, isEnum: false, diagnostics: diagnostics);
                    AddSynthesizedConstructorsIfNecessary(builder.NonTypeNonIndexerMembers, builder.StaticInitializers, diagnostics);
                    break;

                case TypeKind.Enum:
                    CheckForStructDefaultConstructors(builder.NonTypeNonIndexerMembers, isEnum: true, diagnostics: diagnostics);
                    AddSynthesizedConstructorsIfNecessary(builder.NonTypeNonIndexerMembers, builder.StaticInitializers, diagnostics);
                    break;

                case TypeKind.Class:
                case TypeKind.Submission:
                    // No additional checking required.
                    AddSynthesizedConstructorsIfNecessary(builder.NonTypeNonIndexerMembers, builder.StaticInitializers, diagnostics);
                    break;

                default:
                    break;
            }

            // We already built the members and initializers on another thread, we might have detected that condition
            // during member building on this thread and bailed, which results in incomplete data in the builder.
            // In such case we have to avoid creating the instance of MemberAndInitializers since it checks the consistency
            // of the data in the builder and would fail in an assertion if we tried to construct it from incomplete builder.
            if (_lazyMembersAndInitializers != null)
            {
                builder.Free();
                return null;
            }

            return builder.ToReadOnlyAndFree();
        }

        private void AddDeclaredNontypeMembers(MembersAndInitializersBuilder builder, DiagnosticBag diagnostics)
        {
            foreach (var decl in this.declaration.Declarations)
            {
                if (!decl.HasAnyNontypeMembers)
                {
                    continue;
                }

                if (_lazyMembersAndInitializers != null)
                {
                    // membersAndInitializers is already computed. no point to continue.
                    return;
                }

                var syntax = decl.SyntaxReference.GetSyntax();

                switch (syntax.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        AddEnumMembers(builder, (EnumDeclarationSyntax)syntax, diagnostics);
                        break;

                    case SyntaxKind.DelegateDeclaration:
                        SourceDelegateMethodSymbol.AddDelegateMembers(this, builder.NonTypeNonIndexerMembers, (DelegateDeclarationSyntax)syntax, diagnostics);
                        break;

                    case SyntaxKind.NamespaceDeclaration:
                        // The members of a global anonymous type is in a syntax tree of a namespace declaration or a compilation unit.
                        AddNonTypeMembers(builder, ((NamespaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.CompilationUnit:
                        AddNonTypeMembers(builder, ((CompilationUnitSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.ClassDeclaration:
                        var classDecl = (ClassDeclarationSyntax)syntax;
                        AddNonTypeMembers(builder, classDecl.Members, diagnostics);
                        break;

                    case SyntaxKind.InterfaceDeclaration:
                        AddNonTypeMembers(builder, ((InterfaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.StructDeclaration:
                        var structDecl = (StructDeclarationSyntax)syntax;
                        AddNonTypeMembers(builder, structDecl.Members, diagnostics);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                }
            }
        }

        internal Binder GetBinder(CSharpSyntaxNode syntaxNode)
        {
            return this.DeclaringCompilation.GetBinder(syntaxNode);
        }

        private static void MergePartialMethods(
            Dictionary<string, ImmutableArray<Symbol>> membersByName,
            DiagnosticBag diagnostics)
        {
            //key and value will be the same object
            var methodsBySignature = new Dictionary<MethodSymbol, SourceMethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);

            foreach (var name in membersByName.Keys.ToArray())
            {
                methodsBySignature.Clear();
                foreach (var symbol in membersByName[name])
                {
                    var method = symbol as SourceMethodSymbol;
                    if ((object)method == null || !method.IsPartial)
                    {
                        continue; // only partial methods need to be merged
                    }

                    SourceMethodSymbol prev;
                    if (methodsBySignature.TryGetValue(method, out prev))
                    {
                        var prevPart = (SourceMemberMethodSymbol)prev;
                        var methodPart = (SourceMemberMethodSymbol)method;

                        bool hasImplementation = (object)prevPart.OtherPartOfPartial != null || prevPart.IsPartialImplementation;
                        bool hasDefinition = (object)prevPart.OtherPartOfPartial != null || prevPart.IsPartialDefinition;

                        if (hasImplementation && methodPart.IsPartialImplementation)
                        {
                            // A partial method may not have multiple implementing declarations
                            diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyOneActual, methodPart.Locations[0]);
                        }
                        else if (hasDefinition && methodPart.IsPartialDefinition)
                        {
                            // A partial method may not have multiple defining declarations
                            diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyOneLatent, methodPart.Locations[0]);
                        }
                        else
                        {
                            membersByName[name] = FixPartialMember(membersByName[name], prevPart, methodPart);
                        }
                    }
                    else
                    {
                        methodsBySignature.Add(method, method);
                    }
                }

                foreach (SourceMemberMethodSymbol method in methodsBySignature.Values)
                {
                    // partial implementations not paired with a definition
                    if (method.IsPartialImplementation && (object)method.OtherPartOfPartial == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_PartialMethodMustHaveLatent, method.Locations[0], method);
                    }
                }
            }
        }

        /// <summary>
        /// Fix up a partial method by combining its defining and implementing declarations, updating the array of symbols (by name),
        /// and returning the combined symbol.
        /// </summary>
        /// <param name="symbols">The symbols array containing both the latent and implementing declaration</param>
        /// <param name="part1">One of the two declarations</param>
        /// <param name="part2">The other declaration</param>
        /// <returns>An updated symbols array containing only one method symbol representing the two parts</returns>
        private static ImmutableArray<Symbol> FixPartialMember(ImmutableArray<Symbol> symbols, SourceMemberMethodSymbol part1, SourceMemberMethodSymbol part2)
        {
            SourceMemberMethodSymbol definition;
            SourceMemberMethodSymbol implementation;
            if (part1.IsPartialDefinition)
            {
                definition = part1;
                implementation = part2;
            }
            else
            {
                definition = part2;
                implementation = part1;
            }

            SourceMemberMethodSymbol.InitializePartialMethodParts(definition, implementation);

            // a partial method is represented in the member list by its definition part:
            var builder = ArrayBuilder<Symbol>.GetInstance();
            foreach (var s in symbols)
            {
                if (!ReferenceEquals(s, implementation))
                {
                    builder.Add(s);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private static bool DifferByOutOrRef(SourceMethodSymbol m1, SourceMethodSymbol m2)
        {
            var pl1 = m1.Parameters;
            var pl2 = m2.Parameters;
            int n = pl1.Length;
            for (int i = 0; i < n; i++)
            {
                if (pl1[i].RefKind != pl2[i].RefKind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Report an error if a member (other than a method) exists with the same name
        /// as the property accessor, or if a method exists with the same name and signature.
        /// </summary>
        private void CheckForMemberConflictWithPropertyAccessor(
            PropertySymbol propertySymbol,
            bool getNotSet,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(!propertySymbol.IsExplicitInterfaceImplementation); // checked by caller

            MethodSymbol accessor = getNotSet ? propertySymbol.GetMethod : propertySymbol.SetMethod;
            string accessorName;
            if ((object)accessor != null)
            {
                accessorName = accessor.Name;
            }
            else
            {
                string propertyName = propertySymbol.IsIndexer ? propertySymbol.MetadataName : propertySymbol.Name;
                accessorName = SourcePropertyAccessorSymbol.GetAccessorName(propertyName,
                    getNotSet,
                    propertySymbol.IsCompilationOutputWinMdObj());
            }

            foreach (var symbol in GetMembers(accessorName))
            {
                if (symbol.Kind != SymbolKind.Method)
                {
                    // The type '{0}' already contains a definition for '{1}'
                    if (Locations.Length == 1 || IsPartial)
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, GetAccessorOrPropertyLocation(propertySymbol, getNotSet), this, accessorName);
                    return;
                }
                else
                {
                    var methodSymbol = (MethodSymbol)symbol;
                    if ((methodSymbol.MethodKind == MethodKind.Ordinary) &&
                        ParametersMatchPropertyAccessor(propertySymbol, getNotSet, methodSymbol.Parameters))
                    {
                        // Type '{1}' already reserves a member called '{0}' with the same parameter types
                        diagnostics.Add(ErrorCode.ERR_MemberReserved, GetAccessorOrPropertyLocation(propertySymbol, getNotSet), accessorName, this);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Report an error if a member (other than a method) exists with the same name
        /// as the event accessor, or if a method exists with the same name and signature.
        /// </summary>
        private void CheckForMemberConflictWithEventAccessor(
            EventSymbol eventSymbol,
            bool isAdder,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(!eventSymbol.IsExplicitInterfaceImplementation); // checked by caller

            string accessorName = SourceEventSymbol.GetAccessorName(eventSymbol.Name, isAdder);

            foreach (var symbol in GetMembers(accessorName))
            {
                if (symbol.Kind != SymbolKind.Method)
                {
                    // The type '{0}' already contains a definition for '{1}'
                    if (Locations.Length == 1 || IsPartial)
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, GetAccessorOrEventLocation(eventSymbol, isAdder), this, accessorName);
                    return;
                }
                else
                {
                    var methodSymbol = (MethodSymbol)symbol;
                    if ((methodSymbol.MethodKind == MethodKind.Ordinary) &&
                        ParametersMatchEventAccessor(eventSymbol, methodSymbol.Parameters))
                    {
                        // Type '{1}' already reserves a member called '{0}' with the same parameter types
                        diagnostics.Add(ErrorCode.ERR_MemberReserved, GetAccessorOrEventLocation(eventSymbol, isAdder), accessorName, this);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Return the location of the accessor, or if no accessor, the location of the property.
        /// </summary>
        private static Location GetAccessorOrPropertyLocation(PropertySymbol propertySymbol, bool getNotSet)
        {
            var locationFrom = (Symbol)(getNotSet ? propertySymbol.GetMethod : propertySymbol.SetMethod) ?? propertySymbol;
            return locationFrom.Locations[0];
        }

        /// <summary>
        /// Return the location of the accessor, or if no accessor, the location of the event.
        /// </summary>
        private static Location GetAccessorOrEventLocation(EventSymbol propertySymbol, bool isAdder)
        {
            var locationFrom = (Symbol)(isAdder ? propertySymbol.AddMethod : propertySymbol.RemoveMethod) ?? propertySymbol;
            return locationFrom.Locations[0];
        }

        /// <summary>
        /// Return true if the method parameters match the parameters of the
        /// property accessor, including the value parameter for the setter.
        /// </summary>
        private static bool ParametersMatchPropertyAccessor(PropertySymbol propertySymbol, bool getNotSet, ImmutableArray<ParameterSymbol> methodParams)
        {
            var propertyParams = propertySymbol.Parameters;
            var numParams = propertyParams.Length + (getNotSet ? 0 : 1);
            if (numParams != methodParams.Length)
            {
                return false;
            }

            for (int i = 0; i < numParams; i++)
            {
                var methodParam = methodParams[i];
                if (methodParam.RefKind != RefKind.None)
                {
                    return false;
                }

                var propertyParamType = (((i == numParams - 1) && !getNotSet) ? propertySymbol.Type : propertyParams[i].Type).TypeSymbol;
                if (!propertyParamType.Equals(methodParam.Type.TypeSymbol, TypeSymbolEqualityOptions.SameType))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Return true if the method parameters match the parameters of the
        /// event accessor, including the value parameter.
        /// </summary>
        private static bool ParametersMatchEventAccessor(EventSymbol eventSymbol, ImmutableArray<ParameterSymbol> methodParams)
        {
            return
                methodParams.Length == 1 &&
                methodParams[0].RefKind == RefKind.None &&
                eventSymbol.Type.TypeSymbol.Equals(methodParams[0].Type.TypeSymbol, TypeSymbolEqualityOptions.SameType);
        }

        private void AddEnumMembers(MembersAndInitializersBuilder result, EnumDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            // The previous enum constant used to calculate subsequent
            // implicit enum constants. (This is the most recent explicit
            // enum constant or the first implicit constant if no explicit values.)
            SourceEnumConstantSymbol otherSymbol = null;

            // Offset from "otherSymbol".
            int otherSymbolOffset = 0;

            foreach (var member in syntax.Members)
            {
                SourceEnumConstantSymbol symbol;
                var valueOpt = member.EqualsValue;

                if (valueOpt != null)
                {
                    symbol = SourceEnumConstantSymbol.CreateExplicitValuedConstant(this, member, diagnostics);
                }
                else
                {
                    symbol = SourceEnumConstantSymbol.CreateImplicitValuedConstant(this, member, otherSymbol, otherSymbolOffset, diagnostics);
                }

                result.NonTypeNonIndexerMembers.Add(symbol);

                if (valueOpt != null || (object)otherSymbol == null)
                {
                    otherSymbol = symbol;
                    otherSymbolOffset = 1;
                }
                else
                {
                    otherSymbolOffset++;
                }
            }
        }

        private static void AddInitializer(ref ArrayBuilder<FieldOrPropertyInitializer> initializers, ref int aggregateSyntaxLength, FieldSymbol fieldOpt, CSharpSyntaxNode node)
        {
            if (initializers == null)
            {
                initializers = new ArrayBuilder<FieldOrPropertyInitializer>();
            }
            else
            {
                // initializers should be added in syntax order:
                Debug.Assert(node.SyntaxTree == initializers.Last().Syntax.SyntaxTree);
                Debug.Assert(node.SpanStart > initializers.Last().Syntax.GetSyntax().SpanStart);
            }

            int currentLength = aggregateSyntaxLength;

            // A constant field of type decimal needs a field initializer, so
            // check if it is a metadata constant, not just a constant to exclude
            // decimals. Other constants do not need field initializers.
            if (fieldOpt == null || !fieldOpt.IsMetadataConstant)
            {
                // ignore leading and trailing trivia of the node:
                aggregateSyntaxLength += node.Span.Length;
            }

            initializers.Add(new FieldOrPropertyInitializer(fieldOpt, node, currentLength));
        }

        private static void AddInitializers(ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>> allInitializers, ArrayBuilder<FieldOrPropertyInitializer> siblingsOpt)
        {
            if (siblingsOpt != null)
            {
                allInitializers.Add(siblingsOpt.ToImmutableAndFree());
            }
        }

        private static void CheckInterfaceMembers(ImmutableArray<Symbol> nonTypeMembers, DiagnosticBag diagnostics)
        {
            foreach (var member in nonTypeMembers)
            {
                CheckInterfaceMember(member, diagnostics);
            }
        }

        private static void CheckInterfaceMember(Symbol member, DiagnosticBag diagnostics)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                    if ((object)((FieldSymbol)member).AssociatedSymbol == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_InterfacesCantContainFields, member.Locations[0]);
                    }
                    break;

                case SymbolKind.Method:
                    var meth = (MethodSymbol)member;
                    switch (meth.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            diagnostics.Add(ErrorCode.ERR_InterfacesCantContainConstructors, member.Locations[0]);
                            break;
                        case MethodKind.Conversion:
                        case MethodKind.UserDefinedOperator:
                            diagnostics.Add(ErrorCode.ERR_InterfacesCantContainOperators, member.Locations[0]);
                            break;
                        case MethodKind.Destructor:
                            diagnostics.Add(ErrorCode.ERR_OnlyClassesCanContainDestructors, member.Locations[0]);
                            break;
                        case MethodKind.ExplicitInterfaceImplementation:
                        //CS0541 is handled in SourcePropertySymbol
                        case MethodKind.Ordinary:
                        case MethodKind.LocalFunction:
                        case MethodKind.PropertyGet:
                        case MethodKind.PropertySet:
                        case MethodKind.EventAdd:
                        case MethodKind.EventRemove:
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(meth.MethodKind);
                    }
                    break;

                case SymbolKind.Property:
                    break;

                case SymbolKind.Event:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        private static void CheckForStructDefaultConstructors(
            ArrayBuilder<Symbol> members,
            bool isEnum,
            DiagnosticBag diagnostics)
        {
            foreach (var s in members)
            {
                var m = s as MethodSymbol;
                if ((object)m != null)
                {
                    if (m.MethodKind == MethodKind.Constructor && m.ParameterCount == 0)
                    {
                        if (isEnum)
                        {
                            diagnostics.Add(ErrorCode.ERR_EnumsCantContainDefaultConstructor, m.Locations[0]);
                        }
                        else
                        {
                            diagnostics.Add(ErrorCode.ERR_StructsCantContainDefaultConstructor, m.Locations[0]);
                        }
                    }
                }
            }
        }

        private void CheckForStructBadInitializers(MembersAndInitializersBuilder builder, DiagnosticBag diagnostics)
        {
            Debug.Assert(TypeKind == TypeKind.Struct);

            foreach (var initializers in builder.InstanceInitializers)
            {
                foreach (FieldOrPropertyInitializer initializer in initializers)
                {
                    // '{0}': cannot have instance field initializers in structs
                    diagnostics.Add(ErrorCode.ERR_FieldInitializerInStruct, (initializer.FieldOpt.AssociatedSymbol ?? initializer.FieldOpt).Locations[0], this);
                }
            }
        }

        private void AddSynthesizedConstructorsIfNecessary(ArrayBuilder<Symbol> members, ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>> staticInitializers, DiagnosticBag diagnostics)
        {
            //we're not calling the helpers on NamedTypeSymbol base, because those call
            //GetMembers and we're inside a GetMembers call ourselves (i.e. stack overflow)
            var hasInstanceConstructor = false;
            var hasParameterlessInstanceConstructor = false;
            var hasStaticConstructor = false;

            // CONSIDER: if this traversal becomes a bottleneck, the flags could be made outputs of the
            // dictionary construction process.  For now, this is more encapsulated.
            foreach (var member in members)
            {
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)member;
                    switch (method.MethodKind)
                    {
                        case MethodKind.Constructor:
                            hasInstanceConstructor = true;
                            hasParameterlessInstanceConstructor = hasParameterlessInstanceConstructor || method.ParameterCount == 0;
                            break;

                        case MethodKind.StaticConstructor:
                            hasStaticConstructor = true;
                            break;
                    }
                }

                //kick out early if we've seen everything we're looking for
                if (hasInstanceConstructor && hasStaticConstructor)
                {
                    break;
                }
            }

            // NOTE: Per section 11.3.8 of the spec, "every struct implicitly has a parameterless instance constructor".
            // We won't insert a parameterless constructor for a struct if there already is one.
            // We don't expect anything to be emitted, but it should be in the symbol table.
            if ((!hasParameterlessInstanceConstructor && this.IsStructType()) || (!hasInstanceConstructor && !this.IsStatic))
            {
                members.Add((this.TypeKind == TypeKind.Submission) ?
                    new SynthesizedSubmissionConstructor(this, diagnostics) :
                    new SynthesizedInstanceConstructor(this));
            }

            // constants don't count, since they do not exist as fields at runtime
            // NOTE: even for decimal constants (which require field initializers), 
            // we do not create .cctor here since a static constructor implicitly created for a decimal 
            // should not appear in the list returned by public API like GetMembers().
            if (!hasStaticConstructor && HasNonConstantInitializer(staticInitializers))
            {
                // Note: we don't have to put anything in the method - the binder will
                // do that when processing field initializers.
                members.Add(new SynthesizedStaticConstructor(this));
            }

            if (this.IsScriptClass)
            {
                var scriptInitializer = new SynthesizedInteractiveInitializerMethod(this, diagnostics);
                members.Add(scriptInitializer);
                var scriptEntryPoint = SynthesizedEntryPointSymbol.Create(scriptInitializer, diagnostics);
                members.Add(scriptEntryPoint);
            }
        }

        private static bool HasNonConstantInitializer(ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>> initializers)
        {
            return initializers.Any(siblings => siblings.Any(initializer => !initializer.FieldOpt.IsConst));
        }

        private void AddNonTypeMembers(
            MembersAndInitializersBuilder builder,
            SyntaxList<MemberDeclarationSyntax> members,
            DiagnosticBag diagnostics)
        {
            if (members.Count == 0)
            {
                return;
            }

            var firstMember = members[0];
            var bodyBinder = this.GetBinder(firstMember);
            bool globalCodeAllowed = IsGlobalCodeAllowed(firstMember.Parent);

            ArrayBuilder<FieldOrPropertyInitializer> staticInitializers = null;
            ArrayBuilder<FieldOrPropertyInitializer> instanceInitializers = null;

            foreach (var m in members)
            {
                if (_lazyMembersAndInitializers != null)
                {
                    // membersAndInitializers is already computed. no point to continue.
                    return;
                }

                bool reportMisplacedGlobalCode = !globalCodeAllowed && !m.HasErrors;

                switch (m.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        {
                            var fieldSyntax = (FieldDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(fieldSyntax.Declaration.Variables.First().Identifier));
                            }

                            bool modifierErrors;
                            var modifiers = SourceMemberFieldSymbol.MakeModifiers(this, fieldSyntax.Declaration.Variables[0].Identifier, fieldSyntax.Modifiers, diagnostics, out modifierErrors);
                            foreach (var variable in fieldSyntax.Declaration.Variables)
                            {
                                var fieldSymbol = (modifiers & DeclarationModifiers.Fixed) == 0
                                    ? new SourceMemberFieldSymbol(this, variable, modifiers, modifierErrors, diagnostics)
                                    : new SourceFixedFieldSymbol(this, variable, modifiers, modifierErrors, diagnostics);
                                builder.NonTypeNonIndexerMembers.Add(fieldSymbol);

                                if (variable.Initializer != null)
                                {
                                    if (fieldSymbol.IsStatic)
                                    {
                                        AddInitializer(ref staticInitializers, ref builder.StaticSyntaxLength, fieldSymbol, variable.Initializer);
                                    }
                                    else
                                    {
                                        AddInitializer(ref instanceInitializers, ref builder.InstanceSyntaxLength, fieldSymbol, variable.Initializer);
                                    }
                                }
                            }
                        }
                        break;

                    case SyntaxKind.MethodDeclaration:
                        {
                            var methodSyntax = (MethodDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(methodSyntax.Identifier));
                            }

                            var method = SourceMemberMethodSymbol.CreateMethodSymbol(this, bodyBinder, methodSyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(method);
                        }
                        break;

                    case SyntaxKind.ConstructorDeclaration:
                        {
                            var constructorSyntax = (ConstructorDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(constructorSyntax.Identifier));
                            }

                            var constructor = SourceConstructorSymbol.CreateConstructorSymbol(this, constructorSyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(constructor);
                        }
                        break;

                    case SyntaxKind.DestructorDeclaration:
                        {
                            var destructorSyntax = (DestructorDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(destructorSyntax.Identifier));
                            }

                            // CONSIDER: if this doesn't (directly or indirectly) override object.Finalize, the
                            // runtime won't consider it a finalizer and it will not be marked as a destructor
                            // when it is loaded from metadata.  Perhaps we should just treat it as an Ordinary
                            // method in such cases?
                            var destructor = new SourceDestructorSymbol(this, destructorSyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(destructor);
                        }
                        break;

                    case SyntaxKind.PropertyDeclaration:
                        {
                            var propertySyntax = (PropertyDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(propertySyntax.Identifier));
                            }

                            var property = SourcePropertySymbol.Create(this, bodyBinder, propertySyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(property);

                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, property.GetMethod, diagnostics);
                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, property.SetMethod, diagnostics);

                            // TODO: can we leave this out of the member list?
                            // From the 10/12/11 design notes:
                            //   In addition, we will change autoproperties to behavior in 
                            //   a similar manner and make the autoproperty fields private.
                            if ((object)property.BackingField != null)
                            {
                                builder.NonTypeNonIndexerMembers.Add(property.BackingField);

                                var initializer = propertySyntax.Initializer;
                                if (initializer != null)
                                {
                                    if (property.IsStatic)
                                    {
                                        AddInitializer(ref staticInitializers, ref builder.StaticSyntaxLength, property.BackingField, initializer);
                                    }
                                    else
                                    {
                                        AddInitializer(ref instanceInitializers, ref builder.InstanceSyntaxLength, property.BackingField, initializer);
                                    }
                                }
                            }
                        }
                        break;

                    case SyntaxKind.EventFieldDeclaration:
                        {
                            var eventFieldSyntax = (EventFieldDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(
                                    ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(eventFieldSyntax.Declaration.Variables.First().Identifier));
                            }

                            foreach (VariableDeclaratorSyntax declarator in eventFieldSyntax.Declaration.Variables)
                            {
                                SourceFieldLikeEventSymbol @event = new SourceFieldLikeEventSymbol(this, bodyBinder, eventFieldSyntax.Modifiers, declarator, diagnostics);
                                builder.NonTypeNonIndexerMembers.Add(@event);

                                FieldSymbol associatedField = @event.AssociatedField;
                                if ((object)associatedField != null)
                                {
                                    // NOTE: specifically don't add the associated field to the members list
                                    // (regard it as an implementation detail).

                                    if (declarator.Initializer != null)
                                    {
                                        if (associatedField.IsStatic)
                                        {
                                            AddInitializer(ref staticInitializers, ref builder.StaticSyntaxLength, associatedField, declarator.Initializer);
                                        }
                                        else
                                        {
                                            AddInitializer(ref instanceInitializers, ref builder.InstanceSyntaxLength, associatedField, declarator.Initializer);
                                        }
                                    }
                                }

                                Debug.Assert((object)@event.AddMethod != null);
                                Debug.Assert((object)@event.RemoveMethod != null);

                                AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, @event.AddMethod, diagnostics);
                                AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, @event.RemoveMethod, diagnostics);
                            }
                        }
                        break;

                    case SyntaxKind.EventDeclaration:
                        {
                            var eventSyntax = (EventDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(eventSyntax.Identifier));
                            }

                            var @event = new SourceCustomEventSymbol(this, bodyBinder, eventSyntax, diagnostics);

                            builder.NonTypeNonIndexerMembers.Add(@event);

                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, @event.AddMethod, diagnostics);
                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, @event.RemoveMethod, diagnostics);

                            Debug.Assert((object)@event.AssociatedField == null);
                        }
                        break;

                    case SyntaxKind.IndexerDeclaration:
                        {
                            var indexerSyntax = (IndexerDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(indexerSyntax.ThisKeyword));
                            }

                            // We can't create the indexer symbol yet, because we don't know
                            // what name it will have after attribute binding (because of
                            // IndexerNameAttribute).  Instead, we'll keep a (weak) reference
                            // to the syntax and bind it again after early attribute decoding.
                            builder.IndexerDeclarations.Add(indexerSyntax.GetReference());
                        }
                        break;

                    case SyntaxKind.ConversionOperatorDeclaration:
                        {
                            var conversionOperatorSyntax = (ConversionOperatorDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(conversionOperatorSyntax.OperatorKeyword));
                            }

                            var method = SourceUserDefinedConversionSymbol.CreateUserDefinedConversionSymbol
                                (this, conversionOperatorSyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(method);
                        }
                        break;

                    case SyntaxKind.OperatorDeclaration:
                        {
                            var operatorSyntax = (OperatorDeclarationSyntax)m;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(operatorSyntax.OperatorKeyword));
                            }

                            var method = SourceUserDefinedOperatorSymbol.CreateUserDefinedOperatorSymbol
                                (this, operatorSyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(method);
                        }

                        break;

                    case SyntaxKind.GlobalStatement:
                        {
                            var globalStatement = ((GlobalStatementSyntax)m).Statement;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_GlobalStatement, new SourceLocation(globalStatement));
                            }

                            AddInitializer(ref instanceInitializers, ref builder.InstanceSyntaxLength, null, globalStatement);
                        }
                        break;

                    default:
                        Debug.Assert(
                            SyntaxFacts.IsTypeDeclaration(m.Kind()) ||
                            m.Kind() == SyntaxKind.NamespaceDeclaration ||
                            m.Kind() == SyntaxKind.IncompleteMember);
                        break;
                }
            }

            AddInitializers(builder.InstanceInitializers, instanceInitializers);
            AddInitializers(builder.StaticInitializers, staticInitializers);
        }

        private static bool IsGlobalCodeAllowed(CSharpSyntaxNode parent)
        {
            var parentKind = parent.Kind();
            return !(parentKind == SyntaxKind.NamespaceDeclaration ||
                parentKind == SyntaxKind.CompilationUnit && parent.SyntaxTree.Options.Kind == SourceCodeKind.Regular);
        }

        private void AddAccessorIfAvailable(ArrayBuilder<Symbol> symbols, MethodSymbol accessorOpt, DiagnosticBag diagnostics, bool checkName = false)
        {
            if ((object)accessorOpt != null)
            {
                symbols.Add(accessorOpt);
                if (checkName)
                {
                    CheckMemberNameDistinctFromType(accessorOpt, diagnostics);
                }
            }
        }

        #endregion

        #region Extension Methods

        internal bool ContainsExtensionMethods
        {
            get
            {
                if (!_lazyContainsExtensionMethods.HasValue())
                {
                    bool containsExtensionMethods = ((this.IsStatic && !this.IsGenericType) || this.IsScriptClass) && this.declaration.ContainsExtensionMethods;
                    _lazyContainsExtensionMethods = containsExtensionMethods.ToThreeState();
                }

                return _lazyContainsExtensionMethods.Value();
            }
        }

        internal bool AnyMemberHasAttributes
        {
            get
            {
                if (!_lazyAnyMemberHasAttributes.HasValue())
                {
                    bool anyMemberHasAttributes = this.declaration.AnyMemberHasAttributes;
                    _lazyAnyMemberHasAttributes = anyMemberHasAttributes.ToThreeState();
                }

                return _lazyAnyMemberHasAttributes.Value();
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return this.ContainsExtensionMethods;
            }
        }

        #endregion

        public sealed override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }
    }
}

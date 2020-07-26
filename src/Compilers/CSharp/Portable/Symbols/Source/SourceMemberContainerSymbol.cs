// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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
            // We current pack everything into one 32-bit int; layout is given below.
            //
            // |               |vvv|zzzz|f|d|yy|wwwwww|
            //
            // w = special type.  6 bits.
            // y = IsManagedType.  2 bits.
            // d = FieldDefinitionsNoted. 1 bit
            // f = FlattenedMembersIsSorted.  1 bit.
            // z = TypeKind. 4 bits.
            // v = NullableContext. 3 bits.
            private int _flags;

            private const int SpecialTypeOffset = 0;
            private const int SpecialTypeSize = 6;

            private const int ManagedKindOffset = SpecialTypeOffset + SpecialTypeSize;
            private const int ManagedKindSize = 2;

            private const int FieldDefinitionsNotedOffset = ManagedKindOffset + ManagedKindSize;
            private const int FieldDefinitionsNotedSize = 1;

            private const int FlattenedMembersIsSortedOffset = FieldDefinitionsNotedOffset + FieldDefinitionsNotedSize;
            private const int FlattenedMembersIsSortedSize = 1;

            private const int TypeKindOffset = FlattenedMembersIsSortedOffset + FlattenedMembersIsSortedSize;
            private const int TypeKindSize = 4;

            private const int NullableContextOffset = TypeKindOffset + TypeKindSize;
            private const int NullableContextSize = 3;

            private const int SpecialTypeMask = (1 << SpecialTypeSize) - 1;
            private const int ManagedKindMask = (1 << ManagedKindSize) - 1;
            private const int TypeKindMask = (1 << TypeKindSize) - 1;
            private const int NullableContextMask = (1 << NullableContextSize) - 1;

            private const int FieldDefinitionsNotedBit = 1 << FieldDefinitionsNotedOffset;
            private const int FlattenedMembersIsSortedBit = 1 << FlattenedMembersIsSortedOffset;


            public SpecialType SpecialType
            {
                get { return (SpecialType)((_flags >> SpecialTypeOffset) & SpecialTypeMask); }
            }

            public ManagedKind ManagedKind
            {
                get { return (ManagedKind)((_flags >> ManagedKindOffset) & ManagedKindMask); }
            }

            public bool FieldDefinitionsNoted
            {
                get { return (_flags & FieldDefinitionsNotedBit) != 0; }
            }

            // True if "lazyMembersFlattened" is sorted.
            public bool FlattenedMembersIsSorted
            {
                get { return (_flags & FlattenedMembersIsSortedBit) != 0; }
            }

            public TypeKind TypeKind
            {
                get { return (TypeKind)((_flags >> TypeKindOffset) & TypeKindMask); }
            }

#if DEBUG
            static Flags()
            {
                // Verify masks are sufficient for values.
                Debug.Assert(EnumUtilities.ContainsAllValues<SpecialType>(SpecialTypeMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<NullableContextKind>(NullableContextMask));
            }
#endif

            public Flags(SpecialType specialType, TypeKind typeKind)
            {
                int specialTypeInt = ((int)specialType & SpecialTypeMask) << SpecialTypeOffset;
                int typeKindInt = ((int)typeKind & TypeKindMask) << TypeKindOffset;

                _flags = specialTypeInt | typeKindInt;
            }

            public void SetFieldDefinitionsNoted()
            {
                ThreadSafeFlagOperations.Set(ref _flags, FieldDefinitionsNotedBit);
            }

            public void SetFlattenedMembersIsSorted()
            {
                ThreadSafeFlagOperations.Set(ref _flags, (FlattenedMembersIsSortedBit));
            }

            private static bool BitsAreUnsetOrSame(int bits, int mask)
            {
                return (bits & mask) == 0 || (bits & mask) == mask;
            }

            public void SetManagedKind(ManagedKind managedKind)
            {
                int bitsToSet = ((int)managedKind & ManagedKindMask) << ManagedKindOffset;
                Debug.Assert(BitsAreUnsetOrSame(_flags, bitsToSet));
                ThreadSafeFlagOperations.Set(ref _flags, bitsToSet);
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

        private static readonly ObjectPool<PooledDictionary<Symbol, Symbol>> s_duplicateRecordMemberSignatureDictionary =
            PooledDictionary<Symbol, Symbol>.CreatePool(MemberSignatureComparer.RecordAPISignatureComparer);

        protected SymbolCompletionState state;

        private Flags _flags;
        private ImmutableArray<DiagnosticInfo> _managedKindUseSiteDiagnostics;

        private readonly DeclarationModifiers _declModifiers;
        private readonly NamespaceOrTypeSymbol _containingSymbol;
        protected readonly MergedTypeDeclaration declaration;

        private MembersAndInitializers? _lazyMembersAndInitializers;
        private Dictionary<string, ImmutableArray<Symbol>>? _lazyMembersDictionary;
        private Dictionary<string, ImmutableArray<Symbol>>? _lazyEarlyAttributeDecodingMembersDictionary;

        private static readonly Dictionary<string, ImmutableArray<NamedTypeSymbol>> s_emptyTypeMembers = new Dictionary<string, ImmutableArray<NamedTypeSymbol>>(EmptyComparer.Instance);
        private Dictionary<string, ImmutableArray<NamedTypeSymbol>>? _lazyTypeMembers;
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
            DiagnosticBag diagnostics,
            TupleExtraData? tupleData = null)
            : base(tupleData)
        {
            _containingSymbol = containingSymbol;
            this.declaration = declaration;

            TypeKind typeKind = declaration.Kind.ToTypeKind();
            var modifiers = MakeModifiers(typeKind, diagnostics);

            foreach (var singleDeclaration in declaration.Declarations)
            {
                diagnostics.AddRange(singleDeclaration.Diagnostics);
            }

            int access = (int)(modifiers & DeclarationModifiers.AccessibilityMask);
            if ((access & (access - 1)) != 0)
            {   // more than one access modifier
                if ((modifiers & DeclarationModifiers.Partial) != 0)
                    diagnostics.Add(ErrorCode.ERR_PartialModifierConflict, Locations[0], this);
                access = access & ~(access - 1); // narrow down to one access modifier
                modifiers &= ~DeclarationModifiers.AccessibilityMask; // remove them all
                modifiers |= (DeclarationModifiers)access; // except the one
            }
            _declModifiers = modifiers;

            var specialType = access == (int)DeclarationModifiers.Public
                ? MakeSpecialType()
                : SpecialType.None;

            _flags = new Flags(specialType, typeKind);

            var containingType = this.ContainingType;
            if (containingType?.IsSealed == true && this.DeclaredAccessibility.HasProtected())
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
            Symbol containingSymbol = this.ContainingSymbol;
            DeclarationModifiers defaultAccess;
            var allowedModifiers = DeclarationModifiers.AccessibilityMask;

            if (containingSymbol.Kind == SymbolKind.Namespace)
            {
                defaultAccess = DeclarationModifiers.Internal;
            }
            else
            {
                allowedModifiers |= DeclarationModifiers.New;

                if (((NamedTypeSymbol)containingSymbol).IsInterface)
                {
                    defaultAccess = DeclarationModifiers.Public;
                }
                else
                {
                    defaultAccess = DeclarationModifiers.Private;
                }
            }

            switch (typeKind)
            {
                case TypeKind.Class:
                case TypeKind.Submission:
                    allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.Static | DeclarationModifiers.Sealed | DeclarationModifiers.Abstract
                        | DeclarationModifiers.Unsafe;
                    break;
                case TypeKind.Struct:
                    allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.Ref | DeclarationModifiers.ReadOnly | DeclarationModifiers.Unsafe;
                    break;
                case TypeKind.Interface:
                    allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;
                    break;
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
                var decl = declaration.Declarations[i];
                var mods = decl.Modifiers;

                if (partCount > 1 && (mods & DeclarationModifiers.Partial) == 0)
                {
                    missingPartial = true;
                }

                if (!modifierErrors)
                {
                    mods = ModifierUtils.CheckModifiers(
                        mods, allowedModifiers, declaration.Declarations[i].NameLocation, diagnostics,
                        modifierTokens: null, modifierErrors: out modifierErrors);

                    // It is an error for the same modifier to appear multiple times.
                    if (!modifierErrors)
                    {
                        var info = ModifierUtils.CheckAccessibility(mods, this, isExplicitInterfaceImplementation: false);
                        if (info != null)
                        {
                            diagnostics.Add(info, self.Locations[0]);
                            modifierErrors = true;
                        }
                    }
                }

                if (result == DeclarationModifiers.Unset)
                {
                    result = mods;
                }
                else
                {
                    result |= mods;
                }

            }

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
                                if (ContainingType!.Locations.Length == 1 || ContainingType.IsPartial())
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

        internal override void ForceComplete(SourceLocation? locationOpt, CancellationToken cancellationToken)
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
                            var tmp = this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics; // force type arguments
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

                            // We may produce a SymbolDeclaredEvent for the enclosing type before events for its contained members
                            DeclaringCompilation.SymbolDeclaredEvent(this);
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
                            state.NotePartComplete(CompletionPart.MembersCompleted);
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
            var membersAndInitializers = this.GetMembersAndInitializers();
            lock (membersAndInitializers)
            {
                if (!_flags.FieldDefinitionsNoted)
                {
                    var assembly = (SourceAssemblySymbol)ContainingAssembly;

                    Accessibility containerEffectiveAccessibility = EffectiveAccessibility();

                    foreach (var member in membersAndInitializers.NonTypeNonIndexerMembers)
                    {
                        FieldSymbol field;
                        if (!member.IsFieldOrFieldLikeEvent(out field) || field.IsConst || field.IsFixedSizeBuffer)
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
                    _flags.SetFieldDefinitionsNoted();
                }
            }
        }

        #endregion

        #region Containers

        public sealed override NamedTypeSymbol? ContainingType
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

        internal override ManagedKind GetManagedKind(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var managedKind = _flags.ManagedKind;
            if (managedKind == ManagedKind.Unknown || _managedKindUseSiteDiagnostics.IsDefault)
            {
                HashSet<DiagnosticInfo>? managedKindUseSiteDiagnostics = null;
                managedKind = base.GetManagedKind(ref managedKindUseSiteDiagnostics);
                ImmutableInterlocked.InterlockedExchange(ref _managedKindUseSiteDiagnostics, managedKindUseSiteDiagnostics.ToImmutableArrayOrEmpty());
                _flags.SetManagedKind(managedKind);
            }

            if (!_managedKindUseSiteDiagnostics.IsEmpty)
            {
                useSiteDiagnostics ??= new HashSet<DiagnosticInfo>();
                useSiteDiagnostics.AddAll(_managedKindUseSiteDiagnostics);
            }

            return managedKind;
        }

        public override bool IsStatic => _declModifiers.HasFlag(DeclarationModifiers.Static);

        public sealed override bool IsRefLikeType => _declModifiers.HasFlag(DeclarationModifiers.Ref);

        public override bool IsReadOnly => _declModifiers.HasFlag(DeclarationModifiers.ReadOnly);

        public override bool IsSealed => _declModifiers.HasFlag(DeclarationModifiers.Sealed);

        public override bool IsAbstract => _declModifiers.HasFlag(DeclarationModifiers.Abstract);

        internal bool IsPartial => _declModifiers.HasFlag(DeclarationModifiers.Partial);

        internal bool IsNew => _declModifiers.HasFlag(DeclarationModifiers.New);

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(_declModifiers);
            }
        }

        /// <summary>
        /// Compute the "effective accessibility" of the current class for the purpose of warnings about unused fields.
        /// </summary>
        private Accessibility EffectiveAccessibility()
        {
            var result = DeclaredAccessibility;
            if (result == Accessibility.Private) return Accessibility.Private;
            for (Symbol? container = this.ContainingType; !(container is null); container = container.ContainingType)
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

        internal bool IsRecord
        {
            get
            {
                return this.declaration.Declarations[0].Kind == DeclarationKind.Record;
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
                return SyntaxReferences;
            }
        }

        // This method behaves the same was as the base class, but avoids allocations associated with DeclaringSyntaxReferences
        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken)
        {
            var declarations = declaration.Declarations;
            if (IsImplicitlyDeclared && declarations.IsEmpty)
            {
                return ContainingSymbol.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken);
            }

            foreach (var declaration in declarations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntaxRef = declaration.SyntaxReference;
                if (syntaxRef.SyntaxTree == tree &&
                    (!definedWithinSpan.HasValue || syntaxRef.Span.IntersectsWith(definedWithinSpan.Value)))
                {
                    return true;
                }
            }

            return false;
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
        protected sealed class MembersAndInitializers
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

            if (declaration.Declarations.Length >= 1 && position == declaration.Declarations[0].Location.SourceSpan.Start)
            {
                // With dynamic analysis instrumentation, the introducing declaration of a type can provide
                // the syntax associated with both the analysis payload local of a synthesized constructor
                // and with the constructor itself. If the synthesized constructor includes an initializer with a lambda,
                // that lambda needs a closure that captures the analysis payload of the constructor,
                // and the offset of the syntax for the local within the constructor is by definition zero.
                return 0;
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
            get
            {
                return IsTupleType ? GetMembers().Select(m => m.Name).Distinct() : this.declaration.MemberNames;
            }
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
            return GetTypeMembers(name).WhereAsArray((t, arity) => t.Arity == arity, arity);
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
            var conflictDict = new Dictionary<(string, int), SourceNamedTypeSymbol>();
            try
            {
                foreach (var childDeclaration in declaration.Children)
                {
                    var t = new SourceNamedTypeSymbol(this, childDeclaration, diagnostics);
                    this.CheckMemberNameDistinctFromType(t, diagnostics);

                    var key = (t.Name, t.Arity);
                    SourceNamedTypeSymbol? other;
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
                        Binder.CheckFeatureAvailability(t.DeclaringSyntaxReferences[0].GetSyntax(), MessageID.IDS_DefaultInterfaceImplementation, diagnostics, t.Locations[0]);
                    }
                }

                Debug.Assert(s_emptyTypeMembers.Count == 0);
                return symbols.Count > 0 ?
                    symbols.ToDictionary(s => s.Name, StringOrdinalComparer.Instance) :
                    s_emptyTypeMembers;
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
                case TypeKind.Interface:
                    if (member.IsStatic)
                    {
                        goto case TypeKind.Class;
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

            return result.ConditionallyDeOrder();
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
            if (_lazyMembersDictionary != null || declaration.MemberNames.Contains(name))
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
                RoslynDebug.Assert((object)valueField != null);
                yield return valueField;
            }

            foreach (var m in this.GetMembers())
            {
                switch (m.Kind)
                {
                    case SymbolKind.Field:
                        var field = (FieldSymbol)m;
                        yield return field.TupleUnderlyingField ?? field;
                        break;
                    case SymbolKind.Event:
                        FieldSymbol? associatedField = ((EventSymbol)m).AssociatedField;
                        if ((object?)associatedField != null)
                        {
                            yield return associatedField.TupleUnderlyingField ?? associatedField;
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

                var membersByName = membersAndInitializers.NonTypeNonIndexerMembers.ToDictionary(s => s.Name);
                AddNestedTypesToDictionary(membersByName, GetTypeMembersDictionary());

                Interlocked.CompareExchange(ref _lazyEarlyAttributeDecodingMembersDictionary, membersByName, null);
            }

            return _lazyEarlyAttributeDecodingMembersDictionary;
        }

        // NOTE: this method should do as little work as possible
        //       we often need to get members just to do a lookup.
        //       All additional checks and diagnostics may be not
        //       needed yet or at all.
        protected MembersAndInitializers GetMembersAndInitializers()
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

            return membersAndInitializers!;
        }

        protected Dictionary<string, ImmutableArray<Symbol>> GetMembersByName()
        {
            if (this.state.HasComplete(CompletionPart.Members))
            {
                return _lazyMembersDictionary!;
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
                    var memberNames = ArrayBuilder<string>.GetInstance(membersDictionary.Count);
                    memberNames.AddRange(membersDictionary.Keys);
                    MergePartialMembers(memberNames, membersDictionary, diagnostics);
                    memberNames.Free();
                    AddDeclarationDiagnostics(diagnostics);
                    state.NotePartComplete(CompletionPart.Members);
                }

                diagnostics.Free();
            }

            state.SpinWaitComplete(CompletionPart.Members, default(CancellationToken));
            return _lazyMembersDictionary;
        }

        internal override IEnumerable<Symbol> GetInstanceFieldsAndEvents()
        {
            var membersAndInitializers = this.GetMembersAndInitializers();
            return membersAndInitializers.NonTypeNonIndexerMembers.Where(IsInstanceFieldOrEvent);
        }

        protected void AfterMembersChecks(DiagnosticBag diagnostics)
        {
            if (IsInterface)
            {
                CheckInterfaceMembers(this.GetMembersAndInitializers().NonTypeNonIndexerMembers, diagnostics);
            }

            CheckMemberNamesDistinctFromType(diagnostics);
            CheckMemberNameConflicts(diagnostics);
            CheckRecordMemberNames(diagnostics);
            CheckSpecialMemberErrors(diagnostics);
            CheckTypeParameterNameConflicts(diagnostics);
            CheckAccessorNameConflicts(diagnostics);

            bool unused = KnownCircularStruct;

            CheckSequentialOnPartialType(diagnostics);
            CheckForProtectedInStaticClass(diagnostics);
            CheckForUnmatchedOperators(diagnostics);

            var location = Locations[0];
            var compilation = DeclaringCompilation;

            if (this.IsRefLikeType)
            {
                compilation.EnsureIsByRefLikeAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (this.IsReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            var baseType = BaseTypeNoUseSiteDiagnostics;
            var interfaces = GetInterfacesToEmit();

            // https://github.com/dotnet/roslyn/issues/30080: Report diagnostics for base type and interfaces at more specific locations.
            if (hasBaseTypeOrInterface(t => t.ContainsNativeInteger()))
            {
                compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                if (ShouldEmitNullableContextValue(out _))
                {
                    compilation.EnsureNullableContextAttributeExists(diagnostics, location, modifyCompilation: true);
                }

                if (hasBaseTypeOrInterface(t => t.NeedsNullableAttribute()))
                {
                    compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
                }
            }

            if (interfaces.Any(t => needsTupleElementNamesAttribute(t)))
            {
                // Note: we don't need to check base type or directly implemented interfaces (which will be reported during binding)
                // so the checking of all interfaces here involves some redundancy.
                Binder.ReportMissingTupleElementNamesAttributesIfNeeded(compilation, location, diagnostics);
            }

            bool hasBaseTypeOrInterface(Func<NamedTypeSymbol, bool> predicate)
            {
                return ((object)baseType != null && predicate(baseType)) ||
                    interfaces.Any(predicate);
            }

            static bool needsTupleElementNamesAttribute(TypeSymbol type)
            {
                if (type is null)
                {
                    return false;
                }

                var resultType = type.VisitType(
                    predicate: (t, a, b) => !t.TupleElementNames.IsDefaultOrEmpty && !t.IsErrorType(),
                    arg: (object?)null);
                return resultType is object;
            }
        }

        private void CheckMemberNamesDistinctFromType(DiagnosticBag diagnostics)
        {
            foreach (var member in GetMembersAndInitializers().NonTypeNonIndexerMembers)
            {
                CheckMemberNameDistinctFromType(member, diagnostics);
            }
        }

        private void CheckRecordMemberNames(DiagnosticBag diagnostics)
        {
            if (declaration.Kind != DeclarationKind.Record)
            {
                return;
            }

            foreach (var member in GetMembers("Clone"))
            {
                diagnostics.Add(ErrorCode.ERR_CloneDisallowedInRecord, member.Locations[0]);
            }
        }

        private void CheckMemberNameConflicts(DiagnosticBag diagnostics)
        {
            Dictionary<string, ImmutableArray<Symbol>> membersByName = GetMembersByName();

            // Collisions involving indexers are handled specially.
            CheckIndexerNameConflicts(diagnostics, membersByName);

            // key and value will be the same object in these dictionaries.
            var methodsBySignature = new Dictionary<SourceMemberMethodSymbol, SourceMemberMethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);
            var conversionsAsMethods = new Dictionary<SourceMemberMethodSymbol, SourceMemberMethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);
            var conversionsAsConversions = new HashSet<SourceUserDefinedConversionSymbol>(ConversionSignatureComparer.Comparer);

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

            foreach (var pair in membersByName)
            {
                var name = pair.Key;
                Symbol lastSym = GetTypeMembers(name).FirstOrDefault();
                methodsBySignature.Clear();
                // Conversion collisions do not consider the name of the conversion,
                // so do not clear that dictionary.
                foreach (var symbol in pair.Value)
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
                            if (symbol.Kind != SymbolKind.Field || !symbol.IsImplicitlyDeclared)
                            {
                                // The type '{0}' already contains a definition for '{1}'
                                if (Locations.Length == 1 || IsPartial)
                                {
                                    diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, symbol.Locations[0], this, symbol.Name);
                                }
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
                    var method = symbol as SourceMemberMethodSymbol;
                    if (!(conversion is null))
                    {
                        // Does this conversion collide *as a conversion* with any previously-seen
                        // conversion?

                        if (!conversionsAsConversions.Add(conversion))
                        {
                            // CS0557: Duplicate user-defined conversion in type 'C'
                            diagnostics.Add(ErrorCode.ERR_DuplicateConversionInClass, conversion.Locations[0], this);
                        }
                        else
                        {
                            // The other set might already contain a conversion which would collide
                            // *as a method* with the current conversion.
                            if (!conversionsAsMethods.ContainsKey(conversion))
                            {
                                conversionsAsMethods.Add(conversion, conversion);
                            }
                        }

                        // Does this conversion collide *as a method* with any previously-seen
                        // non-conversion method?

                        if (methodsBySignature.TryGetValue(conversion, out var previousMethod))
                        {
                            ReportMethodSignatureCollision(diagnostics, conversion, previousMethod);
                        }
                        // Do not add the conversion to the set of previously-seen methods; that set
                        // is only non-conversion methods.
                    }
                    else if (!(method is null))
                    {
                        // Does this method collide *as a method* with any previously-seen
                        // conversion?

                        if (conversionsAsMethods.TryGetValue(method, out var previousConversion))
                        {
                            ReportMethodSignatureCollision(diagnostics, method, previousConversion);
                        }
                        // Do not add the method to the set of previously-seen conversions.

                        // Does this method collide *as a method* with any previously-seen
                        // non-conversion method?

                        if (methodsBySignature.TryGetValue(method, out var previousMethod))
                        {
                            ReportMethodSignatureCollision(diagnostics, method, previousMethod);
                        }
                        else
                        {
                            // We haven't seen this method before. Make a note of it in case
                            // we see a colliding method later.
                            methodsBySignature.Add(method, method);
                        }
                    }
                }
            }
        }

        // Report a name conflict; the error is reported on the location of method1.
        // UNDONE: Consider adding a secondary location pointing to the second method.
        private void ReportMethodSignatureCollision(DiagnosticBag diagnostics, SourceMemberMethodSymbol method1, SourceMemberMethodSymbol method2)
        {
            switch (method1, method2)
            {
                case (SourceOrdinaryMethodSymbol { IsPartialDefinition: true }, SourceOrdinaryMethodSymbol { IsPartialImplementation: true }):
                case (SourceOrdinaryMethodSymbol { IsPartialImplementation: true }, SourceOrdinaryMethodSymbol { IsPartialDefinition: true }):
                    // these could be 2 parts of the same partial method.
                    // Partial methods are allowed to collide by signature.
                    return;
                case (SynthesizedSimpleProgramEntryPointSymbol { }, SynthesizedSimpleProgramEntryPointSymbol { }):
                    return;
            }

            // If method1 is a constructor only because its return type is missing, then
            // we've already produced a diagnostic for the missing return type and we suppress the
            // diagnostic about duplicate signature.
            if (method1.MethodKind == MethodKind.Constructor &&
                ((ConstructorDeclarationSyntax)method1.SyntaxRef.GetSyntax()).Identifier.ValueText != this.Name)
            {
                return;
            }

            Debug.Assert(method1.ParameterCount == method2.ParameterCount);

            for (int i = 0; i < method1.ParameterCount; i++)
            {
                var refKind1 = method1.Parameters[i].RefKind;
                var refKind2 = method2.Parameters[i].RefKind;

                if (refKind1 != refKind2)
                {
                    // '{0}' cannot define an overloaded {1} that differs only on parameter modifiers '{2}' and '{3}'
                    var methodKind = method1.MethodKind == MethodKind.Constructor ? MessageID.IDS_SK_CONSTRUCTOR : MessageID.IDS_SK_METHOD;
                    diagnostics.Add(ErrorCode.ERR_OverloadRefKind, method1.Locations[0], this, methodKind.Localize(), refKind1.ToParameterDisplayString(), refKind2.ToParameterDisplayString());

                    return;
                }
            }

            // Special case: if there are two destructors, use the destructor syntax instead of "Finalize"
            var methodName = (method1.MethodKind == MethodKind.Destructor && method2.MethodKind == MethodKind.Destructor) ?
                "~" + this.Name :
                method1.Name;
            // Type '{1}' already defines a member called '{0}' with the same parameter types
            diagnostics.Add(ErrorCode.ERR_MemberAlreadyExists, method1.Locations[0], methodName, this);
        }

        private void CheckIndexerNameConflicts(DiagnosticBag diagnostics, Dictionary<string, ImmutableArray<Symbol>> membersByName)
        {
            PooledHashSet<string>? typeParameterNames = null;
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
            foreach (var members in membersByName.Values)
            {
                string? lastIndexerName = null;
                indexersBySignature.Clear();
                foreach (var symbol in members)
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
            ref string? lastIndexerName)
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

            if (indexersBySignature.TryGetValue(indexer, out var prevIndexerBySignature))
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
                    var type = field.NonPointerType();
                    if (((object)type != null) &&
                        (type.TypeKind == TypeKind.Struct) &&
                        BaseTypeAnalysis.StructDependsOn((NamedTypeSymbol)type, this) &&
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

                    if (member.DeclaredAccessibility.HasProtected())
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
                if (!op1.ParameterTypesWithAnnotations[p].Equals(op2.ParameterTypesWithAnnotations[p], TypeCompareKind.AllIgnoreOptions))
                {
                    return false;
                }
            }

            if (!op1.ReturnType.Equals(op2.ReturnType, TypeCompareKind.AllIgnoreOptions))
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
                if (f is null || !f.IsStatic || f.Type.TypeKind != TypeKind.Struct) continue;
                var type = (NamedTypeSymbol)f.Type;
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
            var tOriginal = t.OriginalDefinition;
            if (instanceMap.TryGetValue(tOriginal, out var oldInstance))
            {
                // short circuit when we find a cycle, but only return true when the cycle contains the top struct
                return (!TypeSymbol.Equals(oldInstance, t, TypeCompareKind.AllNullableIgnoreOptions)) && ReferenceEquals(tOriginal, top);
            }
            else
            {
                instanceMap.Add(tOriginal, t);
                try
                {
                    foreach (var m in t.GetMembersUnordered())
                    {
                        var f = m as FieldSymbol;
                        if (f is null || !f.IsStatic || f.Type.TypeKind != TypeKind.Struct) continue;
                        var type = (NamedTypeSymbol)f.Type;
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

            SyntaxReference? whereFoundField = null;
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
                        All(propertyDecl.AccessorList.Accessors, a => a.Body == null && a.ExpressionBody == null);
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
            // For tuples, we also need to take the slow path.
            if (membersAndInitializers.IndexerDeclarations.Length == 0 && !this.IsTupleType)
            {
                return GetEarlyAttributeDecodingMembersDictionary();
            }

            // Add indexers (plus their accessors)
            var indexerMembers = ArrayBuilder<Symbol>.GetInstance();
            Binder? binder = null;
            SyntaxTree? currentTree = null;
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

            var membersByName = mergeIndexersAndNonIndexers(membersAndInitializers.NonTypeNonIndexerMembers, indexerMembers);
            indexerMembers.Free();

            // Merge types into the member dictionary
            AddNestedTypesToDictionary(membersByName, GetTypeMembersDictionary());

            return membersByName;

            // Merge (already ordered) non-type, non-indexer members with (already ordered) indexer members.
            static Dictionary<string, ImmutableArray<Symbol>> mergeIndexersAndNonIndexers(ImmutableArray<Symbol> nonIndexerMembers, ArrayBuilder<Symbol> indexerMembers)
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

                var membersByName = merged.ToDictionary(s => s.Name, StringOrdinalComparer.Instance);
                merged.Free();

                return membersByName;
            }
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
            public ArrayBuilder<Symbol> NonTypeNonIndexerMembers { get; set; } = ArrayBuilder<Symbol>.GetInstance();
            public readonly ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>> StaticInitializers = ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>>.GetInstance();
            public readonly ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>> InstanceInitializers = ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>>.GetInstance();
            public readonly ArrayBuilder<SyntaxReference> IndexerDeclarations = ArrayBuilder<SyntaxReference>.GetInstance();
            public RecordDeclarationSyntax? RecordDeclarationWithParameters;
            public ArrayBuilder<FieldOrPropertyInitializer.Builder>? InstanceInitializersForRecordDeclarationWithParameters;


            public MembersAndInitializers ToReadOnlyAndFree()
            {
                return new MembersAndInitializers(
                    NonTypeNonIndexerMembers.ToImmutableAndFree(),
                    ToReadonlyAndFree(StaticInitializers, out int staticInitializersSyntaxLength),
                    ToReadonlyAndFree(InstanceInitializers, out int instanceInitializersSyntaxLength),
                    IndexerDeclarations.ToImmutableAndFree(),
                    staticInitializersSyntaxLength,
                    instanceInitializersSyntaxLength);
            }

            private static ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> ToReadonlyAndFree(ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>> initializers, out int syntaxLength)
            {
                syntaxLength = 0;

                if (initializers.Count == 0)
                {
                    initializers.Free();
                    return ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>>.Empty;
                }

                var builder = ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>>.GetInstance(initializers.Count);

                foreach (ArrayBuilder<FieldOrPropertyInitializer.Builder> group in initializers)
                {
                    builder.Add(ToReadonlyAndFree(group, ref syntaxLength));
                }

                initializers.Free();
                return builder.ToImmutableAndFree();
            }

            private static ImmutableArray<FieldOrPropertyInitializer> ToReadonlyAndFree(ArrayBuilder<FieldOrPropertyInitializer.Builder> group, ref int syntaxLength)
            {
                if (group.Count == 0)
                {
                    group.Free();
                    return ImmutableArray<FieldOrPropertyInitializer>.Empty;
                }

                var builder = ArrayBuilder<FieldOrPropertyInitializer>.GetInstance(group.Count);

                foreach (FieldOrPropertyInitializer.Builder initializer in group)
                {
                    builder.Add(new FieldOrPropertyInitializer(initializer.FieldOpt, initializer.Syntax, syntaxLength));

                    // A constant field of type decimal needs a field initializer, so
                    // check if it is a metadata constant, not just a constant to exclude
                    // decimals. Other constants do not need field initializers.
                    if (initializer.FieldOpt == null || !initializer.FieldOpt.IsMetadataConstant)
                    {
                        // ignore leading and trailing trivia of the node:
                        syntaxLength += initializer.Syntax.Span.Length;
                    }
                }

                group.Free();
                return builder.ToImmutableAndFree();
            }

            public void Free()
            {
                NonTypeNonIndexerMembers.Free();
                StaticInitializers.Free();
                InstanceInitializers.Free();
                IndexerDeclarations.Free();
            }

            internal void AddOrWrapTupleMembers(SourceMemberContainerTypeSymbol type)
            {
                this.NonTypeNonIndexerMembers = type.AddOrWrapTupleMembers(this.NonTypeNonIndexerMembers.ToImmutableAndFree());
            }
        }

        protected virtual MembersAndInitializers? BuildMembersAndInitializers(DiagnosticBag diagnostics)
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
                case TypeKind.Interface:
                case TypeKind.Submission:
                    // No additional checking required.
                    AddSynthesizedRecordMembersIfNecessary(builder, diagnostics);
                    AddSynthesizedConstructorsIfNecessary(builder.NonTypeNonIndexerMembers, builder.StaticInitializers, diagnostics);
                    break;

                default:
                    break;
            }

            if (IsTupleType)
            {
                builder.AddOrWrapTupleMembers(this);
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
                        AddNonTypeMembers(builder, instanceInitializers: null, ((NamespaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.CompilationUnit:
                        AddNonTypeMembers(builder, instanceInitializers: null, ((CompilationUnitSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        var typeDecl = (TypeDeclarationSyntax)syntax;
                        AddNonTypeMembers(builder, instanceInitializers: null, typeDecl.Members, diagnostics);
                        break;

                    case SyntaxKind.RecordDeclaration:
                        var recordDecl = (RecordDeclarationSyntax)syntax;
                        AddNonTypeMembers(builder,
                            instanceInitializers: noteRecordParameters(recordDecl, builder, diagnostics),
                            recordDecl.Members,
                            diagnostics);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                }
            }

            static ArrayBuilder<FieldOrPropertyInitializer.Builder>? noteRecordParameters(RecordDeclarationSyntax syntax, MembersAndInitializersBuilder builder, DiagnosticBag diagnostics)
            {
                var parameterList = syntax.ParameterList;
                if (parameterList is null)
                {
                    return null;
                }

                if (builder.RecordDeclarationWithParameters is null)
                {
                    builder.RecordDeclarationWithParameters = syntax;
                    builder.InstanceInitializersForRecordDeclarationWithParameters = new ArrayBuilder<FieldOrPropertyInitializer.Builder>();
                    return builder.InstanceInitializersForRecordDeclarationWithParameters;
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_MultipleRecordParameterLists, parameterList.Location);
                    return null;
                }
            }
        }

        internal Binder GetBinder(CSharpSyntaxNode syntaxNode)
        {
            return this.DeclaringCompilation.GetBinder(syntaxNode);
        }

        private static void MergePartialMembers(
            ArrayBuilder<string> memberNames,
            Dictionary<string, ImmutableArray<Symbol>> membersByName,
            DiagnosticBag diagnostics)
        {
            //key and value will be the same object
            var methodsBySignature = new Dictionary<MethodSymbol, SourceMemberMethodSymbol>(MemberSignatureComparer.PartialMethodsComparer);

            foreach (var name in memberNames)
            {
                methodsBySignature.Clear();
                foreach (var symbol in membersByName[name])
                {
                    var method = symbol as SourceMemberMethodSymbol;
                    if (method is null || !method.IsPartial)
                    {
                        continue; // only partial methods need to be merged
                    }

                    if (methodsBySignature.TryGetValue(method, out var prev))
                    {
                        var prevPart = (SourceOrdinaryMethodSymbol)prev;
                        var methodPart = (SourceOrdinaryMethodSymbol)method;

                        bool hasImplementation = (object?)prevPart.OtherPartOfPartial != null || prevPart.IsPartialImplementation;
                        bool hasDefinition = (object?)prevPart.OtherPartOfPartial != null || prevPart.IsPartialDefinition;

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

                foreach (SourceOrdinaryMethodSymbol method in methodsBySignature.Values)
                {
                    // partial implementations not paired with a definition
                    if (method.IsPartialImplementation && method.OtherPartOfPartial is null)
                    {
                        diagnostics.Add(ErrorCode.ERR_PartialMethodMustHaveLatent, method.Locations[0], method);
                    }
                    else if (!(method.OtherPartOfPartial is null) && MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(method, method.OtherPartOfPartial))
                    {
                        diagnostics.Add(ErrorCode.ERR_PartialMethodInconsistentTupleNames, method.Locations[0], method, method.OtherPartOfPartial);
                    }
                    else if (method is { IsPartialDefinition: true, OtherPartOfPartial: null, HasExplicitAccessModifier: true })
                    {
                        diagnostics.Add(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, method.Locations[0], method);
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
        private static ImmutableArray<Symbol> FixPartialMember(ImmutableArray<Symbol> symbols, SourceOrdinaryMethodSymbol part1, SourceOrdinaryMethodSymbol part2)
        {
            SourceOrdinaryMethodSymbol definition;
            SourceOrdinaryMethodSymbol implementation;
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

            SourceOrdinaryMethodSymbol.InitializePartialMethodParts(definition, implementation);

            // a partial method is represented in the member list by its definition part:
            return Remove(symbols, implementation);
        }

        private static ImmutableArray<Symbol> Remove(ImmutableArray<Symbol> symbols, Symbol symbol)
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();
            foreach (var s in symbols)
            {
                if (!ReferenceEquals(s, symbol))
                {
                    builder.Add(s);
                }
            }
            return builder.ToImmutableAndFree();
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
            var locationFrom = (Symbol?)(isAdder ? propertySymbol.AddMethod : propertySymbol.RemoveMethod) ?? propertySymbol;
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

                var propertyParamType = (((i == numParams - 1) && !getNotSet) ? propertySymbol.TypeWithAnnotations : propertyParams[i].TypeWithAnnotations).Type;
                if (!propertyParamType.Equals(methodParam.Type, TypeCompareKind.AllIgnoreOptions))
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
                eventSymbol.Type.Equals(methodParams[0].Type, TypeCompareKind.AllIgnoreOptions);
        }

        private void AddEnumMembers(MembersAndInitializersBuilder result, EnumDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            // The previous enum constant used to calculate subsequent
            // implicit enum constants. (This is the most recent explicit
            // enum constant or the first implicit constant if no explicit values.)
            SourceEnumConstantSymbol? otherSymbol = null;

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

                if (valueOpt != null || otherSymbol is null)
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

        private static void AddInitializer(ref ArrayBuilder<FieldOrPropertyInitializer.Builder>? initializers, FieldSymbol? fieldOpt, CSharpSyntaxNode node)
        {
            if (initializers == null)
            {
                initializers = new ArrayBuilder<FieldOrPropertyInitializer.Builder>();
            }
            else if (initializers.Count != 0)
            {
                // initializers should be added in syntax order:
                Debug.Assert(node.SyntaxTree == initializers.Last().Syntax.SyntaxTree);
                Debug.Assert(node.SpanStart > initializers.Last().Syntax.SpanStart);
            }

            initializers.Add(new FieldOrPropertyInitializer.Builder(fieldOpt, node));
        }

        private static void AddInitializers(
            ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>> allInitializers,
            ArrayBuilder<FieldOrPropertyInitializer.Builder>? siblingsOpt)
        {
            if (siblingsOpt != null)
            {
                allInitializers.Add(siblingsOpt);
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
                    break;

                case SymbolKind.Method:
                    var meth = (MethodSymbol)member;
                    switch (meth.MethodKind)
                    {
                        case MethodKind.Constructor:
                            diagnostics.Add(ErrorCode.ERR_InterfacesCantContainConstructors, member.Locations[0]);
                            break;
                        case MethodKind.Conversion:
                            diagnostics.Add(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, member.Locations[0]);
                            break;
                        case MethodKind.UserDefinedOperator:
                            if (meth.Name == WellKnownMemberNames.EqualityOperatorName || meth.Name == WellKnownMemberNames.InequalityOperatorName)
                            {
                                diagnostics.Add(ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators, member.Locations[0]);
                            }
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
                        case MethodKind.StaticConstructor:
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
                if (!(m is null))
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
                foreach (FieldOrPropertyInitializer.Builder initializer in initializers)
                {
                    // '{0}': cannot have instance field initializers in structs
                    diagnostics.Add(ErrorCode.ERR_FieldInitializerInStruct, (initializer.FieldOpt.AssociatedSymbol ?? initializer.FieldOpt).Locations[0], this);
                }
            }
        }

        private void AddSynthesizedRecordMembersIfNecessary(MembersAndInitializersBuilder builder, DiagnosticBag diagnostics)
        {
            if (declaration.Kind != DeclarationKind.Record)
            {
                return;
            }

            ParameterListSyntax? paramList = builder.RecordDeclarationWithParameters?.ParameterList;

            var memberSignatures = s_duplicateRecordMemberSignatureDictionary.Allocate();
            var members = ArrayBuilder<Symbol>.GetInstance(builder.NonTypeNonIndexerMembers.Count + 1);
            foreach (var member in builder.NonTypeNonIndexerMembers)
            {
                if (!memberSignatures.ContainsKey(member))
                {
                    memberSignatures.Add(member, member);
                }
            }

            CSharpCompilation compilation = this.DeclaringCompilation;

            // Positional record
            if (!(paramList is null))
            {
                Debug.Assert(builder.RecordDeclarationWithParameters is object);
                Debug.Assert(builder.InstanceInitializersForRecordDeclarationWithParameters is object);

                // https://github.com/dotnet/roslyn/issues/44677
                // The semantics of an empty parameter list have not been decided. Error for now
                if (paramList.ParameterCount == 0)
                {
                    diagnostics.Add(ErrorCode.ERR_BadRecordDeclaration, paramList.Location);
                }

                var ctor = addCtor(builder.RecordDeclarationWithParameters);
                var existingOrAddedMembers = addProperties(ctor.Parameters);
                addDeconstruct(ctor, existingOrAddedMembers);
            }

            addCopyCtor();
            addCloneMethod();

            PropertySymbol equalityContract = addEqualityContract();

            var thisEquals = addThisEquals(equalityContract);
            addOtherEquals();
            addObjectEquals(thisEquals);
            addHashCode(equalityContract);

            memberSignatures.Free();

            // We put synthesized record members first so that errors about conflicts show up on user-defined members rather than all
            // going to the record declaration
            members.AddRange(builder.NonTypeNonIndexerMembers);
            builder.NonTypeNonIndexerMembers.Free();
            builder.NonTypeNonIndexerMembers = members;

            return;

            SynthesizedRecordConstructor addCtor(RecordDeclarationSyntax declWithParameters)
            {
                Debug.Assert(declWithParameters.ParameterList is object);
                var ctor = new SynthesizedRecordConstructor(this, declWithParameters, diagnostics);
                members.Add(ctor);
                return ctor;
            }

            void addDeconstruct(SynthesizedRecordConstructor ctor, ImmutableArray<PropertySymbol> properties)
            {
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.DeconstructMethodName,
                    this,
                    MethodKind.Ordinary,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ctor.Parameters.SelectAsArray<ParameterSymbol, ParameterSymbol>(param => new SignatureOnlyParameterSymbol(param.TypeWithAnnotations,
                                                                                                                              ImmutableArray<CustomModifier>.Empty,
                                                                                                                              isParams: false,
                                                                                                                              RefKind.Out
                                                                                                                              )),
                    RefKind.None,
                    isInitOnly: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Void)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingDeconstructMethod))
                {
                    members.Add(new SynthesizedRecordDeconstruct(this, ctor, properties, memberOffset: members.Count, diagnostics));
                }
                else
                {
                    var deconstruct = (MethodSymbol)existingDeconstructMethod;

                    if (deconstruct.DeclaredAccessibility != Accessibility.Public)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonPublicAPIInRecord, deconstruct.Locations[0], deconstruct);
                    }

                    if (deconstruct.ReturnType.SpecialType != SpecialType.System_Void && !deconstruct.ReturnType.IsErrorType())
                    {
                        diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, deconstruct.Locations[0], deconstruct, targetMethod.ReturnType);
                    }

                    if (deconstruct.IsStatic)
                    {
                        diagnostics.Add(ErrorCode.ERR_StaticAPIInRecord, deconstruct.Locations[0], deconstruct);
                    }
                }
            }

            void addCopyCtor()
            {
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.InstanceConstructorName,
                    this,
                    MethodKind.Constructor,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ImmutableArray.Create<ParameterSymbol>(new SignatureOnlyParameterSymbol(
                                                                TypeWithAnnotations.Create(this),
                                                                ImmutableArray<CustomModifier>.Empty,
                                                                isParams: false,
                                                                RefKind.None
                                                                )),
                    RefKind.None,
                    isInitOnly: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Void)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingConstructor))
                {
                    members.Add(new SynthesizedRecordCopyCtor(this, memberOffset: members.Count));
                }
                else
                {
                    var constructor = (MethodSymbol)existingConstructor;

                    if (!this.IsSealed && (constructor.DeclaredAccessibility != Accessibility.Public && constructor.DeclaredAccessibility != Accessibility.Protected))
                    {
                        diagnostics.Add(ErrorCode.ERR_CopyConstructorWrongAccessibility, constructor.Locations[0], constructor);
                    }
                }
            }

            void addCloneMethod()
            {
                members.Add(new SynthesizedRecordClone(this, memberOffset: members.Count, diagnostics));
            }

            ImmutableArray<PropertySymbol> addProperties(ImmutableArray<ParameterSymbol> recordParameters)
            {
                var existingOrAddedMembers = ArrayBuilder<PropertySymbol>.GetInstance(recordParameters.Length);
                int addedCount = 0;
                foreach (ParameterSymbol param in recordParameters)
                {
                    bool isInherited = false;
                    var syntax = param.GetNonNullSyntaxNode();

                    var targetProperty = new SignatureOnlyPropertySymbol(param.Name,
                                                                         this,
                                                                         ImmutableArray<ParameterSymbol>.Empty,
                                                                         RefKind.None,
                                                                         param.TypeWithAnnotations,
                                                                         ImmutableArray<CustomModifier>.Empty,
                                                                         isStatic: false,
                                                                         ImmutableArray<PropertySymbol>.Empty);

                    if (!memberSignatures.TryGetValue(targetProperty, out var existingMember))
                    {
                        existingMember = OverriddenOrHiddenMembersHelpers.FindFirstHiddenMemberIfAny(targetProperty, memberIsFromSomeCompilation: true);
                        isInherited = true;
                    }
                    if (existingMember is null)
                    {
                        addProperty(new SynthesizedRecordPropertySymbol(this, syntax, param, isOverride: false, diagnostics));
                    }
                    else if (existingMember is PropertySymbol { IsStatic: false, GetMethod: { } } prop
                        && prop.TypeWithAnnotations.Equals(param.TypeWithAnnotations, TypeCompareKind.AllIgnoreOptions))
                    {
                        // There already exists a member corresponding to the candidate synthesized property.
                        if (isInherited && prop.IsAbstract)
                        {
                            addProperty(new SynthesizedRecordPropertySymbol(this, syntax, param, isOverride: true, diagnostics));
                        }
                        else
                        {
                            // Deconstruct() is specified to simply assign from this property to the corresponding out parameter.
                            existingOrAddedMembers.Add(prop);
                        }
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_BadRecordMemberForPositionalParameter,
                            param.Locations[0],
                            new FormattedSymbol(existingMember, SymbolDisplayFormat.CSharpErrorMessageFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)),
                            param.TypeWithAnnotations,
                            param.Name);
                    }

                    void addProperty(SynthesizedRecordPropertySymbol property)
                    {
                        existingOrAddedMembers.Add(property);
                        members.Add(property);
                        members.Add(property.GetMethod);
                        members.Add(property.SetMethod);
                        members.Add(property.BackingField);

                        builder.InstanceInitializersForRecordDeclarationWithParameters.Insert(addedCount, new FieldOrPropertyInitializer.Builder(property.BackingField, paramList.Parameters[param.Ordinal]));
                        addedCount++;
                    }
                }

#if DEBUG
                if (addedCount != 0 && builder.InstanceInitializersForRecordDeclarationWithParameters.Count != addedCount)
                {
                    // initializers should be added in syntax order:
                    Debug.Assert(builder.InstanceInitializersForRecordDeclarationWithParameters[addedCount - 1].Syntax.SyntaxTree == builder.InstanceInitializersForRecordDeclarationWithParameters[addedCount].Syntax.SyntaxTree);
                    Debug.Assert(builder.InstanceInitializersForRecordDeclarationWithParameters[addedCount - 1].Syntax.SpanStart < builder.InstanceInitializersForRecordDeclarationWithParameters[addedCount].Syntax.SpanStart);
                }
#endif
                return existingOrAddedMembers.ToImmutableAndFree();
            }

            void addObjectEquals(MethodSymbol thisEquals)
            {
                members.Add(new SynthesizedRecordObjEquals(this, thisEquals, memberOffset: members.Count, diagnostics));
            }

            void addHashCode(PropertySymbol equalityContract)
            {
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.ObjectGetHashCode,
                    this,
                    MethodKind.Ordinary,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ImmutableArray<ParameterSymbol>.Empty,
                    RefKind.None,
                    isInitOnly: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Int32)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingHashCodeMethod))
                {
                    var hashCode = new SynthesizedRecordGetHashCode(this, equalityContract, memberOffset: members.Count, diagnostics);
                    members.Add(hashCode);
                }
                else
                {
                    var method = (MethodSymbol)existingHashCodeMethod;
                    if (!SynthesizedRecordObjectMethod.VerifyOverridesMethodFromObject(method, SpecialType.System_Int32, diagnostics) && method.IsSealed && !IsSealed)
                    {
                        diagnostics.Add(ErrorCode.ERR_SealedGetHashCodeInRecord, method.Locations[0], method);
                    }
                }
            }

            PropertySymbol addEqualityContract()
            {
                var targetProperty = new SignatureOnlyPropertySymbol(SynthesizedRecordEqualityContractProperty.PropertyName,
                                                                     this,
                                                                     ImmutableArray<ParameterSymbol>.Empty,
                                                                     RefKind.None,
                                                                     TypeWithAnnotations.Create(compilation.GetWellKnownType(WellKnownType.System_Type)),
                                                                     ImmutableArray<CustomModifier>.Empty,
                                                                     isStatic: false,
                                                                     ImmutableArray<PropertySymbol>.Empty);

                PropertySymbol equalityContract;

                if (!memberSignatures.TryGetValue(targetProperty, out Symbol? existingEqualityContractProperty))
                {
                    equalityContract = new SynthesizedRecordEqualityContractProperty(this, diagnostics);
                    members.Add(equalityContract);
                    members.Add(equalityContract.GetMethod);
                }
                else
                {
                    equalityContract = (PropertySymbol)existingEqualityContractProperty;

                    if (this.IsSealed && this.BaseTypeNoUseSiteDiagnostics.IsObjectType())
                    {
                        if (equalityContract.DeclaredAccessibility != Accessibility.Private)
                        {
                            diagnostics.Add(ErrorCode.ERR_NonPrivateAPIInRecord, equalityContract.Locations[0], equalityContract);
                        }
                    }
                    else if (equalityContract.DeclaredAccessibility != Accessibility.Protected)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonProtectedAPIInRecord, equalityContract.Locations[0], equalityContract);
                    }

                    if (!equalityContract.Type.Equals(targetProperty.Type, TypeCompareKind.AllIgnoreOptions))
                    {
                        if (!equalityContract.Type.IsErrorType())
                        {
                            diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, equalityContract.Locations[0], equalityContract, targetProperty.Type);
                        }
                    }
                    else
                    {
                        SynthesizedRecordEqualityContractProperty.VerifyOverridesEqualityContractFromBase(equalityContract, diagnostics);
                    }

                    reportStaticOrNotOverridableAPIInRecord(equalityContract, diagnostics);
                }

                return equalityContract;
            }

            MethodSymbol addThisEquals(PropertySymbol equalityContract)
            {
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.ObjectEquals,
                    this,
                    MethodKind.Ordinary,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ImmutableArray.Create<ParameterSymbol>(new SignatureOnlyParameterSymbol(
                                                                TypeWithAnnotations.Create(this),
                                                                ImmutableArray<CustomModifier>.Empty,
                                                                isParams: false,
                                                                RefKind.None
                                                                )),
                    RefKind.None,
                    isInitOnly: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Boolean)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                MethodSymbol thisEquals;

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingEqualsMethod))
                {
                    thisEquals = new SynthesizedRecordEquals(this, equalityContract, memberOffset: members.Count, diagnostics);
                    members.Add(thisEquals);
                }
                else
                {
                    thisEquals = (MethodSymbol)existingEqualsMethod;

                    if (thisEquals.DeclaredAccessibility != Accessibility.Public)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonPublicAPIInRecord, thisEquals.Locations[0], thisEquals);
                    }

                    if (thisEquals.ReturnType.SpecialType != SpecialType.System_Boolean && !thisEquals.ReturnType.IsErrorType())
                    {
                        diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, thisEquals.Locations[0], thisEquals, targetMethod.ReturnType);
                    }

                    reportStaticOrNotOverridableAPIInRecord(thisEquals, diagnostics);
                }

                return thisEquals;
            }

            void reportStaticOrNotOverridableAPIInRecord(Symbol symbol, DiagnosticBag diagnostics)
            {
                if (!IsSealed &&
                    ((!symbol.IsAbstract && !symbol.IsVirtual && !symbol.IsOverride) || symbol.IsSealed))
                {
                    diagnostics.Add(ErrorCode.ERR_NotOverridableAPIInRecord, symbol.Locations[0], symbol);
                }
                else if (symbol.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_StaticAPIInRecord, symbol.Locations[0], symbol);
                }
            }

            void addOtherEquals()
            {
                if (!BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    members.Add(new SynthesizedRecordBaseEquals(this, memberOffset: members.Count, diagnostics));
                }
            }
        }

        private void AddSynthesizedConstructorsIfNecessary(ArrayBuilder<Symbol> members, ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>> staticInitializers, DiagnosticBag diagnostics)
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
                            // Ignore the record copy constructor
                            if (!(method is SynthesizedRecordCopyCtor))
                            {
                                hasInstanceConstructor = true;
                                hasParameterlessInstanceConstructor = hasParameterlessInstanceConstructor || method.ParameterCount == 0;
                            }
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
            if ((!hasParameterlessInstanceConstructor && this.IsStructType()) ||
                (!hasInstanceConstructor && !this.IsStatic && !this.IsInterface))
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

        private static bool HasNonConstantInitializer(ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer.Builder>> initializers)
        {
            return initializers.Any(siblings => siblings.Any(initializer => !initializer.FieldOpt.IsConst));
        }

        private void AddNonTypeMembers(
            MembersAndInitializersBuilder builder,
            ArrayBuilder<FieldOrPropertyInitializer.Builder>? instanceInitializers,
            SyntaxList<MemberDeclarationSyntax> members,
            DiagnosticBag diagnostics)
        {
            if (members.Count == 0)
            {
                AddInitializers(builder.InstanceInitializers, instanceInitializers);
                return;
            }

            var firstMember = members[0];
            var bodyBinder = this.GetBinder(firstMember);

            ArrayBuilder<FieldOrPropertyInitializer.Builder>? staticInitializers = null;

            foreach (var m in members)
            {
                if (_lazyMembersAndInitializers != null)
                {
                    // membersAndInitializers is already computed. no point to continue.
                    return;
                }

                bool reportMisplacedGlobalCode = !m.HasErrors;

                switch (m.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        {
                            var fieldSyntax = (FieldDeclarationSyntax)m;
                            if (IsImplicitClass && reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(fieldSyntax.Declaration.Variables.First().Identifier));
                            }

                            bool modifierErrors;
                            var modifiers = SourceMemberFieldSymbol.MakeModifiers(this, fieldSyntax.Declaration.Variables[0].Identifier, fieldSyntax.Modifiers, diagnostics, out modifierErrors);
                            foreach (var variable in fieldSyntax.Declaration.Variables)
                            {
                                var fieldSymbol = (modifiers & DeclarationModifiers.Fixed) == 0
                                    ? new SourceMemberFieldSymbolFromDeclarator(this, variable, modifiers, modifierErrors, diagnostics)
                                    : new SourceFixedFieldSymbol(this, variable, modifiers, modifierErrors, diagnostics);
                                builder.NonTypeNonIndexerMembers.Add(fieldSymbol);

                                if (IsScriptClass)
                                {
                                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                                    ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeNonIndexerMembers, variable, this,
                                                            DeclarationModifiers.Private | (modifiers & DeclarationModifiers.Static),
                                                            fieldSymbol);
                                }

                                if (variable.Initializer != null)
                                {
                                    if (fieldSymbol.IsStatic)
                                    {
                                        AddInitializer(ref staticInitializers, fieldSymbol, variable.Initializer);
                                    }
                                    else
                                    {
                                        AddInitializer(ref instanceInitializers, fieldSymbol, variable.Initializer);
                                    }
                                }
                            }
                        }
                        break;

                    case SyntaxKind.MethodDeclaration:
                        {
                            var methodSyntax = (MethodDeclarationSyntax)m;
                            if (IsImplicitClass && reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(methodSyntax.Identifier));
                            }

                            var method = SourceOrdinaryMethodSymbol.CreateMethodSymbol(this, bodyBinder, methodSyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(method);
                        }
                        break;

                    case SyntaxKind.ConstructorDeclaration:
                        {
                            var constructorSyntax = (ConstructorDeclarationSyntax)m;
                            if (IsImplicitClass && reportMisplacedGlobalCode)
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
                            if (IsImplicitClass && reportMisplacedGlobalCode)
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
                            if (IsImplicitClass && reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(propertySyntax.Identifier));
                            }

                            var property = SourcePropertySymbol.Create(this, bodyBinder, propertySyntax, diagnostics);
                            builder.NonTypeNonIndexerMembers.Add(property);

                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, property.GetMethod, diagnostics);
                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, property.SetMethod, diagnostics);
                            FieldSymbol backingField = property.BackingField;

                            // TODO: can we leave this out of the member list?
                            // From the 10/12/11 design notes:
                            //   In addition, we will change autoproperties to behavior in
                            //   a similar manner and make the autoproperty fields private.
                            if ((object)backingField != null)
                            {
                                builder.NonTypeNonIndexerMembers.Add(backingField);

                                var initializer = propertySyntax.Initializer;
                                if (initializer != null)
                                {
                                    if (IsScriptClass)
                                    {
                                        // also gather expression-declared variables from the initializer
                                        ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeNonIndexerMembers,
                                                                                      initializer,
                                                                                      this,
                                                                                      DeclarationModifiers.Private | (property.IsStatic ? DeclarationModifiers.Static : 0),
                                                                                      backingField);
                                    }

                                    if (property.IsStatic)
                                    {
                                        AddInitializer(ref staticInitializers, backingField, initializer);
                                    }
                                    else
                                    {
                                        AddInitializer(ref instanceInitializers, backingField, initializer);
                                    }
                                }
                            }
                        }
                        break;

                    case SyntaxKind.EventFieldDeclaration:
                        {
                            var eventFieldSyntax = (EventFieldDeclarationSyntax)m;
                            if (IsImplicitClass && reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(
                                    ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(eventFieldSyntax.Declaration.Variables.First().Identifier));
                            }

                            foreach (VariableDeclaratorSyntax declarator in eventFieldSyntax.Declaration.Variables)
                            {
                                SourceFieldLikeEventSymbol @event = new SourceFieldLikeEventSymbol(this, bodyBinder, eventFieldSyntax.Modifiers, declarator, diagnostics);
                                builder.NonTypeNonIndexerMembers.Add(@event);

                                FieldSymbol? associatedField = @event.AssociatedField;

                                if (IsScriptClass)
                                {
                                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                                    ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeNonIndexerMembers, declarator, this,
                                                            DeclarationModifiers.Private | (@event.IsStatic ? DeclarationModifiers.Static : 0),
                                                            associatedField);
                                }

                                if ((object?)associatedField != null)
                                {
                                    // NOTE: specifically don't add the associated field to the members list
                                    // (regard it as an implementation detail).

                                    if (declarator.Initializer != null)
                                    {
                                        if (associatedField.IsStatic)
                                        {
                                            AddInitializer(ref staticInitializers, associatedField, declarator.Initializer);
                                        }
                                        else
                                        {
                                            AddInitializer(ref instanceInitializers, associatedField, declarator.Initializer);
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
                            if (IsImplicitClass && reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(eventSyntax.Identifier));
                            }

                            var @event = new SourceCustomEventSymbol(this, bodyBinder, eventSyntax, diagnostics);

                            builder.NonTypeNonIndexerMembers.Add(@event);

                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, @event.AddMethod, diagnostics);
                            AddAccessorIfAvailable(builder.NonTypeNonIndexerMembers, @event.RemoveMethod, diagnostics);

                            Debug.Assert(@event.AssociatedField is null);
                        }
                        break;

                    case SyntaxKind.IndexerDeclaration:
                        {
                            var indexerSyntax = (IndexerDeclarationSyntax)m;
                            if (IsImplicitClass && reportMisplacedGlobalCode)
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
                            if (IsImplicitClass && reportMisplacedGlobalCode)
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
                            if (IsImplicitClass && reportMisplacedGlobalCode)
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

                            if (IsScriptClass)
                            {
                                var innerStatement = globalStatement;

                                // drill into any LabeledStatements
                                while (innerStatement.Kind() == SyntaxKind.LabeledStatement)
                                {
                                    innerStatement = ((LabeledStatementSyntax)innerStatement).Statement;
                                }

                                switch (innerStatement.Kind())
                                {
                                    case SyntaxKind.LocalDeclarationStatement:
                                        // We shouldn't reach this place, but field declarations preceded with a label end up here.
                                        // This is tracked by https://github.com/dotnet/roslyn/issues/13712. Let's do our best for now.
                                        var decl = (LocalDeclarationStatementSyntax)innerStatement;
                                        foreach (var vdecl in decl.Declaration.Variables)
                                        {
                                            // also gather expression-declared variables from the bracketed argument lists and the initializers
                                            ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeNonIndexerMembers, vdecl, this, DeclarationModifiers.Private,
                                                                                          containingFieldOpt: null);
                                        }
                                        break;

                                    case SyntaxKind.ExpressionStatement:
                                    case SyntaxKind.IfStatement:
                                    case SyntaxKind.YieldReturnStatement:
                                    case SyntaxKind.ReturnStatement:
                                    case SyntaxKind.ThrowStatement:
                                    case SyntaxKind.SwitchStatement:
                                    case SyntaxKind.LockStatement:
                                        ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeNonIndexerMembers,
                                                  innerStatement,
                                                  this,
                                                  DeclarationModifiers.Private,
                                                  containingFieldOpt: null);
                                        break;

                                    default:
                                        // no other statement introduces variables into the enclosing scope
                                        break;
                                }

                                AddInitializer(ref instanceInitializers, null, globalStatement);
                            }
                            else if (reportMisplacedGlobalCode && !SyntaxFacts.IsSimpleProgramTopLevelStatement((GlobalStatementSyntax)m))
                            {
                                diagnostics.Add(ErrorCode.ERR_GlobalStatement, new SourceLocation(globalStatement));
                            }
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

        private void AddAccessorIfAvailable(ArrayBuilder<Symbol> symbols, MethodSymbol? accessorOpt, DiagnosticBag diagnostics, bool checkName = false)
        {
            if (!(accessorOpt is null))
            {
                symbols.Add(accessorOpt);
                if (checkName)
                {
                    CheckMemberNameDistinctFromType(accessorOpt, diagnostics);
                }
            }
        }

        internal override byte? GetLocalNullableContextValue()
        {
            byte? value;
            if (!_flags.TryGetNullableContext(out value))
            {
                value = ComputeNullableContextValue();
                _flags.SetNullableContext(value);
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
            var baseType = BaseTypeNoUseSiteDiagnostics;
            if (baseType is object)
            {
                builder.AddValue(TypeWithAnnotations.Create(baseType));
            }
            foreach (var @interface in GetInterfacesToEmit())
            {
                builder.AddValue(TypeWithAnnotations.Create(@interface));
            }
            foreach (var typeParameter in TypeParameters)
            {
                typeParameter.GetCommonNullableValues(compilation, ref builder);
            }
            foreach (var member in GetMembersUnordered())
            {
                member.GetCommonNullableValues(compilation, ref builder);
            }
            // Not including lambdas or local functions.
            return builder.MostCommonValue;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = DeclaringCompilation;
            NamedTypeSymbol baseType = this.BaseTypeNoUseSiteDiagnostics;

            if (baseType is object)
            {
                if (baseType.ContainsDynamic())
                {
                    AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(baseType, customModifiersCount: 0));
                }

                if (baseType.ContainsNativeInteger())
                {
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(this, baseType));
                }

                if (baseType.ContainsTupleNames())
                {
                    AddSynthesizedAttribute(ref attributes, compilation.SynthesizeTupleNamesAttribute(baseType));
                }
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                if (ShouldEmitNullableContextValue(out byte nullableContextValue))
                {
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableContextAttribute(this, nullableContextValue));
                }

                if (baseType is object)
                {
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, nullableContextValue, TypeWithAnnotations.Create(baseType)));
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

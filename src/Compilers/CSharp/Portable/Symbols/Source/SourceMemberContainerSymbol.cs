// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
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
            // |            p|ss|vvv|zzzz|f|d|yy|wwwwww|
            //
            // w = special type.  6 bits.
            // y = IsManagedType.  2 bits.
            // d = FieldDefinitionsNoted. 1 bit.
            // f = FlattenedMembersIsSorted.  1 bit.
            // z = TypeKind. 4 bits.
            // v = NullableContext. 3 bits.
            // s = DeclaredRequiredMembers. 2 bits
            // p = HasPrimaryConstructor. 1 bit.
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

            private const int HasDeclaredRequiredMembersOffset = NullableContextOffset + NullableContextSize;
            private const int HasDeclaredRequiredMembersSize = 2;

            private const int HasPrimaryConstructorOffset = HasDeclaredRequiredMembersOffset + HasDeclaredRequiredMembersSize;
            // private const int HasPrimaryConstructorSize = 1;

            private const int SpecialTypeMask = (1 << SpecialTypeSize) - 1;
            private const int ManagedKindMask = (1 << ManagedKindSize) - 1;
            private const int TypeKindMask = (1 << TypeKindSize) - 1;
            private const int NullableContextMask = (1 << NullableContextSize) - 1;

            private const int FieldDefinitionsNotedBit = 1 << FieldDefinitionsNotedOffset;
            private const int FlattenedMembersIsSortedBit = 1 << FlattenedMembersIsSortedOffset;

            private const int HasDeclaredMembersBit = (1 << HasDeclaredRequiredMembersOffset);
            private const int HasDeclaredMembersBitSet = (1 << (HasDeclaredRequiredMembersOffset + 1));

            private const int HasPrimaryConstructorBit = 1 << HasPrimaryConstructorOffset;

            public ExtendedSpecialType ExtendedSpecialType
            {
                get { return (ExtendedSpecialType)((_flags >> SpecialTypeOffset) & SpecialTypeMask); }
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
                _ = new int[SpecialTypeMask - (int)InternalSpecialType.NextAvailable + 1];
                Debug.Assert(EnumUtilities.ContainsAllValues<SpecialType>(SpecialTypeMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<InternalSpecialType>(SpecialTypeMask)); //This assert might false fail in the future, we don't really need to be able to represent NextAvailable
                Debug.Assert(EnumUtilities.ContainsAllValues<NullableContextKind>(NullableContextMask));
            }
#endif

            public Flags(ExtendedSpecialType specialType, TypeKind typeKind, bool hasPrimaryConstructor)
            {
                int specialTypeInt = ((int)specialType & SpecialTypeMask) << SpecialTypeOffset;
                int typeKindInt = ((int)typeKind & TypeKindMask) << TypeKindOffset;
                int hasPrimaryConstructorInt = hasPrimaryConstructor ? HasPrimaryConstructorBit : 0;

                _flags = specialTypeInt | typeKindInt | hasPrimaryConstructorInt;
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

            public bool TryGetHasDeclaredRequiredMembers(out bool value)
            {
                if ((_flags & (HasDeclaredMembersBitSet)) != 0)
                {
                    value = (_flags & HasDeclaredMembersBit) != 0;
                    return true;
                }
                else
                {
                    value = false;
                    return false;
                }
            }

            public bool SetHasDeclaredRequiredMembers(bool value)
            {
                return ThreadSafeFlagOperations.Set(ref _flags, HasDeclaredMembersBitSet | (value ? HasDeclaredMembersBit : 0));
            }

            public readonly bool HasPrimaryConstructor => (_flags & HasPrimaryConstructorBit) != 0;
        }

        private static readonly ObjectPool<PooledDictionary<Symbol, Symbol>> s_duplicateRecordMemberSignatureDictionary =
            PooledDictionary<Symbol, Symbol>.CreatePool(MemberSignatureComparer.RecordAPISignatureComparer);

        protected SymbolCompletionState state;

        private Flags _flags;
        private ImmutableArray<DiagnosticInfo> _managedKindUseSiteDiagnostics;
        private ImmutableArray<AssemblySymbol> _managedKindUseSiteDependencies;

        private readonly DeclarationModifiers _declModifiers;
        private readonly NamespaceOrTypeSymbol _containingSymbol;
        protected readonly MergedTypeDeclaration declaration;

        // The entry point symbol (resulting from top-level statements) is needed to construct non-type members because
        // it contributes to their binders, so we have to compute it first.
        // The value changes from "default" to "real value". The transition from "default" can only happen once.
        private ImmutableArray<SynthesizedSimpleProgramEntryPointSymbol> _lazySimpleProgramEntryPoints;

        // To compute explicitly declared members, binding must be limited (to avoid race conditions where binder cache captures symbols that aren't part of the final set)
        // The value changes from "uninitialized" to "real value" to null. The transition from "uninitialized" can only happen once.
        private DeclaredMembersAndInitializers? _lazyDeclaredMembersAndInitializers = DeclaredMembersAndInitializers.UninitializedSentinel;

        private MembersAndInitializers? _lazyMembersAndInitializers;
        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>? _lazyMembersDictionary;
        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>? _lazyEarlyAttributeDecodingMembersDictionary;

        private static readonly Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>> s_emptyTypeMembers =
            new Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>>(EmptyReadOnlyMemoryOfCharComparer.Instance);

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>>? _lazyTypeMembers;
        private ImmutableArray<Symbol> _lazyMembersFlattened;
        private SynthesizedExplicitImplementations? _lazySynthesizedExplicitImplementations;
        private int _lazyKnownCircularStruct;
        private LexicalSortKey _lazyLexicalSortKey = LexicalSortKey.NotInitialized;

        private ThreeState _lazyContainsExtensions;
        private ThreeState _lazyAnyMemberHasAttributes;

        // Tracked by https://github.com/dotnet/roslyn/issues/78827 : Optimize by moving some fields into "uncommon" class field?
        private ExtensionGroupingInfo? _lazyExtensionGroupingInfo;

        #region Construction

        internal SourceMemberContainerTypeSymbol(
            NamespaceOrTypeSymbol containingSymbol,
            MergedTypeDeclaration declaration,
            BindingDiagnosticBag diagnostics,
            TupleExtraData? tupleData = null)
            : base(tupleData)
        {
            // If we're dealing with a simple program, then we must be in the global namespace
            Debug.Assert(containingSymbol is NamespaceSymbol { IsGlobalNamespace: true } || !declaration.Declarations.Any(static d => d.IsSimpleProgram));

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
                    diagnostics.Add(ErrorCode.ERR_PartialModifierConflict, GetFirstLocation(), this);
                access = access & ~(access - 1); // narrow down to one access modifier
                modifiers &= ~DeclarationModifiers.AccessibilityMask; // remove them all
                modifiers |= (DeclarationModifiers)access; // except the one
            }
            _declModifiers = modifiers;

            var specialType = access == (int)DeclarationModifiers.Public
                ? MakeExtendedSpecialType()
                : default;

            _flags = new Flags(specialType, typeKind, declaration.HasPrimaryConstructor);
            Debug.Assert(typeKind is TypeKind.Struct or TypeKind.Class || !HasPrimaryConstructor);

            var containingType = this.ContainingType;
            if (containingType?.IsSealed == true && this.DeclaredAccessibility.HasProtected())
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), GetFirstLocation(), this);
            }

            state.NotePartComplete(CompletionPart.TypeArguments); // type arguments need not be computed separately
        }

        private ExtendedSpecialType MakeExtendedSpecialType()
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
                return default;
            }
        }

        private DeclarationModifiers MakeModifiers(TypeKind typeKind, BindingDiagnosticBag diagnostics)
        {
            Symbol containingSymbol = this.ContainingSymbol;
            DeclarationModifiers defaultAccess;

            // note: we give a specific diagnostic when a file-local type is nested
            DeclarationModifiers allowedModifiers;
            if (typeKind == TypeKind.Extension)
            {
                allowedModifiers = DeclarationModifiers.None;
                defaultAccess = DeclarationModifiers.Public;
            }
            else
            {
                allowedModifiers = DeclarationModifiers.AccessibilityMask | DeclarationModifiers.File;

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
                        allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.Sealed | DeclarationModifiers.Abstract
                            | DeclarationModifiers.Unsafe;

                        if (!this.IsRecord)
                        {
                            allowedModifiers |= DeclarationModifiers.Static;
                        }

                        break;
                    case TypeKind.Struct:
                        allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.ReadOnly | DeclarationModifiers.Unsafe;

                        if (!this.IsRecordStruct)
                        {
                            allowedModifiers |= DeclarationModifiers.Ref;
                        }

                        break;
                    case TypeKind.Interface:
                        allowedModifiers |= DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;
                        break;
                    case TypeKind.Delegate:
                        allowedModifiers |= DeclarationModifiers.Unsafe;
                        break;
                }
            }

            bool modifierErrors;
            var mods = MakeAndCheckTypeModifiers(
                defaultAccess,
                allowedModifiers,
                diagnostics,
                out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            if (!modifierErrors &&
                (mods & DeclarationModifiers.Abstract) != 0 &&
                (mods & (DeclarationModifiers.Sealed | DeclarationModifiers.Static)) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractSealedStatic, GetFirstLocation(), this);
            }

            if (!modifierErrors &&
                (mods & (DeclarationModifiers.Sealed | DeclarationModifiers.Static)) == (DeclarationModifiers.Sealed | DeclarationModifiers.Static))
            {
                diagnostics.Add(ErrorCode.ERR_SealedStaticClass, GetFirstLocation(), this);
            }

            switch (typeKind)
            {
                case TypeKind.Interface:
                    mods |= DeclarationModifiers.Abstract;
                    break;
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                case TypeKind.Extension:
                    mods |= DeclarationModifiers.Sealed;
                    break;
            }

            return mods;
        }

        private DeclarationModifiers MakeAndCheckTypeModifiers(
            DeclarationModifiers defaultAccess,
            DeclarationModifiers allowedModifiers,
            BindingDiagnosticBag diagnostics,
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
                        isForTypeDeclaration: true, isForInterfaceMember: false,
                        mods, allowedModifiers, declaration.Declarations[i].NameLocation, diagnostics,
                        modifierTokens: null, modifierErrors: out modifierErrors);

                    // It is an error for the same modifier to appear multiple times.
                    if (!modifierErrors)
                    {
                        modifierErrors = ModifierUtils.CheckAccessibility(mods, this, isExplicitInterfaceImplementation: false, diagnostics, this.GetFirstLocation());
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
            else if ((result & DeclarationModifiers.File) != 0)
            {
                diagnostics.Add(ErrorCode.ERR_FileTypeNoExplicitAccessibility, GetFirstLocation(), this);
            }

            if (missingPartial)
            {
                if ((result & DeclarationModifiers.Partial) == 0)
                {
                    // duplicate definitions
                    switch (this.ContainingSymbol.Kind)
                    {
                        case SymbolKind.Namespace:
                            for (var i = 1; i < partCount; i++)
                            {
                                // note: a declaration with the 'file' modifier will only be grouped with declarations in the same file.
                                diagnostics.Add((result & DeclarationModifiers.File) != 0
                                    ? ErrorCode.ERR_FileLocalDuplicateNameInNS
                                    : ErrorCode.ERR_DuplicateNameInNS, declaration.Declarations[i].NameLocation, this.Name, this.ContainingSymbol);
                                modifierErrors = true;
                            }
                            break;

                        case SymbolKind.NamedType:
                            for (var i = 1; i < partCount; i++)
                            {
                                if (ContainingType!.Locations.Length == 1 || ContainingType.IsPartial())
                                    diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, declaration.Declarations[i].NameLocation, this.ContainingSymbol, this.Name);
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
                            diagnostics.Add(ErrorCode.ERR_MissingPartial, singleDeclaration.NameLocation, this.Name);
                            modifierErrors = true;
                        }
                    }
                }
            }

            return result;
        }

        internal static bool IsReservedTypeName(string? name)
        {
            return name is { Length: > 0 } && name.All(c => c >= 'a' && c <= 'z');
        }

        internal static void ReportReservedTypeName(string? name, CSharpCompilation compilation, DiagnosticBag? diagnostics, Location location)
        {
            if (diagnostics is null)
            {
                return;
            }

            if (reportIfContextual(SyntaxKind.RecordKeyword, MessageID.IDS_FeatureRecords, ErrorCode.WRN_RecordNamedDisallowed)
                || reportIfContextual(SyntaxKind.RequiredKeyword, MessageID.IDS_FeatureRequiredMembers, ErrorCode.ERR_RequiredNameDisallowed)
                || reportIfContextual(SyntaxKind.FileKeyword, MessageID.IDS_FeatureFileTypes, ErrorCode.ERR_FileTypeNameDisallowed)
                || reportIfContextual(SyntaxKind.ScopedKeyword, MessageID.IDS_FeatureRefFields, ErrorCode.ERR_ScopedTypeNameDisallowed)
                || reportIfContextual(SyntaxKind.ExtensionKeyword, MessageID.IDS_FeatureExtensions, ErrorCode.ERR_ExtensionTypeNameDisallowed))
            {
                return;
            }
            else if (IsReservedTypeName(name))
            {
                diagnostics.Add(ErrorCode.WRN_LowerCaseTypeName, location, name);
            }

            bool reportIfContextual(SyntaxKind contextualKind, MessageID featureId, ErrorCode error)
            {
                if (name == SyntaxFacts.GetText(contextualKind) && compilation.LanguageVersion >= featureId.RequiredVersion())
                {
                    diagnostics.Add(error, location);
                    return true;
                }

                return false;
            }
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

        protected abstract void CheckBase(BindingDiagnosticBag diagnostics);
        protected abstract void CheckInterfaces(BindingDiagnosticBag diagnostics);

        internal override void ForceComplete(SourceLocation? locationOpt, Predicate<Symbol>? filter, CancellationToken cancellationToken)
        {
            if (filter?.Invoke(this) == false)
            {
                return;
            }

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
                            var diagnostics = BindingDiagnosticBag.GetInstance();
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
                            var diagnostics = BindingDiagnosticBag.GetInstance();
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
                            // We can't filter out type parameters: if this container was requested, then all its type parameters need to be compiled
                            typeParameter.ForceComplete(locationOpt, filter: null, cancellationToken);
                        }

                        state.NotePartComplete(CompletionPart.TypeParameters);
                        break;

                    case CompletionPart.Members:
                        this.GetMembersByName();

                        if (this.IsExtension)
                        {
                            ((SourceNamedTypeSymbol)this).TryGetOrCreateExtensionMarker();
                        }
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
                            var diagnostics = BindingDiagnosticBag.GetInstance();
                            AfterMembersChecks(diagnostics);
                            AddDeclarationDiagnostics(diagnostics);

                            // We may produce a SymbolDeclaredEvent for the enclosing type before events for its contained members
                            DeclaringCompilation.SymbolDeclaredEvent(this);
                            var thisThreadCompleted = state.NotePartComplete(CompletionPart.FinishMemberChecks);
                            Debug.Assert(thisThreadCompleted);
                            diagnostics.Free();
                        }
                        break;

                    case CompletionPart.MembersCompletedChecksStarted:
                    case CompletionPart.MembersCompleted:
                        {
                            if (this.IsExtension)
                            {
                                ((SourceNamedTypeSymbol)this).TryGetOrCreateExtensionMarker()?.ForceComplete(locationOpt, filter: null, cancellationToken);
                            }

                            ImmutableArray<Symbol> members = this.GetMembersUnordered();

                            bool allCompleted = true;

                            if (locationOpt == null && filter == null)
                            {
                                foreach (var member in members)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    member.ForceComplete(locationOpt, filter: null, cancellationToken);
                                }
                            }
                            else
                            {
                                foreach (var member in members)
                                {
                                    ForceCompleteMemberConditionally(locationOpt, filter, member, cancellationToken);
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
                            cancellationToken.ThrowIfCancellationRequested();

                            if (state.NotePartComplete(CompletionPart.MembersCompletedChecksStarted))
                            {
                                var diagnostics = BindingDiagnosticBag.GetInstance();
                                AfterMembersCompletedChecks(diagnostics);
                                AddDeclarationDiagnostics(diagnostics);

                                // We've completed all members, so we're ready for the PointedAtManagedTypeChecks;
                                // proceed to the next iteration.
                                var thisThreadCompleted = state.NotePartComplete(CompletionPart.MembersCompleted);
                                Debug.Assert(thisThreadCompleted);
                                diagnostics.Free();
                            }
                        }
                        break;

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

            throw ExceptionUtilities.Unreachable();
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

                    foreach (var member in membersAndInitializers.NonTypeMembers)
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

        public override ExtendedSpecialType ExtendedSpecialType
        {
            get
            {
                return _flags.ExtendedSpecialType;
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

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var managedKind = _flags.ManagedKind;
            if (managedKind == ManagedKind.Unknown)
            {
                var managedKindUseSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(ContainingAssembly);
                managedKind = base.GetManagedKind(ref managedKindUseSiteInfo);
                ImmutableInterlocked.InterlockedInitialize(ref _managedKindUseSiteDiagnostics, managedKindUseSiteInfo.Diagnostics?.ToImmutableArray() ?? ImmutableArray<DiagnosticInfo>.Empty);
                ImmutableInterlocked.InterlockedInitialize(ref _managedKindUseSiteDependencies, managedKindUseSiteInfo.Dependencies?.ToImmutableArray() ?? ImmutableArray<AssemblySymbol>.Empty);
                _flags.SetManagedKind(managedKind);
            }

            if (useSiteInfo.AccumulatesDiagnostics)
            {
                ImmutableArray<DiagnosticInfo> useSiteDiagnostics = _managedKindUseSiteDiagnostics;
                // Ensure we have the latest value from the field
                useSiteDiagnostics = ImmutableInterlocked.InterlockedCompareExchange(ref _managedKindUseSiteDiagnostics, useSiteDiagnostics, useSiteDiagnostics);
                Debug.Assert(!useSiteDiagnostics.IsDefault);
                useSiteInfo.AddDiagnostics(useSiteDiagnostics);
            }

            if (useSiteInfo.AccumulatesDependencies)
            {
                ImmutableArray<AssemblySymbol> useSiteDependencies = _managedKindUseSiteDependencies;
                // Ensure we have the latest value from the field
                useSiteDependencies = ImmutableInterlocked.InterlockedCompareExchange(ref _managedKindUseSiteDependencies, useSiteDependencies, useSiteDependencies);
                Debug.Assert(!useSiteDependencies.IsDefault);
                useSiteInfo.AddDependencies(useSiteDependencies);
            }

            return managedKind;
        }

        public override bool IsStatic => HasFlag(DeclarationModifiers.Static);

        public sealed override bool IsRefLikeType => HasFlag(DeclarationModifiers.Ref);

        public override bool IsReadOnly => HasFlag(DeclarationModifiers.ReadOnly);

        public override bool IsSealed => HasFlag(DeclarationModifiers.Sealed);

        public override bool IsAbstract => HasFlag(DeclarationModifiers.Abstract);

        internal bool IsPartial => HasFlag(DeclarationModifiers.Partial);

        internal bool IsNew => HasFlag(DeclarationModifiers.New);

        internal sealed override bool IsFileLocal => HasFlag(DeclarationModifiers.File);

        internal bool IsUnsafe => HasFlag(DeclarationModifiers.Unsafe);

        /// <summary>
        /// If this type is file-local, the syntax tree in which the type is declared. Otherwise, null.
        /// </summary>
        private SyntaxTree? AssociatedSyntaxTree => IsFileLocal ? declaration.Declarations[0].Location.SourceTree : null;

        internal sealed override FileIdentifier? AssociatedFileIdentifier
        {
            get
            {
                if (AssociatedSyntaxTree is not SyntaxTree syntaxTree)
                {
                    return null;
                }

                return FileIdentifier.Create(syntaxTree, DeclaringCompilation?.Options?.SourceReferenceResolver);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(DeclarationModifiers flag) => (_declModifiers & flag) != 0;

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

        internal override bool IsRecord
        {
            get
            {
                return this.declaration.Declarations[0].Kind == DeclarationKind.Record;
            }
        }

        internal override bool IsRecordStruct
        {
            get
            {
                return this.declaration.Declarations[0].Kind == DeclarationKind.RecordStruct;
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

        public sealed override ImmutableArray<Location> Locations
            => ImmutableArray<Location>.CastUp(declaration.NameLocations.ToImmutable());

        public override Location TryGetFirstLocation()
            => declaration.Declarations[0].NameLocation;

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
        public override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken)
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
        ///   2) For fields and properties/indexers, the symbols are stored in (1) and their initializers are
        ///      stored with other initialized fields and properties from the same syntax tree with
        ///      the same static-ness.
        /// </summary>
        protected sealed class MembersAndInitializers
        {
            internal readonly SynthesizedPrimaryConstructor? PrimaryConstructor;
            internal readonly ImmutableArray<Symbol> NonTypeMembers;
            internal readonly ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> StaticInitializers;
            internal readonly ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> InstanceInitializers;
            internal readonly bool HaveIndexers;
            internal readonly bool IsNullableEnabledForInstanceConstructorsAndFields;
            internal readonly bool IsNullableEnabledForStaticConstructorsAndFields;

            public MembersAndInitializers(
                SynthesizedPrimaryConstructor? primaryConstructor,
                ImmutableArray<Symbol> nonTypeMembers,
                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> staticInitializers,
                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> instanceInitializers,
                bool haveIndexers,
                bool isNullableEnabledForInstanceConstructorsAndFields,
                bool isNullableEnabledForStaticConstructorsAndFields)
            {
                Debug.Assert(!nonTypeMembers.IsDefault);
                Debug.Assert(!staticInitializers.IsDefault);
                Debug.Assert(staticInitializers.All(g => !g.IsDefault));
                Debug.Assert(!instanceInitializers.IsDefault);
                Debug.Assert(instanceInitializers.All(g => !g.IsDefault));

                Debug.Assert(!nonTypeMembers.Any(static s => s is TypeSymbol));
                Debug.Assert(haveIndexers == nonTypeMembers.Any(static s => s.IsIndexer()));

                this.PrimaryConstructor = primaryConstructor;
                this.NonTypeMembers = nonTypeMembers;
                this.StaticInitializers = staticInitializers;
                this.InstanceInitializers = instanceInitializers;
                this.HaveIndexers = haveIndexers;
                this.IsNullableEnabledForInstanceConstructorsAndFields = isNullableEnabledForInstanceConstructorsAndFields;
                this.IsNullableEnabledForStaticConstructorsAndFields = isNullableEnabledForStaticConstructorsAndFields;
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

                throw ExceptionUtilities.Unreachable();
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
            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// Calculates a syntax offset of a syntax position that is contained in a property or field initializer (if it is in fact contained in one).
        /// </summary>
        internal bool TryCalculateSyntaxOffsetOfPositionInInitializer(int position, SyntaxTree tree, bool isStatic, int ctorInitializerLength, out int syntaxOffset)
        {
            Debug.Assert(ctorInitializerLength >= 0);

            var membersAndInitializers = GetMembersAndInitializers();
            var allInitializers = isStatic ? membersAndInitializers.StaticInitializers : membersAndInitializers.InstanceInitializers;

            if (!findInitializer(allInitializers, position, tree, out FieldOrPropertyInitializer initializer, out int precedingLength))
            {
                syntaxOffset = 0;
                return false;
            }

            //                                 |<-----------distanceFromCtorBody----------->|
            // [      initializer 0    ][ initializer 1 ][ initializer 2 ][ctor initializer][ctor body]
            // |<--preceding init len-->|      ^
            //                             position

            int initializersLength = getInitializersLength(allInitializers);
            int distanceFromInitializerStart = position - initializer.Syntax.Span.Start;

            int distanceFromCtorBody =
                initializersLength + ctorInitializerLength -
                (precedingLength + distanceFromInitializerStart);

            Debug.Assert(distanceFromCtorBody > 0);

            // syntax offset 0 is at the start of the ctor body:
            syntaxOffset = -distanceFromCtorBody;
            return true;

            static bool findInitializer(ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers, int position, SyntaxTree tree,
                out FieldOrPropertyInitializer found, out int precedingLength)
            {
                precedingLength = 0;
                foreach (var group in initializers)
                {
                    if (!group.IsEmpty &&
                        group[0].Syntax.SyntaxTree == tree &&
                        position < group.Last().Syntax.Span.End)
                    {
                        // Found group of interest
                        var initializerIndex = IndexOfInitializerContainingPosition(group, position);
                        if (initializerIndex < 0)
                        {
                            break;
                        }

                        precedingLength += getPrecedingInitializersLength(group, initializerIndex);
                        found = group[initializerIndex];
                        return true;
                    }

                    precedingLength += getGroupLength(group);
                }

                found = default;
                return false;
            }

            static int getGroupLength(ImmutableArray<FieldOrPropertyInitializer> initializers)
            {
                int length = 0;
                foreach (var initializer in initializers)
                {
                    length += getInitializerLength(initializer);
                }

                return length;
            }

            static int getPrecedingInitializersLength(ImmutableArray<FieldOrPropertyInitializer> initializers, int index)
            {
                int length = 0;
                for (var i = 0; i < index; i++)
                {
                    length += getInitializerLength(initializers[i]);
                }

                return length;
            }

            static int getInitializersLength(ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers)
            {
                int length = 0;
                foreach (var group in initializers)
                {
                    length += getGroupLength(group);
                }

                return length;
            }

            static int getInitializerLength(FieldOrPropertyInitializer initializer)
            {
                // A constant field of type decimal needs a field initializer, so
                // check if it is a metadata constant, not just a constant to exclude
                // decimals. Other constants do not need field initializers.
                if (initializer.FieldOpt == null || !initializer.FieldOpt.IsMetadataConstant)
                {
                    // ignore leading and trailing trivia of the node:
                    return initializer.Syntax.Span.Length;
                }

                return 0;
            }
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
                return (IsTupleType || IsRecord || IsRecordStruct || this.declaration.ContainsExtensionDeclarations) ? GetMembers().Select(m => m.Name) : this.declaration.MemberNames;
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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            ImmutableArray<NamedTypeSymbol> members;
            if (GetTypeMembersDictionary().TryGetValue(name, out members))
            {
                return members;
            }

            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray((t, arity) => t.Arity == arity, arity);
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>> GetTypeMembersDictionary()
        {
            if (_lazyTypeMembers == null)
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref _lazyTypeMembers, MakeTypeMembers(diagnostics), null) == null)
                {
                    AddDeclarationDiagnostics(diagnostics);

                    state.NotePartComplete(CompletionPart.TypeMembers);
                }

                diagnostics.Free();
            }

            return _lazyTypeMembers;
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>> MakeTypeMembers(BindingDiagnosticBag diagnostics)
        {
            var symbols = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var conflictDict = new Dictionary<(string name, int arity, SyntaxTree? syntaxTree), SourceNamedTypeSymbol>();
            try
            {
                // Declarations which can be merged into a single type symbol have already been merged at this phase.
                // Merging behaves the same in either presence or absence of 'partial' modifiers.
                // However, type declarations which can never be partial won't merge, e.g. 'enum',
                // and type declarations with different kinds, e.g. 'class' and 'struct' will never merge.
                // Now we want to figure out if declarations which didn't merge have name conflicts.
                foreach (var childDeclaration in declaration.Children)
                {
                    var t = new SourceNamedTypeSymbol(this, childDeclaration, diagnostics);
                    if (!t.IsExtension)
                    {
                        this.CheckMemberNameDistinctFromType(t, diagnostics);

                        var key = (t.Name, t.Arity, t.AssociatedSyntaxTree);
                        SourceNamedTypeSymbol? other;
                        if (conflictDict.TryGetValue(key, out other))
                        {
                            if (Locations.Length == 1 || IsPartial)
                            {
                                if (t.IsPartial && other.IsPartial)
                                {
                                    diagnostics.Add(ErrorCode.ERR_PartialTypeKindConflict, t.GetFirstLocation(), t);
                                }
                                else
                                {
                                    diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, t.GetFirstLocation(), this, t.Name);
                                }
                            }
                        }
                        else
                        {
                            conflictDict.Add(key, t);
                        }
                    }

                    symbols.Add(t);
                }

                if (IsInterface)
                {
                    foreach (var t in symbols)
                    {
                        Binder.CheckFeatureAvailability(t.DeclaringSyntaxReferences[0].GetSyntax(), MessageID.IDS_DefaultInterfaceImplementation, diagnostics, t.GetFirstLocation());
                    }
                }

                Debug.Assert(s_emptyTypeMembers.Count == 0);
                return symbols.Count > 0 ?
                    symbols.ToDictionary(s => s.Name.AsMemory(), ReadOnlyMemoryOfCharComparer.Instance) :
                    s_emptyTypeMembers;
            }
            finally
            {
                symbols.Free();
            }
        }

        private void CheckMemberNameDistinctFromType(Symbol member, BindingDiagnosticBag diagnostics)
        {
            switch (this.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                    checkContainingTypeName(member, this.Name, diagnostics);
                    break;
                case TypeKind.Interface:
                    if (member.IsStatic)
                    {
                        checkContainingTypeName(member, this.Name, diagnostics);
                    }

                    break;
                case TypeKind.Extension:
                    // Since implementation methods have the same name as the extension method, we don't need to report the problem twice
                    if (member.Kind != SymbolKind.Method && this.ContainingType is { } containingType)
                    {
                        checkContainingTypeName(member, containingType.Name, diagnostics);
                    }

                    if (this.ExtensionParameter is { Type: NamedTypeSymbol { Name: var extendedTypeName } })
                    {
                        checkExtendedTypeName(member, extendedTypeName, diagnostics);
                    }

                    break;
            }

            static void checkContainingTypeName(Symbol member, string typeName, BindingDiagnosticBag diagnostics)
            {
                if (member.Name == typeName)
                {
                    diagnostics.Add(ErrorCode.ERR_MemberNameSameAsType, member.GetFirstLocation(), typeName);
                }
            }

            static void checkExtendedTypeName(Symbol member, string typeName, BindingDiagnosticBag diagnostics)
            {
                if (member.Name == typeName)
                {
                    diagnostics.Add(ErrorCode.ERR_MemberNameSameAsExtendedType, member.GetFirstLocation(), typeName);
                }
            }
        }

        internal override bool HasDeclaredRequiredMembers
        {
            get
            {
                if (_flags.TryGetHasDeclaredRequiredMembers(out bool hasDeclaredMembers))
                {
                    return hasDeclaredMembers;
                }

                hasDeclaredMembers = declaration.Declarations.Any(static decl => decl.HasRequiredMembers);
                _flags.SetHasDeclaredRequiredMembers(hasDeclaredMembers);
                return hasDeclaredMembers;
            }
        }

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            return HasAsyncMethodBuilderAttribute(this, out builderArgument);
        }

        /// <summary>
        /// Returns true if the method has a [AsyncMethodBuilder(typeof(B))] attribute. If so it returns type B.
        /// Validation of builder type B is left for elsewhere. This method returns B without validation of any kind.
        /// </summary>
        internal static bool HasAsyncMethodBuilderAttribute(Symbol symbol, [NotNullWhen(true)] out TypeSymbol? builderArgument)
        {
            Debug.Assert(symbol is not null);

            // Find the AsyncMethodBuilder attribute.
            foreach (var attr in symbol.GetAttributes())
            {
                Debug.Assert(attr is SourceAttributeData);

                if (attr.IsTargetAttribute(AttributeDescription.AsyncMethodBuilderAttribute)
                    && attr.CommonConstructorArguments.Length == 1
                    && attr.CommonConstructorArguments[0].Kind == TypedConstantKind.Type)
                {
                    builderArgument = (TypeSymbol)attr.CommonConstructorArguments[0].ValueInternal!;
                    return true;
                }
            }

            builderArgument = null;
            return false;
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
            if (GetMembersByName().TryGetValue(name.AsMemory(), out members))
            {
                return members;
            }

            return ImmutableArray<Symbol>.Empty;
        }

        /// <remarks>
        /// For source symbols, there can only be a valid clone method if this is a record, which is a
        /// simple syntax check. This will need to change when we generalize cloning, but it's a good
        /// heuristic for now.
        /// </remarks>
        internal override bool HasPossibleWellKnownCloneMethod()
            => IsRecord;

        internal override OneOrMany<Symbol> GetSimpleNonTypeMembers(string name)
        {
            if (_lazyMembersDictionary != null || declaration.ContainsExtensionDeclarations || declaration.MemberNames.Contains(name) || declaration.Kind is DeclarationKind.Record or DeclarationKind.RecordStruct)
            {
                return OneOrMany.Create(GetMembers(name));
            }

            return OneOrMany<Symbol>.Empty;
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
                        if (m is TupleErrorFieldSymbol)
                        {
                            break;
                        }

                        yield return (FieldSymbol)m;
                        break;
                    case SymbolKind.Event:
                        FieldSymbol? associatedField = ((EventSymbol)m).AssociatedField;
                        if ((object?)associatedField != null)
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
            return GetEarlyAttributeDecodingMembersDictionary().TryGetValue(name.AsMemory(), out result) ? result : ImmutableArray<Symbol>.Empty;
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> GetEarlyAttributeDecodingMembersDictionary()
        {
            if (_lazyEarlyAttributeDecodingMembersDictionary == null)
            {
                if (Volatile.Read(ref _lazyMembersDictionary) is Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> result)
                {
                    return result;
                }

                var membersAndInitializers = GetMembersAndInitializers(); //NOTE: separately cached

                // NOTE: members were added in a single pass over the syntax, so they're already
                // in lexical order.

                Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName;

                if (!membersAndInitializers.HaveIndexers)
                {
                    membersByName = ToNameKeyedDictionary(membersAndInitializers.NonTypeMembers);
                }
                else
                {
                    // We can't include indexer symbol yet, because we don't know
                    // what name it will have after attribute binding (because of
                    // IndexerNameAttribute).
                    membersByName = ToNameKeyedDictionary(membersAndInitializers.NonTypeMembers.
                        WhereAsArray(s => !s.IsIndexer() && (!s.IsAccessor() || ((MethodSymbol)s).AssociatedSymbol?.IsIndexer() != true)));
                }

                AddNestedTypesToDictionary(membersByName, GetTypeMembersDictionary());

                Interlocked.CompareExchange(ref _lazyEarlyAttributeDecodingMembersDictionary, membersByName, null);
            }

            return _lazyEarlyAttributeDecodingMembersDictionary;
        }

        private static Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> ToNameKeyedDictionary(ImmutableArray<Symbol> symbols)
        {
            if (symbols is [var symbol])
            {
                return new Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>(1, ReadOnlyMemoryOfCharComparer.Instance)
                {
                    {  symbol.Name.AsMemory(), ImmutableArray.Create(symbol) },
                };
            }

            if (symbols.Length == 0)
            {
                return new Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>(ReadOnlyMemoryOfCharComparer.Instance);
            }

            // bucketize
            // prevent reallocation. it may not have 'count' entries, but it won't have more. 
            //
            // We store a mapping from keys to either a single item (very common in practice as this is used from
            // callers that maps names to symbols with that name, and most names are unique), or an array builder of items.

            var accumulator = s_nameToObjectPool.Allocate();
            foreach (var item in symbols)
                ImmutableArrayExtensions.AddToMultiValueDictionaryBuilder(accumulator, item.Name.AsMemory(), item);

            var dictionary = new Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>(accumulator.Count, ReadOnlyMemoryOfCharComparer.Instance);

            // freeze
            foreach (var pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value is ArrayBuilder<Symbol> arrayBuilder
                    ? arrayBuilder.ToImmutableAndFree()
                    : ImmutableArray.Create((Symbol)pair.Value));
            }

            accumulator.Free();

            return dictionary;
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

            var diagnostics = BindingDiagnosticBag.GetInstance();
            membersAndInitializers = BuildMembersAndInitializers(diagnostics);

            var alreadyKnown = Interlocked.CompareExchange(ref _lazyMembersAndInitializers, membersAndInitializers, null);
            if (alreadyKnown != null)
            {
                diagnostics.Free();
                return alreadyKnown;
            }

            AddDeclarationDiagnostics(diagnostics);
            diagnostics.Free();
            _lazyDeclaredMembersAndInitializers = null;

            return membersAndInitializers!;
        }

        /// <summary>
        /// The purpose of this function is to assert that the <paramref name="member"/> symbol
        /// is actually among the symbols cached by this type symbol in a way that ensures
        /// that any consumer of standard APIs to get to type's members is going to get the same
        /// symbol (same instance) for the member rather than an equivalent, but different instance.
        /// </summary>
        [Conditional("DEBUG")]
        internal void AssertMemberExposure(Symbol member, bool forDiagnostics = false)
        {
            if (member is NamedTypeSymbol type)
            {
                RoslynDebug.AssertOrFailFast(forDiagnostics);
                RoslynDebug.AssertOrFailFast(Volatile.Read(ref _lazyTypeMembers)?.Values.Any(types => types.Contains(t => t == (object)type)) == true);
                return;
            }
            else if (member is TypeParameterSymbol || member is SynthesizedMethodBaseSymbol)
            {
                RoslynDebug.AssertOrFailFast(forDiagnostics);
                return;
            }
            else if (member is FieldSymbol field && field.AssociatedSymbol is EventSymbol e)
            {
                RoslynDebug.AssertOrFailFast(forDiagnostics);
                // Backing fields for field-like events are not added to the members list.
                member = e;
            }
            else if (member is FieldSymbol { AssociatedSymbol: SourcePropertySymbolBase { PartialDefinitionPart: PropertySymbol definition } implementation } &&
                     definition.PartialImplementationPart == (object)implementation && implementation.BackingField != (object)member)
            {
                member = implementation; // This is a workaround for https://github.com/dotnet/roslyn/issues/76870, remove once the issue is addressed.
            }
            else if (member is SynthesizedExtensionMarker)
            {
                return;
            }

            var membersAndInitializers = Volatile.Read(ref _lazyMembersAndInitializers);

            if (isMemberInCompleteMemberList(membersAndInitializers, member))
            {
                return;
            }

            if (membersAndInitializers is null)
            {
                if (member is SynthesizedSimpleProgramEntryPointSymbol)
                {
                    RoslynDebug.AssertOrFailFast(GetSimpleProgramEntryPoints().Contains(m => m == (object)member));
                    return;
                }

                var declared = Volatile.Read(ref _lazyDeclaredMembersAndInitializers);
                RoslynDebug.AssertOrFailFast(declared != DeclaredMembersAndInitializers.UninitializedSentinel);

                if (declared is object)
                {
                    if (declared.NonTypeMembersWithPartialImplementations.Contains(m => m == (object)member) || declared.PrimaryConstructor == (object)member)
                    {
                        return;
                    }
                }
                else
                {
                    // It looks like there was a race and we need to check _lazyMembersAndInitializers again
                    membersAndInitializers = Volatile.Read(ref _lazyMembersAndInitializers);
                    RoslynDebug.AssertOrFailFast(membersAndInitializers is object);

                    if (isMemberInCompleteMemberList(membersAndInitializers, member))
                    {
                        return;
                    }
                }
            }

            RoslynDebug.AssertOrFailFast(false, "Premature symbol exposure.");

            static bool isMemberInCompleteMemberList(MembersAndInitializers? membersAndInitializers, Symbol member)
            {
                member = member.GetPartialDefinitionPart() ?? member;
                return membersAndInitializers?.NonTypeMembers.Contains(m => m == (object)member) == true;
            }
        }

        protected Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> GetMembersByName()
        {
            if (this.state.HasComplete(CompletionPart.Members))
            {
                return _lazyMembersDictionary!;
            }

            return GetMembersByNameSlow();
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> GetMembersByNameSlow()
        {
            if (_lazyMembersDictionary == null)
            {
                var membersDictionary = MakeAllMembers();

                if (Interlocked.CompareExchange(ref _lazyMembersDictionary, membersDictionary, null) == null)
                {
                    state.NotePartComplete(CompletionPart.Members);
                }
            }

            state.SpinWaitComplete(CompletionPart.Members, default(CancellationToken));
            return _lazyMembersDictionary;
        }

        internal bool AreMembersComplete => state.HasComplete(CompletionPart.Members);

        internal override IEnumerable<Symbol> GetInstanceFieldsAndEvents()
        {
            var membersAndInitializers = this.GetMembersAndInitializers();

            IEnumerable<Symbol> result = membersAndInitializers.NonTypeMembers.Where(IsInstanceFieldOrEvent);

            return result;
        }

        protected void AfterMembersChecks(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = GetFirstLocation();

            if (IsInterface)
            {
                CheckInterfaceMembers(this.GetMembersAndInitializers().NonTypeMembers, diagnostics);
            }
            else if (IsExtension)
            {
                CheckExtensionMembers(this.GetMembers(), diagnostics);
                MessageID.IDS_FeatureExtensions.CheckFeatureAvailability(diagnostics, compilation, location);
            }

            CheckMemberNamesDistinctFromType(diagnostics);
            CheckMemberNameConflictsAndUnmatchedOperators(diagnostics);
            CheckRecordMemberNames(diagnostics);
            CheckSpecialMemberErrors(diagnostics);
            CheckTypeParameterNameConflicts(diagnostics);

            bool unused = KnownCircularStruct;

            CheckSequentialOnPartialType(diagnostics);
            CheckForProtectedInStaticClass(diagnostics);
            CheckForRequiredMemberAttribute(diagnostics);

            if (IsScriptClass || IsSubmissionClass)
            {
                ReportRequiredMembers(diagnostics);
            }

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

            if (compilation.ShouldEmitNativeIntegerAttributes())
            {
                // https://github.com/dotnet/roslyn/issues/30080: Report diagnostics for base type and interfaces at more specific locations.
                if (hasBaseTypeOrInterface(static t => t.ContainsNativeIntegerWrapperType()))
                {
                    compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
                }
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                if (ShouldEmitNullableContextValue(out _))
                {
                    compilation.EnsureNullableContextAttributeExists(diagnostics, location, modifyCompilation: true);
                }

                if (hasBaseTypeOrInterface(static t => t.NeedsNullableAttribute()))
                {
                    compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
                }
            }

            if (interfaces.Any(needsTupleElementNamesAttribute))
            {
                // Note: we don't need to check base type or directly implemented interfaces (which will be reported during binding)
                // so the checking of all interfaces here involves some redundancy.
                Binder.ReportMissingTupleElementNamesAttributesIfNeeded(compilation, location, diagnostics);
            }

            if (IsReservedTypeName(Name))
            {
                foreach (var syntaxRef in SyntaxReferences)
                {
                    SyntaxToken? identifier = syntaxRef.GetSyntax() switch
                    {
                        BaseTypeDeclarationSyntax typeDecl => typeDecl.Identifier,
                        DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier,
                        _ => null
                    };

                    ReportReservedTypeName(identifier?.Text, this.DeclaringCompilation, diagnostics.DiagnosticBag, identifier?.GetLocation() ?? Location.None);
                }
            }

            if (AssociatedFileIdentifier is { } fileIdentifier)
            {
                Debug.Assert(IsFileLocal);

                // A well-behaved file-local type only has declarations in one syntax tree.
                // There may be multiple syntax trees across declarations in error scenarios,
                // but we're not interested in handling that for the purposes of producing this error.
                var tree = declaration.Declarations[0].SyntaxReference.SyntaxTree;
                if (fileIdentifier.EncoderFallbackErrorMessage is { } errorMessage)
                {
                    Debug.Assert(fileIdentifier.FilePathChecksumOpt.IsDefault);
                    diagnostics.Add(ErrorCode.ERR_FilePathCannotBeConvertedToUtf8, location, this, errorMessage);
                }

                if ((object?)ContainingType != null)
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeNested, location, this);
                }
            }

            if (IsExtension)
            {
                // Verify ExtensionAttribute is available.
                SourceOrdinaryMethodSymbol.CheckExtensionAttributeAvailability(DeclaringCompilation, location, diagnostics);
            }

            return;

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

        protected virtual void AfterMembersCompletedChecks(BindingDiagnosticBag diagnostics)
        {
        }

        private void CheckMemberNamesDistinctFromType(BindingDiagnosticBag diagnostics)
        {
            foreach (var member in GetMembersAndInitializers().NonTypeMembers)
            {
                CheckMemberNameDistinctFromType(member, diagnostics);
            }
        }

        private void CheckRecordMemberNames(BindingDiagnosticBag diagnostics)
        {
            if (declaration.Kind != DeclarationKind.Record &&
                declaration.Kind != DeclarationKind.RecordStruct)
            {
                return;
            }

            foreach (var member in GetMembers("Clone"))
            {
                diagnostics.Add(ErrorCode.ERR_CloneDisallowedInRecord, member.GetFirstLocation());
            }
        }

        private static void CheckMemberNameConflicts(
            SourceMemberContainerTypeSymbol containerForDiagnostics,
            bool mightHaveMembersFromDistinctNonPartialDeclarations,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>>? typesByName,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            BindingDiagnosticBag diagnostics)
        {

            // Collisions involving indexers are handled specially.
            CheckIndexerNameConflicts(containerForDiagnostics, mightHaveMembersFromDistinctNonPartialDeclarations, diagnostics, membersByName);

            // key and value will be the same object in these dictionaries.
            var methodsBySignature = new Dictionary<MethodSymbol, MethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);
            var conversionsAsMethods = new Dictionary<MethodSymbol, MethodSymbol>(MemberSignatureComparer.DuplicateSourceComparer);
            var conversionsAsConversions = new HashSet<MethodSymbol>(ConversionSignatureComparer.Comparer);

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
                Symbol? lastSym = typesByName?.TryGetValue(name, out var types) == true ? types.FirstOrDefault() : null;
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

                    if (lastSym is object)
                    {
                        if (symbol.Kind != SymbolKind.Method || lastSym.Kind != SymbolKind.Method)
                        {
                            if (symbol.Kind != SymbolKind.Field || !symbol.IsImplicitlyDeclared)
                            {
                                // The type '{0}' already contains a definition for '{1}'
                                if (!mightHaveMembersFromDistinctNonPartialDeclarations)
                                {
                                    diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, symbol.GetFirstLocation(), containerForDiagnostics, symbol.Name);
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

                    // We don't want to consider explicit interface implementations
                    if (symbol is MethodSymbol { MethodKind: MethodKind.Conversion } conversion)
                    {
                        // Does this conversion collide *as a conversion* with any previously-seen
                        // conversion?

                        if (!conversionsAsConversions.Add(conversion))
                        {
                            // CS0557: Duplicate user-defined conversion in type 'C'
                            diagnostics.Add(ErrorCode.ERR_DuplicateConversionInClass, conversion.GetFirstLocation(), containerForDiagnostics);
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
                            ReportMethodSignatureCollision(containerForDiagnostics, diagnostics, conversion, previousMethod);
                        }
                        // Do not add the conversion to the set of previously-seen methods; that set
                        // is only non-conversion methods.
                    }
                    else if (symbol is MethodSymbol method)
                    {
                        // Does this method collide *as a method* with any previously-seen
                        // conversion?

                        if (conversionsAsMethods.TryGetValue(method, out var previousConversion))
                        {
                            ReportMethodSignatureCollision(containerForDiagnostics, diagnostics, method, previousConversion);
                        }
                        // Do not add the method to the set of previously-seen conversions.

                        // Does this method collide *as a method* with any previously-seen
                        // non-conversion method?

                        if (methodsBySignature.TryGetValue(method, out var previousMethod))
                        {
                            ReportMethodSignatureCollision(containerForDiagnostics, diagnostics, method, previousMethod);
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
        private static void ReportMethodSignatureCollision(SourceMemberContainerTypeSymbol containerForDiagnostics, BindingDiagnosticBag diagnostics, MethodSymbol method1, MethodSymbol method2)
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
            if (method1.OriginalDefinition is SourceMemberMethodSymbol { MethodKind: MethodKind.Constructor } constructor &&
                ((ConstructorDeclarationSyntax)constructor.SyntaxRef.GetSyntax()).Identifier.ValueText != method1.ContainingType.Name)
            {
                return;
            }

            if (method1 is SourceExtensionImplementationMethodSymbol { UnderlyingMethod: var underlying1 } &&
                method2 is SourceExtensionImplementationMethodSymbol { UnderlyingMethod: var underlying2 } &&
                underlying1.IsStatic == underlying2.IsStatic &&
                ((object)underlying1.ContainingType == underlying2.ContainingType ||
                ((SourceNamedTypeSymbol)underlying1.ContainingType).ExtensionGroupingName == ((SourceNamedTypeSymbol)underlying2.ContainingType).ExtensionGroupingName) &&
                diagnostics.DiagnosticBag?.AsEnumerableWithoutResolution().Any(
                    static (d, arg) =>
                        (d.Code is (int)ErrorCode.ERR_OverloadRefKind or (int)ErrorCode.ERR_MemberAlreadyExists or
                                   (int)ErrorCode.ERR_DuplicateNameInClass or (int)ErrorCode.ERR_MemberReserved) &&
                        (d.Location == arg.method1.GetFirstLocation() || d.Location == arg.underlying1.AssociatedSymbol?.TryGetFirstLocation() ||
                            d.Location == arg.method2.GetFirstLocation() || d.Location == arg.underlying2.AssociatedSymbol?.TryGetFirstLocation()),
                    (method1, underlying1, method2, underlying2)) == true)
            {
                return; // The conflict is reported in context of extension declaration
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
                    diagnostics.Add(ErrorCode.ERR_OverloadRefKind, method1.GetFirstLocation(), containerForDiagnostics, methodKind.Localize(), refKind1.ToParameterDisplayString(), refKind2.ToParameterDisplayString());

                    return;
                }
            }

            if (method1 is SourceExtensionImplementationMethodSymbol extensionImplementation)
            {
                method1 = extensionImplementation.UnderlyingMethod;
            }

            // Special case: if there are two destructors, use the destructor syntax instead of "Finalize"
            var methodName = (method1.MethodKind == MethodKind.Destructor && method2.MethodKind == MethodKind.Destructor) ?
                "~" + method1.ContainingType.Name :
                (method1.IsConstructor() ? method1.ContainingType.Name : method1.Name);

            // Type '{1}' already defines a member called '{0}' with the same parameter types
            diagnostics.Add(ErrorCode.ERR_MemberAlreadyExists, method1.GetFirstLocation(), methodName, containerForDiagnostics);
        }

        private static void CheckIndexerNameConflicts(
            SourceMemberContainerTypeSymbol containerForDiagnostics,
            bool mightHaveMembersFromDistinctNonPartialDeclarations,
            BindingDiagnosticBag diagnostics, Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName)
        {
            PooledHashSet<string>? typeParameterNames = null;
            bool checkCollisionWithTypeParameters = true;

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
                            containerForDiagnostics,
                            mightHaveMembersFromDistinctNonPartialDeclarations,
                            indexer,
                            diagnostics,
                            membersByName,
                            indexersBySignature,
                            ref lastIndexerName);

                        // Also check for collisions with type parameters, which aren't in the member map.
                        // NOTE: Accessors have normal names and are handled in CheckTypeParameterNameConflicts.

                        if (checkCollisionWithTypeParameters && typeParameterNames == null)
                        {
                            if (!indexer.IsExtensionBlockMember() && indexer.ContainingType.Arity > 0)
                            {
                                typeParameterNames = PooledHashSet<string>.GetInstance();
                                foreach (TypeParameterSymbol typeParameter in indexer.ContainingType.TypeParameters)
                                {
                                    typeParameterNames.Add(typeParameter.Name);
                                }
                            }
                            else
                            {
                                checkCollisionWithTypeParameters = false;
                            }
                        }

                        Debug.Assert(checkCollisionWithTypeParameters || typeParameterNames == null);

                        if (typeParameterNames != null)
                        {
                            string indexerName = indexer.MetadataName;
                            if (typeParameterNames.Contains(indexerName))
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, indexer.GetFirstLocation(), containerForDiagnostics, indexerName);
                                continue;
                            }
                        }
                    }
                }
            }

            typeParameterNames?.Free();
        }

        private static void CheckIndexerSignatureCollisions(
            SourceMemberContainerTypeSymbol containerForDiagnostics,
            bool mightHaveMembersFromDistinctNonPartialDeclarations,
            PropertySymbol indexer,
            BindingDiagnosticBag diagnostics,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
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

                    diagnostics.Add(ErrorCode.ERR_InconsistentIndexerNames, indexer.GetFirstLocation());
                }

                lastIndexerName = indexerName;

                if (!mightHaveMembersFromDistinctNonPartialDeclarations)
                {
#pragma warning disable CA1854 //Prefer a 'TryGetValue' call over a Dictionary indexer access guarded by a 'ContainsKey' check to avoid double lookup
                    if (membersByName.ContainsKey(indexerName.AsMemory()))
#pragma warning restore CA1854
                    {
                        // The name of the indexer is reserved - it can only be used by other indexers.
                        Debug.Assert(!membersByName[indexerName.AsMemory()].Any(SymbolExtensions.IsIndexer));
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, indexer.GetFirstLocation(), containerForDiagnostics, indexerName);
                    }
                }
            }

            if (indexersBySignature.TryGetValue(indexer, out var prevIndexerBySignature))
            {
                // Type '{1}' already defines a member called '{0}' with the same parameter types
                // NOTE: Dev10 prints "this" as the name of the indexer.
                diagnostics.Add(ErrorCode.ERR_MemberAlreadyExists, indexer.GetFirstLocation(), SyntaxFacts.GetText(SyntaxKind.ThisKeyword), containerForDiagnostics);
            }
            else
            {
                indexersBySignature[indexer] = indexer;
            }
        }

        private void CheckMemberNameConflictsAndUnmatchedOperators(BindingDiagnosticBag diagnostics)
        {
            if (IsExtension)
            {
                return; // Conflicts are checked in context of the enclosing type
            }

            if (this.declaration.ContainsExtensionDeclarations)
            {
                checkMemberNameConflictsInExtensions(diagnostics);
                this.GetExtensionGroupingInfo().CheckSignatureCollisions(diagnostics);
            }

            checkMemberNameConflicts(GetMembersByName(), GetTypeMembersDictionary(), GetMembersUnordered(), diagnostics);

            // We also produce a warning if == / != is overridden without also overriding
            // Equals and GetHashCode, or if Equals is overridden without GetHashCode.

            CheckForEqualityAndGetHashCode(diagnostics);

            return;

            void checkMemberNameConflicts(
                Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
                Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>>? typesByName,
                ImmutableArray<Symbol> membersUnordered,
                BindingDiagnosticBag diagnostics)
            {
                bool mightHaveMembersFromDistinctNonPartialDeclarations = !(Locations.Length == 1 || IsPartial);
                CheckMemberNameConflicts(this, mightHaveMembersFromDistinctNonPartialDeclarations, typesByName, membersByName, diagnostics);
                CheckAccessorNameConflicts(this, mightHaveMembersFromDistinctNonPartialDeclarations, membersByName, membersUnordered, diagnostics);
                CheckForUnmatchedOperators(membersByName, diagnostics);
            }

            void checkMemberNameConflictsInExtensions(BindingDiagnosticBag diagnostics)
            {
                IEnumerable<IGrouping<string, NamedTypeSymbol>> extensionsByReceiverType = GetTypeMembers("").Where(static t => t.IsExtension).GroupBy(static t => ((SourceNamedTypeSymbol)t).ExtensionGroupingName!);

                foreach (var grouping in extensionsByReceiverType)
                {
                    Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>? membersByName;
                    ImmutableArray<Symbol> membersUnordered;

                    (membersByName, membersUnordered) = mergeMembersInGroup(grouping);

                    if (membersByName is not null)
                    {
                        checkMemberNameConflicts(membersByName, typesByName: null /* nested types not supported */, membersUnordered, diagnostics);
                    }
                }
            }

            static (Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>? membersByName, ImmutableArray<Symbol> membersUnordered) mergeMembersInGroup(IGrouping<string, NamedTypeSymbol> grouping)
            {
                Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>? membersByName = null;
                ImmutableArray<Symbol> membersUnordered = [];
                NamedTypeSymbol? masterExtension = null;
                bool cloneMembersByName = true;

                foreach (NamedTypeSymbol item in grouping)
                {
                    var extension = item;
                    Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByNameToMerge = ((SourceMemberContainerTypeSymbol)extension).GetMembersByName();

                    if (membersByNameToMerge.Count == 0)
                    {
                        continue; // This is an optimization
                    }

                    if (membersByName == null)
                    {
                        membersByName = membersByNameToMerge;
                        membersUnordered = extension.GetMembersUnordered();
                        masterExtension = extension;
                        Debug.Assert(cloneMembersByName);
                    }
                    else
                    {
                        Debug.Assert(masterExtension is not null);

                        if (cloneMembersByName)
                        {
                            membersByName = new Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>>(membersByName, ReadOnlyMemoryOfCharComparer.Instance);
                            cloneMembersByName = false;
                        }

                        if (extension.Arity != 0)
                        {
                            extension = extension.Construct(masterExtension.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
                        }
                        else
                        {
                            membersUnordered = membersUnordered.Concat(extension.GetMembersUnordered());
                        }

                        foreach (var pair in membersByNameToMerge)
                        {
                            if (membersByName.TryGetValue(pair.Key, out var members))
                            {
                                membersByName[pair.Key] = concatMembers(members, extension, pair.Value, ref membersUnordered);
                            }
                            else
                            {
                                membersByName.Add(pair.Key, concatMembers([], extension, pair.Value, ref membersUnordered));
                            }
                        }
                    }
                }

                return (membersByName, membersUnordered);
            }

            static ImmutableArray<Symbol> concatMembers(ImmutableArray<Symbol> existingMembers, NamedTypeSymbol extension, ImmutableArray<Symbol> newMembers, ref ImmutableArray<Symbol> membersUnordered)
            {
                Debug.Assert(!newMembers.IsEmpty);

                if (extension.IsDefinition)
                {
                    Debug.Assert(newMembers.All(static (m, membersUnordered) => membersUnordered.Contains(m), membersUnordered));
                    return existingMembers.Concat(newMembers);
                }

                var membersBuilder = ArrayBuilder<Symbol>.GetInstance(existingMembers.Length + newMembers.Length);
                var membersUnorderedBuilder = ArrayBuilder<Symbol>.GetInstance(membersUnordered.Length + newMembers.Length);

                membersBuilder.AddRange(existingMembers);
                membersUnorderedBuilder.AddRange(membersUnordered);

                foreach (var member in newMembers)
                {
                    Symbol toAdd = member.SymbolAsMember(extension);
                    membersBuilder.Add(toAdd);
                    membersUnorderedBuilder.Add(toAdd);
                }

                membersUnordered = membersUnorderedBuilder.ToImmutableAndFree();
                return membersBuilder.ToImmutableAndFree();
            }
        }

        private void CheckSpecialMemberErrors(BindingDiagnosticBag diagnostics)
        {
            var conversions = this.ContainingAssembly.CorLibrary.TypeConversions;

            if (this.IsExtension)
            {
                ((SourceNamedTypeSymbol)this).TryGetOrCreateExtensionMarker()?.AfterAddingTypeMembersChecks(conversions, diagnostics);
            }

            foreach (var member in this.GetMembersUnordered())
            {
                member.AfterAddingTypeMembersChecks(conversions, diagnostics);
            }
        }

        private void CheckTypeParameterNameConflicts(BindingDiagnosticBag diagnostics)
        {
            if (this.TypeKind is TypeKind.Delegate or TypeKind.Extension)
            {
                // Delegates and extensions do not have conflicts between their type parameter
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
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, dup.GetFirstLocation(), this, tp.Name);
                    }
                }
            }
        }

        private static void CheckAccessorNameConflicts(
            SourceMemberContainerTypeSymbol containerForDiagnostics,
            bool mightHaveMembersFromDistinctNonPartialDeclarations,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            ImmutableArray<Symbol> membersUnordered,
            BindingDiagnosticBag diagnostics)
        {
            // Report errors where property and event accessors
            // conflict with other members of the same name.
            foreach (Symbol symbol in membersUnordered)
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
                            CheckForMemberConflictWithPropertyAccessor(containerForDiagnostics, mightHaveMembersFromDistinctNonPartialDeclarations, membersByName, propertySymbol, getNotSet: true, diagnostics: diagnostics);
                            CheckForMemberConflictWithPropertyAccessor(containerForDiagnostics, mightHaveMembersFromDistinctNonPartialDeclarations, membersByName, propertySymbol, getNotSet: false, diagnostics: diagnostics);
                            break;
                        }
                    case SymbolKind.Event:
                        {
                            var eventSymbol = (EventSymbol)symbol;
                            CheckForMemberConflictWithEventAccessor(containerForDiagnostics, mightHaveMembersFromDistinctNonPartialDeclarations, membersByName, eventSymbol, isAdder: true, diagnostics: diagnostics);
                            CheckForMemberConflictWithEventAccessor(containerForDiagnostics, mightHaveMembersFromDistinctNonPartialDeclarations, membersByName, eventSymbol, isAdder: false, diagnostics: diagnostics);
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
                        var diagnostics = BindingDiagnosticBag.GetInstance();
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

        private bool CheckStructCircularity(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(TypeKind == TypeKind.Struct);

            CheckFiniteFlatteningGraph(diagnostics);
            return HasStructCircularity(diagnostics);
        }

        private bool HasStructCircularity(BindingDiagnosticBag diagnostics)
        {
            foreach (var valuesByName in GetMembersByName().Values)
            {
                foreach (var member in valuesByName)
                {
                    FieldSymbol? field;

                    // Only instance fields (including field-like events) affect the outcome.
                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                            field = (FieldSymbol)member;
                            Debug.Assert(field.AssociatedSymbol is not EventSymbol, "Didn't expect to find a field-like event backing field in the member list.");
                            break;
                        case SymbolKind.Event:
                            field = ((EventSymbol)member).AssociatedField;
                            break;
                        default:
                            continue;
                    }

                    if (field is null || field.IsStatic)
                    {
                        continue;
                    }
                    var type = field.NonPointerType();
                    if (((object)type != null) &&
                        (type.TypeKind == TypeKind.Struct) &&
                        BaseTypeAnalysis.StructDependsOn((NamedTypeSymbol)type, this) &&
                        !type.IsPrimitiveRecursiveStruct()) // allow System.Int32 to contain a field of its own type
                    {
                        if (field is SynthesizedPrimaryConstructorParameterBackingFieldSymbol { ParameterSymbol: var parameterSymbol })
                        {
                            diagnostics.Add(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, parameterSymbol.GetFirstLocation(), parameterSymbol, type);
                        }
                        else
                        {
                            // If this is a backing field, report the error on the associated property.
                            var symbol = field.AssociatedSymbol ?? field;

                            // Struct member '{0}' of type '{1}' causes a cycle in the struct layout
                            diagnostics.Add(ErrorCode.ERR_StructLayoutCycle, symbol.GetFirstLocation(), symbol, type);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private void CheckForProtectedInStaticClass(BindingDiagnosticBag diagnostics)
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

                    if (member.DeclaredAccessibility.HasProtected() && member is not SourceExtensionImplementationMethodSymbol)
                    {
                        if (member.Kind != SymbolKind.Method || ((MethodSymbol)member).MethodKind != MethodKind.Destructor)
                        {
                            diagnostics.Add(ErrorCode.ERR_ProtectedInStatic, member.GetFirstLocation(), member);
                        }
                    }
                }
            }
        }

        private static void CheckForUnmatchedOperators(
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            BindingDiagnosticBag diagnostics)
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

            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.TrueOperatorName, WellKnownMemberNames.FalseOperatorName);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.EqualityOperatorName, WellKnownMemberNames.InequalityOperatorName);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.LessThanOperatorName, WellKnownMemberNames.GreaterThanOperatorName);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.LessThanOrEqualOperatorName, WellKnownMemberNames.GreaterThanOrEqualOperatorName);

            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedDecrementOperatorName, WellKnownMemberNames.DecrementOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedIncrementOperatorName, WellKnownMemberNames.IncrementOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedUnaryNegationOperatorName, WellKnownMemberNames.UnaryNegationOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedAdditionOperatorName, WellKnownMemberNames.AdditionOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedDivisionOperatorName, WellKnownMemberNames.DivisionOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedMultiplyOperatorName, WellKnownMemberNames.MultiplyOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedSubtractionOperatorName, WellKnownMemberNames.SubtractionOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedExplicitConversionName, WellKnownMemberNames.ExplicitConversionName, symmetricCheck: false);

            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedAdditionAssignmentOperatorName, WellKnownMemberNames.AdditionAssignmentOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedDivisionAssignmentOperatorName, WellKnownMemberNames.DivisionAssignmentOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedMultiplicationAssignmentOperatorName, WellKnownMemberNames.MultiplicationAssignmentOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedSubtractionAssignmentOperatorName, WellKnownMemberNames.SubtractionAssignmentOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedDecrementAssignmentOperatorName, WellKnownMemberNames.DecrementAssignmentOperatorName, symmetricCheck: false);
            CheckForUnmatchedOperator(membersByName, diagnostics, WellKnownMemberNames.CheckedIncrementAssignmentOperatorName, WellKnownMemberNames.IncrementAssignmentOperatorName, symmetricCheck: false);
        }

        private static void CheckForUnmatchedOperator(
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            BindingDiagnosticBag diagnostics, string operatorName1, string operatorName2, bool symmetricCheck = true)
        {
            var ops1 = ArrayBuilder<MethodSymbol>.GetInstance();
            addOperators(membersByName, operatorName1, ops1);

            if (symmetricCheck)
            {
                var ops2 = ArrayBuilder<MethodSymbol>.GetInstance();
                addOperators(membersByName, operatorName2, ops2);
                CheckForUnmatchedOperator(diagnostics, ops1, ops2, operatorName2, reportOperatorNeedsMatch);
                CheckForUnmatchedOperator(diagnostics, ops2, ops1, operatorName1, reportOperatorNeedsMatch);
                ops2.Free();
            }
            else if (!ops1.IsEmpty)
            {
                var ops2 = ArrayBuilder<MethodSymbol>.GetInstance();
                addOperators(membersByName, operatorName2, ops2);
                CheckForUnmatchedOperator(diagnostics, ops1, ops2, operatorName2, reportCheckedOperatorNeedsMatch);
                ops2.Free();
            }

            ops1.Free();

            return;

            static void reportOperatorNeedsMatch(BindingDiagnosticBag diagnostics, string operatorName2, MethodSymbol op1)
            {
                // CS0216: The operator 'C.operator true(C)' requires a matching operator 'false' to also be defined
                diagnostics.Add(ErrorCode.ERR_OperatorNeedsMatch, op1.GetFirstLocation(), op1,
                    SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(operatorName2)));
            }

            static void reportCheckedOperatorNeedsMatch(BindingDiagnosticBag diagnostics, string operatorName2, MethodSymbol op1)
            {
                diagnostics.Add(ErrorCode.ERR_CheckedOperatorNeedsMatch, op1.GetFirstLocation(), op1);
            }

            static void addOperators(
                Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
                string operatorName1, ArrayBuilder<MethodSymbol> ops1)
            {
                if (membersByName.TryGetValue(operatorName1.AsMemory(), out ImmutableArray<Symbol> candidates))
                {
                    AddOperators(ops1, OneOrMany.Create(candidates));
                }
            }
        }

        private static void CheckForUnmatchedOperator(
            BindingDiagnosticBag diagnostics,
            ArrayBuilder<MethodSymbol> ops1,
            ArrayBuilder<MethodSymbol> ops2,
            string operatorName2,
            Action<BindingDiagnosticBag, string, MethodSymbol> reportMatchNotFoundError)
        {
            foreach (var op1 in ops1)
            {
                if (op1.IsOverride)
                {
                    continue;
                }

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
                    reportMatchNotFoundError(diagnostics, operatorName2, op1);
                }
            }
        }

        internal static bool DoOperatorsPair(MethodSymbol op1, MethodSymbol op2)
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

        private void CheckForEqualityAndGetHashCode(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!this.IsExtension);

            if (this.IsInterfaceType())
            {
                // Interfaces are allowed to define Equals without GetHashCode if they want.
                return;
            }

            if (IsRecord || IsRecordStruct)
            {
                // For records the warnings reported below are simply going to echo record specific errors,
                // producing more noise.
                return;
            }

            var ops = ArrayBuilder<MethodSymbol>.GetInstance();
            this.AddOperators(WellKnownMemberNames.EqualityOperatorName, ops);
            this.AddOperators(WellKnownMemberNames.InequalityOperatorName, ops);

            bool hasOp = ops.Any();
            bool overridesEquals = this.TypeOverridesObjectMethod("Equals");

            if (hasOp || overridesEquals)
            {
                bool overridesGHC = this.TypeOverridesObjectMethod("GetHashCode");
                if (overridesEquals && !overridesGHC)
                {
                    // CS0659: 'C' overrides Object.Equals(object o) but does not override Object.GetHashCode()
                    diagnostics.Add(ErrorCode.WRN_EqualsWithoutGetHashCode, this.GetFirstLocation(), this);
                }

                if (hasOp && !overridesEquals)
                {
                    // CS0660: 'C' defines operator == or operator != but does not override Object.Equals(object o)
                    diagnostics.Add(ErrorCode.WRN_EqualityOpWithoutEquals, this.GetFirstLocation(), this);
                }

                if (hasOp && !overridesGHC)
                {
                    // CS0661: 'C' defines operator == or operator != but does not override Object.GetHashCode()
                    diagnostics.Add(ErrorCode.WRN_EqualityOpWithoutGetHashCode, this.GetFirstLocation(), this);
                }
            }

            ops.Free();
        }

        private void CheckForRequiredMemberAttribute(BindingDiagnosticBag diagnostics)
        {
            if (HasDeclaredRequiredMembers)
            {
                // Ensure that an error is reported if the required constructor isn't present.
                _ = Binder.GetWellKnownTypeMember(DeclaringCompilation, WellKnownMember.System_Runtime_CompilerServices_RequiredMemberAttribute__ctor, diagnostics, GetFirstLocation());
            }

            if (HasAnyRequiredMembers)
            {
                _ = Binder.GetWellKnownTypeMember(DeclaringCompilation, WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor, diagnostics, GetFirstLocation());

                if (this.IsRecord)
                {
                    // Copy constructors need to emit SetsRequiredMembers on the ctor
                    _ = Binder.GetWellKnownTypeMember(DeclaringCompilation, WellKnownMember.System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute__ctor, diagnostics, GetFirstLocation());
                }
            }

            if (BaseTypeNoUseSiteDiagnostics is (not SourceMemberContainerTypeSymbol) and { HasRequiredMembersError: true })
            {
                foreach (var member in GetMembersUnordered())
                {
                    if (member is not MethodSymbol method || !method.ShouldCheckRequiredMembers())
                    {
                        continue;
                    }

                    // The required members list for the base type '{0}' is malformed and cannot be interpreted. To use this constructor, apply the 'SetsRequiredMembers' attribute.
                    diagnostics.Add(ErrorCode.ERR_RequiredMembersBaseTypeInvalid, method.GetFirstLocation(), BaseTypeNoUseSiteDiagnostics);
                }
            }
        }

        private void ReportRequiredMembers(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(IsSubmissionClass || IsScriptClass);

            foreach (var member in GetMembersUnordered())
            {
                if (member.IsRequired())
                {
                    // Required members are not allowed on the top level of a script or submission.
                    diagnostics.Add(ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers, member.GetFirstLocation());
                }
            }
        }

        private bool TypeOverridesObjectMethod(string name)
        {
            foreach (var method in this.GetMembers(name).OfType<MethodSymbol>())
            {
                if (method.IsOverride && method.GetConstructedLeastOverriddenMethod(this, requireSameReturnType: false).ContainingType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckFiniteFlatteningGraph(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(ReferenceEquals(this, this.OriginalDefinition));
            if (AllTypeArgumentCount() == 0) return;
            var instanceMap = new Dictionary<NamedTypeSymbol, NamedTypeSymbol>(ReferenceEqualityComparer.Instance);
            instanceMap.Add(this, this);
            foreach (var m in this.GetMembersUnordered())
            {
                FieldSymbol? f;

                // Only instance fields (including field-like events) affect the outcome.
                switch (m.Kind)
                {
                    case SymbolKind.Field:
                        f = (FieldSymbol)m;
                        Debug.Assert(f.AssociatedSymbol is not EventSymbol, "Didn't expect to find a field-like event backing field in the member list.");
                        break;
                    case SymbolKind.Event:
                        f = ((EventSymbol)m).AssociatedField;
                        break;
                    default:
                        continue;
                }

                if (f is null || !f.IsStatic || f.Type.TypeKind != TypeKind.Struct) continue;
                var type = (NamedTypeSymbol)f.Type;
                if (InfiniteFlatteningGraph(this, type, instanceMap))
                {
                    // Struct member '{0}' of type '{1}' causes a cycle in the struct layout
                    diagnostics.Add(ErrorCode.ERR_StructLayoutCycle, f.GetFirstLocation(), f, type);
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

        private void CheckSequentialOnPartialType(BindingDiagnosticBag diagnostics)
        {
            if (!IsPartial || (this.Layout.Kind != LayoutKind.Sequential && this.Layout.Kind != LayoutKind.Extended))
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
                    if (hasInstanceData(m))
                    {
                        if (whereFoundField != null && whereFoundField != syntaxRef)
                        {
                            diagnostics.Add(ErrorCode.WRN_SequentialOnPartialClass, GetFirstLocation(), this);
                            return;
                        }

                        whereFoundField = syntaxRef;
                    }
                }
            }

            if (whereFoundField != null &&
                PrimaryConstructor is { } primaryConstructor && primaryConstructor.GetCapturedParameters().Any() &&
                (primaryConstructor.SyntaxRef.SyntaxTree != whereFoundField.SyntaxTree || primaryConstructor.SyntaxRef.Span != whereFoundField.Span))
            {
                diagnostics.Add(ErrorCode.WRN_SequentialOnPartialClass, GetFirstLocation(), this);
                return;
            }

            static bool hasInstanceData(MemberDeclarationSyntax m)
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
                            !ContainsModifier(propertyDecl.Modifiers, SyntaxKind.PartialKeyword) &&
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
        }

        private static bool All<T>(SyntaxList<T> list, Func<T, bool> predicate) where T : CSharpSyntaxNode
        {
            foreach (var t in list) { if (predicate(t)) return true; }
            return false;
        }

        private static bool ContainsModifier(SyntaxTokenList modifiers, SyntaxKind modifier)
        {
            foreach (var m in modifiers) { if (m.IsKind(modifier)) return true; }
            return false;
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> MakeAllMembers()
        {
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName;
            var membersAndInitializers = GetMembersAndInitializers();

            // Most types don't have indexers.  If this is one of those types,
            // just reuse the dictionary we build for early attribute decoding.
            // For tuples, we also need to take the slow path.
            if (!membersAndInitializers.HaveIndexers && !this.IsTupleType && _lazyEarlyAttributeDecodingMembersDictionary is object)
            {
                membersByName = _lazyEarlyAttributeDecodingMembersDictionary;
            }
            else
            {
                membersByName = ToNameKeyedDictionary(membersAndInitializers.NonTypeMembers);

                // Merge types into the member dictionary
                AddNestedTypesToDictionary(membersByName, GetTypeMembersDictionary());
            }

            return membersByName;
        }

        private static void AddNestedTypesToDictionary(
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>> typesByName)
        {
            foreach ((ReadOnlyMemory<char> name, ImmutableArray<NamedTypeSymbol> types) in typesByName)
            {
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

        private sealed class DeclaredMembersAndInitializersBuilder
        {
            public ArrayBuilder<Symbol> NonTypeMembersWithPartialImplementations = ArrayBuilder<Symbol>.GetInstance();
            public readonly ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer>> StaticInitializers = ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer>>.GetInstance();
            public readonly ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer>> InstanceInitializers = ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer>>.GetInstance();
            public bool HaveIndexers;
            public TypeDeclarationSyntax? DeclarationWithParameters;

            public SynthesizedPrimaryConstructor? PrimaryConstructor;
            public bool IsNullableEnabledForInstanceConstructorsAndFields;
            public bool IsNullableEnabledForStaticConstructorsAndFields;

            public DeclaredMembersAndInitializers ToReadOnlyAndFree(CSharpCompilation compilation)
            {
                return new DeclaredMembersAndInitializers(
                    NonTypeMembersWithPartialImplementations.ToImmutableAndFree(),
                    MembersAndInitializersBuilder.ToReadOnlyAndFree(StaticInitializers),
                    MembersAndInitializersBuilder.ToReadOnlyAndFree(InstanceInitializers),
                    HaveIndexers,
                    DeclarationWithParameters,
                    PrimaryConstructor,
                    isNullableEnabledForInstanceConstructorsAndFields: IsNullableEnabledForInstanceConstructorsAndFields,
                    isNullableEnabledForStaticConstructorsAndFields: IsNullableEnabledForStaticConstructorsAndFields,
                    compilation);
            }

            public void UpdateIsNullableEnabledForConstructorsAndFields(bool useStatic, CSharpCompilation compilation, CSharpSyntaxNode syntax)
            {
                ref bool isNullableEnabled = ref GetIsNullableEnabledForConstructorsAndFields(useStatic);
                isNullableEnabled = isNullableEnabled || compilation.IsNullableAnalysisEnabledIn(syntax);
            }

            public void UpdateIsNullableEnabledForConstructorsAndFields(bool useStatic, bool value)
            {
                ref bool isNullableEnabled = ref GetIsNullableEnabledForConstructorsAndFields(useStatic);
                isNullableEnabled = isNullableEnabled || value;
            }

            private ref bool GetIsNullableEnabledForConstructorsAndFields(bool useStatic)
            {
                return ref useStatic ? ref IsNullableEnabledForStaticConstructorsAndFields : ref IsNullableEnabledForInstanceConstructorsAndFields;
            }

            public void Free()
            {
                NonTypeMembersWithPartialImplementations.Free();

                foreach (var group in StaticInitializers)
                {
                    group.Free();
                }
                StaticInitializers.Free();

                foreach (var group in InstanceInitializers)
                {
                    group.Free();
                }
                InstanceInitializers.Free();
            }
        }

        protected sealed class DeclaredMembersAndInitializers
        {
            public readonly ImmutableArray<Symbol> NonTypeMembersWithPartialImplementations;
            public readonly ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> StaticInitializers;
            public readonly ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> InstanceInitializers;
            public readonly bool HaveIndexers;
            public readonly TypeDeclarationSyntax? DeclarationWithParameters;
            public readonly SynthesizedPrimaryConstructor? PrimaryConstructor;
            public readonly bool IsNullableEnabledForInstanceConstructorsAndFields;
            public readonly bool IsNullableEnabledForStaticConstructorsAndFields;

            private ImmutableArray<Symbol> _lazyNonTypeMembers;

            public static readonly DeclaredMembersAndInitializers UninitializedSentinel = new DeclaredMembersAndInitializers();

            private DeclaredMembersAndInitializers()
            {
            }

            public DeclaredMembersAndInitializers(
                ImmutableArray<Symbol> nonTypeMembersWithPartialImplementations,
                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> staticInitializers,
                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> instanceInitializers,
                bool haveIndexers,
                TypeDeclarationSyntax? declarationWithParameters,
                SynthesizedPrimaryConstructor? primaryConstructor,
                bool isNullableEnabledForInstanceConstructorsAndFields,
                bool isNullableEnabledForStaticConstructorsAndFields,
                CSharpCompilation compilation)
            {
                Debug.Assert(!nonTypeMembersWithPartialImplementations.IsDefault);
                AssertInitializers(staticInitializers, compilation);
                AssertInitializers(instanceInitializers, compilation);

                Debug.Assert(!nonTypeMembersWithPartialImplementations.Any(static s => s is TypeSymbol));
                Debug.Assert(declarationWithParameters is object == primaryConstructor is object);

                this.NonTypeMembersWithPartialImplementations = nonTypeMembersWithPartialImplementations;
                this.StaticInitializers = staticInitializers;
                this.InstanceInitializers = instanceInitializers;
                this.HaveIndexers = haveIndexers;
                this.DeclarationWithParameters = declarationWithParameters;
                this.PrimaryConstructor = primaryConstructor;
                this.IsNullableEnabledForInstanceConstructorsAndFields = isNullableEnabledForInstanceConstructorsAndFields;
                this.IsNullableEnabledForStaticConstructorsAndFields = isNullableEnabledForStaticConstructorsAndFields;
            }

            [Conditional("DEBUG")]
            public static void AssertInitializers(ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers, CSharpCompilation compilation)
            {
                Debug.Assert(!initializers.IsDefault);
                if (initializers.IsEmpty)
                {
                    return;
                }

                foreach (ImmutableArray<FieldOrPropertyInitializer> group in initializers)
                {
                    Debug.Assert(!group.IsDefaultOrEmpty);
                }

                for (int i = 0; i < initializers.Length; i++)
                {
                    if (i > 0)
                    {
                        Debug.Assert(LexicalSortKey.Compare(new LexicalSortKey(initializers[i - 1].First().Syntax, compilation), new LexicalSortKey(initializers[i].Last().Syntax, compilation)) < 0);
                    }

                    if (i + 1 < initializers.Length)
                    {
                        Debug.Assert(LexicalSortKey.Compare(new LexicalSortKey(initializers[i].First().Syntax, compilation), new LexicalSortKey(initializers[i + 1].Last().Syntax, compilation)) < 0);
                    }

                    if (initializers[i].Length != 1)
                    {
                        Debug.Assert(LexicalSortKey.Compare(new LexicalSortKey(initializers[i].First().Syntax, compilation), new LexicalSortKey(initializers[i].Last().Syntax, compilation)) < 0);
                    }
                }
            }

            public ImmutableArray<Symbol> GetNonTypeMembers(SourceMemberContainerTypeSymbol container)
            {
                if (_lazyNonTypeMembers.IsDefault)
                {
                    container.MergePartialMembersAndInitializeNonTypeMembers(NonTypeMembersWithPartialImplementations, ref _lazyNonTypeMembers);
                }

                return _lazyNonTypeMembers;
            }
        }

        private sealed class MembersAndInitializersBuilder
        {
            private ArrayBuilder<Symbol>? NonTypeMembers;
            private ArrayBuilder<FieldOrPropertyInitializer>? InstanceInitializersForPositionalMembers;
            private bool IsNullableEnabledForInstanceConstructorsAndFields;
            private bool IsNullableEnabledForStaticConstructorsAndFields;

            public MembersAndInitializersBuilder(DeclaredMembersAndInitializers declaredMembersAndInitializers)
            {
                Debug.Assert(declaredMembersAndInitializers != DeclaredMembersAndInitializers.UninitializedSentinel);

                this.IsNullableEnabledForInstanceConstructorsAndFields = declaredMembersAndInitializers.IsNullableEnabledForInstanceConstructorsAndFields;
                this.IsNullableEnabledForStaticConstructorsAndFields = declaredMembersAndInitializers.IsNullableEnabledForStaticConstructorsAndFields;
            }

            public MembersAndInitializers ToReadOnlyAndFree(SourceMemberContainerTypeSymbol container, DeclaredMembersAndInitializers declaredMembers)
            {
                var nonTypeMembers = NonTypeMembers?.ToImmutableAndFree() ?? declaredMembers.GetNonTypeMembers(container);

                var instanceInitializers = InstanceInitializersForPositionalMembers is null
                    ? declaredMembers.InstanceInitializers
                    : mergeInitializers();

                return new MembersAndInitializers(
                    declaredMembers.PrimaryConstructor,
                    nonTypeMembers,
                    declaredMembers.StaticInitializers,
                    instanceInitializers,
                    declaredMembers.HaveIndexers,
                    isNullableEnabledForInstanceConstructorsAndFields: IsNullableEnabledForInstanceConstructorsAndFields,
                    isNullableEnabledForStaticConstructorsAndFields: IsNullableEnabledForStaticConstructorsAndFields);

                ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> mergeInitializers()
                {
                    Debug.Assert(InstanceInitializersForPositionalMembers.Count != 0);
                    Debug.Assert(declaredMembers.PrimaryConstructor is object);
                    Debug.Assert(declaredMembers.DeclarationWithParameters is object);
                    Debug.Assert(declaredMembers.DeclarationWithParameters.SyntaxTree == InstanceInitializersForPositionalMembers[0].Syntax.SyntaxTree);
                    Debug.Assert(declaredMembers.DeclarationWithParameters.Span.Contains(InstanceInitializersForPositionalMembers[0].Syntax.Span.Start));

                    var groupCount = declaredMembers.InstanceInitializers.Length;

                    if (groupCount == 0)
                    {
                        return ImmutableArray.Create(InstanceInitializersForPositionalMembers.ToImmutableAndFree());
                    }

                    var compilation = declaredMembers.PrimaryConstructor.DeclaringCompilation;
                    var sortKey = new LexicalSortKey(InstanceInitializersForPositionalMembers.First().Syntax, compilation);

                    int insertAt;

                    for (insertAt = 0; insertAt < groupCount; insertAt++)
                    {
                        if (LexicalSortKey.Compare(sortKey, new LexicalSortKey(declaredMembers.InstanceInitializers[insertAt][0].Syntax, compilation)) < 0)
                        {
                            break;
                        }
                    }

                    ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>> groupsBuilder;

                    if (insertAt != groupCount &&
                        declaredMembers.DeclarationWithParameters.SyntaxTree == declaredMembers.InstanceInitializers[insertAt][0].Syntax.SyntaxTree &&
                        declaredMembers.DeclarationWithParameters.Span.Contains(declaredMembers.InstanceInitializers[insertAt][0].Syntax.Span.Start))
                    {
                        // Need to merge into the previous group
                        var declaredInitializers = declaredMembers.InstanceInitializers[insertAt];
                        var insertedInitializers = InstanceInitializersForPositionalMembers;
#if DEBUG
                        // initializers should be added in syntax order:
                        Debug.Assert(insertedInitializers[insertedInitializers.Count - 1].Syntax.SyntaxTree == declaredInitializers[0].Syntax.SyntaxTree);
                        Debug.Assert(insertedInitializers[insertedInitializers.Count - 1].Syntax.Span.Start < declaredInitializers[0].Syntax.Span.Start);
#endif

                        insertedInitializers.AddRange(declaredInitializers);

                        groupsBuilder = ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>>.GetInstance(groupCount);
                        groupsBuilder.AddRange(declaredMembers.InstanceInitializers, insertAt);
                        groupsBuilder.Add(insertedInitializers.ToImmutableAndFree());
                        groupsBuilder.AddRange(declaredMembers.InstanceInitializers, insertAt + 1, groupCount - (insertAt + 1));
                        Debug.Assert(groupsBuilder.Count == groupCount);
                    }
                    else
                    {
                        Debug.Assert(!declaredMembers.InstanceInitializers.Any(g => declaredMembers.DeclarationWithParameters.SyntaxTree == g[0].Syntax.SyntaxTree &&
                                                                                    declaredMembers.DeclarationWithParameters.Span.Contains(g[0].Syntax.Span.Start)));
                        groupsBuilder = ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>>.GetInstance(groupCount + 1);
                        groupsBuilder.AddRange(declaredMembers.InstanceInitializers, insertAt);
                        groupsBuilder.Add(InstanceInitializersForPositionalMembers.ToImmutableAndFree());
                        groupsBuilder.AddRange(declaredMembers.InstanceInitializers, insertAt, groupCount - insertAt);
                        Debug.Assert(groupsBuilder.Count == groupCount + 1);
                    }

                    var result = groupsBuilder.ToImmutableAndFree();

                    DeclaredMembersAndInitializers.AssertInitializers(result, compilation);
                    return result;
                }
            }

            public void AddInstanceInitializerForPositionalMembers(FieldOrPropertyInitializer initializer)
            {
                if (InstanceInitializersForPositionalMembers is null)
                {
                    InstanceInitializersForPositionalMembers = ArrayBuilder<FieldOrPropertyInitializer>.GetInstance();
                }

                InstanceInitializersForPositionalMembers.Add(initializer);
            }

            public IReadOnlyCollection<Symbol> GetNonTypeMembers(SourceMemberContainerTypeSymbol container, DeclaredMembersAndInitializers declaredMembers)
            {
                return NonTypeMembers ?? (IReadOnlyCollection<Symbol>)declaredMembers.GetNonTypeMembers(container);
            }

            public void AddNonTypeMember(SourceMemberContainerTypeSymbol container, Symbol member, DeclaredMembersAndInitializers declaredMembers)
            {
                if (NonTypeMembers is null)
                {
                    var declaredNonTypeMembers = declaredMembers.GetNonTypeMembers(container);
                    NonTypeMembers = ArrayBuilder<Symbol>.GetInstance(declaredNonTypeMembers.Length + 1);
                    NonTypeMembers.AddRange(declaredNonTypeMembers);
                }

                NonTypeMembers.Add(member);
            }

            public void SetNonTypeMembers(ArrayBuilder<Symbol> members)
            {
                NonTypeMembers?.Free();
                NonTypeMembers = members;
            }

            public void UpdateIsNullableEnabledForConstructorsAndFields(bool useStatic, CSharpCompilation compilation, CSharpSyntaxNode syntax)
            {
                ref bool isNullableEnabled = ref GetIsNullableEnabledForConstructorsAndFields(useStatic);
                isNullableEnabled = isNullableEnabled || compilation.IsNullableAnalysisEnabledIn(syntax);
            }

            private ref bool GetIsNullableEnabledForConstructorsAndFields(bool useStatic)
            {
                return ref useStatic ? ref IsNullableEnabledForStaticConstructorsAndFields : ref IsNullableEnabledForInstanceConstructorsAndFields;
            }

            internal static ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> ToReadOnlyAndFree(ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer>> initializers)
            {
                if (initializers.Count == 0)
                {
                    initializers.Free();
                    return ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>>.Empty;
                }

                var builder = ArrayBuilder<ImmutableArray<FieldOrPropertyInitializer>>.GetInstance(initializers.Count);
                foreach (ArrayBuilder<FieldOrPropertyInitializer> group in initializers)
                {
                    builder.Add(group.ToImmutableAndFree());
                }

                initializers.Free();
                return builder.ToImmutableAndFree();
            }

            public void Free()
            {
                NonTypeMembers?.Free();
                InstanceInitializersForPositionalMembers?.Free();
            }
        }

        private MembersAndInitializers? BuildMembersAndInitializers(BindingDiagnosticBag diagnostics)
        {
            var declaredMembersAndInitializers = getDeclaredMembersAndInitializers();
            if (declaredMembersAndInitializers is null)
            {
                // Another thread completed the work before this one
                return null;
            }

            var membersAndInitializersBuilder = new MembersAndInitializersBuilder(declaredMembersAndInitializers);
            AddSynthesizedMembers(membersAndInitializersBuilder, declaredMembersAndInitializers, diagnostics);

            if (Volatile.Read(ref _lazyMembersAndInitializers) != null)
            {
                // Another thread completed the work before this one
                membersAndInitializersBuilder.Free();
                return null;
            }

            return membersAndInitializersBuilder.ToReadOnlyAndFree(this, declaredMembersAndInitializers);

            DeclaredMembersAndInitializers? getDeclaredMembersAndInitializers()
            {
                var declaredMembersAndInitializers = _lazyDeclaredMembersAndInitializers;
                if (declaredMembersAndInitializers != DeclaredMembersAndInitializers.UninitializedSentinel)
                {
                    return declaredMembersAndInitializers;
                }

                if (Volatile.Read(ref _lazyMembersAndInitializers) is not null)
                {
                    // We're previously computed declared members and already cleared them out
                    // No need to compute them again
                    return null;
                }

                var diagnostics = BindingDiagnosticBag.GetInstance();
                declaredMembersAndInitializers = buildDeclaredMembersAndInitializers(diagnostics);

                var alreadyKnown = Interlocked.CompareExchange(ref _lazyDeclaredMembersAndInitializers, declaredMembersAndInitializers, DeclaredMembersAndInitializers.UninitializedSentinel);
                if (alreadyKnown != DeclaredMembersAndInitializers.UninitializedSentinel)
                {
                    diagnostics.Free();
                    return alreadyKnown;
                }

                AddDeclarationDiagnostics(diagnostics);
                diagnostics.Free();

                return declaredMembersAndInitializers;
            }

            // Builds explicitly declared members (as opposed to synthesized members).
            // This should not attempt to bind any method parameters as that would cause
            // the members being built to be captured in the binder cache before the final
            // list of members is determined.
            DeclaredMembersAndInitializers? buildDeclaredMembersAndInitializers(BindingDiagnosticBag diagnostics)
            {
                var builder = new DeclaredMembersAndInitializersBuilder();
                AddDeclaredNontypeMembers(builder, diagnostics);

                switch (TypeKind)
                {
                    case TypeKind.Struct:
                        CheckForStructBadInitializers(builder, diagnostics);
                        CheckForStructDefaultConstructors(builder.NonTypeMembersWithPartialImplementations, isEnum: false, diagnostics: diagnostics);
                        break;

                    case TypeKind.Enum:
                        CheckForStructDefaultConstructors(builder.NonTypeMembersWithPartialImplementations, isEnum: true, diagnostics: diagnostics);
                        break;

                    case TypeKind.Class:
                    case TypeKind.Interface:
                    case TypeKind.Submission:
                        // No additional checking required.
                        break;

                    default:
                        break;
                }

                if (Volatile.Read(ref _lazyDeclaredMembersAndInitializers) != DeclaredMembersAndInitializers.UninitializedSentinel)
                {
                    // _lazyDeclaredMembersAndInitializers is already computed. no point to continue.
                    builder.Free();
                    return null;
                }

                return builder.ToReadOnlyAndFree(DeclaringCompilation);
            }
        }

        private void MergePartialMembersAndInitializeNonTypeMembers(ImmutableArray<Symbol> nonTypeMembersWithPartialImplementations, ref ImmutableArray<Symbol> nonTypeMembers)
        {
            PooledDictionary<ReadOnlyMemory<char>, object>? partialMembersToMerge = null;

            foreach (Symbol member in nonTypeMembersWithPartialImplementations)
            {
                if (member.IsPartialMember())
                {
                    ImmutableArrayExtensions.AddToMultiValueDictionaryBuilder(
                        partialMembersToMerge ??= s_nameToObjectPool.Allocate(),
                        (member.IsIndexer() ? WellKnownMemberNames.Indexer : member.Name).AsMemory(),
                        member);
                }
            }

            if (partialMembersToMerge is null)
            {
                ImmutableInterlocked.InterlockedInitialize(ref nonTypeMembers, nonTypeMembersWithPartialImplementations);
                return;
            }

            Debug.Assert(partialMembersToMerge.Count != 0);

            var diagnostics = BindingDiagnosticBag.GetInstance();
            var nonTypeMembersBuilder = ArrayBuilder<Symbol>.GetInstance(nonTypeMembersWithPartialImplementations.Length);
            nonTypeMembersBuilder.AddRange(nonTypeMembersWithPartialImplementations);
            MergePartialMembers(partialMembersToMerge, nonTypeMembersBuilder, diagnostics);
            partialMembersToMerge.Free();

            if (ImmutableInterlocked.InterlockedInitialize(ref nonTypeMembers, nonTypeMembersBuilder.ToImmutableAndFree()))
            {
                AddDeclarationDiagnostics(diagnostics);
            }

            diagnostics.Free();
        }

        internal ImmutableArray<SynthesizedSimpleProgramEntryPointSymbol> GetSimpleProgramEntryPoints()
        {

            if (_lazySimpleProgramEntryPoints.IsDefault)
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();
                var simpleProgramEntryPoints = buildSimpleProgramEntryPoint(diagnostics);

                if (ImmutableInterlocked.InterlockedInitialize(ref _lazySimpleProgramEntryPoints, simpleProgramEntryPoints))
                {
                    AddDeclarationDiagnostics(diagnostics);
                }

                diagnostics.Free();
            }

            Debug.Assert(!_lazySimpleProgramEntryPoints.IsDefault);
            return _lazySimpleProgramEntryPoints;

            ImmutableArray<SynthesizedSimpleProgramEntryPointSymbol> buildSimpleProgramEntryPoint(BindingDiagnosticBag diagnostics)
            {
                if (this.ContainingSymbol is not NamespaceSymbol { IsGlobalNamespace: true }
                    || this.Name != WellKnownMemberNames.TopLevelStatementsEntryPointTypeName)
                {
                    return ImmutableArray<SynthesizedSimpleProgramEntryPointSymbol>.Empty;
                }

                ArrayBuilder<SynthesizedSimpleProgramEntryPointSymbol>? builder = null;

                foreach (var singleDecl in declaration.Declarations)
                {
                    if (singleDecl.IsSimpleProgram)
                    {
                        if (builder is null)
                        {
                            builder = ArrayBuilder<SynthesizedSimpleProgramEntryPointSymbol>.GetInstance();
                        }
                        else
                        {
                            Binder.Error(diagnostics, ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements, singleDecl.NameLocation);
                        }

                        builder.Add(new SynthesizedSimpleProgramEntryPointSymbol(this, singleDecl, diagnostics));
                    }
                }

                if (builder is null)
                {
                    return ImmutableArray<SynthesizedSimpleProgramEntryPointSymbol>.Empty;
                }

                return builder.ToImmutableAndFree();
            }
        }

        internal bool HasPrimaryConstructor => this._flags.HasPrimaryConstructor;

        internal SynthesizedPrimaryConstructor? PrimaryConstructor
        {
            get
            {
                if (!HasPrimaryConstructor)
                    return null;

                var declared = Volatile.Read(ref _lazyDeclaredMembersAndInitializers);
                SynthesizedPrimaryConstructor? result;
                if (declared is not null && declared != DeclaredMembersAndInitializers.UninitializedSentinel)
                {
                    result = declared.PrimaryConstructor;
                }
                else
                {
                    result = GetMembersAndInitializers().PrimaryConstructor;
                }

                Debug.Assert(result is object);
                return result;
            }
        }

        internal IEnumerable<SourceMemberMethodSymbol> GetMethodsPossiblyCapturingPrimaryConstructorParameters()
        {
            ImmutableArray<Symbol> nonTypeMembersToCheck;
            SynthesizedPrimaryConstructor? primaryConstructor;

            var declared = Volatile.Read(ref _lazyDeclaredMembersAndInitializers);
            if (declared is not null && declared != DeclaredMembersAndInitializers.UninitializedSentinel)
            {
                nonTypeMembersToCheck = declared.GetNonTypeMembers(this);
                primaryConstructor = declared.PrimaryConstructor;
            }
            else
            {
                var membersAndInitializers = GetMembersAndInitializers();
                nonTypeMembersToCheck = membersAndInitializers.NonTypeMembers;
                primaryConstructor = membersAndInitializers.PrimaryConstructor;
            }

            Debug.Assert(primaryConstructor is not null);
            Debug.Assert(!this.IsDelegateType());

            foreach (var member in nonTypeMembersToCheck)
            {
                if ((object)member == primaryConstructor)
                {
                    continue;
                }

                if (member.IsStatic ||
                    !(member is MethodSymbol method && MethodCompiler.GetMethodToCompile(method) is SourceMemberMethodSymbol sourceMethod))
                {
                    continue;
                }

                if (sourceMethod.IsExtern)
                {
                    continue;
                }

                if (sourceMethod.IsAbstract || sourceMethod.SynthesizesLoweredBoundBody)
                {
                    continue;
                }

                yield return sourceMethod;
            }
        }

        internal ImmutableArray<Symbol> GetMembersToMatchAgainstDeclarationSpan()
        {
            var declared = Volatile.Read(ref _lazyDeclaredMembersAndInitializers);
            if (declared is not null && declared != DeclaredMembersAndInitializers.UninitializedSentinel)
            {
                Debug.Assert(declared.PrimaryConstructor is not null);
                return declared.GetNonTypeMembers(this);
            }
            else
            {
                var membersAndInitializers = GetMembersAndInitializers();
                Debug.Assert(membersAndInitializers.PrimaryConstructor is not null);
                return membersAndInitializers.NonTypeMembers;
            }
        }

        internal ImmutableArray<Symbol> GetCandidateMembersForLookup(string name)
        {
            if (this is { IsRecord: true } or { IsRecordStruct: true } ||
                this.state.HasComplete(CompletionPart.Members))
            {
                return GetMembers(name);
            }

            ImmutableArray<Symbol> nonTypeMembersToCheck;
            SynthesizedPrimaryConstructor? primaryConstructor;

            var declared = Volatile.Read(ref _lazyDeclaredMembersAndInitializers);
            if (declared is not null && declared != DeclaredMembersAndInitializers.UninitializedSentinel)
            {
                nonTypeMembersToCheck = declared.GetNonTypeMembers(this);
                primaryConstructor = declared.PrimaryConstructor;
            }
            else
            {
                var membersAndInitializers = GetMembersAndInitializers();
                nonTypeMembersToCheck = membersAndInitializers.NonTypeMembers;
                primaryConstructor = membersAndInitializers.PrimaryConstructor;
            }

            Debug.Assert(primaryConstructor is not null);

            if (primaryConstructor.ParameterCount == 0)
            {
                return GetMembers(name);
            }

            ImmutableArray<Symbol> types = GetTypeMembers(name).Cast<NamedTypeSymbol, Symbol>();
            ArrayBuilder<Symbol>? memberBuilder = null;

            foreach (var member in nonTypeMembersToCheck)
            {
                if (member.IsAccessor())
                {
                    continue;
                }

                if (member.Name == name)
                {
                    memberBuilder ??= ArrayBuilder<Symbol>.GetInstance(types.Length + 1);
                    memberBuilder.Add(member);
                }
            }

            if (memberBuilder is null)
            {
                return types;
            }

            memberBuilder.AddRange(types);
            return memberBuilder.ToImmutableAndFree();
        }

        private void AddSynthesizedMembers(MembersAndInitializersBuilder builder, DeclaredMembersAndInitializers declaredMembersAndInitializers, BindingDiagnosticBag diagnostics)
        {
            if (TypeKind is TypeKind.Class)
            {
                AddSynthesizedSimpleProgramEntryPointIfNecessary(builder, declaredMembersAndInitializers);
            }

            switch (TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Enum:
                case TypeKind.Class:
                case TypeKind.Interface:
                case TypeKind.Submission:
                    AddSynthesizedTypeMembersIfNecessary(builder, declaredMembersAndInitializers, diagnostics);
                    AddSynthesizedConstructorsIfNecessary(builder, declaredMembersAndInitializers, diagnostics);

                    if (TypeKind == TypeKind.Class) // Tracked by https://github.com/dotnet/roslyn/issues/78827 : MQ, Consider tightening this check to only top-level non-generic static classes, however optimizing for error scenarios is usually not a goal.
                    {
                        AddSynthesizedExtensionImplementationsIfNecessary(builder, declaredMembersAndInitializers);
                    }
                    break;

                default:
                    break;
            }

            AddSynthesizedTupleMembersIfNecessary(builder, declaredMembersAndInitializers);
        }

        private void AddSynthesizedExtensionImplementationsIfNecessary(MembersAndInitializersBuilder builder, DeclaredMembersAndInitializers declaredMembersAndInitializers)
        {
            foreach (var type in GetTypeMembers(""))
            {
                if (type.TypeKind == TypeKind.Extension)
                {
                    foreach (var member in type.GetMembers())
                    {
                        if (member is MethodSymbol { IsImplicitlyDeclared: false, MethodKind: not (MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.Destructor or MethodKind.ExplicitInterfaceImplementation) } method &&
                            (method.IsStatic || type.ExtensionParameter is not null))
                        {
                            builder.AddNonTypeMember(this, new SourceExtensionImplementationMethodSymbol(method), declaredMembersAndInitializers);
                        }
                    }
                }
            }
        }

        private void AddDeclaredNontypeMembers(DeclaredMembersAndInitializersBuilder builder, BindingDiagnosticBag diagnostics)
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
                        SourceDelegateMethodSymbol.AddDelegateMembers(this, builder.NonTypeMembersWithPartialImplementations, (DelegateDeclarationSyntax)syntax, diagnostics);
                        break;

                    case SyntaxKind.NamespaceDeclaration:
                    case SyntaxKind.FileScopedNamespaceDeclaration:
                        // The members of a global anonymous type is in a syntax tree of a namespace declaration or a compilation unit.
                        AddNonTypeMembers(builder, ((BaseNamespaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.CompilationUnit:
                        AddNonTypeMembers(builder, ((CompilationUnitSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.InterfaceDeclaration:
                        AddNonTypeMembers(builder, ((InterfaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                    case SyntaxKind.ExtensionBlockDeclaration:
                        var typeDecl = (TypeDeclarationSyntax)syntax;
                        noteTypeParameters(typeDecl, builder, diagnostics);
                        AddNonTypeMembers(builder, typeDecl.Members, diagnostics);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                }
            }

            void noteTypeParameters(TypeDeclarationSyntax syntax, DeclaredMembersAndInitializersBuilder builder, BindingDiagnosticBag diagnostics)
            {
                var parameterList = syntax.ParameterList;

                if (parameterList is null)
                {
                    return;
                }

                if (!this.IsExtension)
                {
                    if (builder.DeclarationWithParameters is null)
                    {
                        builder.DeclarationWithParameters = syntax;
                        var ctor = new SynthesizedPrimaryConstructor(this, syntax);

                        if (this.IsStatic)
                        {
                            diagnostics.Add(ErrorCode.ERR_ConstructorInStaticClass, syntax.Identifier.GetLocation());
                        }

                        builder.PrimaryConstructor = ctor;

                        var compilation = DeclaringCompilation;
                        builder.UpdateIsNullableEnabledForConstructorsAndFields(ctor.IsStatic, compilation, parameterList);
                        if (syntax is { PrimaryConstructorBaseTypeIfClass: { ArgumentList: { } baseParamList } })
                        {
                            builder.UpdateIsNullableEnabledForConstructorsAndFields(ctor.IsStatic, compilation, baseParamList);
                        }
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_MultipleRecordParameterLists, parameterList.Location);
                    }
                }
            }
        }

        internal Binder GetBinder(CSharpSyntaxNode syntaxNode)
        {
            return this.DeclaringCompilation.GetBinder(syntaxNode);
        }

        private static void MergePartialMembers(
            Dictionary<ReadOnlyMemory<char>, object> membersByName,
            ArrayBuilder<Symbol> nonTypeMembers,
            BindingDiagnosticBag diagnostics)
        {
            //key and value will be the same object
            var membersBySignature = new Dictionary<Symbol, Symbol>(MemberSignatureComparer.PartialMethodsComparer);

            foreach (var pair in membersByName)
            {
                membersBySignature.Clear();

                if (pair.Value is ArrayBuilder<Symbol> arrayBuilder)
                {
                    foreach (var symbol in arrayBuilder)
                    {
                        Debug.Assert(symbol.IsPartialMember());

                        // Accessor symbols and their diagnostics are handled by processing the associated member.
                        // We cannot add them to the map (signature comparison on partial event accessors can lead to cycles through IsWindowsRuntimeEvent).
                        if (symbol is SourcePropertyAccessorSymbol or SourceEventAccessorSymbol)
                        {
                            continue;
                        }

                        if (!membersBySignature.TryGetValue(symbol, out var prev))
                        {
                            membersBySignature.Add(symbol, symbol);
                            continue;
                        }

                        switch (symbol, prev)
                        {
                            case (SourceOrdinaryMethodSymbol currentMethod, SourceOrdinaryMethodSymbol prevMethod):
                                mergePartialMethods(nonTypeMembers, currentMethod, prevMethod, diagnostics);
                                break;

                            case (SourcePropertySymbol currentProperty, SourcePropertySymbol prevProperty):
                                mergePartialProperties(nonTypeMembers, currentProperty, prevProperty, diagnostics);
                                break;

                            case (SourceConstructorSymbol { IsStatic: false } currentConstructor, SourceConstructorSymbol { IsStatic: false } prevConstructor):
                                Debug.Assert(pair.Key.Equals(WellKnownMemberNames.InstanceConstructorName.AsMemory()));
                                mergePartialConstructors(nonTypeMembers, currentConstructor, prevConstructor, diagnostics);
                                break;

                            case (SourceEventSymbol currentEvent, SourceEventSymbol prevEvent):
                                mergePartialEvents(nonTypeMembers, currentEvent, prevEvent, diagnostics);
                                break;

                            default:
                                // This is an error scenario. We simply don't merge the symbols in this case and a duplicate name diagnostic is reported separately.
                                // One way this case can be reached is if type contains both `public partial int P { get; }` and `public partial int get_P();`.
                                Debug.Assert(symbol.Kind != prev.Kind);
                                Debug.Assert(symbol is SourceOrdinaryMethodSymbol or SourcePropertySymbol or SourcePropertyAccessorSymbol or SourceEventAccessorSymbol);
                                Debug.Assert(prev is SourceOrdinaryMethodSymbol or SourcePropertySymbol or SourcePropertyAccessorSymbol or SourceEventAccessorSymbol);
                                break;
                        }
                    }
                }
                else
                {
                    var symbol = (Symbol)pair.Value;
                    Debug.Assert(symbol.IsPartialMember());

                    if (symbol is SourcePropertyAccessorSymbol or SourceEventAccessorSymbol)
                    {
                        continue;
                    }

                    membersBySignature.Add(symbol, symbol);
                }

                foreach (var symbol in membersBySignature.Values)
                {
                    switch (symbol)
                    {
                        case SourceOrdinaryMethodSymbol method:
                            // partial implementations not paired with a definition
                            if (method.IsPartialImplementation && method.OtherPartOfPartial is null)
                            {
                                diagnostics.Add(ErrorCode.ERR_PartialMethodMustHaveLatent, method.GetFirstLocation(), method);
                            }
                            else if (method is { IsPartialDefinition: true, OtherPartOfPartial: null, HasExplicitAccessModifier: true })
                            {
                                diagnostics.Add(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, method.GetFirstLocation(), method);
                            }
                            break;

                        case SourcePropertySymbol property:
                            if (property.OtherPartOfPartial is null)
                            {
                                diagnostics.Add(
                                    property.IsPartialDefinition ? ErrorCode.ERR_PartialPropertyMissingImplementation : ErrorCode.ERR_PartialPropertyMissingDefinition,
                                    property.GetFirstLocation(),
                                    property);
                            }
                            break;

                        case SourceConstructorSymbol constructor:
                            if (constructor.OtherPartOfPartial is null)
                            {
                                diagnostics.Add(
                                    constructor.IsPartialDefinition ? ErrorCode.ERR_PartialMemberMissingImplementation : ErrorCode.ERR_PartialMemberMissingDefinition,
                                    constructor.GetFirstLocation(),
                                    constructor);
                            }
                            break;

                        case SourceEventSymbol ev:
                            if (ev.OtherPartOfPartial is null)
                            {
                                diagnostics.Add(
                                    ev.IsPartialDefinition ? ErrorCode.ERR_PartialMemberMissingImplementation : ErrorCode.ERR_PartialMemberMissingDefinition,
                                    ev.GetFirstLocation(),
                                    ev);
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(symbol);
                    }
                }
            }

            foreach (var pair in membersByName)
            {
                if (pair.Value is ArrayBuilder<Symbol> arrayBuilder)
                {
                    foreach (var symbol in arrayBuilder)
                    {
                        fixupNotMergedPartialProperty(symbol);
                    }
                }
                else
                {
                    fixupNotMergedPartialProperty((Symbol)pair.Value);
                }
            }

            static void fixupNotMergedPartialProperty(Symbol symbol)
            {
                Debug.Assert(symbol.IsPartialMember());
                if (symbol is SourcePropertySymbol { OtherPartOfPartial: null } property)
                {
                    property.SetMergedBackingField(property.DeclaredBackingField);
                }
            }

            static void mergePartialMethods(ArrayBuilder<Symbol> nonTypeMembers, SourceOrdinaryMethodSymbol currentMethod, SourceOrdinaryMethodSymbol prevMethod, BindingDiagnosticBag diagnostics)
            {
                if (currentMethod.IsPartialImplementation &&
                    (prevMethod.IsPartialImplementation || (prevMethod.OtherPartOfPartial is MethodSymbol otherImplementation && (object)otherImplementation != currentMethod)))
                {
                    // A partial method may not have multiple implementing declarations
                    diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyOneActual, currentMethod.GetFirstLocation());
                }
                else if (currentMethod.IsPartialDefinition &&
                    (prevMethod.IsPartialDefinition || (prevMethod.OtherPartOfPartial is MethodSymbol otherDefinition && (object)otherDefinition != currentMethod)))
                {
                    // A partial method may not have multiple defining declarations
                    diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyOneLatent, currentMethod.GetFirstLocation());
                }
                else
                {
                    FixPartialMethod(nonTypeMembers, prevMethod, currentMethod);
                }
            }

            static void mergePartialProperties(ArrayBuilder<Symbol> nonTypeMembers, SourcePropertySymbol currentProperty, SourcePropertySymbol prevProperty, BindingDiagnosticBag diagnostics)
            {
                if (currentProperty.IsPartialImplementation &&
                    (prevProperty.IsPartialImplementation || (prevProperty.OtherPartOfPartial is SourcePropertySymbol otherImplementation && (object)otherImplementation != currentProperty)))
                {
                    diagnostics.Add(ErrorCode.ERR_PartialPropertyDuplicateImplementation, currentProperty.GetFirstLocation());
                }
                else if (currentProperty.IsPartialDefinition &&
                    (prevProperty.IsPartialDefinition || (prevProperty.OtherPartOfPartial is SourcePropertySymbol otherDefinition && (object)otherDefinition != currentProperty)))
                {
                    diagnostics.Add(ErrorCode.ERR_PartialPropertyDuplicateDefinition, currentProperty.GetFirstLocation());
                }
                else
                {
                    if (hasInitializer(prevProperty) && hasInitializer(currentProperty))
                    {
                        diagnostics.Add(ErrorCode.ERR_PartialPropertyDuplicateInitializer, currentProperty.GetFirstLocation());
                    }

                    mergeAccessors(nonTypeMembers, (SourcePropertyAccessorSymbol?)currentProperty.GetMethod, (SourcePropertyAccessorSymbol?)prevProperty.GetMethod);
                    mergeAccessors(nonTypeMembers, (SourcePropertyAccessorSymbol?)currentProperty.SetMethod, (SourcePropertyAccessorSymbol?)prevProperty.SetMethod);
                    FixPartialProperty(nonTypeMembers, prevProperty, currentProperty);
                }

                void mergeAccessors(ArrayBuilder<Symbol> nonTypeMembers, SourcePropertyAccessorSymbol? currentAccessor, SourcePropertyAccessorSymbol? prevAccessor)
                {
                    if (currentAccessor is { } && prevAccessor is { })
                    {
                        var implementationAccessor = currentProperty.IsPartialDefinition ? prevAccessor : currentAccessor;
                        Remove(nonTypeMembers, implementationAccessor);
                    }
                    else if (currentAccessor is { } || prevAccessor is { })
                    {
                        var (foundAccessor, containingProperty, otherProperty) = prevAccessor is { } ? (prevAccessor, prevProperty, currentProperty) : (currentAccessor!, currentProperty, prevProperty);
                        // When an accessor is present on definition but not on implementation, the accessor is said to be missing on the implementation.
                        // When an accessor is present on implementation but not on definition, the accessor is said to be unexpected on the implementation.
                        var (errorCode, propertyToBlame) = foundAccessor.IsPartialDefinition
                            ? (ErrorCode.ERR_PartialPropertyMissingAccessor, otherProperty)
                            : (ErrorCode.ERR_PartialPropertyUnexpectedAccessor, containingProperty);
                        diagnostics.Add(errorCode, propertyToBlame.GetFirstLocation(), foundAccessor);
                    }
                }

                static bool hasInitializer(SourcePropertySymbol property)
                {
                    return property.DeclaredBackingField?.HasInitializer == true;
                }
            }

            static void mergePartialConstructors(ArrayBuilder<Symbol> nonTypeMembers, SourceConstructorSymbol currentConstructor, SourceConstructorSymbol prevConstructor, BindingDiagnosticBag diagnostics)
            {
                if (currentConstructor.IsPartialImplementation &&
                    (prevConstructor.IsPartialImplementation || (prevConstructor.OtherPartOfPartial is { } otherImplementation && !ReferenceEquals(otherImplementation, currentConstructor))))
                {
                    // A partial constructor may not have multiple implementing declarations
                    diagnostics.Add(ErrorCode.ERR_PartialMemberDuplicateImplementation, currentConstructor.GetFirstLocation(), currentConstructor);
                }
                else if (currentConstructor.IsPartialDefinition &&
                    (prevConstructor.IsPartialDefinition || (prevConstructor.OtherPartOfPartial is { } otherDefinition && !ReferenceEquals(otherDefinition, currentConstructor))))
                {
                    // A partial constructor may not have multiple defining declarations
                    diagnostics.Add(ErrorCode.ERR_PartialMemberDuplicateDefinition, currentConstructor.GetFirstLocation(), currentConstructor);
                }
                else
                {
                    FixPartialConstructor(nonTypeMembers, prevConstructor, currentConstructor);
                }
            }

            static void mergePartialEvents(ArrayBuilder<Symbol> nonTypeMembers, SourceEventSymbol currentEvent, SourceEventSymbol prevEvent, BindingDiagnosticBag diagnostics)
            {
                if (currentEvent.IsPartialImplementation &&
                    (prevEvent.IsPartialImplementation || (prevEvent.OtherPartOfPartial is { } otherImplementation && !ReferenceEquals(otherImplementation, currentEvent))))
                {
                    // A partial event may not have multiple implementing declarations
                    diagnostics.Add(ErrorCode.ERR_PartialMemberDuplicateImplementation, currentEvent.GetFirstLocation(), currentEvent);
                }
                else if (currentEvent.IsPartialDefinition &&
                    (prevEvent.IsPartialDefinition || (prevEvent.OtherPartOfPartial is { } otherDefinition && !ReferenceEquals(otherDefinition, currentEvent))))
                {
                    // A partial event may not have multiple defining declarations
                    diagnostics.Add(ErrorCode.ERR_PartialMemberDuplicateDefinition, currentEvent.GetFirstLocation(), currentEvent);
                }
                else
                {
                    mergeAccessors(nonTypeMembers, (SourceEventAccessorSymbol?)currentEvent.AddMethod, (SourceEventAccessorSymbol?)prevEvent.AddMethod);
                    mergeAccessors(nonTypeMembers, (SourceEventAccessorSymbol?)currentEvent.RemoveMethod, (SourceEventAccessorSymbol?)prevEvent.RemoveMethod);
                    FixPartialEvent(nonTypeMembers, prevEvent, currentEvent);
                }

                static void mergeAccessors(ArrayBuilder<Symbol> nonTypeMembers, SourceEventAccessorSymbol? currentAccessor, SourceEventAccessorSymbol? prevAccessor)
                {
                    if (currentAccessor?.IsPartialImplementation == true)
                    {
                        Remove(nonTypeMembers, currentAccessor);
                    }
                    else if (prevAccessor?.IsPartialImplementation == true)
                    {
                        Remove(nonTypeMembers, prevAccessor);
                    }
                }
            }
        }

        /// <summary>Links together the definition and implementation parts of a partial method. Removes implementation part from <paramref name="nonTypeMembers"/>.</summary>
        private static void FixPartialMethod(ArrayBuilder<Symbol> nonTypeMembers, SourceOrdinaryMethodSymbol part1, SourceOrdinaryMethodSymbol part2)
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
            Remove(nonTypeMembers, implementation);
        }

        /// <summary>Links together the definition and implementation parts of a partial property. Removes implementation part from <paramref name="nonTypeMembers"/></summary>
        private static void FixPartialProperty(ArrayBuilder<Symbol> nonTypeMembers, SourcePropertySymbol part1, SourcePropertySymbol part2)
        {
            SourcePropertySymbol definition;
            SourcePropertySymbol implementation;
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

            if (implementation.DeclaredBackingField is { } implementationField &&
                definition.DeclaredBackingField is { })
            {
                Remove(nonTypeMembers, implementationField);
            }

            SourcePropertySymbol.InitializePartialPropertyParts(definition, implementation);

            // a partial property is represented in the member list by its definition part:
            Remove(nonTypeMembers, implementation);
        }

        /// <summary>Links together the definition and implementation parts of a partial constructor. Removes implementation part from <paramref name="nonTypeMembers"/>.</summary>
        private static void FixPartialConstructor(ArrayBuilder<Symbol> nonTypeMembers, SourceConstructorSymbol part1, SourceConstructorSymbol part2)
        {
            SourceConstructorSymbol definition;
            SourceConstructorSymbol implementation;
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

            SourceConstructorSymbol.InitializePartialConstructorParts(definition, implementation);

            // a partial constructor is represented in the member list by its definition part:
            Remove(nonTypeMembers, implementation);
        }

        /// <summary>Links together the definition and implementation parts of a partial event. Removes implementation part from <paramref name="nonTypeMembers"/>.</summary>
        private static void FixPartialEvent(ArrayBuilder<Symbol> nonTypeMembers, SourceEventSymbol part1, SourceEventSymbol part2)
        {
            SourceEventSymbol definition;
            SourceEventSymbol implementation;
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

            SourceEventSymbol.InitializePartialEventParts(definition, implementation);

            // a partial event is represented in the member list by its definition part:
            Remove(nonTypeMembers, implementation);
        }

        private static void Remove(ArrayBuilder<Symbol> symbols, Symbol symbol)
        {
            for (int i = 0; i < symbols.Count; i++)
            {
                Symbol s = symbols[i];
                if (ReferenceEquals(s, symbol))
                {
                    symbols.RemoveAt(i);
                    return;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// Report an error if a member (other than a method) exists with the same name
        /// as the property accessor, or if a method exists with the same name and signature.
        /// </summary>
        private static void CheckForMemberConflictWithPropertyAccessor(
            SourceMemberContainerTypeSymbol containerForDiagnostics,
            bool mightHaveMembersFromDistinctNonPartialDeclarations,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            PropertySymbol propertySymbol,
            bool getNotSet,
            BindingDiagnosticBag diagnostics)
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

            foreach (var symbol in membersByName.TryGetValue(accessorName.AsMemory(), out var members) ? members : [])
            {
                if (symbol.Kind != SymbolKind.Method)
                {
                    // The type '{0}' already contains a definition for '{1}'
                    if (!mightHaveMembersFromDistinctNonPartialDeclarations)
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, GetAccessorOrPropertyLocation(propertySymbol, getNotSet), containerForDiagnostics, accessorName);
                    return;
                }
                else
                {
                    var methodSymbol = (MethodSymbol)symbol;
                    if ((methodSymbol.MethodKind == MethodKind.Ordinary) &&
                        ParametersMatchPropertyAccessor(propertySymbol, getNotSet, methodSymbol.Parameters))
                    {
                        // Type '{1}' already reserves a member called '{0}' with the same parameter types
                        diagnostics.Add(ErrorCode.ERR_MemberReserved, GetAccessorOrPropertyLocation(propertySymbol, getNotSet), accessorName, containerForDiagnostics);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Report an error if a member (other than a method) exists with the same name
        /// as the event accessor, or if a method exists with the same name and signature.
        /// </summary>
        private static void CheckForMemberConflictWithEventAccessor(
            SourceMemberContainerTypeSymbol containerForDiagnostics,
            bool mightHaveMembersFromDistinctNonPartialDeclarations,
            Dictionary<ReadOnlyMemory<char>, ImmutableArray<Symbol>> membersByName,
            EventSymbol eventSymbol,
            bool isAdder,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!eventSymbol.IsExplicitInterfaceImplementation); // checked by caller

            string accessorName = SourceEventSymbol.GetAccessorName(eventSymbol.Name, isAdder);

            foreach (var symbol in membersByName.TryGetValue(accessorName.AsMemory(), out var members) ? members : [])
            {
                if (symbol.Kind != SymbolKind.Method)
                {
                    // The type '{0}' already contains a definition for '{1}'
                    if (!mightHaveMembersFromDistinctNonPartialDeclarations)
                        diagnostics.Add(ErrorCode.ERR_DuplicateNameInClass, GetAccessorOrEventLocation(eventSymbol, isAdder), containerForDiagnostics, accessorName);
                    return;
                }
                else
                {
                    var methodSymbol = (MethodSymbol)symbol;
                    if ((methodSymbol.MethodKind == MethodKind.Ordinary) &&
                        ParametersMatchEventAccessor(eventSymbol, methodSymbol.Parameters))
                    {
                        // Type '{1}' already reserves a member called '{0}' with the same parameter types
                        diagnostics.Add(ErrorCode.ERR_MemberReserved, GetAccessorOrEventLocation(eventSymbol, isAdder), accessorName, containerForDiagnostics);
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
            return locationFrom.GetFirstLocation();
        }

        /// <summary>
        /// Return the location of the accessor, or if no accessor, the location of the event.
        /// </summary>
        private static Location GetAccessorOrEventLocation(EventSymbol propertySymbol, bool isAdder)
        {
            var locationFrom = (Symbol?)(isAdder ? propertySymbol.AddMethod : propertySymbol.RemoveMethod) ?? propertySymbol;
            return locationFrom.GetFirstLocation();
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

        private void AddEnumMembers(DeclaredMembersAndInitializersBuilder result, EnumDeclarationSyntax syntax, BindingDiagnosticBag diagnostics)
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

                result.NonTypeMembersWithPartialImplementations.Add(symbol);

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

        private static void AddInitializer(ref ArrayBuilder<FieldOrPropertyInitializer>? initializers, FieldSymbol? fieldOpt, CSharpSyntaxNode node)
        {
            if (initializers == null)
            {
                initializers = ArrayBuilder<FieldOrPropertyInitializer>.GetInstance();
            }
            else if (initializers.Count != 0)
            {
                // initializers should be added in syntax order:
                Debug.Assert(node.SyntaxTree == initializers.Last().Syntax.SyntaxTree);
                Debug.Assert(node.SpanStart > initializers.Last().Syntax.Span.Start);
            }

            initializers.Add(new FieldOrPropertyInitializer(fieldOpt, node));
        }

        private static void AddInitializers(
            ArrayBuilder<ArrayBuilder<FieldOrPropertyInitializer>> allInitializers,
            ArrayBuilder<FieldOrPropertyInitializer>? siblingsOpt)
        {
            if (siblingsOpt != null)
            {
                allInitializers.Add(siblingsOpt);
            }
        }

        private static void CheckInterfaceMembers(ImmutableArray<Symbol> nonTypeMembers, BindingDiagnosticBag diagnostics)
        {
            foreach (var member in nonTypeMembers)
            {
                CheckInterfaceMember(member, diagnostics);
            }
        }

        private static void CheckInterfaceMember(Symbol member, BindingDiagnosticBag diagnostics)
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
                            diagnostics.Add(ErrorCode.ERR_InterfacesCantContainConstructors, member.GetFirstLocation());
                            break;
                        case MethodKind.Conversion:
                            break;
                        case MethodKind.UserDefinedOperator:
                            break;
                        case MethodKind.Destructor:
                            diagnostics.Add(ErrorCode.ERR_OnlyClassesCanContainDestructors, member.GetFirstLocation());
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

        private static void CheckExtensionMembers(ImmutableArray<Symbol> members, BindingDiagnosticBag diagnostics)
        {
            foreach (var member in members)
            {
                checkExtensionMember(member, diagnostics);
            }

            return;

            static void checkExtensionMember(Symbol member, BindingDiagnosticBag diagnostics)
            {
                if (!IsAllowedExtensionMember(member))
                {
                    diagnostics.Add(ErrorCode.ERR_ExtensionDisallowsMember, member.GetFirstLocation());
                }
            }
        }

        internal static bool IsAllowedExtensionMember(Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    var meth = (MethodSymbol)member;
                    switch (meth.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.Conversion:
                        case MethodKind.Destructor:
                        case MethodKind.EventAdd:
                        case MethodKind.EventRemove:
                        case MethodKind.StaticConstructor:
                        case MethodKind.ExplicitInterfaceImplementation:
                            break;
                        case MethodKind.Ordinary:
                        case MethodKind.UserDefinedOperator:
                        case MethodKind.PropertyGet:
                        case MethodKind.PropertySet:
                            return true;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(meth.MethodKind);
                    }
                    break;

                case SymbolKind.Property:
                    if (!((PropertySymbol)member).IsIndexer)
                    {
                        return true;
                    }
                    break;

                case SymbolKind.Field:
                case SymbolKind.Event:
                case SymbolKind.NamedType:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }

            return false;
        }

        private static void CheckForStructDefaultConstructors(
            ArrayBuilder<Symbol> members,
            bool isEnum,
            BindingDiagnosticBag diagnostics)
        {
            foreach (var s in members)
            {
                var m = s as MethodSymbol;
                if (!(m is null))
                {
                    if (m.MethodKind == MethodKind.Constructor && m.ParameterCount == 0)
                    {
                        var location = m.GetFirstLocation();
                        if (isEnum)
                        {
                            diagnostics.Add(ErrorCode.ERR_EnumsCantContainDefaultConstructor, location);
                        }
                        else
                        {
                            MessageID.IDS_FeatureParameterlessStructConstructors.CheckFeatureAvailability(diagnostics, m.DeclaringCompilation, location);
                            if (m.DeclaredAccessibility != Accessibility.Public)
                            {
                                diagnostics.Add(ErrorCode.ERR_NonPublicParameterlessStructConstructor, location);
                            }
                        }
                    }
                }
            }
        }

        private void CheckForStructBadInitializers(DeclaredMembersAndInitializersBuilder builder, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(TypeKind == TypeKind.Struct);

            if (builder.DeclarationWithParameters is not null)
            {
                Debug.Assert(builder.DeclarationWithParameters is TypeDeclarationSyntax { ParameterList: not null } type
                    && type.Kind() is (SyntaxKind.RecordStructDeclaration or SyntaxKind.StructDeclaration));
                return;
            }

            bool hasInitializers = false;
            foreach (var initializers in builder.InstanceInitializers)
            {
                foreach (FieldOrPropertyInitializer initializer in initializers)
                {
                    hasInitializers = true;
                    var symbol = initializer.FieldOpt.AssociatedSymbol ?? initializer.FieldOpt;
                    MessageID.IDS_FeatureStructFieldInitializers.CheckFeatureAvailability(diagnostics, symbol.DeclaringCompilation, symbol.GetFirstLocation());
                }
            }

            if (hasInitializers && !builder.NonTypeMembersWithPartialImplementations.Any(member => member is MethodSymbol { MethodKind: MethodKind.Constructor }))
            {
                diagnostics.Add(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, GetFirstLocation());
            }
        }

        private void AddSynthesizedSimpleProgramEntryPointIfNecessary(MembersAndInitializersBuilder builder, DeclaredMembersAndInitializers declaredMembersAndInitializers)
        {
            var simpleProgramEntryPoints = GetSimpleProgramEntryPoints();
            foreach (var member in simpleProgramEntryPoints)
            {
                builder.AddNonTypeMember(this, member, declaredMembersAndInitializers);
            }
        }

        private void AddSynthesizedTypeMembersIfNecessary(MembersAndInitializersBuilder builder, DeclaredMembersAndInitializers declaredMembersAndInitializers, BindingDiagnosticBag diagnostics)
        {
            if (declaration.Kind is not (DeclarationKind.Record or DeclarationKind.RecordStruct) && declaredMembersAndInitializers.PrimaryConstructor is null)
            {
                return;
            }

            var membersSoFar = builder.GetNonTypeMembers(this, declaredMembersAndInitializers);
            var members = ArrayBuilder<Symbol>.GetInstance(membersSoFar.Count + 1);

            if (declaration.Kind is not (DeclarationKind.Record or DeclarationKind.RecordStruct))
            {
                // primary ctor
                var ctor = declaredMembersAndInitializers.PrimaryConstructor;
                Debug.Assert(ctor is object);

                members.Add(ctor);
                members.AddRange(ctor.GetBackingFields());
                members.AddRange(membersSoFar);
                builder.SetNonTypeMembers(members);

                return;
            }

            Debug.Assert(declaredMembersAndInitializers.PrimaryConstructor?.GetBackingFields().Any() != true);

            ParameterListSyntax? paramList = declaredMembersAndInitializers.DeclarationWithParameters?.ParameterList;
            var memberSignatures = s_duplicateRecordMemberSignatureDictionary.Allocate();
            var fieldsByName = PooledDictionary<string, Symbol>.GetInstance();
            var memberNames = PooledHashSet<string>.GetInstance();
            foreach (var member in membersSoFar)
            {
                memberNames.Add(member.Name);

                switch (member)
                {
                    case EventSymbol:
                    case MethodSymbol { MethodKind: not (MethodKind.Ordinary or MethodKind.Constructor) }:
                        continue;
                    case FieldSymbol { Name: var fieldName }:
                        if (!fieldsByName.ContainsKey(fieldName))
                        {
                            fieldsByName.Add(fieldName, member);
                        }
                        continue;
                }

                if (!memberSignatures.ContainsKey(member))
                {
                    memberSignatures.Add(member, member);
                }
            }

            CSharpCompilation compilation = this.DeclaringCompilation;
            bool isRecordClass = declaration.Kind == DeclarationKind.Record;

            // Positional record
            bool primaryAndCopyCtorAmbiguity = false;
            if (!(paramList is null))
            {
                Debug.Assert(declaredMembersAndInitializers.DeclarationWithParameters is object);

                // primary ctor
                var ctor = declaredMembersAndInitializers.PrimaryConstructor;
                Debug.Assert(ctor is object);
                members.Add(ctor);

                if (!memberSignatures.ContainsKey(ctor))
                {
                    memberSignatures.Add(ctor, ctor);
                }

                if (ctor.ParameterCount != 0)
                {
                    // properties and Deconstruct
                    var existingOrAddedMembers = addProperties(ctor.Parameters);
                    addDeconstruct(ctor, existingOrAddedMembers);
                }

                if (isRecordClass)
                {
                    primaryAndCopyCtorAmbiguity = ctor.ParameterCount == 1 && ctor.Parameters[0].Type.Equals(this, TypeCompareKind.AllIgnoreOptions);
                }
            }

            if (isRecordClass)
            {
                addCopyCtor(primaryAndCopyCtorAmbiguity, declaredMembersAndInitializers);
                addCloneMethod();
            }

            PropertySymbol? equalityContract = isRecordClass ? addEqualityContract() : null;

            var thisEquals = addThisEquals(equalityContract);

            if (isRecordClass)
            {
                addBaseEquals();
            }

            addObjectEquals(thisEquals);
            var getHashCode = addGetHashCode(equalityContract);
            addEqualityOperators();

            if (thisEquals is not SynthesizedRecordEquals && getHashCode is SynthesizedRecordGetHashCode)
            {
                diagnostics.Add(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, thisEquals.GetFirstLocation(), declaration.Name);
            }

            var printMembers = addPrintMembersMethod(membersSoFar);
            addToStringMethod(printMembers);

            memberSignatures.Free();
            fieldsByName.Free();
            memberNames.Free();

            // Synthesizing non-readonly properties in struct would require changing readonly logic for PrintMembers method synthesis
            Debug.Assert(isRecordClass || !members.Any(m => m is PropertySymbol { GetMethod.IsEffectivelyReadOnly: false }));

            // We put synthesized record members first so that errors about conflicts show up on user-defined members rather than all
            // going to the record declaration
            members.AddRange(membersSoFar);
            builder.SetNonTypeMembers(members);

            return;

            void addDeconstruct(SynthesizedPrimaryConstructor ctor, ImmutableArray<Symbol> positionalMembers)
            {
                Debug.Assert(positionalMembers.All(p => p is PropertySymbol or FieldSymbol));

                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.DeconstructMethodName,
                    this,
                    MethodKind.Ordinary,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ctor.Parameters.SelectAsArray<ParameterSymbol, ParameterSymbol>(param => new SignatureOnlyParameterSymbol(param.TypeWithAnnotations,
                                                                                                                              ImmutableArray<CustomModifier>.Empty,
                                                                                                                              isParamsArray: false,
                                                                                                                              isParamsCollection: false,
                                                                                                                              RefKind.Out
                                                                                                                              )),
                    RefKind.None,
                    isInitOnly: false,
                    isStatic: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Void)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingDeconstructMethod))
                {
                    members.Add(new SynthesizedRecordDeconstruct(this, ctor, positionalMembers, memberOffset: members.Count));
                }
                else
                {
                    var deconstruct = (MethodSymbol)existingDeconstructMethod;

                    if (deconstruct.DeclaredAccessibility != Accessibility.Public)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonPublicAPIInRecord, deconstruct.GetFirstLocation(), deconstruct);
                    }

                    if (deconstruct.ReturnType.SpecialType != SpecialType.System_Void && !deconstruct.ReturnType.IsErrorType())
                    {
                        diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, deconstruct.GetFirstLocation(), deconstruct, targetMethod.ReturnType);
                    }

                    if (deconstruct.IsStatic)
                    {
                        diagnostics.Add(ErrorCode.ERR_StaticAPIInRecord, deconstruct.GetFirstLocation(), deconstruct);
                    }
                }
            }

            void addCopyCtor(bool primaryAndCopyCtorAmbiguity, DeclaredMembersAndInitializers declaredMembersAndInitializers)
            {
                Debug.Assert(isRecordClass);
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.InstanceConstructorName,
                    this,
                    MethodKind.Constructor,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ImmutableArray.Create<ParameterSymbol>(new SignatureOnlyParameterSymbol(
                                                                TypeWithAnnotations.Create(this),
                                                                ImmutableArray<CustomModifier>.Empty,
                                                                isParamsArray: false,
                                                                isParamsCollection: false,
                                                                RefKind.None
                                                                )),
                    RefKind.None,
                    isInitOnly: false,
                    isStatic: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Void)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingConstructor))
                {
                    var copyCtor = new SynthesizedRecordCopyCtor(this, memberOffset: members.Count);
                    members.Add(copyCtor);

                    if (primaryAndCopyCtorAmbiguity)
                    {
                        diagnostics.Add(ErrorCode.ERR_RecordAmbigCtor, copyCtor.GetFirstLocation());
                    }
                }
                else
                {
                    var constructor = (MethodSymbol)existingConstructor;

                    if ((object)constructor == declaredMembersAndInitializers.PrimaryConstructor && primaryAndCopyCtorAmbiguity)
                    {
                        diagnostics.Add(ErrorCode.ERR_RecordAmbigCtor, this.GetFirstLocation());
                    }
                    else if (!this.IsSealed && (constructor.DeclaredAccessibility != Accessibility.Public && constructor.DeclaredAccessibility != Accessibility.Protected))
                    {
                        diagnostics.Add(ErrorCode.ERR_CopyConstructorWrongAccessibility, constructor.GetFirstLocation(), constructor);
                    }
                }
            }

            void addCloneMethod()
            {
                Debug.Assert(isRecordClass);
                members.Add(new SynthesizedRecordClone(this, memberOffset: members.Count));
            }

            MethodSymbol addPrintMembersMethod(IEnumerable<Symbol> userDefinedMembers)
            {
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.PrintMembersMethodName,
                    this,
                    MethodKind.Ordinary,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ImmutableArray.Create<ParameterSymbol>(new SignatureOnlyParameterSymbol(
                        TypeWithAnnotations.Create(compilation.GetWellKnownType(WellKnownType.System_Text_StringBuilder)),
                        ImmutableArray<CustomModifier>.Empty,
                        isParamsArray: false,
                        isParamsCollection: false,
                        RefKind.None)),
                    RefKind.None,
                    isInitOnly: false,
                    isStatic: false,
                    returnType: TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Boolean)),
                    refCustomModifiers: ImmutableArray<CustomModifier>.Empty,
                    explicitInterfaceImplementations: ImmutableArray<MethodSymbol>.Empty);

                MethodSymbol printMembersMethod;
                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingPrintMembersMethod))
                {
                    printMembersMethod = new SynthesizedRecordPrintMembers(this, userDefinedMembers, memberOffset: members.Count);
                    members.Add(printMembersMethod);
                }
                else
                {
                    printMembersMethod = (MethodSymbol)existingPrintMembersMethod;
                    if (!isRecordClass || (this.IsSealed && this.BaseTypeNoUseSiteDiagnostics.IsObjectType()))
                    {
                        if (printMembersMethod.DeclaredAccessibility != Accessibility.Private)
                        {
                            diagnostics.Add(ErrorCode.ERR_NonPrivateAPIInRecord, printMembersMethod.GetFirstLocation(), printMembersMethod);
                        }
                    }
                    else if (printMembersMethod.DeclaredAccessibility != Accessibility.Protected)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonProtectedAPIInRecord, printMembersMethod.GetFirstLocation(), printMembersMethod);
                    }

                    if (!printMembersMethod.ReturnType.Equals(targetMethod.ReturnType, TypeCompareKind.AllIgnoreOptions))
                    {
                        if (!printMembersMethod.ReturnType.IsErrorType())
                        {
                            diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, printMembersMethod.GetFirstLocation(), printMembersMethod, targetMethod.ReturnType);
                        }
                    }
                    else if (isRecordClass)
                    {
                        SynthesizedRecordPrintMembers.VerifyOverridesPrintMembersFromBase(printMembersMethod, diagnostics);
                    }

                    reportStaticOrNotOverridableAPIInRecord(printMembersMethod, diagnostics);
                }

                return printMembersMethod;
            }

            void addToStringMethod(MethodSymbol printMethod)
            {
                var targetMethod = new SignatureOnlyMethodSymbol(
                    WellKnownMemberNames.ObjectToString,
                    this,
                    MethodKind.Ordinary,
                    Cci.CallingConvention.HasThis,
                    ImmutableArray<TypeParameterSymbol>.Empty,
                    ImmutableArray<ParameterSymbol>.Empty,
                    RefKind.None,
                    isInitOnly: false,
                    isStatic: false,
                    returnType: TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_String)),
                    refCustomModifiers: ImmutableArray<CustomModifier>.Empty,
                    explicitInterfaceImplementations: ImmutableArray<MethodSymbol>.Empty);

                var baseToStringMethod = getBaseToStringMethod();

                if (baseToStringMethod is { IsSealed: true })
                {
                    if (baseToStringMethod.ContainingModule != this.ContainingModule && !this.DeclaringCompilation.IsFeatureEnabled(MessageID.IDS_FeatureSealedToStringInRecord))
                    {
                        var languageVersion = ((CSharpParseOptions)this.GetFirstLocation().SourceTree!.Options).LanguageVersion;
                        var requiredVersion = MessageID.IDS_FeatureSealedToStringInRecord.RequiredVersion();
                        diagnostics.Add(
                            ErrorCode.ERR_InheritingFromRecordWithSealedToString,
                            this.GetFirstLocation(),
                            languageVersion.ToDisplayString(),
                            new CSharpRequiredLanguageVersion(requiredVersion));
                    }
                }
                else
                {
                    if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingToStringMethod))
                    {
                        var toStringMethod = new SynthesizedRecordToString(
                            this,
                            printMethod,
                            memberOffset: members.Count);
                        members.Add(toStringMethod);
                    }
                    else
                    {
                        var toStringMethod = (MethodSymbol)existingToStringMethod;
                        if (!SynthesizedRecordObjectMethod.VerifyOverridesMethodFromObject(toStringMethod, SpecialMember.System_Object__ToString, diagnostics) && toStringMethod.IsSealed && !IsSealed)
                        {
                            MessageID.IDS_FeatureSealedToStringInRecord.CheckFeatureAvailability(
                                diagnostics,
                                this.DeclaringCompilation,
                                toStringMethod.GetFirstLocation());
                        }
                    }
                }

                MethodSymbol? getBaseToStringMethod()
                {
                    var objectToString = this.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString);
                    var currentBaseType = this.BaseTypeNoUseSiteDiagnostics;
                    while (currentBaseType is not null)
                    {
                        foreach (var member in currentBaseType.GetSimpleNonTypeMembers(WellKnownMemberNames.ObjectToString))
                        {
                            if (member is not MethodSymbol method)
                                continue;

                            if (method.GetLeastOverriddenMethod(null) == objectToString)
                                return method;
                        }

                        currentBaseType = currentBaseType.BaseTypeNoUseSiteDiagnostics;
                    }

                    return null;
                }
            }

            ImmutableArray<Symbol> addProperties(ImmutableArray<ParameterSymbol> recordParameters)
            {
                var existingOrAddedMembers = ArrayBuilder<Symbol>.GetInstance(recordParameters.Length);
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
                    if (!memberSignatures.TryGetValue(targetProperty, out var existingMember)
                        && !fieldsByName.TryGetValue(param.Name, out existingMember))
                    {
                        existingMember = OverriddenOrHiddenMembersHelpers.FindFirstHiddenMemberIfAny(targetProperty, memberIsFromSomeCompilation: true);
                        isInherited = true;
                    }

                    // There should be an error if we picked a member that is hidden
                    // This will be fixed in C# 9 as part of 16.10. Tracked by https://github.com/dotnet/roslyn/issues/52630

                    if (existingMember is null)
                    {
                        addProperty(new SynthesizedRecordPropertySymbol(this, syntax, param, isOverride: false, diagnostics));
                    }
                    else if (existingMember is FieldSymbol { IsStatic: false } field
                        && field.TypeWithAnnotations.Equals(param.TypeWithAnnotations, TypeCompareKind.AllIgnoreOptions))
                    {
                        Binder.CheckFeatureAvailability(syntax, MessageID.IDS_FeaturePositionalFieldsInRecords, diagnostics);
                        if (!isInherited || checkMemberNotHidden(field, param))
                        {
                            existingOrAddedMembers.Add(field);
                        }
                    }
                    else if (existingMember is PropertySymbol { IsStatic: false, GetMethod: { } } prop
                        && prop.TypeWithAnnotations.Equals(param.TypeWithAnnotations, TypeCompareKind.AllIgnoreOptions))
                    {
                        // There already exists a member corresponding to the candidate synthesized property.
                        if (isInherited && prop.IsAbstract)
                        {
                            addProperty(new SynthesizedRecordPropertySymbol(this, syntax, param, isOverride: true, diagnostics));
                        }
                        else if (!isInherited || checkMemberNotHidden(prop, param))
                        {
                            // Deconstruct() is specified to simply assign from this property to the corresponding out parameter.
                            existingOrAddedMembers.Add(prop);
                        }
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_BadRecordMemberForPositionalParameter,
                            param.GetFirstLocation(),
                            new FormattedSymbol(existingMember, SymbolDisplayFormat.CSharpErrorMessageFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)),
                            param.TypeWithAnnotations,
                            param.Name);
                    }

                    void addProperty(SynthesizedRecordPropertySymbol property)
                    {
                        existingOrAddedMembers.Add(property);
                        members.Add(property);
                        Debug.Assert(property.GetMethod is object);
                        Debug.Assert(property.SetMethod is object);
                        members.Add(property.GetMethod);
                        members.Add(property.SetMethod);
                        var backingField = property.DeclaredBackingField;
                        Debug.Assert(backingField is object);
                        members.Add(backingField);

                        builder.AddInstanceInitializerForPositionalMembers(new FieldOrPropertyInitializer(property.BackingField, paramList.Parameters[param.Ordinal]));
                        addedCount++;
                    }
                }

                return existingOrAddedMembers.ToImmutableAndFree();

                bool checkMemberNotHidden(Symbol symbol, ParameterSymbol param)
                {
                    if (memberNames.Contains(symbol.Name) || this.GetTypeMembersDictionary().ContainsKey(symbol.Name.AsMemory()))
                    {
                        diagnostics.Add(ErrorCode.ERR_HiddenPositionalMember, param.GetFirstLocation(), symbol);
                        return false;
                    }
                    return true;
                }
            }

            void addObjectEquals(MethodSymbol thisEquals)
            {
                members.Add(new SynthesizedRecordObjEquals(this, thisEquals, memberOffset: members.Count));
            }

            MethodSymbol addGetHashCode(PropertySymbol? equalityContract)
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
                    isStatic: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Int32)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                MethodSymbol getHashCode;

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingHashCodeMethod))
                {
                    getHashCode = new SynthesizedRecordGetHashCode(this, equalityContract, memberOffset: members.Count);
                    members.Add(getHashCode);
                }
                else
                {
                    getHashCode = (MethodSymbol)existingHashCodeMethod;
                    if (!SynthesizedRecordObjectMethod.VerifyOverridesMethodFromObject(getHashCode, SpecialMember.System_Object__GetHashCode, diagnostics) && getHashCode.IsSealed && !IsSealed)
                    {
                        diagnostics.Add(ErrorCode.ERR_SealedAPIInRecord, getHashCode.GetFirstLocation(), getHashCode);
                    }
                }
                return getHashCode;
            }

            PropertySymbol addEqualityContract()
            {
                Debug.Assert(isRecordClass);
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
                            diagnostics.Add(ErrorCode.ERR_NonPrivateAPIInRecord, equalityContract.GetFirstLocation(), equalityContract);
                        }
                    }
                    else if (equalityContract.DeclaredAccessibility != Accessibility.Protected)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonProtectedAPIInRecord, equalityContract.GetFirstLocation(), equalityContract);
                    }

                    if (!equalityContract.Type.Equals(targetProperty.Type, TypeCompareKind.AllIgnoreOptions))
                    {
                        if (!equalityContract.Type.IsErrorType())
                        {
                            diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, equalityContract.GetFirstLocation(), equalityContract, targetProperty.Type);
                        }
                    }
                    else
                    {
                        SynthesizedRecordEqualityContractProperty.VerifyOverridesEqualityContractFromBase(equalityContract, diagnostics);
                    }

                    if (equalityContract.GetMethod is null)
                    {
                        diagnostics.Add(ErrorCode.ERR_EqualityContractRequiresGetter, equalityContract.GetFirstLocation(), equalityContract);
                    }

                    reportStaticOrNotOverridableAPIInRecord(equalityContract, diagnostics);
                }

                return equalityContract;
            }

            MethodSymbol addThisEquals(PropertySymbol? equalityContract)
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
                                                                isParamsArray: false,
                                                                isParamsCollection: false,
                                                                RefKind.None
                                                                )),
                    RefKind.None,
                    isInitOnly: false,
                    isStatic: false,
                    TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Boolean)),
                    ImmutableArray<CustomModifier>.Empty,
                    ImmutableArray<MethodSymbol>.Empty);

                MethodSymbol thisEquals;

                if (!memberSignatures.TryGetValue(targetMethod, out Symbol? existingEqualsMethod))
                {
                    thisEquals = new SynthesizedRecordEquals(this, equalityContract, memberOffset: members.Count);
                    members.Add(thisEquals);
                }
                else
                {
                    thisEquals = (MethodSymbol)existingEqualsMethod;

                    if (thisEquals.DeclaredAccessibility != Accessibility.Public)
                    {
                        diagnostics.Add(ErrorCode.ERR_NonPublicAPIInRecord, thisEquals.GetFirstLocation(), thisEquals);
                    }

                    if (thisEquals.ReturnType.SpecialType != SpecialType.System_Boolean && !thisEquals.ReturnType.IsErrorType())
                    {
                        diagnostics.Add(ErrorCode.ERR_SignatureMismatchInRecord, thisEquals.GetFirstLocation(), thisEquals, targetMethod.ReturnType);
                    }

                    reportStaticOrNotOverridableAPIInRecord(thisEquals, diagnostics);
                }

                return thisEquals;
            }

            void reportStaticOrNotOverridableAPIInRecord(Symbol symbol, BindingDiagnosticBag diagnostics)
            {
                if (isRecordClass &&
                    !IsSealed &&
                    ((!symbol.IsAbstract && !symbol.IsVirtual && !symbol.IsOverride) || symbol.IsSealed))
                {
                    diagnostics.Add(ErrorCode.ERR_NotOverridableAPIInRecord, symbol.GetFirstLocation(), symbol);
                }
                else if (symbol.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_StaticAPIInRecord, symbol.GetFirstLocation(), symbol);
                }
            }

            void addBaseEquals()
            {
                Debug.Assert(isRecordClass);
                if (!BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    members.Add(new SynthesizedRecordBaseEquals(this, memberOffset: members.Count));
                }
            }

            void addEqualityOperators()
            {
                members.Add(new SynthesizedRecordEqualityOperator(this, memberOffset: members.Count, diagnostics));
                members.Add(new SynthesizedRecordInequalityOperator(this, memberOffset: members.Count, diagnostics));
            }
        }

        private void AddSynthesizedConstructorsIfNecessary(MembersAndInitializersBuilder builder, DeclaredMembersAndInitializers declaredMembersAndInitializers, BindingDiagnosticBag diagnostics)
        {
            //we're not calling the helpers on NamedTypeSymbol base, because those call
            //GetMembers and we're inside a GetMembers call ourselves (i.e. stack overflow)
            var hasInstanceConstructor = false;
            var hasParameterlessInstanceConstructor = false;
            var hasStaticConstructor = false;

            // CONSIDER: if this traversal becomes a bottleneck, the flags could be made outputs of the
            // dictionary construction process.  For now, this is more encapsulated.
            var membersSoFar = builder.GetNonTypeMembers(this, declaredMembersAndInitializers);
            foreach (var member in membersSoFar)
            {
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)member;
                    switch (method.MethodKind)
                    {
                        case MethodKind.Constructor:
                            // Ignore the record copy constructor
                            if (!IsRecord ||
                                !(SynthesizedRecordCopyCtor.HasCopyConstructorSignature(method) && method is not SynthesizedPrimaryConstructor))
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
                if (hasInstanceConstructor &&
                    hasParameterlessInstanceConstructor &&
                    hasStaticConstructor)
                {
                    break;
                }
            }

            // NOTE: Per section 11.3.8 of the spec, "every struct implicitly has a parameterless instance constructor".
            // We won't insert a parameterless constructor for a struct if there already is one.
            // The synthesized constructor will only be emitted if there are field initializers, but it should be in the symbol table.
            if ((!hasParameterlessInstanceConstructor && this.IsStructType()) ||
                (!hasInstanceConstructor && !this.IsStatic && !this.IsInterface))
            {
                builder.AddNonTypeMember(this, (this.TypeKind == TypeKind.Submission) ?
                    new SynthesizedSubmissionConstructor(this, diagnostics) :
                    new SynthesizedInstanceConstructor(this),
                    declaredMembersAndInitializers);
            }

            // constants don't count, since they do not exist as fields at runtime
            // NOTE: even for decimal constants (which require field initializers),
            // we do not create .cctor here since a static constructor implicitly created for a decimal
            // should not appear in the list returned by public API like GetMembers().
            if (!hasStaticConstructor && hasNonConstantInitializer(declaredMembersAndInitializers.StaticInitializers))
            {
                // Note: we don't have to put anything in the method - the binder will
                // do that when processing field initializers.
                builder.AddNonTypeMember(this, new SynthesizedStaticConstructor(this), declaredMembersAndInitializers);
            }

            if (this.IsScriptClass)
            {
                var scriptInitializer = new SynthesizedInteractiveInitializerMethod(this, diagnostics);
                builder.AddNonTypeMember(this, scriptInitializer, declaredMembersAndInitializers);
                var scriptEntryPoint = SynthesizedEntryPointSymbol.Create(scriptInitializer, diagnostics);
                builder.AddNonTypeMember(this, scriptEntryPoint, declaredMembersAndInitializers);
            }

            static bool hasNonConstantInitializer(ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>> initializers)
            {
                return initializers.Any(static siblings => siblings.Any(static initializer => !initializer.FieldOpt.IsConst));
            }
        }

        private void AddSynthesizedTupleMembersIfNecessary(MembersAndInitializersBuilder builder, DeclaredMembersAndInitializers declaredMembersAndInitializers)
        {
            if (!this.IsTupleType)
            {
                return;
            }

            var synthesizedMembers = this.MakeSynthesizedTupleMembers(declaredMembersAndInitializers.GetNonTypeMembers(this));
            if (synthesizedMembers is null)
            {
                return;
            }

            foreach (var synthesizedMember in synthesizedMembers)
            {
                builder.AddNonTypeMember(this, synthesizedMember, declaredMembersAndInitializers);
            }

            synthesizedMembers.Free();
        }

        private void AddNonTypeMembers(
            DeclaredMembersAndInitializersBuilder builder,
            SyntaxList<MemberDeclarationSyntax> members,
            BindingDiagnosticBag diagnostics)
        {
            if (members.Count == 0)
            {
                return;
            }

            var firstMember = members[0];
            var bodyBinder = this.GetBinder(firstMember);

            ArrayBuilder<FieldOrPropertyInitializer>? staticInitializers = null;
            ArrayBuilder<FieldOrPropertyInitializer>? instanceInitializers = null;
            var compilation = DeclaringCompilation;

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

                            // Lang version check for ref-fields is done inside SourceMemberFieldSymbol;
                            _ = fieldSyntax.Declaration.Type.SkipScoped(out _).SkipRefInField(out var refKind);
                            reportMisplacedMemberInNamespace(
                                 fieldSyntax,
                                 fieldSyntax.Declaration.Variables.First().Identifier,
                                 reportMisplacedGlobalCode);

                            bool modifierErrors;
                            var modifiers = SourceMemberFieldSymbol.MakeModifiers(this, fieldSyntax.Declaration.Variables[0].Identifier, fieldSyntax.Modifiers, isRefField: refKind != RefKind.None, diagnostics, out modifierErrors);
                            foreach (var variable in fieldSyntax.Declaration.Variables)
                            {
                                var fieldSymbol = (modifiers & DeclarationModifiers.Fixed) == 0
                                    ? new SourceMemberFieldSymbolFromDeclarator(this, variable, modifiers, modifierErrors, diagnostics)
                                    : new SourceFixedFieldSymbol(this, variable, modifiers, modifierErrors, diagnostics);
                                builder.NonTypeMembersWithPartialImplementations.Add(fieldSymbol);
                                // All fields are included in the nullable context for constructors and initializers, even fields without
                                // initializers, to ensure warnings are reported for uninitialized non-nullable fields in NullableWalker.
                                builder.UpdateIsNullableEnabledForConstructorsAndFields(useStatic: fieldSymbol.IsStatic, compilation, variable);

                                if (IsScriptClass)
                                {
                                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                                    ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeMembersWithPartialImplementations, variable, this,
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
                            reportMisplacedMemberInNamespace(
                                 methodSyntax,
                                 methodSyntax.Identifier,
                                 reportMisplacedGlobalCode);

                            var method = SourceOrdinaryMethodSymbol.CreateMethodSymbol(this, bodyBinder, methodSyntax, compilation.IsNullableAnalysisEnabledIn(methodSyntax), diagnostics);
                            builder.NonTypeMembersWithPartialImplementations.Add(method);
                        }
                        break;

                    case SyntaxKind.ConstructorDeclaration:
                        {
                            var constructorSyntax = (ConstructorDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                 constructorSyntax,
                                 constructorSyntax.Identifier,
                                 reportMisplacedGlobalCode);

                            bool isNullableEnabled = compilation.IsNullableAnalysisEnabledIn(constructorSyntax);
                            var constructor = SourceConstructorSymbol.CreateConstructorSymbol(this, constructorSyntax, isNullableEnabled, diagnostics);
                            builder.NonTypeMembersWithPartialImplementations.Add(constructor);
                            if (constructorSyntax.Initializer?.Kind() != SyntaxKind.ThisConstructorInitializer)
                            {
                                builder.UpdateIsNullableEnabledForConstructorsAndFields(useStatic: constructor.IsStatic, isNullableEnabled);
                            }
                        }
                        break;

                    case SyntaxKind.DestructorDeclaration:
                        {
                            var destructorSyntax = (DestructorDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                 destructorSyntax,
                                 destructorSyntax.Identifier,
                                 reportMisplacedGlobalCode);

                            // CONSIDER: if this doesn't (directly or indirectly) override object.Finalize, the
                            // runtime won't consider it a finalizer and it will not be marked as a destructor
                            // when it is loaded from metadata.  Perhaps we should just treat it as an Ordinary
                            // method in such cases?
                            var destructor = new SourceDestructorSymbol(this, destructorSyntax, compilation.IsNullableAnalysisEnabledIn(destructorSyntax), diagnostics);
                            builder.NonTypeMembersWithPartialImplementations.Add(destructor);
                        }
                        break;

                    case SyntaxKind.PropertyDeclaration:
                        {
                            var propertySyntax = (PropertyDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                 propertySyntax,
                                 propertySyntax.Identifier,
                                 reportMisplacedGlobalCode);

                            var property = SourcePropertySymbol.Create(this, bodyBinder, propertySyntax, diagnostics);
                            builder.NonTypeMembersWithPartialImplementations.Add(property);

                            AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, property.GetMethod);
                            AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, property.SetMethod);
                            FieldSymbol? backingField = property.DeclaredBackingField;

                            // TODO: can we leave this out of the member list?
                            // From the 10/12/11 design notes:
                            //   In addition, we will change autoproperties to behavior in
                            //   a similar manner and make the autoproperty fields private.
                            if (backingField is { })
                            {
                                builder.NonTypeMembersWithPartialImplementations.Add(backingField);
                                builder.UpdateIsNullableEnabledForConstructorsAndFields(useStatic: backingField.IsStatic, compilation, propertySyntax);

                                var initializer = propertySyntax.Initializer;
                                if (initializer != null)
                                {
                                    if (IsScriptClass)
                                    {
                                        // also gather expression-declared variables from the initializer
                                        ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeMembersWithPartialImplementations,
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
                            reportMisplacedMemberInNamespace(
                                 eventFieldSyntax,
                                 eventFieldSyntax.Declaration.Variables.First().Identifier,
                                 reportMisplacedGlobalCode);

                            foreach (VariableDeclaratorSyntax declarator in eventFieldSyntax.Declaration.Variables)
                            {
                                SourceFieldLikeEventSymbol @event = new SourceFieldLikeEventSymbol(this, bodyBinder, eventFieldSyntax.Modifiers, declarator, diagnostics);
                                builder.NonTypeMembersWithPartialImplementations.Add(@event);

                                FieldSymbol? associatedField = @event.AssociatedField;

                                if (IsScriptClass)
                                {
                                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                                    ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeMembersWithPartialImplementations, declarator, this,
                                                            DeclarationModifiers.Private | (@event.IsStatic ? DeclarationModifiers.Static : 0),
                                                            associatedField);
                                }

                                if ((object?)associatedField != null)
                                {
                                    // NOTE: specifically don't add the associated field to the members list
                                    // (regard it as an implementation detail).

                                    builder.UpdateIsNullableEnabledForConstructorsAndFields(useStatic: associatedField.IsStatic, compilation, declarator);

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

                                Debug.Assert(@event.IsPartial || @event.AddMethod is not null);
                                Debug.Assert(@event.IsPartial || @event.RemoveMethod is not null);

                                AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, @event.AddMethod);
                                AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, @event.RemoveMethod);
                            }
                        }
                        break;

                    case SyntaxKind.EventDeclaration:
                        {
                            var eventSyntax = (EventDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                eventSyntax,
                                eventSyntax.Identifier,
                                reportMisplacedGlobalCode);

                            var @event = new SourceCustomEventSymbol(this, bodyBinder, eventSyntax, diagnostics);

                            builder.NonTypeMembersWithPartialImplementations.Add(@event);

                            AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, @event.AddMethod);
                            AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, @event.RemoveMethod);

                            Debug.Assert(@event.AssociatedField is null);
                        }
                        break;

                    case SyntaxKind.IndexerDeclaration:
                        {
                            var indexerSyntax = (IndexerDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                indexerSyntax,
                                indexerSyntax.ThisKeyword,
                                reportMisplacedGlobalCode);

                            var indexer = SourcePropertySymbol.Create(this, bodyBinder, indexerSyntax, diagnostics);
                            builder.HaveIndexers = true;
                            builder.NonTypeMembersWithPartialImplementations.Add(indexer);
                            AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, indexer.GetMethod);
                            AddAccessorIfAvailable(builder.NonTypeMembersWithPartialImplementations, indexer.SetMethod);
                        }
                        break;

                    case SyntaxKind.ConversionOperatorDeclaration:
                        {
                            var conversionOperatorSyntax = (ConversionOperatorDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                conversionOperatorSyntax,
                                conversionOperatorSyntax.OperatorKeyword,
                                reportMisplacedGlobalCode);

                            var method = SourceUserDefinedConversionSymbol.CreateUserDefinedConversionSymbol(
                                this, bodyBinder, conversionOperatorSyntax, compilation.IsNullableAnalysisEnabledIn(conversionOperatorSyntax), diagnostics);
                            builder.NonTypeMembersWithPartialImplementations.Add(method);
                        }
                        break;

                    case SyntaxKind.OperatorDeclaration:
                        {
                            var operatorSyntax = (OperatorDeclarationSyntax)m;
                            reportMisplacedMemberInNamespace(
                                operatorSyntax,
                                operatorSyntax.OperatorKeyword,
                                reportMisplacedGlobalCode);

                            var method = SourceUserDefinedOperatorSymbol.CreateUserDefinedOperatorSymbol(
                                this, bodyBinder, operatorSyntax, compilation.IsNullableAnalysisEnabledIn(operatorSyntax), diagnostics);
                            builder.NonTypeMembersWithPartialImplementations.Add(method);
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
                                            ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeMembersWithPartialImplementations, vdecl, this, DeclarationModifiers.Private,
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
                                        ExpressionFieldFinder.FindExpressionVariables(builder.NonTypeMembersWithPartialImplementations,
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
                            m.Kind() is SyntaxKind.NamespaceDeclaration or
                                        SyntaxKind.FileScopedNamespaceDeclaration or
                                        SyntaxKind.IncompleteMember);
                        break;
                }
            }

            AddInitializers(builder.InstanceInitializers, instanceInitializers);
            AddInitializers(builder.StaticInitializers, staticInitializers);

            void reportMisplacedMemberInNamespace(SyntaxNode member, SyntaxToken locationSyntax, bool reportMisplacedGlobalCode)
            {
                if (IsImplicitClass && reportMisplacedGlobalCode)
                {
                    var parentSyntax = member.Parent;
                    var errorCode = parentSyntax.IsKind(SyntaxKind.CompilationUnit)
                        ? ErrorCode.ERR_CompilationUnitUnexpected
                        : ErrorCode.ERR_NamespaceUnexpected;
                    diagnostics.Add(errorCode, new SourceLocation(locationSyntax));
                }
            }
        }

        private void AddAccessorIfAvailable(ArrayBuilder<Symbol> symbols, MethodSymbol? accessorOpt)
        {
            if (!(accessorOpt is null))
            {
                symbols.Add(accessorOpt);
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
            if (IsExtension)
            {
                // Tracked by https://github.com/dotnet/roslyn/issues/78828 : nullability, figure out how to calculate and emit this for extensions. 
                //            We probably should do that per grouping type. Leaving as is should be fine too, I think.
                //            Otherwise, marker method should be processed explicitly because it is not among members.
                return null;
            }

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

        /// <summary>
        /// Returns true if the overall nullable context is enabled for constructors and initializers.
        /// </summary>
        /// <param name="useStatic">Consider static constructor and fields rather than instance constructors and fields.</param>
        internal bool IsNullableEnabledForConstructorsAndInitializers(bool useStatic)
        {
            var membersAndInitializers = GetMembersAndInitializers();
            return useStatic ?
                membersAndInitializers.IsNullableEnabledForStaticConstructorsAndFields :
                membersAndInitializers.IsNullableEnabledForInstanceConstructorsAndFields;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
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

                if (compilation.ShouldEmitNativeIntegerAttributes(baseType))
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

        internal bool ContainsExtensions
        {
            get
            {
                if (!_lazyContainsExtensions.HasValue())
                {
                    bool containsExtensions = ((this.IsStatic && !this.IsGenericType) || this.IsScriptClass) &&
                                                    (this.declaration.ContainsExtensionMethods || this.declaration.ContainsExtensionDeclarations);
                    _lazyContainsExtensions = containsExtensions.ToThreeState();
                }

                return _lazyContainsExtensions.Value();
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

        public override bool MightContainExtensions
        {
            get
            {
                return this.ContainsExtensions;
            }
        }

        #endregion

        internal void DiscoverInterceptors(ArrayBuilder<NamespaceOrTypeSymbol> toSearch)
        {
            foreach (var type in this.GetTypeMembers())
            {
                toSearch.Add(type);
            }

            if (!declaration.AnyMemberHasAttributes)
            {
                return;
            }

            foreach (var member in this.GetMembersUnordered())
            {
                if (member is MethodSymbol { MethodKind: MethodKind.Ordinary })
                {
                    // force binding attributes and populating compilation-level structures
                    member.GetAttributes();
                }
            }
        }

        public sealed override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }

        internal ExtensionGroupingInfo GetExtensionGroupingInfo()
        {
            Debug.Assert(this.declaration.ContainsExtensionDeclarations);

            if (_lazyExtensionGroupingInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionGroupingInfo, new ExtensionGroupingInfo(this), null);
            }

            return _lazyExtensionGroupingInfo;
        }

        internal class SynthesizedExplicitImplementations
        {
            public static readonly SynthesizedExplicitImplementations Empty = new SynthesizedExplicitImplementations(ImmutableArray<SynthesizedExplicitImplementationForwardingMethod>.Empty,
                                                                                                                     ImmutableArray<(MethodSymbol Body, MethodSymbol Implemented)>.Empty);

            public readonly ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> ForwardingMethods;
            public readonly ImmutableArray<(MethodSymbol Body, MethodSymbol Implemented)> MethodImpls;

            private SynthesizedExplicitImplementations(
                ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> forwardingMethods,
                ImmutableArray<(MethodSymbol Body, MethodSymbol Implemented)> methodImpls)
            {
                ForwardingMethods = forwardingMethods.NullToEmpty();
                MethodImpls = methodImpls.NullToEmpty();
            }

            internal static SynthesizedExplicitImplementations Create(
                ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> forwardingMethods,
                ImmutableArray<(MethodSymbol Body, MethodSymbol Implemented)> methodImpls)
            {
                if (forwardingMethods.IsDefaultOrEmpty && methodImpls.IsDefaultOrEmpty)
                {
                    return Empty;
                }

                return new SynthesizedExplicitImplementations(forwardingMethods, methodImpls);
            }
        }
    }
}

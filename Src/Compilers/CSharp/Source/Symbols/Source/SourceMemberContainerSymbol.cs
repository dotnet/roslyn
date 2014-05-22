// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a named type symbol whose members are declared in source.
    /// </summary>
    internal abstract partial class SourceMemberContainerTypeSymbol : NamedTypeSymbol
    {        
#if DEBUG
        static SourceMemberContainerTypeSymbol()
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

        protected SymbolCompletionState state;

        // The flags int is used to compact many different bits of information efficiently into a 32
        // bit int.  The layout is currently:
        //
        // |  |d|yy|xxxxxxxxxxxxxxxxxxxxx|wwwwww|
        //
        // w = special type.  6 bits.
        // x = modifiers.  21 bits.
        // y = IsManagedType.  2 bits.
        // d = FieldDefinitionsNoted. 1 bit
        private const int SpecialTypeMask = 0x3F;
        private const int DeclarationModifiersMask = 0x1FFFFF;
        private const int IsManagedTypeMask = 0x3;

        private const int SpecialTypeOffset = 0;
        private const int DeclarationModifiersOffset = 6;
        private const int IsManagedTypeOffset = 26;
        private const int FieldDefinitionsNotedOffset = 28;

        private int flags;

        // More flags.
        //
        // |                           |zzzz|f|
        //
        // f = FlattenedMembersIsSorted.  1 bit.
        // z = TypeKind. 4 bits.
        private const int FlattenedMembersIsSortedMask = 0x1;   // Set if "lazyMembersFlattened" is sorted.
        private const int TypeKindMask = 0xF;

        private const int FlattenedMembersIsSortedOffset = 0;
        private const int TypeKindOffset = 1;

        private int flags2;

        private readonly NamespaceOrTypeSymbol containingSymbol;
        protected readonly MergedTypeDeclaration declaration;

        private MembersAndInitializers lazyMembersAndInitializers;
        private Dictionary<string, ImmutableArray<Symbol>> lazyMembersDictionary;
        private Dictionary<string, ImmutableArray<Symbol>> lazyEarlyAttributeDecodingMembersDictionary;

        private static readonly Dictionary<string, ImmutableArray<NamedTypeSymbol>> emptyTypeMembers = new Dictionary<string, ImmutableArray<NamedTypeSymbol>>();
        private Dictionary<string, ImmutableArray<NamedTypeSymbol>> lazyTypeMembers;
        private ImmutableArray<Symbol> lazyMembersFlattened;
        private ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> lazySynthesizedExplicitImplementations;
        private int lazyKnownCircularStruct;
        private LexicalSortKey lazyLexicalSortKey = LexicalSortKey.NotInitialized;

        private ThreeState lazyContainsExtensionMethods;
        private ThreeState lazyAnyMemberHasAttributes;

        #region Construction

        internal SourceMemberContainerTypeSymbol(
            NamespaceOrTypeSymbol containingSymbol,
            MergedTypeDeclaration declaration,
            DiagnosticBag diagnostics)
        {
            this.containingSymbol = containingSymbol;
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

            this.flags = CreateFlags(specialType, modifiers);
            this.flags2 = CreateFlags2(typeKind);

            if ((object)ContainingType != null && ContainingType.IsSealed &&
                (this.DeclaredAccessibility == Accessibility.Protected || this.DeclaredAccessibility == Accessibility.ProtectedOrInternal))
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), Locations[0], this);
            }

            state.NotePartComplete(CompletionPart.TypeArguments); // type arguments need not be computed separately
        }

        private static int CreateFlags(SpecialType specialType, DeclarationModifiers declarationModifiers)
        {
            int specialTypeInt = (int)specialType;
            int declarationModifiersInt = (int)declarationModifiers;
            const int isManagedTypeInt = 0;

            specialTypeInt &= SpecialTypeMask;
            declarationModifiersInt &= DeclarationModifiersMask;
            //isManagedTypeInt &= IsManagedTypeMask;

            return
                (specialTypeInt << SpecialTypeOffset) |
                (declarationModifiersInt << DeclarationModifiersOffset) |
                (isManagedTypeInt << IsManagedTypeOffset);
        }

        private static int CreateFlags2(TypeKind typeKind)
        {
            int typeKindInt = (int)typeKind;
            return (typeKindInt << TypeKindOffset);
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
                            AddSemanticDiagnostics(diagnostics);
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
                            AddSemanticDiagnostics(diagnostics);
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
                            AddSemanticDiagnostics(diagnostics);
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
                                    ForceCompleteMemberByLocation(locationOpt, cancellationToken, member);
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
            if ((this.flags & (1 << FieldDefinitionsNotedOffset)) != 0)
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
                if ((this.flags & (1 << FieldDefinitionsNotedOffset)) == 0)
                {
                    if (!this.IsAbstract)
                    {
                        var assembly = (SourceAssemblySymbol)ContainingAssembly;

                        Accessibility containerEffectiveAccessibility = EffectiveAccessibility();

                        foreach (var member in this.lazyMembersAndInitializers.NonTypeNonIndexerMembers)
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
                    ThreadSafeFlagOperations.Set(ref this.flags, 1 << FieldDefinitionsNotedOffset);
                }
            }
        }

        #endregion

        #region Containers

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingSymbol as NamedTypeSymbol;
            }
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return this.containingSymbol;
            }
        }

        #endregion

        #region Flags Encoded Properties

        public override SpecialType SpecialType
        {
            get
            {
                var value = flags >> SpecialTypeOffset;
                return (SpecialType)(value & SpecialTypeMask);
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return (TypeKind)((((uint)flags2) >> TypeKindOffset) & TypeKindMask);
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
                const int IsManagedTypeTrue = 3 << IsManagedTypeOffset;
                const int IsManagedTypeFalse = 1 << IsManagedTypeOffset;

                switch (this.flags & (IsManagedTypeMask << IsManagedTypeOffset))
                {
                    case 0:
                        bool value = base.IsManagedType;
                        ThreadSafeFlagOperations.Set(ref this.flags, value ? IsManagedTypeTrue : IsManagedTypeFalse);
                        return value;
                    case IsManagedTypeTrue:
                        return true;
                    case IsManagedTypeFalse:
                        return false;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.flags);
                }
            }
        }

        private DeclarationModifiers DeclarationModifiers
        {
            get
            {
                var value = flags >> DeclarationModifiersOffset;
                return (DeclarationModifiers)(value & DeclarationModifiersMask);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Static) != 0;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Sealed) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Abstract) != 0;
            }
        }

        internal bool IsPartial
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.Partial) != 0;
            }
        }

        internal bool IsNew
        {
            get
            {
                return (this.DeclarationModifiers & DeclarationModifiers.New) != 0;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(this.DeclarationModifiers);
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
            if (!lazyLexicalSortKey.IsInitialized)
            {
                lazyLexicalSortKey.SetFrom(declaration.GetLexicalSortKey(this.DeclaringCompilation));
            }
            return lazyLexicalSortKey;
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

        #endregion

        #region Members

        /// <summary>
        /// Encapsulates information about the non-type members of a (i.e. this) type.
        ///   1) For non-initializers, symbols are created and stored in a list.
        ///   2) For fields, the symbols are stored in (1) and the initializers are
        ///        stored with other initialized fields from the same syntax tree with
        ///        the same static-ness.
        ///   3) For indexers, syntax (weak) references are stored for later binding.
        /// </summary>
        /// <remarks>
        /// CONSIDER: most types won't have indexers, so we could move the indexer list
        /// into a subclass to spare most instances the space required for the field.
        /// </remarks>
        private sealed class MembersAndInitializers
        {
            internal readonly SourceConstructorSymbol PrimaryCtor;
            internal readonly ImmutableArray<Symbol> NonTypeNonIndexerMembers;
            internal readonly ImmutableArray<FieldInitializers> StaticInitializers;
            internal readonly ImmutableArray<FieldInitializers> InstanceInitializers;
            internal readonly ImmutableArray<SyntaxReference> IndexerDeclarations;

            public MembersAndInitializers(
                SourceConstructorSymbol primaryCtor,
                ImmutableArray<Symbol> nonTypeNonIndexerMembers,
                ImmutableArray<FieldInitializers> staticInitializers,
                ImmutableArray<FieldInitializers> instanceInitializers,
                ImmutableArray<SyntaxReference> indexerDeclarations)
            {
                Debug.Assert(!nonTypeNonIndexerMembers.IsDefault);
                Debug.Assert(!staticInitializers.IsDefault);
                Debug.Assert(!instanceInitializers.IsDefault);
                Debug.Assert(!indexerDeclarations.IsDefault);

                Debug.Assert((object)primaryCtor == null || nonTypeNonIndexerMembers.Any(s => (object)primaryCtor == (object)s));
                Debug.Assert(!nonTypeNonIndexerMembers.Any(s => s is TypeSymbol));
                Debug.Assert(!nonTypeNonIndexerMembers.Any(s => s.IsIndexer()));
                Debug.Assert(!nonTypeNonIndexerMembers.Any(s => s.IsAccessor() && ((MethodSymbol)s).AssociatedSymbol.IsIndexer()));

                this.PrimaryCtor = primaryCtor;
                this.NonTypeNonIndexerMembers = nonTypeNonIndexerMembers;
                this.StaticInitializers = staticInitializers;
                this.InstanceInitializers = instanceInitializers;
                this.IndexerDeclarations = indexerDeclarations;
            }
        }

        internal ImmutableArray<FieldInitializers> StaticInitializers
        {
            get { return GetMembersAndInitializers().StaticInitializers; }
        }

        internal ImmutableArray<FieldInitializers> InstanceInitializers
        {
            get { return GetMembersAndInitializers().InstanceInitializers; }
        }

        /// <summary>
        /// Returns the "main" Primary Constructor. If more than one partial declaration declares a 
        /// Primary Constructor, we will have several distinct Primary Constructor symbols.
        /// This property returns the first one we encounter, it's parameters will be in scope within
        /// type members and initializers.
        /// </summary>
        internal SourceConstructorSymbol PrimaryCtor
        {
            get
            {
                if (this.lazyMembersAndInitializers == null)
                {
                    foreach (var decl in this.declaration.Declarations)
                    {
                        if (decl.HasPrimaryCtor)
                        {
                            return GetMembersAndInitializers().PrimaryCtor;
                        }
                    }

                    return null;
                }

                return this.lazyMembersAndInitializers.PrimaryCtor;
            }
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
            if (lazyTypeMembers == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref lazyTypeMembers, MakeTypeMembers(diagnostics), null) == null)
                {
                    AddSemanticDiagnostics(diagnostics);

                    state.NotePartComplete(CompletionPart.TypeMembers);
                }

                diagnostics.Free();
            }

            return lazyTypeMembers;
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

                Debug.Assert(emptyTypeMembers.Count == 0);
                return symbols.Count > 0 ? symbols.ToDictionary(s => s.Name) : emptyTypeMembers;
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
            var result = this.lazyMembersFlattened;

            if (result.IsDefault)
            {
                result = GetMembersByName().Flatten(null);  // do not sort.
                ImmutableInterlocked.InterlockedInitialize(ref this.lazyMembersFlattened, result);
                result = this.lazyMembersFlattened;
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
            if ((flags2 & (FlattenedMembersIsSortedMask << FlattenedMembersIsSortedOffset)) != 0) 
            {
                return this.lazyMembersFlattened;
            }
            else
            {
                var allMembers = this.GetMembersUnordered();

                if (allMembers.Length > 1)
                {
                    // The array isn't sorted. Sort it and remember that we sorted it.
                    allMembers = allMembers.Sort(LexicalOrderSymbolComparer.Instance);
                    ImmutableInterlocked.InterlockedExchange(ref this.lazyMembersFlattened, allMembers);
                }

                ThreadSafeFlagOperations.Set(ref flags2, (FlattenedMembersIsSortedMask << FlattenedMembersIsSortedOffset));
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
            if (lazyMembersDictionary != null || MemberNames.Contains(name))
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
            if (lazyEarlyAttributeDecodingMembersDictionary == null)
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

                Interlocked.CompareExchange(ref lazyEarlyAttributeDecodingMembersDictionary, membersByName, null);
            }

            return lazyEarlyAttributeDecodingMembersDictionary;
        }

        // NOTE: this method should do as little work as possible
        //       we often need to get members just to do a lookup.
        //       All additional checks and diagnostics may be not
        //       needed yet or at all.
        private MembersAndInitializers GetMembersAndInitializers()
        {
            var membersAndInitializers = this.lazyMembersAndInitializers;
            if (membersAndInitializers != null)
            {
                return membersAndInitializers;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            membersAndInitializers = BuildMembersAndInitializers(diagnostics);

            var alreadyKnown = Interlocked.CompareExchange(ref this.lazyMembersAndInitializers, membersAndInitializers, null);
            if (alreadyKnown != null)
            {
                diagnostics.Free();
                return alreadyKnown;
            }

            AddSemanticDiagnostics(diagnostics);
            diagnostics.Free();

            return membersAndInitializers;
        }

        protected Dictionary<string, ImmutableArray<Symbol>> GetMembersByName()
        {
            if (this.state.HasComplete(CompletionPart.Members))
            {
                return lazyMembersDictionary;
            }

            return GetMembersByNameSlow();
        }

        private Dictionary<string, ImmutableArray<Symbol>> GetMembersByNameSlow()
        {
            if (lazyMembersDictionary == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var membersDictionary = MakeAllMembers(diagnostics);
                if (Interlocked.CompareExchange(ref lazyMembersDictionary, membersDictionary, null) == null)
                {
                    MergePartialMethods(lazyMembersDictionary, diagnostics);
                    AddSemanticDiagnostics(diagnostics);
                    state.NotePartComplete(CompletionPart.Members);
                }

                diagnostics.Free();
            }

            state.SpinWaitComplete(CompletionPart.Members, default(CancellationToken));
            return lazyMembersDictionary;
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
            else if(!method1.IsPrimaryCtor || !method2.IsPrimaryCtor)
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

            if (typeParameterNames != null)
            {
                typeParameterNames.Free();
            }
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
                var primaryCtor = this.PrimaryCtor;

                foreach (var tp in TypeParameters)
                {
                    if ((object)primaryCtor != null)
                    {
                        foreach (var dup in primaryCtor.Parameters)
                        {
                            if (dup.Name.Equals(tp.Name))
                            {
                                diagnostics.Add(ErrorCode.ERR_PrimaryCtorParameterSameNameAsTypeParam, dup.Locations[0], this, tp.Name);
                                break;
                            }
                        }
                    }

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
                if (this.lazyKnownCircularStruct == (int)ThreeState.Unknown)
                {
                    if (TypeKind != TypeKind.Struct)
                    {
                        Interlocked.CompareExchange(ref this.lazyKnownCircularStruct, (int)ThreeState.False, (int)ThreeState.Unknown);
                    }
                    else
                    {
                        var diagnostics = DiagnosticBag.GetInstance();
                        var value = (int)CheckStructCircularity(diagnostics).ToThreeState();

                        if (Interlocked.CompareExchange(ref this.lazyKnownCircularStruct, value, (int)ThreeState.Unknown) == (int)ThreeState.Unknown)
                        {
                            AddSemanticDiagnostics(diagnostics);
                        }

                        Debug.Assert(value == this.lazyKnownCircularStruct);
                        diagnostics.Free();
                    }
                }

                return this.lazyKnownCircularStruct == (int)ThreeState.True;
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
                    var type = field.Type;
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
                if (!op1.ParameterTypes[p].Equals(op2.ParameterTypes[p], ignoreCustomModifiers: true, ignoreDynamic: true))
                {
                    return false;
                }
            }

            if (!op1.ReturnType.Equals(op2.ReturnType, ignoreCustomModifiers: true, ignoreDynamic: true))
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
            switch (m.Kind)
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
            foreach (var t in list) if (predicate(t)) return true;
            return false;
        }

        private static bool ContainsModifier(SyntaxTokenList modifiers, SyntaxKind modifier)
        {
            foreach (var m in modifiers) if (m.CSharpKind() == modifier) return true;
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
            public SourceConstructorSymbol PrimaryCtor;
            public ArrayBuilder<Symbol> NonTypeNonIndexerMembers = ArrayBuilder<Symbol>.GetInstance();
            public ArrayBuilder<FieldInitializers> StaticInitializers = ArrayBuilder<FieldInitializers>.GetInstance();
            public ArrayBuilder<FieldInitializers> InstanceInitializers = ArrayBuilder<FieldInitializers>.GetInstance();
            public ArrayBuilder<SyntaxReference> IndexerDeclarations = ArrayBuilder<SyntaxReference>.GetInstance();

            public MembersAndInitializers ToReadOnlyAndFree()
            {
                return new MembersAndInitializers(
                    PrimaryCtor,
                    NonTypeNonIndexerMembers.ToImmutableAndFree(),
                    StaticInitializers.ToImmutableAndFree(),
                    InstanceInitializers.ToImmutableAndFree(),
                    IndexerDeclarations.ToImmutableAndFree());
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
                    goto case TypeKind.Enum;

                case TypeKind.Enum:
                    CheckForStructDefaultConstructors(builder.NonTypeNonIndexerMembers, diagnostics);
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

                if (this.lazyMembersAndInitializers != null)
                {
                    // membersAndInitializers is already computed. no point to continue.
                    return;
                }

                var syntax = decl.SyntaxReference.GetSyntax();

                switch (syntax.CSharpKind())
                {
                    case SyntaxKind.EnumDeclaration:
                        AddEnumMembers(builder, (EnumDeclarationSyntax)syntax, diagnostics);
                        break;

                    case SyntaxKind.DelegateDeclaration:
                        SourceDelegateMethodSymbol.AddDelegateMembers(this, builder.NonTypeNonIndexerMembers, (DelegateDeclarationSyntax)syntax, diagnostics);
                        break;

                    case SyntaxKind.NamespaceDeclaration:
                        // The members of a global anonymous type is in a syntax tree of a namespace declaration or a compilation unit.
                        AddNonTypeMembers(builder, null, null, ((NamespaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.CompilationUnit:
                        AddNonTypeMembers(builder, null, null, ((CompilationUnitSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.ClassDeclaration:
                        var classDecl = (ClassDeclarationSyntax)syntax;
                        AddNonTypeMembers(builder, classDecl, classDecl.ParameterList, classDecl.Members, diagnostics);
                        break;

                    case SyntaxKind.InterfaceDeclaration:
                        AddNonTypeMembers(builder, (InterfaceDeclarationSyntax)syntax, null, ((InterfaceDeclarationSyntax)syntax).Members, diagnostics);
                        break;

                    case SyntaxKind.StructDeclaration:
                        var structDecl = (StructDeclarationSyntax)syntax;
                        AddNonTypeMembers(builder, structDecl, structDecl.ParameterList, structDecl.Members, diagnostics);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntax.CSharpKind());
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

                var propertyParamType = ((i == numParams - 1) && !getNotSet) ? propertySymbol.Type : propertyParams[i].Type;
                if (!propertyParamType.Equals(methodParam.Type, ignoreCustomModifiers: true, ignoreDynamic: true))
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
                eventSymbol.Type.Equals(methodParams[0].Type, ignoreCustomModifiers: true, ignoreDynamic: true);
        }

        private void AddEnumMembers(MembersAndInitializersBuilder result, EnumDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            ArrayBuilder<FieldInitializer> initializers = null;

            // The previous enum constant used to calculate subsequent
            // implicit enum constants. (This is the most recent explicit
            // enum constant or the first implicit constant if no explicit values.)
            SourceEnumConstantSymbol otherSymbol = null;

            // Offset from "otherSymbol".
            int otherSymbolOffset = 0;

            foreach (var m in syntax.Members)
            {
                switch (m.Kind)
                {
                    case SyntaxKind.EnumMemberDeclaration:
                        {
                            SourceEnumConstantSymbol symbol;
                            var valueOpt = m.EqualsValue;

                            if (valueOpt != null)
                            {
                                symbol = SourceEnumConstantSymbol.CreateExplicitValuedConstant(this, m, diagnostics);
                            }
                            else
                            {
                                symbol = SourceEnumConstantSymbol.CreateImplicitValuedConstant(this, m, otherSymbol, otherSymbolOffset, diagnostics);
                            }

                            result.NonTypeNonIndexerMembers.Add(symbol);

                            if ((valueOpt != null) || ((object)otherSymbol == null))
                            {
                                otherSymbol = symbol;
                                otherSymbolOffset = 1;
                            }
                            else
                            {
                                otherSymbolOffset++;
                            }

                            // The symbol is added to the set of initializers, even for
                            // implicit values since it's necessary to generate constants
                            // for each member to catch errors such as overflow.
                            AddInitializer(ref initializers, symbol, valueOpt);
                        }
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(m.Kind);
                }
            }

            AddInitializers(ref result.StaticInitializers, null, initializers);
        }

        private static void AddInitializer(ref ArrayBuilder<FieldInitializer> initializers, FieldSymbol field, CSharpSyntaxNode node)
        {
            if (initializers == null)
            {
                initializers = new ArrayBuilder<FieldInitializer>();
            }

            initializers.Add(new FieldInitializer(field, node.GetReferenceOrNull()));
        }

        private static void AddInitializers(ref ArrayBuilder<FieldInitializers> allInitializers, TypeDeclarationSyntax typeDeclaration, ArrayBuilder<FieldInitializer> siblings)
        {
            if (siblings != null)
            {
                if (allInitializers == null)
                {
                    allInitializers = new ArrayBuilder<FieldInitializers>();
                }

                allInitializers.Add(new FieldInitializers(typeDeclaration.GetReferenceOrNull(), siblings.ToImmutableAndFree()));
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
                    diagnostics.Add(ErrorCode.ERR_InterfacesCantContainFields, member.Locations[0]);
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

        private static void CheckForStructDefaultConstructors(ArrayBuilder<Symbol> members, DiagnosticBag diagnostics)
        {
            foreach (var s in members)
            {
                var m = s as MethodSymbol;
                if ((object)m != null)
                {
                    if (m.MethodKind == MethodKind.Constructor && m.ParameterCount == 0)
                    {
                        diagnostics.Add(ErrorCode.ERR_StructsCantContainDefaultConstructor, m.Locations[0]);
                    }
                }
            }
        }

        private void CheckForStructBadInitializers(MembersAndInitializersBuilder builder, DiagnosticBag diagnostics)
        {
            Debug.Assert(TypeKind == TypeKind.Struct);
            if (builder.InstanceInitializers.Count > 0)
            {
                var members = builder.NonTypeNonIndexerMembers;
                foreach (var s in members)
                {
                    if (s.Kind == SymbolKind.Method)
                    {
                        var method = s as MethodSymbol;
                        if (method.MethodKind == MethodKind.Constructor
                            && !method.IsParameterlessValueTypeConstructor())
                        {
                            return;
                        }

                    }
                }

                foreach (var s in members)
                {
                    var p = s as SourcePropertySymbol;
                    if (p != null && !p.IsStatic && p.IsAutoProperty
                        && p.BackingField.HasInitializer)
                    {
                        diagnostics.Add(
                            ErrorCode.ERR_InitializerInStructWithoutExplicitConstructor,
                            p.Location, p);
                    }
                    else
                    {
                        var f = s as SourceMemberFieldSymbol;
                        if (f != null && !f.IsStatic && f.HasInitializer)
                        {
                            diagnostics.Add(
                                ErrorCode.ERR_InitializerInStructWithoutExplicitConstructor,
                                f.Locations[0], f);
                        }
                    }
                }
            }
        }

        private void AddSynthesizedConstructorsIfNecessary(ArrayBuilder<Symbol> members, ArrayBuilder<FieldInitializers> staticInitializers, DiagnosticBag diagnostics)
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

            var hasStaticInitializer = false;
            if (staticInitializers != null)
            {
                foreach (var siblings in staticInitializers)
                {
                    foreach (var initializer in siblings.Initializers)
                    {
                        // constants don't count, since they do not exist as fields at runtime
                        // NOTE: even for decimal constants (which require field initializers), 
                        // we do not create .cctor here since a static constructor implicitly created for a decimal 
                        // should not appear in the list returned by public API like GetMembers().
                        if (!initializer.Field.IsConst)
                        {
                            hasStaticInitializer = true;
                            goto OUTER;
                        }
                    }
                }
            }

        OUTER:

            // NOTE: Per section 11.3.8 of the spec, "every struct implicitly has a parameterless instance constructor".
            // We won't insert a parameterless constructor for a struct if there already (erroneously) is one.
            // We don't expect anything to be emitted, but it should be in the symbol table.
            if ((!hasParameterlessInstanceConstructor && this.IsStructType()) || (!hasInstanceConstructor && !this.IsStatic))
            {
                if (this.TypeKind == TypeKind.Submission)
                {
                    members.Add(new SynthesizedSubmissionConstructor(this, diagnostics));
                }
                else
                {
                    members.Add(new SynthesizedInstanceConstructor(this));
                }
            }

            if (!hasStaticConstructor && hasStaticInitializer)
            {
                // Note: we don't have to put anything in the method - the binder will
                // do that when processing field initializers.
                members.Add(new SynthesizedStaticConstructor(this));
            }
        }

        private void AddNonTypeMembers(
            MembersAndInitializersBuilder result,
            TypeDeclarationSyntax typeDeclaration,
            ParameterListSyntax primaryCtorParameterList,
            SyntaxList<MemberDeclarationSyntax> members,
            DiagnosticBag diagnostics)
        {
            if (primaryCtorParameterList != null)
            {
                var constructor = SourceConstructorSymbol.CreatePrimaryConstructorSymbol(this, primaryCtorParameterList, diagnostics);
                result.NonTypeNonIndexerMembers.Add(constructor);

                if ((object)result.PrimaryCtor == null)
                {
                    result.PrimaryCtor = constructor;
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, new SourceLocation(primaryCtorParameterList));
                }
            }

            if (members.Count == 0)
            {
                return;
            }

            var firstMember = members[0];
            var bodyBinder = this.GetBinder(firstMember);
            bool globalCodeAllowed = IsGlobalCodeAllowed(firstMember.Parent);

            ArrayBuilder<FieldInitializer> staticInitializers = null;
            ArrayBuilder<FieldInitializer> instanceInitializers = null;

            foreach (var m in members)
            {
                if (this.lazyMembersAndInitializers != null)
                {
                    // membersAndInitializers is already computed. no point to continue.
                    return;
                }

                bool reportMisplacedGlobalCode = !globalCodeAllowed && !m.HasErrors;

                switch (m.Kind)
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
                                result.NonTypeNonIndexerMembers.Add(fieldSymbol);

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
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_NamespaceUnexpected,
                                    new SourceLocation(methodSyntax.Identifier));
                            }

                            var method = SourceMemberMethodSymbol.CreateMethodSymbol(this, bodyBinder, methodSyntax, diagnostics);
                            result.NonTypeNonIndexerMembers.Add(method);
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
                            result.NonTypeNonIndexerMembers.Add(constructor);
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
                            result.NonTypeNonIndexerMembers.Add(destructor);
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
                            result.NonTypeNonIndexerMembers.Add(property);

                            AddAccessorIfAvailable(result.NonTypeNonIndexerMembers, property.GetMethod, diagnostics);
                            AddAccessorIfAvailable(result.NonTypeNonIndexerMembers, property.SetMethod, diagnostics);

                            // TODO: can we leave this out of the member list?
                            // From the 10/12/11 design notes:
                            //   In addition, we will change autoproperties to behavior in 
                            //   a similar manner and make the autoproperty fields private.
                            if ((object)property.BackingField != null)
                            {
                                result.NonTypeNonIndexerMembers.Add(property.BackingField);

                                var initializer = propertySyntax.Initializer;
                                if (initializer != null)
                                {
                                    if (property.IsStatic)
                                    {
                                        AddInitializer(ref staticInitializers, property.BackingField, initializer);
                                    }
                                    else
                                    {
                                        AddInitializer(ref instanceInitializers, property.BackingField, initializer);
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
                                result.NonTypeNonIndexerMembers.Add(@event);

                                FieldSymbol associatedField = @event.AssociatedField;
                                if ((object)associatedField != null)
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

                                AddAccessorIfAvailable(result.NonTypeNonIndexerMembers, @event.AddMethod, diagnostics);
                                AddAccessorIfAvailable(result.NonTypeNonIndexerMembers, @event.RemoveMethod, diagnostics);
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

                            result.NonTypeNonIndexerMembers.Add(@event);

                            AddAccessorIfAvailable(result.NonTypeNonIndexerMembers, @event.AddMethod, diagnostics);
                            AddAccessorIfAvailable(result.NonTypeNonIndexerMembers, @event.RemoveMethod, diagnostics);

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
                            result.IndexerDeclarations.Add(indexerSyntax.GetReference());
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
                            result.NonTypeNonIndexerMembers.Add(method);
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
                            result.NonTypeNonIndexerMembers.Add(method);
                        }

                        break;

                    case SyntaxKind.GlobalStatement:
                        {
                            var globalStatement = ((GlobalStatementSyntax)m).Statement;
                            if (reportMisplacedGlobalCode)
                            {
                                diagnostics.Add(ErrorCode.ERR_GlobalStatement, new SourceLocation(globalStatement));
                            }

                            AddInitializer(ref instanceInitializers, null, globalStatement);
                        }
                        break;

                    default:
                        Debug.Assert(
                            SyntaxFacts.IsTypeDeclaration(m.Kind) ||
                            m.Kind == SyntaxKind.NamespaceDeclaration ||
                            m.Kind == SyntaxKind.IncompleteMember);
                        break;
                }
            }

            AddInitializers(ref result.InstanceInitializers, typeDeclaration, instanceInitializers);
            AddInitializers(ref result.StaticInitializers, typeDeclaration, staticInitializers);
        }

        private static bool IsGlobalCodeAllowed(CSharpSyntaxNode parent)
        {
            var parentKind = parent.Kind;
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
                if (!this.lazyContainsExtensionMethods.HasValue())
                {
                    bool containsExtensionMethods = (this.IsStatic && !this.IsGenericType && this.declaration.ContainsExtensionMethods);
                    this.lazyContainsExtensionMethods = containsExtensionMethods.ToThreeState();
                }

                return this.lazyContainsExtensionMethods.Value();
            }
        }

        internal bool AnyMemberHasAttributes
        {
            get
            {
                if (!this.lazyAnyMemberHasAttributes.HasValue())
                {
                    bool anyMemberHasAttributes = this.declaration.AnyMemberHasAttributes;
                    this.lazyAnyMemberHasAttributes = anyMemberHasAttributes.ToThreeState();
                }

                return this.lazyAnyMemberHasAttributes.Value();
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

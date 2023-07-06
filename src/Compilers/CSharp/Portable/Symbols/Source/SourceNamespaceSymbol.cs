// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class SourceNamespaceSymbol : NamespaceSymbol
    {
        private static readonly ImmutableDictionary<SingleNamespaceDeclaration, AliasesAndUsings> s_emptyMap =
            ImmutableDictionary<SingleNamespaceDeclaration, AliasesAndUsings>.Empty.WithComparers(ReferenceEqualityComparer.Instance);

        private readonly SourceModuleSymbol _module;
        private readonly Symbol _container;
        private readonly MergedNamespaceDeclaration _mergedDeclaration;

        private SymbolCompletionState _state;
        private ImmutableArray<Location> _locations;
        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamespaceOrTypeSymbol>> _nameToMembersMap;
        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>> _nameToTypeMembersMap;
        private ImmutableArray<Symbol> _lazyAllMembers;
        private ImmutableArray<NamedTypeSymbol> _lazyTypeMembersUnordered;

        /// <summary>
        /// Should only be read using <see cref="GetAliasesAndUsings(SingleNamespaceDeclaration)"/>.
        /// </summary>
        private ImmutableDictionary<SingleNamespaceDeclaration, AliasesAndUsings> _aliasesAndUsings_doNotAccessDirectly = s_emptyMap;
#if DEBUG
        /// <summary>
        /// Should only be read using <see cref="GetAliasesAndUsingsForAsserts"/>.
        /// </summary>
        private ImmutableDictionary<SingleNamespaceDeclaration, AliasesAndUsings> _aliasesAndUsingsForAsserts_doNotAccessDirectly = s_emptyMap;
#endif
        private MergedGlobalAliasesAndUsings _lazyMergedGlobalAliasesAndUsings;

        private const int LazyAllMembersIsSorted = 0x1;   // Set if "lazyAllMembers" is sorted.
        private int _flags;

        private LexicalSortKey _lazyLexicalSortKey = LexicalSortKey.NotInitialized;

        internal SourceNamespaceSymbol(
            SourceModuleSymbol module, Symbol container,
            MergedNamespaceDeclaration mergedDeclaration,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(mergedDeclaration != null);
            _module = module;
            _container = container;
            _mergedDeclaration = mergedDeclaration;

            foreach (var singleDeclaration in mergedDeclaration.Declarations)
                diagnostics.AddRange(singleDeclaration.Diagnostics);
        }

        internal MergedNamespaceDeclaration MergedDeclaration
            => _mergedDeclaration;

        public override Symbol ContainingSymbol
            => _container;

        public override AssemblySymbol ContainingAssembly
            => _module.ContainingAssembly;

        public override string Name
            => _mergedDeclaration.Name;

        internal override LexicalSortKey GetLexicalSortKey()
        {
            if (!_lazyLexicalSortKey.IsInitialized)
            {
                _lazyLexicalSortKey.SetFrom(_mergedDeclaration.GetLexicalSortKey(this.DeclaringCompilation));
            }
            return _lazyLexicalSortKey;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                if (_locations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _locations,
                        _mergedDeclaration.NameLocations,
                        default);
                }

                return _locations;
            }
        }

        public override Location TryGetFirstLocation()
            => _mergedDeclaration.Declarations[0].NameLocation;

        public override bool HasLocationContainedWithin(SyntaxTree tree, TextSpan declarationSpan, out bool wasZeroWidthMatch)
        {
            // Avoid the allocation of .Locations in the base method.
            foreach (var decl in _mergedDeclaration.Declarations)
            {
                if (IsLocationContainedWithin(decl.NameLocation, tree, declarationSpan, out wasZeroWidthMatch))
                    return true;
            }

            wasZeroWidthMatch = false;
            return false;
        }

        private static readonly Func<SingleNamespaceDeclaration, SyntaxReference> s_declaringSyntaxReferencesSelector = d =>
            new NamespaceDeclarationSyntaxReference(d.SyntaxReference);

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ComputeDeclaringReferencesCore();

        private ImmutableArray<SyntaxReference> ComputeDeclaringReferencesCore()
        {
            // SyntaxReference in the namespace declaration points to the name node of the namespace decl node not
            // namespace decl node we want to return. here we will wrap the original syntax reference in
            // the translation syntax reference so that we can lazily manipulate a node return to the caller
            return _mergedDeclaration.Declarations.SelectAsArray(s_declaringSyntaxReferencesSelector);
        }

        internal override ImmutableArray<Symbol> GetMembersUnordered()
        {
            var result = _lazyAllMembers;

            if (result.IsDefault)
            {
                var members = StaticCast<Symbol>.From(this.GetNameToMembersMap().Flatten(null));  // don't sort.
                ImmutableInterlocked.InterlockedInitialize(ref _lazyAllMembers, members);
                result = _lazyAllMembers;
            }

            return result.ConditionallyDeOrder();
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            if ((_flags & LazyAllMembersIsSorted) != 0)
            {
                return _lazyAllMembers;
            }
            else
            {
                var allMembers = this.GetMembersUnordered();

                if (allMembers.Length >= 2)
                {
                    // The array isn't sorted. Sort it and remember that we sorted it.
                    allMembers = allMembers.Sort(LexicalOrderSymbolComparer.Instance);
                    ImmutableInterlocked.InterlockedExchange(ref _lazyAllMembers, allMembers);
                }

                ThreadSafeFlagOperations.Set(ref _flags, LazyAllMembersIsSorted);
                return allMembers;
            }
        }

        public override ImmutableArray<Symbol> GetMembers(ReadOnlyMemory<char> name)
        {
            ImmutableArray<NamespaceOrTypeSymbol> members;
            return this.GetNameToMembersMap().TryGetValue(name, out members)
                ? members.Cast<NamespaceOrTypeSymbol, Symbol>()
                : ImmutableArray<Symbol>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            if (_lazyTypeMembersUnordered.IsDefault)
            {
                var members = this.GetNameToTypeMembersMap().Flatten();
                ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeMembersUnordered, members);
            }

            return _lazyTypeMembersUnordered;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return this.GetNameToTypeMembersMap().Flatten(LexicalOrderSymbolComparer.Instance);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            ImmutableArray<NamedTypeSymbol> members;
            return this.GetNameToTypeMembersMap().TryGetValue(name, out members)
                ? members
                : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray((s, arity) => s.Arity == arity, arity);
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _module;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                return new NamespaceExtent(_module);
            }
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamespaceOrTypeSymbol>> GetNameToMembersMap()
        {
            if (_nameToMembersMap == null)
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref _nameToMembersMap, MakeNameToMembersMap(diagnostics), null) == null)
                {
                    // NOTE: the following is not cancellable.  Once we've set the
                    // members, we *must* do the following to make sure we're in a consistent state.
                    this.AddDeclarationDiagnostics(diagnostics);
                    RegisterDeclaredCorTypes();

                    // We may produce a SymbolDeclaredEvent for the enclosing namespace before events for its contained members
                    DeclaringCompilation.SymbolDeclaredEvent(this);
                    var wasSetThisThread = _state.NotePartComplete(CompletionPart.NameToMembersMap);
                    Debug.Assert(wasSetThisThread);
                }

                diagnostics.Free();
            }

            return _nameToMembersMap;
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamedTypeSymbol>> GetNameToTypeMembersMap()
        {
            if (_nameToTypeMembersMap == null)
            {
                // NOTE: This method depends on MakeNameToMembersMap() on creating a proper
                // NOTE: type of the array, see comments in MakeNameToMembersMap() for details
                Interlocked.CompareExchange(
                    ref _nameToTypeMembersMap,
                    ImmutableArrayExtensions.GetTypesFromMemberMap<ReadOnlyMemory<char>, NamespaceOrTypeSymbol, NamedTypeSymbol>(
                        GetNameToMembersMap(), ReadOnlyMemoryOfCharComparer.Instance),
                    comparand: null);
            }

            return _nameToTypeMembersMap;
        }

        private Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamespaceOrTypeSymbol>> MakeNameToMembersMap(BindingDiagnosticBag diagnostics)
        {
            // NOTE: Even though the resulting map stores ImmutableArray<NamespaceOrTypeSymbol> as
            // NOTE: values if the name is mapped into an array of named types, which is frequently
            // NOTE: the case, we actually create an array of NamedTypeSymbol[] and wrap it in
            // NOTE: ImmutableArray<NamespaceOrTypeSymbol>
            // NOTE:
            // NOTE: This way we can save time and memory in GetNameToTypeMembersMap() -- when we see that
            // NOTE: a name maps into values collection containing types only instead of allocating another
            // NOTE: array of NamedTypeSymbol[] we downcast the array to ImmutableArray<NamedTypeSymbol>

            var builder = s_nameToObjectPool.Allocate();
            foreach (var declaration in _mergedDeclaration.Children)
            {
                NamespaceOrTypeSymbol symbol = BuildSymbol(declaration, diagnostics);
                ImmutableArrayExtensions.AddToMultiValueDictionaryBuilder(builder, symbol.Name.AsMemory(), symbol);
            }

            var result = new Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamespaceOrTypeSymbol>>(builder.Count, ReadOnlyMemoryOfCharComparer.Instance);
            ImmutableArrayExtensions.CreateNameToMembersMap<ReadOnlyMemory<char>, NamespaceOrTypeSymbol, NamedTypeSymbol, NamespaceSymbol>(builder, result);
            builder.Free();

            CheckMembers(this, result, diagnostics);

            return result;
        }

        private static void CheckMembers(NamespaceSymbol @namespace, Dictionary<ReadOnlyMemory<char>, ImmutableArray<NamespaceOrTypeSymbol>> result, BindingDiagnosticBag diagnostics)
        {
            var memberOfArity = new Symbol[10];
            MergedNamespaceSymbol mergedAssemblyNamespace = null;

            if (@namespace.ContainingAssembly.Modules.Length > 1)
            {
                mergedAssemblyNamespace = @namespace.ContainingAssembly.GetAssemblyNamespace(@namespace) as MergedNamespaceSymbol;
            }

            foreach (var name in result.Keys)
            {
                Array.Clear(memberOfArity, 0, memberOfArity.Length);
                foreach (var symbol in result[name])
                {
                    var nts = symbol as SourceMemberContainerTypeSymbol;
                    // It should be impossible to have a type member of a source namespace symbol which is not a SourceMemberContainerTypeSymbol
                    Debug.Assert((object)nts != null || symbol is not TypeSymbol);

                    var arity = ((object)nts != null) ? nts.Arity : 0;
                    if (arity >= memberOfArity.Length)
                    {
                        Array.Resize(ref memberOfArity, arity + 1);
                    }

                    var other = memberOfArity[arity];

                    if ((object)other == null && (object)mergedAssemblyNamespace != null)
                    {
                        // Check for collision with declarations from added modules.
                        foreach (NamespaceSymbol constituent in mergedAssemblyNamespace.ConstituentNamespaces)
                        {
                            if ((object)constituent != (object)@namespace)
                            {
                                // For whatever reason native compiler only detects conflicts against types.
                                // It doesn't complain when source declares a type with the same name as
                                // a namespace in added module, but complains when source declares a namespace
                                // with the same name as a type in added module.
                                var types = constituent.GetTypeMembers(symbol.Name, arity);

                                if (types.Length > 0)
                                {
                                    other = types[0];
                                    // Since the error doesn't specify what added module this type belongs to, we can stop searching
                                    // at the first match.
                                    break;
                                }
                            }
                        }
                    }

                    if ((object)other != null)
                    {
                        // To decide whether type declarations are duplicates, we need to access members which are only meaningful on source original definition symbols.
                        Debug.Assert((object)nts?.OriginalDefinition == nts && (object)other.OriginalDefinition == other);

                        switch (nts, other)
                        {
                            case ({ } left, SourceMemberContainerTypeSymbol right) when isFileLocalTypeInSeparateFileFrom(left, right) || isFileLocalTypeInSeparateFileFrom(right, left):
                                // no error
                                break;
                            case ({ IsFileLocal: true }, _) or (_, SourceMemberContainerTypeSymbol { IsFileLocal: true }):
                                diagnostics.Add(ErrorCode.ERR_FileLocalDuplicateNameInNS, symbol.GetFirstLocationOrNone(), symbol.Name, @namespace);
                                break;
                            case ({ IsPartial: true }, SourceMemberContainerTypeSymbol { IsPartial: true }):
                                diagnostics.Add(ErrorCode.ERR_PartialTypeKindConflict, symbol.GetFirstLocationOrNone(), symbol);
                                break;
                            default:
                                diagnostics.Add(ErrorCode.ERR_DuplicateNameInNS, symbol.GetFirstLocationOrNone(), symbol.Name, @namespace);
                                break;
                        }
                    }

                    memberOfArity[arity] = symbol;

                    if ((object)nts != null)
                    {
                        //types declared at the namespace level may only have declared accessibility of public or internal (Section 3.5.1)
                        Accessibility declaredAccessibility = nts.DeclaredAccessibility;
                        if (declaredAccessibility != Accessibility.Public && declaredAccessibility != Accessibility.Internal)
                        {
                            diagnostics.Add(ErrorCode.ERR_NoNamespacePrivate, symbol.GetFirstLocationOrNone());
                        }
                    }
                }
            }

            static bool isFileLocalTypeInSeparateFileFrom(SourceMemberContainerTypeSymbol possibleFileLocalType, SourceMemberContainerTypeSymbol otherSymbol)
            {
                if (!possibleFileLocalType.IsFileLocal)
                {
                    return false;
                }

                var leftTree = possibleFileLocalType.MergedDeclaration.Declarations[0].Location.SourceTree;
                if (otherSymbol.MergedDeclaration.NameLocations.Any((loc, leftTree) => (object)loc.SourceTree == leftTree, leftTree))
                {
                    return false;
                }

                return true;
            }
        }

        private NamespaceOrTypeSymbol BuildSymbol(MergedNamespaceOrTypeDeclaration declaration, BindingDiagnosticBag diagnostics)
        {
            switch (declaration.Kind)
            {
                case DeclarationKind.Namespace:
                    return new SourceNamespaceSymbol(_module, this, (MergedNamespaceDeclaration)declaration, diagnostics);

                case DeclarationKind.Struct:
                case DeclarationKind.Interface:
                case DeclarationKind.Enum:
                case DeclarationKind.Delegate:
                case DeclarationKind.Class:
                case DeclarationKind.Record:
                case DeclarationKind.RecordStruct:
                    return new SourceNamedTypeSymbol(this, (MergedTypeDeclaration)declaration, diagnostics);

                case DeclarationKind.Script:
                case DeclarationKind.Submission:
                case DeclarationKind.ImplicitClass:
                    return new ImplicitNamedTypeSymbol(this, (MergedTypeDeclaration)declaration, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaration.Kind);
            }
        }

        /// <summary>
        /// Register COR types declared in this namespace, if any, in the COR types cache.
        /// </summary>
        private void RegisterDeclaredCorTypes()
        {
            AssemblySymbol containingAssembly = ContainingAssembly;

            if (containingAssembly.KeepLookingForDeclaredSpecialTypes)
            {
                // Register newly declared COR types
                foreach (var array in _nameToMembersMap.Values)
                {
                    foreach (var member in array)
                    {
                        var type = member as NamedTypeSymbol;

                        if ((object)type != null && type.SpecialType != SpecialType.None)
                        {
                            containingAssembly.RegisterDeclaredSpecialType(type);

                            if (!containingAssembly.KeepLookingForDeclaredSpecialTypes)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        public override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.IsGlobalNamespace)
            {
                return true;
            }

            // Check if any namespace declaration block intersects with the given tree/span.
            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != tree)
                {
                    continue;
                }

                if (!definedWithinSpan.HasValue)
                {
                    return true;
                }

                var syntax = NamespaceDeclarationSyntaxReference.GetSyntax(declarationSyntaxRef, cancellationToken);
                if (syntax.FullSpan.IntersectsWith(definedWithinSpan.Value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

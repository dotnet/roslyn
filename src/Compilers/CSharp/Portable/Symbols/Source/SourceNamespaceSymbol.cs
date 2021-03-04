// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class SourceNamespaceSymbol : NamespaceSymbol
    {
        private readonly SourceModuleSymbol _module;
        private readonly Symbol _container;
        private readonly MergedNamespaceDeclaration _mergedDeclaration;

        private SymbolCompletionState _state;
        private ImmutableArray<Location> _locations;
        private Dictionary<string, ImmutableArray<NamespaceOrTypeSymbol>> _nameToMembersMap;
        private Dictionary<string, ImmutableArray<NamedTypeSymbol>> _nameToTypeMembersMap;
        private ImmutableArray<Symbol> _lazyAllMembers;
        private ImmutableArray<NamedTypeSymbol> _lazyTypeMembersUnordered;
        private readonly ImmutableDictionary<SingleNamespaceDeclaration, AliasesAndUsings> _aliasesAndUsings;

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

            var builder = ImmutableDictionary.CreateBuilder<SingleNamespaceDeclaration, AliasesAndUsings>(ReferenceEqualityComparer.Instance);

            foreach (var singleDeclaration in mergedDeclaration.Declarations)
            {
                if (singleDeclaration.HasExternAliases || singleDeclaration.HasGlobalUsings || singleDeclaration.HasUsings)
                {
                    builder.Add(singleDeclaration, new AliasesAndUsings());
                }

                diagnostics.AddRange(singleDeclaration.Diagnostics);
            }

            _aliasesAndUsings = builder.ToImmutable();
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

        public override ImmutableArray<Symbol> GetMembers(string name)
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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            ImmutableArray<NamedTypeSymbol> members;
            return this.GetNameToTypeMembersMap().TryGetValue(name, out members)
                ? members
                : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
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

        private Dictionary<string, ImmutableArray<NamespaceOrTypeSymbol>> GetNameToMembersMap()
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

        private Dictionary<string, ImmutableArray<NamedTypeSymbol>> GetNameToTypeMembersMap()
        {
            if (_nameToTypeMembersMap == null)
            {
                // NOTE: This method depends on MakeNameToMembersMap() on creating a proper 
                // NOTE: type of the array, see comments in MakeNameToMembersMap() for details
                Interlocked.CompareExchange(ref _nameToTypeMembersMap, GetTypesFromMemberMap(GetNameToMembersMap()), null);
            }

            return _nameToTypeMembersMap;
        }

        private static Dictionary<string, ImmutableArray<NamedTypeSymbol>> GetTypesFromMemberMap(Dictionary<string, ImmutableArray<NamespaceOrTypeSymbol>> map)
        {
            var dictionary = new Dictionary<string, ImmutableArray<NamedTypeSymbol>>(StringOrdinalComparer.Instance);

            foreach (var kvp in map)
            {
                ImmutableArray<NamespaceOrTypeSymbol> members = kvp.Value;

                bool hasType = false;
                bool hasNamespace = false;

                foreach (var symbol in members)
                {
                    if (symbol.Kind == SymbolKind.NamedType)
                    {
                        hasType = true;
                        if (hasNamespace)
                        {
                            break;
                        }
                    }
                    else
                    {
                        Debug.Assert(symbol.Kind == SymbolKind.Namespace);
                        hasNamespace = true;
                        if (hasType)
                        {
                            break;
                        }
                    }
                }

                if (hasType)
                {
                    if (hasNamespace)
                    {
                        dictionary.Add(kvp.Key, members.OfType<NamedTypeSymbol>().AsImmutable());
                    }
                    else
                    {
                        dictionary.Add(kvp.Key, members.As<NamedTypeSymbol>());
                    }
                }
            }

            return dictionary;
        }

        private Dictionary<string, ImmutableArray<NamespaceOrTypeSymbol>> MakeNameToMembersMap(BindingDiagnosticBag diagnostics)
        {
            // NOTE: Even though the resulting map stores ImmutableArray<NamespaceOrTypeSymbol> as 
            // NOTE: values if the name is mapped into an array of named types, which is frequently 
            // NOTE: the case, we actually create an array of NamedTypeSymbol[] and wrap it in 
            // NOTE: ImmutableArray<NamespaceOrTypeSymbol>
            // NOTE: 
            // NOTE: This way we can save time and memory in GetNameToTypeMembersMap() -- when we see that
            // NOTE: a name maps into values collection containing types only instead of allocating another 
            // NOTE: array of NamedTypeSymbol[] we downcast the array to ImmutableArray<NamedTypeSymbol>

            var builder = new NameToSymbolMapBuilder(_mergedDeclaration.Children.Length);
            foreach (var declaration in _mergedDeclaration.Children)
            {
                builder.Add(BuildSymbol(declaration, diagnostics));
            }

            var result = builder.CreateMap();

            CheckMembers(this, result, diagnostics);

            return result;
        }

        private static void CheckMembers(NamespaceSymbol @namespace, Dictionary<string, ImmutableArray<NamespaceOrTypeSymbol>> result, BindingDiagnosticBag diagnostics)
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
                    var nts = symbol as NamedTypeSymbol;
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
                        if ((nts as SourceNamedTypeSymbol)?.IsPartial == true && (other as SourceNamedTypeSymbol)?.IsPartial == true)
                        {
                            diagnostics.Add(ErrorCode.ERR_PartialTypeKindConflict, symbol.Locations.FirstOrNone(), symbol);
                        }
                        else
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateNameInNS, symbol.Locations.FirstOrNone(), name, @namespace);
                        }
                    }

                    memberOfArity[arity] = symbol;

                    if ((object)nts != null)
                    {
                        //types declared at the namespace level may only have declared accessibility of public or internal (Section 3.5.1)
                        Accessibility declaredAccessibility = nts.DeclaredAccessibility;
                        if (declaredAccessibility != Accessibility.Public && declaredAccessibility != Accessibility.Internal)
                        {
                            diagnostics.Add(ErrorCode.ERR_NoNamespacePrivate, symbol.Locations.FirstOrNone());
                        }
                    }
                }
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
                    return new SourceNamedTypeSymbol(this, (MergedTypeDeclaration)declaration, diagnostics);

                case DeclarationKind.Script:
                case DeclarationKind.Submission:
                case DeclarationKind.ImplicitClass:
                    return new ImplicitNamedTypeSymbol(this, (MergedTypeDeclaration)declaration, diagnostics);

                case DeclarationKind.SimpleProgram:
                    return new SimpleProgramNamedTypeSymbol(this, (MergedTypeDeclaration)declaration, diagnostics);

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

        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
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

        private struct NameToSymbolMapBuilder
        {
            private readonly Dictionary<string, object> _dictionary;

            public NameToSymbolMapBuilder(int capacity)
            {
                _dictionary = new Dictionary<string, object>(capacity, StringOrdinalComparer.Instance);
            }

            public void Add(NamespaceOrTypeSymbol symbol)
            {
                string name = symbol.Name;
                object item;
                if (_dictionary.TryGetValue(name, out item))
                {
                    var builder = item as ArrayBuilder<NamespaceOrTypeSymbol>;
                    if (builder == null)
                    {
                        builder = ArrayBuilder<NamespaceOrTypeSymbol>.GetInstance();
                        builder.Add((NamespaceOrTypeSymbol)item);
                        _dictionary[name] = builder;
                    }
                    builder.Add(symbol);
                }
                else
                {
                    _dictionary[name] = symbol;
                }
            }

            public Dictionary<String, ImmutableArray<NamespaceOrTypeSymbol>> CreateMap()
            {
                var result = new Dictionary<String, ImmutableArray<NamespaceOrTypeSymbol>>(_dictionary.Count, StringOrdinalComparer.Instance);

                foreach (var kvp in _dictionary)
                {
                    object value = kvp.Value;
                    ImmutableArray<NamespaceOrTypeSymbol> members;

                    var builder = value as ArrayBuilder<NamespaceOrTypeSymbol>;
                    if (builder != null)
                    {
                        Debug.Assert(builder.Count > 1);
                        bool hasNamespaces = false;
                        for (int i = 0; (i < builder.Count) && !hasNamespaces; i++)
                        {
                            hasNamespaces |= (builder[i].Kind == SymbolKind.Namespace);
                        }

                        members = hasNamespaces
                            ? builder.ToImmutable()
                            : StaticCast<NamespaceOrTypeSymbol>.From(builder.ToDowncastedImmutable<NamedTypeSymbol>());

                        builder.Free();
                    }
                    else
                    {
                        NamespaceOrTypeSymbol symbol = (NamespaceOrTypeSymbol)value;
                        members = symbol.Kind == SymbolKind.Namespace
                            ? ImmutableArray.Create<NamespaceOrTypeSymbol>(symbol)
                            : StaticCast<NamespaceOrTypeSymbol>.From(ImmutableArray.Create<NamedTypeSymbol>((NamedTypeSymbol)symbol));
                    }

                    result.Add(kvp.Key, members);
                }

                return result;
            }
        }

#nullable enable

        public Imports GetImports(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Externs.Any() && !compilationUnit.Usings.Any())
                    {
                        return Imports.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Externs.Any() && !namespaceDecl.Usings.Any())
                    {
                        return Imports.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetImports(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return Imports.Empty;
        }

        public ImmutableArray<AliasAndExternAliasDirective> GetExternAliases(CSharpSyntaxNode declarationSyntax)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Externs.Any())
                    {
                        return ImmutableArray<AliasAndExternAliasDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Externs.Any())
                    {
                        return ImmutableArray<AliasAndExternAliasDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetExternAliases(this, declarationSyntax);
                }
            }

            Debug.Assert(false);
            return ImmutableArray<AliasAndExternAliasDirective>.Empty;
        }

        public ImmutableArray<AliasAndUsingDirective> GetUsingAliases(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        return ImmutableArray<AliasAndUsingDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
                        return ImmutableArray<AliasAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetUsingAliases(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return ImmutableArray<AliasAndUsingDirective>.Empty;
        }

        public ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
                        return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetUsingAliasesMap(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
        }

        public ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsingNamespacesOrTypes(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
                        return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetUsingNamespacesOrTypes(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
        }

        private class AliasesAndUsings
        {
            private ExternAliasesAndDiagnostics? _lazyExternAliases;
            private UsingsAndDiagnostics? _lazyUsings;
            private Imports? _lazyImports;

            /// <summary>
            /// Completion state that tracks whether validation was done/not done/currently in process. 
            /// </summary>
            private SymbolCompletionState _state;

            internal ImmutableArray<AliasAndExternAliasDirective> GetExternAliases(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax)
            {
                return GetExternAliasesAndDiagnostics(declaringSymbol, declarationSyntax).ExternAliases;
            }

            private ExternAliasesAndDiagnostics GetExternAliasesAndDiagnostics(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax)
            {
                if (_lazyExternAliases is null)
                {
                    SyntaxList<ExternAliasDirectiveSyntax> externAliasDirectives;
                    switch (declarationSyntax)
                    {
                        case CompilationUnitSyntax compilationUnit:
                            externAliasDirectives = compilationUnit.Externs;
                            break;

                        case NamespaceDeclarationSyntax namespaceDecl:
                            externAliasDirectives = namespaceDecl.Externs;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
                    }

                    if (!externAliasDirectives.Any())
                    {
                        _lazyExternAliases = ExternAliasesAndDiagnostics.Empty;
                    }
                    else
                    {
                        var diagnostics = DiagnosticBag.GetInstance();
                        Interlocked.CompareExchange(
                            ref _lazyExternAliases,
                            new ExternAliasesAndDiagnostics() { ExternAliases = buildExternAliases(externAliasDirectives, declaringSymbol, diagnostics), Diagnostics = diagnostics.ToReadOnlyAndFree() },
                            null);
                    }
                }

                return _lazyExternAliases;

                static ImmutableArray<AliasAndExternAliasDirective> buildExternAliases(
                    SyntaxList<ExternAliasDirectiveSyntax> syntaxList,
                    SourceNamespaceSymbol declaringSymbol,
                    DiagnosticBag diagnostics)
                {
                    CSharpCompilation compilation = declaringSymbol.DeclaringCompilation;

                    var builder = ArrayBuilder<AliasAndExternAliasDirective>.GetInstance();

                    foreach (ExternAliasDirectiveSyntax aliasSyntax in syntaxList)
                    {
                        compilation.RecordImport(aliasSyntax);
                        bool skipInLookup = false;

                        // Extern aliases not allowed in interactive submissions:
                        if (compilation.IsSubmission)
                        {
                            diagnostics.Add(ErrorCode.ERR_ExternAliasNotAllowed, aliasSyntax.Location);
                            skipInLookup = true;
                        }
                        else
                        {
                            // some n^2 action, but n should be very small.
                            foreach (var existingAlias in builder)
                            {
                                if (existingAlias.Alias.Name == aliasSyntax.Identifier.ValueText)
                                {
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, existingAlias.Alias.Locations[0], existingAlias.Alias.Name);
                                    break;
                                }
                            }

                            if (aliasSyntax.Identifier.ContextualKind() == SyntaxKind.GlobalKeyword)
                            {
                                diagnostics.Add(ErrorCode.ERR_GlobalExternAlias, aliasSyntax.Identifier.GetLocation());
                            }
                        }

                        builder.Add(new AliasAndExternAliasDirective(new AliasSymbolFromSyntax(declaringSymbol, aliasSyntax), aliasSyntax, skipInLookup));
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            internal ImmutableArray<AliasAndUsingDirective> GetUsingAliases(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingAliases;
            }

            internal ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            }

            internal ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsingNamespacesOrTypes(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingNamespacesOrTypes;
            }

            private UsingsAndDiagnostics GetUsingsAndDiagnostics(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyUsings is null)
                {
                    SyntaxList<UsingDirectiveSyntax> usingDirectives;
                    switch (declarationSyntax)
                    {
                        case CompilationUnitSyntax compilationUnit:
                            usingDirectives = compilationUnit.Usings;
                            break;

                        case NamespaceDeclarationSyntax namespaceDecl:
                            usingDirectives = namespaceDecl.Usings;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
                    }

                    if (!usingDirectives.Any())
                    {
                        _lazyUsings = UsingsAndDiagnostics.Empty;
                    }
                    else
                    {
                        Interlocked.CompareExchange(
                            ref _lazyUsings,
                            buildUsings(usingDirectives, declaringSymbol, declarationSyntax, basesBeingResolved),
                            null);
                    }
                }

                return _lazyUsings;

                UsingsAndDiagnostics buildUsings(
                    SyntaxList<UsingDirectiveSyntax> usingDirectives,
                    SourceNamespaceSymbol declaringSymbol,
                    CSharpSyntaxNode declarationSyntax,
                    ConsList<TypeSymbol>? basesBeingResolved)
                {
                    // define all of the extern aliases first. They may used by the target of a using
                    var externAliases = GetExternAliases(declaringSymbol, declarationSyntax);
                    var diagnostics = new DiagnosticBag();

                    var compilation = declaringSymbol.DeclaringCompilation;

                    var usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
                    ImmutableDictionary<string, AliasAndUsingDirective>.Builder? usingAliasesMap = null;
                    var usingAliases = ArrayBuilder<AliasAndUsingDirective>.GetInstance();

                    // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                    // should not make use of other peer usings.
                    Binder? declarationBinder = null;

                    var uniqueUsings = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<NamespaceOrTypeSymbol>();

                    foreach (var usingDirective in usingDirectives)
                    {
                        compilation.RecordImport(usingDirective);

                        if (usingDirective.Alias != null)
                        {
                            SyntaxToken identifier = usingDirective.Alias.Name.Identifier;
                            Location location = usingDirective.Alias.Name.Location;

                            if (identifier.ContextualKind() == SyntaxKind.GlobalKeyword)
                            {
                                diagnostics.Add(ErrorCode.WRN_GlobalAliasDefn, location);
                            }

                            if (usingDirective.StaticKeyword != default(SyntaxToken))
                            {
                                diagnostics.Add(ErrorCode.ERR_NoAliasHere, location);
                            }

                            SourceMemberContainerTypeSymbol.ReportTypeNamedRecord(identifier.Text, compilation, diagnostics, location);

                            string identifierValueText = identifier.ValueText;
                            bool skipInLookup = false;

                            if (usingAliasesMap != null && usingAliasesMap.ContainsKey(identifierValueText))
                            {
                                skipInLookup = true;

                                // Suppress diagnostics if we're already broken.
                                if (!usingDirective.Name.IsMissing)
                                {
                                    // The using alias '{0}' appeared previously in this namespace
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, location, identifierValueText);
                                }
                            }
                            else
                            {
                                // an O(m*n) algorithm here but n (number of extern aliases) will likely be very small.
                                foreach (var externAlias in externAliases)
                                {
                                    if (externAlias.Alias.Name == identifierValueText)
                                    {
                                        // The using alias '{0}' appeared previously in this namespace
                                        diagnostics.Add(ErrorCode.ERR_DuplicateAlias, usingDirective.Location, identifierValueText);
                                        break;
                                    }
                                }
                            }

                            // construct the alias sym with the binder for which we are building imports. That
                            // way the alias target can make use of extern alias definitions.
                            var aliasAndDirective = new AliasAndUsingDirective(new AliasSymbolFromSyntax(declaringSymbol, usingDirective), usingDirective);
                            usingAliases.Add(aliasAndDirective);

                            if (usingAliasesMap == null)
                            {
                                Debug.Assert(!skipInLookup);
                                usingAliasesMap = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
                            }

                            if (!skipInLookup)
                            {
                                usingAliasesMap.Add(identifierValueText, aliasAndDirective);
                            }
                        }
                        else
                        {
                            if (usingDirective.Name.IsMissing)
                            {
                                //don't try to lookup namespaces inserted by parser error recovery
                                continue;
                            }

                            var directiveDiagnostics = BindingDiagnosticBag.GetInstance();
                            Debug.Assert(directiveDiagnostics.DiagnosticBag is object);
                            Debug.Assert(directiveDiagnostics.DependenciesBag is object);

                            declarationBinder ??= compilation.GetBinderFactory(declarationSyntax.SyntaxTree).GetBinder(usingDirective.Name).WithAdditionalFlags(BinderFlags.SuppressConstraintChecks);
                            var imported = declarationBinder.BindNamespaceOrTypeSymbol(usingDirective.Name, directiveDiagnostics, basesBeingResolved).NamespaceOrTypeSymbol;

                            if (imported.Kind == SymbolKind.Namespace)
                            {
                                Debug.Assert(directiveDiagnostics.DependenciesBag.IsEmpty());

                                if (usingDirective.StaticKeyword != default(SyntaxToken))
                                {
                                    diagnostics.Add(ErrorCode.ERR_BadUsingType, usingDirective.Name.Location, imported);
                                }
                                else if (!uniqueUsings.Add(imported))
                                {
                                    diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, imported);
                                }
                                else
                                {
                                    usings.Add(new NamespaceOrTypeAndUsingDirective(imported, usingDirective, dependencies: default));
                                }
                            }
                            else if (imported.Kind == SymbolKind.NamedType)
                            {
                                if (usingDirective.StaticKeyword == default(SyntaxToken))
                                {
                                    diagnostics.Add(ErrorCode.ERR_BadUsingNamespace, usingDirective.Name.Location, imported);
                                }
                                else
                                {
                                    var importedType = (NamedTypeSymbol)imported;
                                    if (uniqueUsings.Contains(importedType))
                                    {
                                        diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, importedType);
                                    }
                                    else
                                    {
                                        declarationBinder.ReportDiagnosticsIfObsolete(diagnostics, importedType, usingDirective.Name, hasBaseReceiver: false);

                                        uniqueUsings.Add(importedType);
                                        usings.Add(new NamespaceOrTypeAndUsingDirective(importedType, usingDirective, directiveDiagnostics.DependenciesBag.ToImmutableArray()));
                                    }
                                }
                            }
                            else if (imported.Kind != SymbolKind.ErrorType)
                            {
                                // Do not report additional error if the symbol itself is erroneous.

                                // error: '<symbol>' is a '<symbol kind>' but is used as 'type or namespace'
                                diagnostics.Add(ErrorCode.ERR_BadSKknown, usingDirective.Name.Location,
                                    usingDirective.Name,
                                    imported.GetKindText(),
                                    MessageID.IDS_SK_TYPE_OR_NAMESPACE.Localize());
                            }

                            diagnostics.AddRange(directiveDiagnostics.DiagnosticBag);
                            directiveDiagnostics.Free();
                        }
                    }

                    uniqueUsings.Free();

                    if (diagnostics.IsEmptyWithoutResolution)
                    {
                        diagnostics = null;
                    }

                    return new UsingsAndDiagnostics() { UsingAliases = usingAliases.ToImmutableAndFree(), UsingAliasesMap = usingAliasesMap?.ToImmutable(), UsingNamespacesOrTypes = usings.ToImmutableAndFree(), Diagnostics = diagnostics };
                }
            }

            internal Imports GetImports(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyImports is null)
                {
                    Interlocked.CompareExchange(ref _lazyImports,
                                                Imports.Create(GetUsingAliasesMap(declaringSymbol, declarationSyntax, basesBeingResolved),
                                                               GetUsingNamespacesOrTypes(declaringSymbol, declarationSyntax, basesBeingResolved),
                                                               GetExternAliases(declaringSymbol, declarationSyntax)),
                                                null);
                }

                return _lazyImports;
            }

            internal void Complete(SourceNamespaceSymbol declaringSymbol, SyntaxReference declarationSyntax, CancellationToken cancellationToken)
            {
                var externAliasesAndDiagnostics = _lazyExternAliases ?? GetExternAliasesAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax());
                cancellationToken.ThrowIfCancellationRequested();

                var usingsAndDiagnostics = _lazyUsings ?? GetUsingsAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(), basesBeingResolved: null);
                cancellationToken.ThrowIfCancellationRequested();

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var incompletePart = _state.NextIncompletePart;
                    switch (incompletePart)
                    {
                        case CompletionPart.StartValidatingImports:
                            {
                                if (_state.NotePartComplete(CompletionPart.StartValidatingImports))
                                {
                                    Validate(declaringSymbol, externAliasesAndDiagnostics, usingsAndDiagnostics);
                                    _state.NotePartComplete(CompletionPart.FinishValidatingImports);
                                }
                            }
                            break;

                        case CompletionPart.FinishValidatingImports:
                            // some other thread has started validating imports (otherwise we would be in the case above) so
                            // we just wait for it to both finish and report the diagnostics.
                            Debug.Assert(_state.HasComplete(CompletionPart.StartValidatingImports));
                            _state.SpinWaitComplete(CompletionPart.FinishValidatingImports, cancellationToken);
                            break;

                        case CompletionPart.None:
                            return;

                        default:
                            // any other values are completion parts intended for other kinds of symbols
                            _state.NotePartComplete(CompletionPart.All & ~CompletionPart.ImportsAll);
                            break;
                    }

                    _state.SpinWaitComplete(incompletePart, cancellationToken);
                }
            }

            private static void Validate(SourceNamespaceSymbol declaringSymbol, ExternAliasesAndDiagnostics externAliasesAndDiagnostics, UsingsAndDiagnostics usingsAndDiagnostics)
            {
                var compilation = declaringSymbol.DeclaringCompilation;
                DiagnosticBag semanticDiagnostics = compilation.DeclarationDiagnostics;

                // Check constraints within named aliases.
                var diagnostics = BindingDiagnosticBag.GetInstance();
                Debug.Assert(diagnostics.DiagnosticBag is object);
                Debug.Assert(diagnostics.DependenciesBag is object);

                if (usingsAndDiagnostics.UsingAliasesMap is object)
                {
                    // Force resolution of named aliases.
                    foreach (var (_, alias) in usingsAndDiagnostics.UsingAliasesMap)
                    {
                        NamespaceOrTypeSymbol target = alias.Alias.GetAliasTarget(basesBeingResolved: null);

                        diagnostics.Clear();
                        if (alias.Alias is AliasSymbolFromSyntax aliasFromSyntax)
                        {
                            diagnostics.AddRange(aliasFromSyntax.AliasTargetDiagnostics);
                        }

                        alias.Alias.CheckConstraints(diagnostics);

                        semanticDiagnostics.AddRange(diagnostics.DiagnosticBag);
                        recordImportDependencies(alias.UsingDirective!, target);
                    }
                }

                var corLibrary = compilation.SourceAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                foreach (var @using in usingsAndDiagnostics.UsingNamespacesOrTypes)
                {
                    diagnostics.Clear();
                    diagnostics.AddDependencies(@using.Dependencies);

                    NamespaceOrTypeSymbol target = @using.NamespaceOrType;

                    // Check if `using static` directives meet constraints.
                    UsingDirectiveSyntax usingDirective = @using.UsingDirective!;
                    if (target.IsType)
                    {
                        var typeSymbol = (TypeSymbol)target;
                        var location = usingDirective.Name.Location;
                        typeSymbol.CheckAllConstraints(compilation, conversions, location, diagnostics);
                    }

                    semanticDiagnostics.AddRange(diagnostics.DiagnosticBag);
                    recordImportDependencies(usingDirective, target);
                }

                // Force resolution of extern aliases.
                foreach (var alias in externAliasesAndDiagnostics.ExternAliases)
                {
                    if (alias.SkipInLookup)
                    {
                        continue;
                    }

                    var target = (NamespaceSymbol)alias.Alias.GetAliasTarget(null);
                    Debug.Assert(target.IsGlobalNamespace);

                    if (alias.Alias is AliasSymbolFromSyntax aliasFromSyntax)
                    {
                        semanticDiagnostics.AddRange(aliasFromSyntax.AliasTargetDiagnostics.DiagnosticBag!);
                    }

                    if (!Compilation.ReportUnusedImportsInTree(alias.ExternAliasDirective!.SyntaxTree))
                    {
                        diagnostics.Clear();
                        diagnostics.AddAssembliesUsedByNamespaceReference(target);
                        compilation.AddUsedAssemblies(diagnostics.DependenciesBag);
                    }
                }

                semanticDiagnostics.AddRange(externAliasesAndDiagnostics.Diagnostics);

                if (usingsAndDiagnostics.Diagnostics?.IsEmptyWithoutResolution == false)
                {
                    semanticDiagnostics.AddRange(usingsAndDiagnostics.Diagnostics.AsEnumerable());
                }

                diagnostics.Free();

                void recordImportDependencies(UsingDirectiveSyntax usingDirective, NamespaceOrTypeSymbol target)
                {
                    if (Compilation.ReportUnusedImportsInTree(usingDirective.SyntaxTree))
                    {
                        compilation.RecordImportDependencies(usingDirective, diagnostics.DependenciesBag.ToImmutableArray());
                    }
                    else
                    {
                        if (target.IsNamespace)
                        {
                            diagnostics.AddAssembliesUsedByNamespaceReference((NamespaceSymbol)target);
                        }

                        compilation.AddUsedAssemblies(diagnostics.DependenciesBag);
                    }
                }
            }

            private class ExternAliasesAndDiagnostics
            {
                public static readonly ExternAliasesAndDiagnostics Empty = new ExternAliasesAndDiagnostics() { ExternAliases = ImmutableArray<AliasAndExternAliasDirective>.Empty, Diagnostics = ImmutableArray<Diagnostic>.Empty };

                public ImmutableArray<AliasAndExternAliasDirective> ExternAliases { get; init; }
                public ImmutableArray<Diagnostic> Diagnostics { get; init; }
            }

            private class UsingsAndDiagnostics
            {
                public static readonly UsingsAndDiagnostics Empty =
                    new UsingsAndDiagnostics()
                    {
                        UsingAliases = ImmutableArray<AliasAndUsingDirective>.Empty,
                        UsingAliasesMap = null,
                        UsingNamespacesOrTypes = ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                        Diagnostics = null
                    };

                public ImmutableArray<AliasAndUsingDirective> UsingAliases { get; init; }
                public ImmutableDictionary<string, AliasAndUsingDirective>? UsingAliasesMap { get; init; }
                public ImmutableArray<NamespaceOrTypeAndUsingDirective> UsingNamespacesOrTypes { get; init; }
                public DiagnosticBag? Diagnostics { get; init; }
            }
        }
    }
}

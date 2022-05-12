// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration;

using Aliases = ArrayBuilder<(string aliasName, string symbolName)>;

internal static class SyntaxValueProviderExtensions
{
    /// <summary>
    /// Returns all syntax nodes of type <typeparamref name="T"/> if that node has an attribute on it that could
    /// possibly bind to the provided <paramref name="attributeName"/>. <paramref name="attributeName"/> should be the
    /// simple, non-qualified, name of the attribute, including the <c>Attribute</c> suffix, and not containing any
    /// generics, containing types, or namespaces.  For example <c>CLSCompliantAttribute</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// <para/> This provider understands <see langword="using"/> aliases and will find matches even when the attribute
    /// references an alias name.  For example, given:
    /// <code>
    /// using XAttribute = CLSCompilation;
    /// [X]
    /// class C { }
    /// </code>
    /// Then
    /// <c>context.SyntaxProvider.CreateSyntaxProviderForAttribute&lt;ClassDeclarationSyntax&gt;("CLSCompliant")</c>
    /// will find the <c>C</c> class.
    /// </summary>
    public static IncrementalValuesProvider<T> CreateSyntaxProviderForAttribute<T>(this SyntaxValueProvider provider, string attributeName)
        where T : SyntaxNode
    {
        // Create a provider that provides (and updates) the global aliases for any particular file when it is edited.
        var individualFileGlobalAliasesProvider = provider.CreateSyntaxProvider(
            (n, _) => n is CompilationUnitSyntax,
            (context, _) => GetGlobalAliasesInCompilationUnit((CompilationUnitSyntax)context.Node));

        // Create an aggregated view of all global aliases across all files.  This should only update when an individual
        // file changes its global aliases.
        var allUpGlobalAliasesProvider = individualFileGlobalAliasesProvider
            .Collect().Select((arrays, _) => GlobalAliases.Create(arrays.SelectMany(a => a.AliasAndSymbolNames).ToImmutableArray()));

        // Create a syntax provider for every compilation unit.
        var compilationUnitProvider = provider.CreateSyntaxProvider(
            (n, _) => n is CompilationUnitSyntax,
            (context, _) => (CompilationUnitSyntax)context.Node);

        // Combine the two providers so that we reanalyze every file if the global aliases change, or we reanalyze a
        // particular file when it's compilation unit changes.
        var compilationUnitAndGlobalAliasesProvider = compilationUnitProvider.Combine(allUpGlobalAliasesProvider);

        // For each pair of compilation unit + global aliases, walk the compilation unit 
        var result = compilationUnitAndGlobalAliasesProvider.SelectMany((globalAliasesAndCompilationUnit, cancellationToken) =>
            GetMatchingNodes<T>(
                globalAliasesAndCompilationUnit.Right, globalAliasesAndCompilationUnit.Left, attributeName, cancellationToken));

        return result;
    }

    private static ImmutableArray<T> GetMatchingNodes<T>(
        GlobalAliases globalAliases,
        CompilationUnitSyntax compilationUnit,
        string attributeName,
        CancellationToken cancellationToken) where T : SyntaxNode
    {
        var localAliases = Aliases.GetInstance();
        var results = ArrayBuilder<T>.GetInstance();
        var seenNames = PooledHashSet<string>.GetInstance();

        recurse(compilationUnit);

        return results.ToImmutableAndFree();

        void recurse(SyntaxNode node)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is CompilationUnitSyntax compilationUnit)
            {
                addLocalAliases(compilationUnit.Usings);
                foreach (var child in compilationUnit.Members)
                    recurse(child);
            }
            else if (node is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                var localAliasCount = localAliases.Count;
                addLocalAliases(namespaceDeclaration.Usings);

                foreach (var child in namespaceDeclaration.Members)
                    recurse(child);

                // after recursing into this namespace, dump any local aliases we added from this namespace decl itself.
                localAliases.Count = localAliasCount;
            }
            else if (node is AttributeSyntax attribute &&
                     node.Parent is T parent &&
                     // no need to examine another attribute on a node if we already added it due to a prior attribute
                     results.LastOrDefault() != parent)
            {
                seenNames.Clear();

                // attributes can't have attributes inside of them.  so no need to recurse when we're done.
                //
                // Have to lookup both with the name in the attribute, as well as adding the 'Attribute' suffix.
                // e.g. if there is [X] then we have to lookup with X and with XAttribute.
                if (matchesAttributeName(attribute.Name.GetUnqualifiedName().Identifier.ValueText) ||
                    matchesAttributeName(attribute.Name.GetUnqualifiedName().Identifier.ValueText + "Attribute"))
                {
                    results.Add(parent);
                }
            }
            else
            {
                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                        recurse(child.AsNode()!);
                }
            }
        }

        void addLocalAliases(SyntaxList<UsingDirectiveSyntax> usings)
        {
            foreach (var directive in usings)
            {
                if (directive.GlobalKeyword == default)
                    AddAlias(directive, localAliases);
            }
        }

        bool matchesAttributeName(string currentAttributeName)
        {
            // If the names match, we're done.
            if (StringOrdinalComparer.Equals(currentAttributeName, attributeName))
                return true;

            // Otherwise, keep searching through aliases.  Check that this is the first time seeing this name so we
            // don't infinite recurse in error code where aliases reference each other.
            if (seenNames.Add(currentAttributeName))
            {
                foreach (var (aliasName, symbolName) in localAliases)
                {
                    // see if user wrote `[SomeAlias]`.  If so, if we find a `using SomeAlias = ...` recurse using the
                    // ... name portion to see if it might bind to the attr name the caller is searching for.
                    if (StringOrdinalComparer.Equals(currentAttributeName, aliasName))
                    {
                        if (matchesAttributeName(symbolName))
                            return true;
                    }
                }

                foreach (var (aliasName, symbolName) in globalAliases.AliasAndSymbolNames)
                {
                    if (StringOrdinalComparer.Equals(currentAttributeName, aliasName))
                    {
                        if (matchesAttributeName(symbolName))
                            return true;
                    }
                }
            }

            return false;
        }
    }

    private static GlobalAliases GetGlobalAliasesInCompilationUnit(CompilationUnitSyntax compilationUnit)
    {
        var globalAliases = Aliases.GetInstance();

        foreach (var usingDirective in compilationUnit.Usings)
        {
            if (usingDirective.GlobalKeyword == default)
                continue;

            AddAlias(usingDirective, globalAliases);
        }

        return GlobalAliases.Create(globalAliases.ToImmutableAndFree());
    }

    private static void AddAlias(UsingDirectiveSyntax usingDirective, Aliases aliases)
    {
        if (usingDirective.Alias == null)
            return;

        var aliasName = usingDirective.Alias.Name.Identifier.ValueText;
        var symbolName = usingDirective.Name.GetUnqualifiedName().Identifier.ValueText;
        aliases.Add((aliasName, symbolName));
    }

    /// <summary>
    /// Simple wrapper class around an immutable array so we can have the value-semantics needed for the incremental
    /// generator to know when a change actually happened and it should run later transform stages.
    /// </summary>
    private class GlobalAliases : IEquatable<GlobalAliases>
    {
        public static readonly GlobalAliases Empty = new(ImmutableArray<(string aliasName, string symbolName)>.Empty);

        public readonly ImmutableArray<(string aliasName, string symbolName)> AliasAndSymbolNames;

        private int _hashCode;

        private GlobalAliases(ImmutableArray<(string aliasName, string symbolName)> aliasAndSymbolNames)
        {
            AliasAndSymbolNames = aliasAndSymbolNames;
        }

        public static GlobalAliases Create(ImmutableArray<(string aliasName, string symbolName)> aliasAndSymbolNames)
        {
            return aliasAndSymbolNames.IsEmpty ? Empty : new GlobalAliases(aliasAndSymbolNames);
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                var hashCode = 0;
                foreach (var tuple in this.AliasAndSymbolNames)
                    hashCode = Hash.Combine(tuple.GetHashCode(), hashCode);

                _hashCode = hashCode == 0 ? 1 : hashCode;
            }

            return _hashCode;
        }

        public override bool Equals(object? obj)
            => this.Equals(obj as GlobalAliases);

        public bool Equals(GlobalAliases? array)
        {
            if (array is null)
                return false;

            if (ReferenceEquals(this, array))
                return true;

            if (this.AliasAndSymbolNames == array.AliasAndSymbolNames)
                return true;

            if (this.AliasAndSymbolNames.Length != array.AliasAndSymbolNames.Length)
                return false;

            for (int i = 0, n = this.AliasAndSymbolNames.Length; i < n; i++)
            {
                if (this.AliasAndSymbolNames[i] != array.AliasAndSymbolNames[i])
                    return false;
            }

            return true;
        }
    }
}

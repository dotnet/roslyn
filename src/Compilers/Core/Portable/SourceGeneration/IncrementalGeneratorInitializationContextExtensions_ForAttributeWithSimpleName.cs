// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

using Aliases = ArrayBuilder<(string aliasName, string symbolName)>;

internal static partial class IncrementalGeneratorInitializationContextExtensions
{
    private static readonly ObjectPool<Stack<string>> s_stackPool = new(static () => new());

    /// <summary>
    /// Returns all syntax nodes of type <typeparamref name="T"/> if that node has an attribute on it that could
    /// possibly bind to the provided <paramref name="simpleName"/>. <paramref name="simpleName"/> should be the
    /// simple, non-qualified, name of the attribute, including the <c>Attribute</c> suffix, and not containing any
    /// generics, containing types, or namespaces.  For example <c>CLSCompliantAttribute</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// <para/> This provider understands <see langword="using"/> aliases and will find matches even when the attribute
    /// references an alias name.  For example, given:
    /// <code>
    /// using XAttribute = System.CLSCompliantAttribute;
    /// [X]
    /// class C { }
    /// </code>
    /// Then
    /// <c>context.SyntaxProvider.CreateSyntaxProviderForAttribute&lt;ClassDeclarationSyntax&gt;(nameof(CLSCompliantAttribute))</c>
    /// will find the <c>C</c> class.
    /// </summary>
    internal static IncrementalValuesProvider<T> ForAttributeWithSimpleName<T>(
        this IncrementalGeneratorInitializationContext context,
        string simpleName)
        where T : SyntaxNode
    {
        var syntaxHelper = context.SyntaxHelper;
        if (!syntaxHelper.IsValidIdentifier(simpleName))
            throw new ArgumentException("<todo: add error message>", nameof(simpleName));

        // Create a provider that provides (and updates) the global aliases for any particular file when it is edited.
        var individualFileGlobalAliasesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n is ICompilationUnitSyntax,
            static (context, _) => GetGlobalAliasesInCompilationUnit(context.SyntaxHelper, context.Node)).WithTrackingName("individualFileGlobalAliases_ForAttribute");

        // Create an aggregated view of all global aliases across all files.  This should only update when an individual
        // file changes its global aliases.
        var collectedGlobalAliasesProvider = individualFileGlobalAliasesProvider
            .Collect()
            .WithTrackingName("collectedGlobalAliases_ForAttribute");

        var allUpGlobalAliasesProvider = collectedGlobalAliasesProvider
            .Select(static (arrays, _) => GlobalAliases.Create(arrays.SelectMany(a => a.AliasAndSymbolNames).ToImmutableArray()))
            .WithTrackingName("allUpGlobalAliases_ForAttribute");

        // TODO: it would be nice if we had a compilation-options provider, that was we didn't need to regenerate this
        // if the compilation options stayed the same, but the compilation changed.
        var compilationGlobalAliases = context.CompilationProvider.Select(
            (c, _) =>
            {
                var aliases = Aliases.GetInstance();
                context.SyntaxHelper.AddAliases(c.Options, aliases);
                return GlobalAliases.Create(aliases.ToImmutableAndFree());
            }).WithTrackingName("compilationGlobalAliases_ForAttributeWithMetadataName");

        allUpGlobalAliasesProvider = allUpGlobalAliasesProvider
            .Combine(compilationGlobalAliases)
            .Select((tuple, _) => GlobalAliases.Concat(tuple.Left, tuple.Right))
            .WithTrackingName("allUpIncludingCompilationGlobalAliases_ForAttribute");

        // Create a syntax provider for every compilation unit.
        var compilationUnitProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n is ICompilationUnitSyntax,
            static (context, _) => context.Node).WithTrackingName("compilationUnit_ForAttribute");

        // Combine the two providers so that we reanalyze every file if the global aliases change, or we reanalyze a
        // particular file when it's compilation unit changes.
        var compilationUnitAndGlobalAliasesProvider = compilationUnitProvider
            .Combine(allUpGlobalAliasesProvider)
            .WithTrackingName("compilationUnitAndGlobalAliases_ForAttribute");

        // For each pair of compilation unit + global aliases, walk the compilation unit 
        var result = compilationUnitAndGlobalAliasesProvider
            .SelectMany((globalAliasesAndCompilationUnit, cancellationToken) => GetMatchingNodes<T>(
                syntaxHelper, globalAliasesAndCompilationUnit.Right, globalAliasesAndCompilationUnit.Left, simpleName, cancellationToken))
            .WithTrackingName("result_ForAttribute");

        return result;
    }

    private static GlobalAliases GetGlobalAliasesInCompilationUnit(
        ISyntaxHelper syntaxHelper,
        SyntaxNode compilationUnit)
    {
        Debug.Assert(syntaxHelper.IsCompilationUnit(compilationUnit));
        var globalAliases = Aliases.GetInstance();

        syntaxHelper.AddAliases(compilationUnit, globalAliases, global: true);

        return GlobalAliases.Create(globalAliases.ToImmutableAndFree());
    }

    private static ImmutableArray<T> GetMatchingNodes<T>(
        ISyntaxHelper syntaxHelper,
        GlobalAliases globalAliases,
        SyntaxNode compilationUnit,
        string name,
        CancellationToken cancellationToken) where T : SyntaxNode
    {
        Debug.Assert(syntaxHelper.IsCompilationUnit(compilationUnit));

        var isCaseSensitive = syntaxHelper.IsCaseSensitive;
        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        // As we walk down the compilation unit and nested namespaces, we may encounter additional using aliases local
        // to this file. Keep track of them so we can determine if they would allow an attribute in code to bind to the
        // attribute being searched for.
        var localAliases = Aliases.GetInstance();
        var nameHasAttributeSuffix = name.HasAttributeSuffix(isCaseSensitive);

        // Used to ensure that as we recurse through alias names to see if they could bind to attributeName that we
        // don't get into cycles.
        var seenNames = s_stackPool.Allocate();
        var results = ArrayBuilder<T>.GetInstance();

        try
        {
            recurse(compilationUnit);
        }
        finally
        {
            localAliases.Free();
            seenNames.Clear();
            s_stackPool.Free(seenNames);
        }

        return results.ToImmutableAndFree();

        void recurse(SyntaxNode node)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (syntaxHelper.IsCompilationUnit(node))
            {
                syntaxHelper.AddAliases(node, localAliases, global: false);

                recurseChildren(node);
            }
            else if (syntaxHelper.IsAnyNamespaceBlock(node))
            {
                var localAliasCount = localAliases.Count;
                syntaxHelper.AddAliases(node, localAliases, global: false);

                recurseChildren(node);

                // after recursing into this namespace, dump any local aliases we added from this namespace decl itself.
                localAliases.Count = localAliasCount;
            }
            else if (syntaxHelper.IsAttributeList(node) &&
                     node.Parent is T parent &&
                     // no need to examine another attribute on a node if we already added it due to a prior attribute
                     results.LastOrDefault() != parent)
            {
                foreach (var attribute in syntaxHelper.GetAttributesOfAttributeList(node))
                {
                    // Have to lookup both with the name in the attribute, as well as adding the 'Attribute' suffix.
                    // e.g. if there is [X] then we have to lookup with X and with XAttribute.
                    var simpleAttributeName = syntaxHelper.GetUnqualifiedIdentifierOfName(
                        syntaxHelper.GetNameOfAttribute(attribute)).ValueText;
                    if (matchesAttributeName(simpleAttributeName, withAttributeSuffix: false) ||
                        matchesAttributeName(simpleAttributeName, withAttributeSuffix: true))
                    {
                        results.Add(parent);
                        return;
                    }
                }

                // attributes can't have attributes inside of them.  so no need to recurse when we're done.
            }
            else
            {
                // For any other node, just keep recursing deeper to see if we can find an attribute. Note: we cannot
                // terminate the search anywhere as attributes may be found on things like local functions, and that
                // means having to dive deep into statements and expressions.
                recurseChildren(node);
            }

            return;

            void recurseChildren(SyntaxNode node)
            {
                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                        recurse(child.AsNode()!);
                }
            }
        }

        // Checks if `name` is equal to `matchAgainst`.  if `withAttributeSuffix` is true, then
        // will check if `name` + "Attribute" is equal to `matchAgainst`
        bool matchesName(string name, string matchAgainst, bool withAttributeSuffix)
        {
            if (withAttributeSuffix)
            {
                return name.Length + "Attribute".Length == matchAgainst.Length &&
                    matchAgainst.HasAttributeSuffix(isCaseSensitive) &&
                    matchAgainst.StartsWith(name, comparison);
            }
            else
            {
                return name.Equals(matchAgainst, comparison);
            }
        }

        bool matchesAttributeName(string currentAttributeName, bool withAttributeSuffix)
        {
            // If the names match, we're done.
            if (withAttributeSuffix)
            {
                if (nameHasAttributeSuffix &&
                    matchesName(currentAttributeName, name, withAttributeSuffix))
                {
                    return true;
                }
            }
            else
            {
                if (matchesName(currentAttributeName, name, withAttributeSuffix: false))
                    return true;
            }

            // Otherwise, keep searching through aliases.  Check that this is the first time seeing this name so we
            // don't infinite recurse in error code where aliases reference each other.
            //
            // note: as we recurse up the aliases, we do not want to add the attribute suffix anymore.  aliases must
            // reference the actual real name of the symbol they are aliasing.
            if (seenNames.Contains(currentAttributeName))
                return false;

            seenNames.Push(currentAttributeName);

            foreach (var (aliasName, symbolName) in localAliases)
            {
                // see if user wrote `[SomeAlias]`.  If so, if we find a `using SomeAlias = ...` recurse using the
                // ... name portion to see if it might bind to the attr name the caller is searching for.
                if (matchesName(currentAttributeName, aliasName, withAttributeSuffix) &&
                    matchesAttributeName(symbolName, withAttributeSuffix: false))
                {
                    return true;
                }
            }

            foreach (var (aliasName, symbolName) in globalAliases.AliasAndSymbolNames)
            {
                if (matchesName(currentAttributeName, aliasName, withAttributeSuffix) &&
                    matchesAttributeName(symbolName, withAttributeSuffix: false))
                {
                    return true;
                }
            }

            seenNames.Pop();
            return false;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;
using CSharpSyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

/// <summary>
/// Compilation-backed alias targets, without retaining symbols or the compilation. Component descriptors
/// alone cannot reveal a nearer namespace or type that shadows an outer component.
/// </summary>
internal sealed class ComponentAliasTargetMap : IEquatable<ComponentAliasTargetMap>
{
    internal sealed record AliasInfo(string Namespace, string Target);
    internal readonly record struct AliasTarget(string? Name, bool IsType);

    public static readonly ComponentAliasTargetMap Empty = new(ImmutableDictionary<(string Namespace, string Target), AliasTarget>.Empty);

    private readonly ImmutableDictionary<(string Namespace, string Target), AliasTarget> _targets;

    private ComponentAliasTargetMap(ImmutableDictionary<(string Namespace, string Target), AliasTarget> targets)
    {
        _targets = targets;
    }

    public bool TryGetTarget(string @namespace, string target, out AliasTarget result)
        => _targets.TryGetValue((@namespace, target), out result);

    public bool EqualsForDocument(RazorCodeDocument document, ComponentAliasTargetMap other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        foreach (var alias in GetAliasInfos(document))
        {
            var hasTarget = TryGetTarget(alias.Namespace, alias.Target, out var target);
            var hasOtherTarget = other.TryGetTarget(alias.Namespace, alias.Target, out var otherTarget);
            if (hasTarget != hasOtherTarget || target != otherTarget)
            {
                return false;
            }
        }

        return true;
    }

    public static ImmutableArray<AliasInfo> GetAliasInfos(RazorCodeDocument document)
    {
        // Aliases only affect component binding starting in Razor 12.
        if (document.ParserOptions.LanguageVersion < RazorLanguageVersion.Version_12_0)
        {
            return [];
        }

        // Views still emit their C# aliases, but do not use alias targets for component tag matching.
        if (!document.FileKind.IsComponent())
        {
            return [];
        }

        using var aliases = new PooledArrayBuilder<AliasInfo>();
        document.TryGetNamespace(fallbackToRootNamespace: true, out var currentNamespace);
        foreach (var import in document.GetImportSyntaxTrees())
        {
            Collect(import, currentNamespace ?? string.Empty, ref aliases.AsRef());
        }

        if (document.GetSyntaxTree() is { } tree)
        {
            Collect(tree, currentNamespace ?? string.Empty, ref aliases.AsRef());
        }

        return aliases.ToImmutableAndClear();

        static void Collect(RazorSyntaxTree tree, string currentNamespace, ref PooledArrayBuilder<AliasInfo> aliases)
        {
            foreach (var node in tree.Root.DescendantNodes())
            {
                if (node is CSharpStatementLiteralSyntax { ChunkGenerator: AddImportChunkGenerator { IsStatic: false } directive })
                {
                    var equals = directive.ParsedNamespace.IndexOf('=');
                    if (equals >= 0)
                    {
                        aliases.Add(new(currentNamespace, directive.ParsedNamespace[(equals + 1)..]));
                    }
                }
            }
        }
    }

    public static ComponentAliasTargetMap Create(
        ImmutableArray<ImmutableArray<AliasInfo>> documents,
        Compilation compilation,
        CancellationToken cancellationToken = default)
    {
        // Deduplicate shared imports by their actual lookup context, not syntax-tree identity.
        // No aliases means no probe tree, augmented compilation or semantic model.
        using var _ = HashSetPool<AliasInfo>.GetPooledObject(out var seen);
        using var aliases = new PooledArrayBuilder<AliasInfo>();
        foreach (var document in documents)
        {
            foreach (var alias in document)
            {
                if (seen.Add(alias))
                {
                    aliases.Add(alias);
                }
            }
        }

        if (aliases.Count == 0)
        {
            return Empty;
        }

        // Parse each target separately so an incomplete name can't consume another alias during
        // error recovery. Reuse parsed names when they occur in multiple namespace contexts.
        using var _1 = DictionaryPool<string, NameSyntax>.GetPooledObject(out var names);
        using var usings = new PooledArrayBuilder<UsingDirectiveSyntax>();
        using var members = new PooledArrayBuilder<MemberDeclarationSyntax>();
        var probeAlias = CSharpSyntaxFactory.NameEquals(CSharpSyntaxFactory.IdentifierName("__RazorAlias"));
        foreach (var alias in aliases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetName = GetName(alias.Target);
            if (targetName.ContainsDiagnostics)
            {
                continue;
            }

            var namespaceName = alias.Namespace.Length == 0 ? null : GetName(alias.Namespace);
            if (namespaceName is { ContainsDiagnostics: true })
            {
                continue;
            }

            var usingDirective = CSharpSyntaxFactory.UsingDirective(targetName).WithAlias(probeAlias);
            if (namespaceName is null)
            {
                usings.Add(usingDirective);
            }
            else
            {
                // Each using gets its own namespace block so it resolves in the document's namespace
                // without the probe aliases conflicting with one another.
                members.Add(CSharpSyntaxFactory.NamespaceDeclaration(namespaceName).WithUsings(CSharpSyntaxFactory.SingletonList(usingDirective)));
            }
        }

        if (usings.Count == 0 && members.Count == 0)
        {
            return Empty;
        }

        // Bind the constructed syntax tree to resolve the targets against the compilation.
        var probe = CSharpSyntaxFactory.CompilationUnit()
            .WithUsings(CSharpSyntaxFactory.List(usings.ToImmutableAndClear()))
            .WithMembers(CSharpSyntaxFactory.List(members.ToImmutableAndClear()));
        var options = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions ?? CSharpParseOptions.Default;
        var tree = CSharpSyntaxTree.Create(probe, options);
        var augmented = compilation.AddSyntaxTrees(tree);
        var model = augmented.GetSemanticModel(tree);
        var root = (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
        using var _2 = DictionaryBuilderPool<(string Namespace, string Target), AliasTarget>.GetPooledObject(out var targets);
        foreach (var usingDirective in root.Usings)
        {
            AddTarget(string.Empty, usingDirective);
        }

        foreach (var member in root.Members)
        {
            if (member is NamespaceDeclarationSyntax declaration)
            {
                foreach (var usingDirective in declaration.Usings)
                {
                    AddTarget(declaration.Name.ToString(), usingDirective);
                }
            }
        }

        return new(targets.ToImmutable());

        NameSyntax GetName(string text)
        {
            if (!names.TryGetValue(text, out var name))
            {
                name = CSharpSyntaxFactory.ParseName(text);
                names.Add(text, name);
            }

            return name;
        }

        void AddTarget(string currentNamespace, UsingDirectiveSyntax directive)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (directive.Name is not { } targetName || directive.ContainsDiagnostics)
            {
                return;
            }

            var symbol = (model.GetDeclaredSymbol(directive, cancellationToken) as IAliasSymbol)?.Target;
            // Error results are intentional entries: they must suppress descriptor-based fallback.
            var target = symbol is INamespaceSymbol or INamedTypeSymbol { TypeKind: not TypeKind.Error }
                ? new AliasTarget(symbol.GetFullName(), symbol is INamedTypeSymbol)
                : default;
            targets.Add((currentNamespace, targetName.ToString()), target);
        }
    }

    public bool Equals(ComponentAliasTargetMap? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || _targets.Count != other._targets.Count)
        {
            return false;
        }

        foreach (var pair in _targets)
        {
            if (!other._targets.TryGetValue(pair.Key, out var otherTarget) || pair.Value != otherTarget)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
        => obj is ComponentAliasTargetMap other && Equals(other);

    public override int GetHashCode()
        => _targets.Count;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

/// <summary>
/// A value-comparable map that determines whether a file's <c>@inherits</c> base type supports
/// UTF-8 <c>WriteLiteral</c>. Uses a two-level lookup:
/// <list type="number">
///   <item>Per-file: maps <c>filePath</c> to a fully-qualified type name</item>
///   <item>Per-type: maps fully-qualified type name to <see langword="bool"/></item>
/// </list>
/// This handles cases where the same <c>@inherits</c> text resolves to different types
/// in different files (e.g., via <c>@using</c> aliases).
/// </summary>
internal sealed class Utf8SupportMap : IEquatable<Utf8SupportMap>
{
    /// <summary>
    /// Information about an <c>@inherits</c> directive extracted from a parsed document.
    /// </summary>
    internal readonly record struct InheritsInfo(string FilePath, string BaseTypeName, ImmutableArray<string> Usings);

    public static readonly Utf8SupportMap Empty = new(
        ImmutableSortedDictionary<string, string>.Empty,
        ImmutableSortedDictionary<string, bool>.Empty);

    // filePath -> fully-qualified type name
    private readonly ImmutableSortedDictionary<string, string> _fileToType;
    // fully-qualified type name -> supports UTF-8
    private readonly ImmutableSortedDictionary<string, bool> _typeSupport;

    internal Utf8SupportMap(
        ImmutableSortedDictionary<string, string> fileToType,
        ImmutableSortedDictionary<string, bool> typeSupport)
    {
        _fileToType = fileToType;
        _typeSupport = typeSupport;
    }

    /// <summary>
    /// Builds a <see cref="Utf8SupportMap"/> by resolving each file's <c>@inherits</c> to a
    /// fully-qualified type name, then checking whether each unique type supports UTF-8.
    /// </summary>
    public static Utf8SupportMap Create(ImmutableArray<InheritsInfo> inheritsInfos, Compilation compilation)
    {
        // UTF-8 string literals (e.g. "..."u8) require C# 11 or later. If the consuming
        // compilation is targeting an older C# version (or isn't C# at all), generating
        // u8 literals would produce uncompilable code, so opt every file out by returning
        // an empty map.
        if (compilation is not CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp11 })
        {
            return Empty;
        }

        var fileToType = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        var typeSupport = ImmutableSortedDictionary.CreateBuilder<string, bool>(StringComparer.Ordinal);

        // First pass: resolve fully-qualified names via fast path, collect unresolved entries.
        using var _1 = ListPool<(int Index, InheritsInfo Info)>.GetPooledObject(out var unresolvedEntries);

        for (var i = 0; i < inheritsInfos.Length; i++)
        {
            var info = inheritsInfos[i];
            var type = compilation.GetTypeByMetadataName(info.BaseTypeName);
            if (type is not null && type.TypeKind != TypeKind.Error)
            {
                var fqn = type.GetFullName();
                fileToType[info.FilePath] = fqn;

                if (!typeSupport.ContainsKey(fqn))
                {
                    // Use the resolved symbol directly rather than round-tripping the display name
                    // back through GetTypeByMetadataName (which expects metadata syntax).
                    typeSupport[fqn] = compilation.HasCallableUtf8WriteLiteralOverload(type);
                }
            }
            else
            {
                unresolvedEntries.Add((i, info));
            }
        }

        // Second pass: resolve remaining entries via a single augmented compilation.
        if (unresolvedEntries.Count > 0 && compilation is CSharpCompilation csharpCompilation)
        {
            using var _2 = ListPool<(int Index, string Fqn)>.GetPooledObject(out var resolved);
            ResolveTypeNamesWithUsings(unresolvedEntries, csharpCompilation, resolved);
            foreach (var (index, fqn) in resolved)
            {
                var info = inheritsInfos[index];
                fileToType[info.FilePath] = fqn;

                if (!typeSupport.ContainsKey(fqn))
                {
                    typeSupport[fqn] = compilation.HasCallableUtf8WriteLiteralOverload(fqn);
                }
            }
        }

        return fileToType.Count == 0
            ? Empty
            : new Utf8SupportMap(fileToType.ToImmutable(), typeSupport.ToImmutable());
    }

    /// <summary>
    /// Resolves multiple short or partially-qualified type names in a single augmented
    /// compilation. Each entry's usings are scoped to a unique namespace block to prevent
    /// cross-contamination.
    /// </summary>
    private static void ResolveTypeNamesWithUsings(
        List<(int Index, InheritsInfo Info)> entries,
        CSharpCompilation compilation,
        List<(int Index, string Fqn)> results)
    {
        // Build a single probe tree with namespace-scoped usings for each entry.
        using var _ = StringBuilderPool.GetPooledObject(out var sb);
        for (var i = 0; i < entries.Count; i++)
        {
            var info = entries[i].Info;

            sb.Append("namespace __Utf8Probe_").Append(i).AppendLine(" {");
            foreach (var u in info.Usings)
            {
                sb.Append("    using ").Append(u).AppendLine(";");
            }

            // Alias TModel to a known type so that the common MVC pattern
            // `@inherits SomeBase<TModel>` (which is normally rewritten to the actual
            // model type by ModelDirective.Pass during code generation) still binds in
            // the probe compilation. WriteLiteral overloads don't depend on the model
            // type argument, so binding to <object> is sufficient for detection.
            sb.AppendLine("    using TModel = global::System.Object;");

            sb.Append("    class __Probe__ : ").Append(info.BaseTypeName).AppendLine(" { }");
            sb.AppendLine("}");
        }

        var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
            ?? CSharpParseOptions.Default;
        var probeText = SourceText.From(sb.ToString());
        var probeTree = CSharpSyntaxTree.ParseText(probeText, parseOptions);
        var augmented = compilation.AddSyntaxTrees(probeTree);
        var semanticModel = augmented.GetSemanticModel(probeTree);

        // Query each probe class's base type. The probe tree has a known shallow shape:
        //   CompilationUnit -> NamespaceDeclaration (one per entry, in order) -> ClassDeclaration
        // so we walk only direct children at each level rather than realizing the whole tree,
        // and rely on entry order to map back to the original index.
        var namespaceDecls = probeTree.GetRoot().ChildNodes().OfType<BaseNamespaceDeclarationSyntax>();

        var entryIndex = 0;
        foreach (var namespaceDecl in namespaceDecls)
        {
            var classDecl = namespaceDecl.ChildNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var baseTypeSyntax = classDecl?.BaseList?.Types.FirstOrDefault();
            if (baseTypeSyntax is not null)
            {
                var symbol = semanticModel.GetSymbolInfo(baseTypeSyntax.Type).Symbol as INamedTypeSymbol;
                if (symbol is not null && symbol.TypeKind != TypeKind.Error)
                {
                    results.Add((entries[entryIndex].Index, GetFullMetadataName(symbol)));
                }
            }

            entryIndex++;
        }
    }

    /// <summary>
    /// Builds a fully-qualified metadata name for a type symbol, suitable for
    /// <see cref="Compilation.GetTypeByMetadataName"/>. Unlike <c>GetFullName()</c>
    /// which produces C# display syntax, this uses CLR metadata conventions:
    /// backtick arity for generics and <c>+</c> for nested types.
    /// </summary>
    private static string GetFullMetadataName(INamedTypeSymbol symbol)
    {
        string typePart;

        if (symbol.ContainingType is null)
        {
            typePart = symbol.MetadataName;
        }
        else
        {
            // Walk containing types inner -> outer, prepending each name (and a separating '+')
            // to build the Outer`1+Inner chain.
            using var _ = StringBuilderPool.GetPooledObject(out var builder);
            builder.Append(symbol.MetadataName);
            for (var current = symbol.ContainingType; current is not null; current = current.ContainingType)
            {
                builder.Insert(0, '+').Insert(0, current.MetadataName);
            }

            typePart = builder.ToString();
        }

        return symbol.ContainingNamespace is { IsGlobalNamespace: false } ns
            ? $"{ns.GetFullName()}.{typePart}"
            : typePart;
    }

    public bool IsSupported(string? filePath, string baseTypeName)
    {
        // Create() stores keys as (Source.FilePath ?? string.Empty); normalize null the same way
        // so a document without a file path still hits its per-file entry.
        if (_fileToType.TryGetValue(filePath ?? string.Empty, out var fqn))
        {
            return _typeSupport.TryGetValue(fqn, out var supported) && supported;
        }

        // Fallback: try the raw name directly as a fully-qualified name.
        return _typeSupport.TryGetValue(baseTypeName, out var fallback) && fallback;
    }

    public bool Equals(Utf8SupportMap? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // _fileToType keys are file paths compared case-insensitively (matching the builder's
        // OrdinalIgnoreCase comparer and GetHashCode below), so compare via the dictionary's own
        // lookup rather than SequenceEqual, which would compare keys case-sensitively. _typeSupport
        // keys are ordinal fully-qualified names, so SequenceEqual is correct there.
        if (_fileToType.Count != other._fileToType.Count)
        {
            return false;
        }

        foreach (var (filePath, fqn) in _fileToType)
        {
            if (!other._fileToType.TryGetValue(filePath, out var otherFqn) ||
                !string.Equals(fqn, otherFqn, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return _typeSupport.SequenceEqual(other._typeSupport);
    }

    public override bool Equals(object? obj) => Equals(obj as Utf8SupportMap);

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        foreach (var kvp in _fileToType)
        {
            hash.Add(kvp.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(kvp.Value, StringComparer.Ordinal);
        }

        foreach (var kvp in _typeSupport)
        {
            hash.Add(kvp.Key, StringComparer.Ordinal);
            hash.Add(kvp.Value);
        }

        return hash;
    }
}

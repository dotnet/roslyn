// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

/// <summary>
/// Default implementation of <see cref="IUtf8WriteLiteralFeature"/> backed by a pre-computed
/// <see cref="SupportMap"/>. The map is set by the source generator before code generation runs.
/// </summary>
/// <remarks>
/// This type implements <see cref="IRazorEngineFeature"/> directly (rather than extending
/// <see cref="RazorEngineFeatureBase"/>) because the same instance is shared across multiple
/// per-file <see cref="RazorEngine"/> instances in the source generator, and
/// <see cref="RazorEngineFeatureBase"/> does not allow re-initialization.
/// </remarks>
internal sealed class DefaultUtf8WriteLiteralFeature : IUtf8WriteLiteralFeature
{
    private RazorEngine? _engine;

    /// <summary>
    /// Information about an <c>@inherits</c> directive extracted from a parsed document.
    /// </summary>
    internal readonly record struct InheritsInfo(string FilePath, string BaseTypeName, ImmutableArray<string> Usings);

    public RazorEngine Engine
    {
        get => _engine!;
        init => _engine = value;
    }

    public Utf8SupportMap SupportMap { get; set; } = Utf8SupportMap.Empty;

    public void Initialize(RazorEngine engine)
    {
        _engine = engine;
    }

    public bool IsSupported(string? filePath, string baseTypeName)
        => SupportMap.IsSupported(filePath, baseTypeName);

    /// <summary>
    /// A value-comparable map that determines whether a file's <c>@inherits</c> base type supports
    /// UTF-8 <c>WriteLiteral</c>. Uses a two-level lookup:
    /// <list type="number">
    ///   <item>Per-file: maps <c>(filePath, rawInheritsText)</c> to a fully-qualified type name</item>
    ///   <item>Per-type: maps fully-qualified type name to <see langword="bool"/></item>
    /// </list>
    /// This handles cases where the same <c>@inherits</c> text resolves to different types
    /// in different files (e.g., via <c>@using</c> aliases).
    /// </summary>
    internal sealed class Utf8SupportMap : IEquatable<Utf8SupportMap>
    {
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
            var fileToType = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            var typeSupport = ImmutableSortedDictionary.CreateBuilder<string, bool>(StringComparer.Ordinal);

            // First pass: resolve fully-qualified names via fast path, collect unresolved entries.
            List<(int Index, InheritsInfo Info)>? unresolvedEntries = null;

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
                        typeSupport[fqn] = compilation.HasCallableUtf8WriteLiteralOverload(fqn);
                    }
                }
                else
                {
                    unresolvedEntries ??= [];
                    unresolvedEntries.Add((i, info));
                }
            }

            // Second pass: resolve remaining entries via a single augmented compilation.
            if (unresolvedEntries is { Count: > 0 } && compilation is CSharpCompilation csharpCompilation)
            {
                var resolved = ResolveTypeNamesWithUsings(unresolvedEntries, csharpCompilation);
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
        private static List<(int Index, string Fqn)> ResolveTypeNamesWithUsings(
            List<(int Index, InheritsInfo Info)> entries,
            CSharpCompilation compilation)
        {
            var results = new List<(int, string)>();

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

                sb.Append("    class __Probe__ : ").Append(info.BaseTypeName).AppendLine(" { }");
                sb.AppendLine("}");
            }

            var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
                ?? CSharpParseOptions.Default;
            var probeTree = CSharpSyntaxTree.ParseText(sb.ToString(), parseOptions);
            var augmented = compilation.AddSyntaxTrees(probeTree);
            var semanticModel = augmented.GetSemanticModel(probeTree);

            // Query each probe class's base type.
            var namespaceDecls = probeTree.GetRoot().DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .ToArray();

            for (var i = 0; i < namespaceDecls.Length; i++)
            {
                var classDecl = namespaceDecls[i].DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault();
                var baseTypeSyntax = classDecl?.BaseList?.Types.FirstOrDefault();
                if (baseTypeSyntax is null)
                {
                    continue;
                }

                var symbol = semanticModel.GetSymbolInfo(baseTypeSyntax.Type).Symbol as INamedTypeSymbol;
                if (symbol is not null && symbol.TypeKind != TypeKind.Error)
                {
                    results.Add((entries[i].Index, GetFullMetadataName(symbol)));
                }
            }

            return results;
        }

        /// <summary>
        /// Builds a fully-qualified metadata name for a type symbol, suitable for
        /// <see cref="Compilation.GetTypeByMetadataName"/>. Unlike <c>GetFullName()</c>
        /// which produces C# display syntax, this uses CLR metadata conventions:
        /// backtick arity for generics and <c>+</c> for nested types.
        /// </summary>
        private static string GetFullMetadataName(INamedTypeSymbol symbol)
        {
            var typePart = symbol.MetadataName;

            if (symbol.ContainingType is not null)
            {
                // Walk containing types to build Outer`1+Inner chain.
                var parts = new List<string> { typePart };
                for (var current = symbol.ContainingType; current is not null; current = current.ContainingType)
                {
                    parts.Add(current.MetadataName);
                }

                parts.Reverse();
                typePart = string.Join("+", parts);
            }

            return symbol.ContainingNamespace is { IsGlobalNamespace: false } ns
                ? $"{ns.GetFullName()}.{typePart}"
                : typePart;
        }

        public bool IsSupported(string? filePath, string baseTypeName)
        {
            if (filePath is not null && _fileToType.TryGetValue(filePath, out var fqn))
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

            return _fileToType.SequenceEqual(other._fileToType) &&
                   _typeSupport.SequenceEqual(other._typeSupport);
        }

        public override bool Equals(object? obj) => Equals(obj as Utf8SupportMap);

        public override int GetHashCode()
        {
            var hash = 17;

            foreach (var kvp in _fileToType)
            {
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(kvp.Key);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(kvp.Value);
            }

            foreach (var kvp in _typeSupport)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(kvp.Key);
                hash = hash * 31 + kvp.Value.GetHashCode();
            }

            return hash;
        }
    }
}

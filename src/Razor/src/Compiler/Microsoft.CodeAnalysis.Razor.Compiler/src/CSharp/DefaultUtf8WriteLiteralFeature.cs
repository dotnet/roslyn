// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
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

            foreach (var info in inheritsInfos)
            {
                var filePath = info.FilePath;
                var baseTypeName = info.BaseTypeName;

                // Fast path: try fully-qualified metadata name lookup.
                var type = compilation.GetTypeByMetadataName(baseTypeName);
                string? fqn;
                if (type is null || type.TypeKind == TypeKind.Error)
                {
                    // Slow path: use the document's @using directives to resolve short names
                    // via an augmented compilation.
                    fqn = ResolveTypeNameWithUsings(baseTypeName, info.Usings, compilation);
                }
                else
                {
                    fqn = type.GetFullName();
                }

                if (fqn is null)
                {
                    continue;
                }

                fileToType[filePath] = fqn;

                if (!typeSupport.ContainsKey(fqn))
                {
                    typeSupport[fqn] = compilation.HasCallableUtf8WriteLiteralOverload(fqn);
                }
            }

            return fileToType.Count == 0
                ? Empty
                : new Utf8SupportMap(fileToType.ToImmutable(), typeSupport.ToImmutable());
        }

        /// <summary>
        /// Resolves a short or partially-qualified type name to a fully-qualified metadata name
        /// using the document's <c>@using</c> directives and an augmented compilation.
        /// </summary>
        private static string? ResolveTypeNameWithUsings(
            string typeName,
            ImmutableArray<string> usings,
            Compilation compilation)
        {
            if (compilation is not CSharpCompilation csharpCompilation || usings.IsEmpty)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var u in usings)
            {
                sb.Append("using ").Append(u).AppendLine(";");
            }

            sb.Append("class __Utf8Probe__ : ").Append(typeName).AppendLine(" { }");

            var parseOptions = csharpCompilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
                ?? CSharpParseOptions.Default;
            var probeTree = CSharpSyntaxTree.ParseText(sb.ToString(), parseOptions);

            var augmented = csharpCompilation.AddSyntaxTrees(probeTree);
            var semanticModel = augmented.GetSemanticModel(probeTree);
            var classDecl = probeTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var baseTypeSyntax = classDecl?.BaseList?.Types.FirstOrDefault();

            if (baseTypeSyntax is null)
            {
                return null;
            }

            return (semanticModel.GetSymbolInfo(baseTypeSyntax.Type).Symbol as INamedTypeSymbol)?.GetFullName();
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

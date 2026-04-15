// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

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

    public bool IsSupported(string baseTypeName)
        => SupportMap.IsSupported(baseTypeName);

    /// <summary>
    /// A value-comparable map of base type names to whether they support UTF-8 <c>WriteLiteral</c>.
    /// Used to flow pre-computed results through the incremental pipeline without carrying a
    /// <see cref="Compilation"/> reference.
    /// </summary>
    internal sealed class Utf8SupportMap : IEquatable<Utf8SupportMap>
    {
        public static readonly Utf8SupportMap Empty = new(ImmutableSortedDictionary<string, bool>.Empty);

        private readonly ImmutableSortedDictionary<string, bool> _entries;

        internal Utf8SupportMap(ImmutableSortedDictionary<string, bool> entries)
        {
            _entries = entries;
        }

        /// <summary>
        /// Builds a <see cref="Utf8SupportMap"/> by checking each base type name against the compilation.
        /// Null and duplicate entries are filtered out.
        /// </summary>
        public static Utf8SupportMap Create(ImmutableArray<string?> baseTypeNames, Compilation compilation)
        {
            var builder = ImmutableSortedDictionary.CreateBuilder<string, bool>(StringComparer.Ordinal);

            foreach (var name in baseTypeNames)
            {
                if (!string.IsNullOrEmpty(name) && !builder.ContainsKey(name))
                {
                    builder[name] = compilation.HasCallableUtf8WriteLiteralOverload(name);
                }
            }

            return builder.Count == 0 ? Empty : new Utf8SupportMap(builder.ToImmutable());
        }

        public bool IsSupported(string baseTypeName)
            => _entries.TryGetValue(baseTypeName, out var supported) && supported;

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

            return _entries.SequenceEqual(other._entries);
        }

        public override bool Equals(object? obj) => Equals(obj as Utf8SupportMap);

        public override int GetHashCode()
        {
            var hash = 17;

            foreach (var kvp in _entries)
            {
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(kvp.Key);
                hash = hash * 31 + kvp.Value.GetHashCode();
            }

            return hash;
        }
    }
}

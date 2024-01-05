// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// <para>
    /// A <see cref="SymbolKey"/> is a lightweight identifier for a symbol that can be used to 
    /// resolve the "same" symbol across compilations.  Different symbols have different concepts 
    /// of "same-ness". Same-ness is recursively defined as follows:
    /// <list type="number">
    ///   <item>Two <see cref="IArrayTypeSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="IArrayTypeSymbol.ElementType"/> and 
    ///         equal <see cref="IArrayTypeSymbol.Rank"/>.</item>
    ///   <item>Two <see cref="IAssemblySymbol"/>s are the "same" if 
    ///         they have equal <see cref="IAssemblySymbol.Identity"/>.Name</item>
    ///   <item>Two <see cref="IEventSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingType"/> and 
    ///         equal <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two <see cref="IMethodSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingType"/>,
    ///         equal <see cref="ISymbol.MetadataName"/>,
    ///         equal <see cref="IMethodSymbol.Arity"/>, 
    ///         the "same" <see cref="IMethodSymbol.TypeArguments"/>, and have
    ///         the "same" <see cref="IParameterSymbol.Type"/>s and  
    ///         equal <see cref="IParameterSymbol.RefKind"/>s.</item>
    ///   <item>Two <see cref="IModuleSymbol"/>s are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingAssembly"/>.
    ///         <see cref="ISymbol.MetadataName"/> is not used because module identity is not important in practice.</item>
    ///   <item>Two <see cref="INamedTypeSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/>,
    ///         equal <see cref="ISymbol.MetadataName"/>,
    ///         equal <see cref="INamedTypeSymbol.Arity"/> and 
    ///         the "same" <see cref="INamedTypeSymbol.TypeArguments"/>.</item>
    ///   <item>Two <see cref="INamespaceSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and
    ///         equal <see cref="ISymbol.MetadataName"/>.
    ///     If the <see cref="INamespaceSymbol"/> is the global namespace for a
    ///     compilation, then it will only match another
    ///     global namespace of another compilation.</item>
    ///   <item>Two <see cref="IParameterSymbol"/>s are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and 
    ///         equal <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two <see cref="IPointerTypeSymbol"/>s are the "same" if they have 
    ///         the "same" <see cref="IPointerTypeSymbol.PointedAtType"/>.</item>
    ///   <item>Two <see cref="IPropertySymbol"/>s are the "same" if they have 
    ///         the "same" the "same" <see cref="ISymbol.ContainingType"/>, 
    ///         the "same" <see cref="ISymbol.MetadataName"/>, and have 
    ///         the "same" <see cref="IParameterSymbol.Type"/>s and  
    ///         the "same" <see cref="IParameterSymbol.RefKind"/>s.</item>
    ///   <item>Two <see cref="ITypeParameterSymbol"/> are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and 
    ///         the "same" <see cref="ISymbol.MetadataName"/>.</item>
    ///   <item>Two <see cref="IFieldSymbol"/>s are the "same" if they have
    ///         the "same" <see cref="ISymbol.ContainingSymbol"/> and 
    ///         the "same" <see cref="ISymbol.MetadataName"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Interior-method-level symbols (i.e. <see cref="ILabelSymbol"/>, <see cref="ILocalSymbol"/>, <see
    /// cref="IRangeVariableSymbol"/> and <see cref="MethodKind.LocalFunction"/> <see cref="IMethodSymbol"/>s can also
    /// be represented and restored in a different compilation.  To resolve these the destination compilation's <see
    /// cref="SyntaxTree"/> is enumerated to list all the symbols with the same <see cref="ISymbol.Name"/> and <see
    /// cref="ISymbol.Kind"/> as the original symbol.  The symbol with the same index in the destination tree as the
    /// symbol in the original tree is returned.  This allows these sorts of symbols to be resolved in a way that is
    /// resilient to basic forms of edits.  For example, adding whitespace edits, or adding removing symbols with
    /// different names and types.  However, it may not find a matching symbol in the face of other sorts of edits.
    /// </para>
    /// <para>
    /// Symbol keys cannot be created for interior-method symbols that were created in a speculative semantic model.
    /// </para>
    /// <para>
    ///     Due to issues arising from errors and ambiguity, it's possible for a SymbolKey to resolve to
    ///     multiple symbols. For example, in the following type:
    ///     <code>
    ///     class C
    ///     {
    ///        int M();
    ///        bool M();
    ///     }
    ///     </code>
    ///     The SymbolKey for both 'M' methods will be the same.  The SymbolKey will then resolve to both methods.
    /// </para>
    /// <para>
    /// <see cref="SymbolKey"/>s are not guaranteed to work across different versions of Roslyn. They can be persisted
    /// in their <see cref="ToString()"/> form and used across sessions with the same version of Roslyn. However, future
    /// versions may change the encoded format and may no longer be able to <see cref="Resolve"/> previous keys.  As
    /// such, only persist if using for a cache that can be regenerated if necessary.
    /// </para>
    /// <para>
    /// The string values produced by <see cref="CreateString"/> (or <see cref="SymbolKey.ToString"/>) should not be
    /// directly compared for equality or used in hashing scenarios.  Specifically, two symbol keys which represent the
    /// 'same' symbol might produce different strings.  Instead, to compare keys use <see cref="SymbolKey.GetComparer"/>
    /// to get a suitable comparer that exposes the desired semantics.
    /// </para>
    /// </summary>
    [DataContract]
    internal partial struct SymbolKey(string data) : IEquatable<SymbolKey>
    {
        /// <summary>
        /// Current format version.  Any time we change anything about our format, we should
        /// change this.  This will help us detect and reject any cases where a person serializes
        /// out a SymbolKey from a previous version of Roslyn and then attempt to use it in a 
        /// newer version where the encoding has changed.
        /// </summary>
        internal const int FormatVersion = 7;

        [DataMember(Order = 0)]
        private readonly string _symbolKeyData = data ?? throw new ArgumentNullException(nameof(data));

        /// <summary>
        /// Constructs a new <see cref="SymbolKey"/> representing the provided <paramref name="symbol"/>.
        /// </summary>
        public static SymbolKey Create(ISymbol? symbol, CancellationToken cancellationToken = default)
            => new(CreateString(symbol, cancellationToken));

        /// <summary>
        /// Returns an <see cref="IEqualityComparer{T}"/> that determines if two <see cref="SymbolKey"/>s
        /// represent the same effective symbol.
        /// </summary>
        /// <param name="ignoreCase">Whether or not casing should be considered when comparing keys. 
        /// For example, with <c>ignoreCase=true</c> then <c>X.SomeClass</c> and <c>X.Someclass</c> would be 
        /// considered the same effective symbol</param>
        /// <param name="ignoreAssemblyKeys">Whether or not the originating assembly of referenced
        /// symbols should be compared when determining if two symbols are effectively the same.
        /// For example, with <c>ignoreAssemblyKeys=true</c> then an <c>X.SomeClass</c> from assembly 
        /// <c>A</c> and <c>X.SomeClass</c> from assembly <c>B</c> will be considered the same
        /// effective symbol.
        /// </param>
        public static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase = false, bool ignoreAssemblyKeys = false)
            => SymbolKeyComparer.GetComparer(ignoreCase, ignoreAssemblyKeys);

        public static bool CanCreate(ISymbol symbol, CancellationToken cancellationToken)
        {
            if (IsBodyLevelSymbol(symbol))
            {
                var locations = BodyLevelSymbolKey.GetBodyLevelSourceLocations(symbol, cancellationToken);
                if (locations.Length == 0)
                    return false;

                // Ensure that the tree we're looking at is actually in this compilation.  It may not be in the
                // compilation in the case of work done with a speculative model.
                var compilation = ((ISourceAssemblySymbol)symbol.ContainingAssembly).Compilation;
                return compilation.SyntaxTrees.Contains(locations.First().SourceTree);
            }

            return true;
        }

        public static SymbolKeyResolution ResolveString(
            string symbolKey, Compilation compilation,
            bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default)
        {
            return ResolveString(symbolKey, compilation, ignoreAssemblyKey, out _, cancellationToken);
        }

        public static SymbolKeyResolution ResolveString(
            string symbolKey, Compilation compilation,
            out string? failureReason, CancellationToken cancellationToken)
        {
            return ResolveString(symbolKey, compilation, ignoreAssemblyKey: false, out failureReason, cancellationToken);
        }

        public static SymbolKeyResolution ResolveString(
            string symbolKey, Compilation compilation, bool ignoreAssemblyKey,
            out string? failureReason, CancellationToken cancellationToken)
        {
            using var reader = SymbolKeyReader.GetReader(
                symbolKey, compilation, ignoreAssemblyKey, cancellationToken);
            var version = reader.ReadFormatVersion();
            if (version != FormatVersion)
            {
                failureReason = $"({nameof(SymbolKey)} invalid format '${version}')";
                return default;
            }

            // Read out the language info which was included just for diagnostic purposes.
            var language = reader.ReadString();

            // Initial entrypoint.  No contextual symbol to pass along.
            var result = reader.ReadSymbolKey(contextualSymbol: null, out failureReason);
            Debug.Assert(reader.Position == symbolKey.Length);
            return result;
        }

        public static string CreateString(ISymbol? symbol, CancellationToken cancellationToken = default)
            => CreateStringWorker(FormatVersion, symbol, cancellationToken);

        // Internal for testing purposes.
        internal static string CreateStringWorker(int version, ISymbol? symbol, CancellationToken cancellationToken = default)
        {
            using var writer = SymbolKeyWriter.GetWriter(cancellationToken);
            writer.WriteFormatVersion(version);

            // include the language just for help diagnosing issues.  Note: the language is not considered part of the
            // 'value' of the key.  In other words two keys that represent the same symbol (like 'System.Int32'), but
            // which differ on language, will still be considered equal. to each other.
            writer.WriteString(symbol?.Language);

            writer.WriteSymbolKey(symbol);
            return writer.CreateKey();
        }

        /// <summary>
        /// Tries to resolve this <see cref="SymbolKey"/> in the given 
        /// <paramref name="compilation"/> to a matching symbol.
        /// </summary>
        public readonly SymbolKeyResolution Resolve(
            Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default)
        {
            return ResolveString(_symbolKeyData, compilation, ignoreAssemblyKey, cancellationToken);
        }

        /// <summary>
        /// Returns this <see cref="SymbolKey"/> encoded as a string.  This can be persisted
        /// and used later with <see cref="SymbolKey(string)"/> to then try to resolve back
        /// to the corresponding <see cref="ISymbol"/> in a future <see cref="Compilation"/>.
        /// 
        /// This string form is not guaranteed to be reusable across all future versions of 
        /// Roslyn.  As such it should only be used for caching data, with the knowledge that
        /// the data may need to be recomputed if the cached data can no longer be used.
        /// </summary>
        public override readonly string ToString()
            => _symbolKeyData;

        private static SymbolKeyResolution CreateResolution<TSymbol>(
            PooledArrayBuilder<TSymbol> symbols, string reasonIfFailed, out string? failureReason)
            where TSymbol : class, ISymbol
        {
            if (symbols.Builder.Count == 0)
            {
                failureReason = reasonIfFailed;
                return default;
            }
            else if (symbols.Builder.Count == 1)
            {
                failureReason = null;
                return new SymbolKeyResolution(symbols.Builder[0]);
            }
            else
            {
                failureReason = null;
                return new SymbolKeyResolution(
                    ImmutableArray<ISymbol>.CastUp(symbols.Builder.ToImmutable()),
                    CandidateReason.Ambiguous);
            }
        }

        private static T? SafeGet<T>(ImmutableArray<T> values, int index) where T : class
            => index < values.Length ? values[index] : null;

        private static bool Equals(Compilation compilation, string? name1, string? name2)
            => Equals(compilation.IsCaseSensitive, name1, name2);

        private static bool Equals(bool isCaseSensitive, string? name1, string? name2)
            => string.Equals(name1, name2, isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        private static bool ParameterRefKindsMatch(
            ImmutableArray<IParameterSymbol> parameters,
            PooledArrayBuilder<RefKind> refKinds)
        {
            if (parameters.Length != refKinds.Count)
                return false;

            for (var i = 0; i < refKinds.Count; i++)
            {
                // The ref-out distinction is not interesting for SymbolKey because you can't overload
                // based on the difference.
                if (!SymbolEquivalenceComparer.AreRefKindsEquivalent(
                        refKinds[i], parameters[i].RefKind, distinguishRefFromOut: false))
                {
                    return false;
                }
            }

            return true;
        }

        private static PooledArrayBuilder<TSymbol> GetMembersOfNamedType<TSymbol>(
            SymbolKeyResolution containingTypeResolution,
            string? metadataName) where TSymbol : ISymbol
        {
            var result = PooledArrayBuilder<TSymbol>.GetInstance();
            foreach (var containingType in containingTypeResolution.OfType<INamedTypeSymbol>())
            {
                var members = metadataName == null
                    ? containingType.GetMembers()
                    : containingType.GetMembers(metadataName);

                foreach (var member in members)
                {
                    if (member is TSymbol symbol)
                        result.AddIfNotNull(symbol);
                }
            }

            return result;
        }

        public static bool IsBodyLevelSymbol(ISymbol symbol)
            => symbol switch
            {
                ILabelSymbol => true,
                IRangeVariableSymbol => true,
                ILocalSymbol => true,
                IMethodSymbol { MethodKind: MethodKind.LocalFunction } => true,
                _ => false,
            };

        private static int GetDataStartPosition(string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            using var reader = SymbolKeyReader.GetReader(key, compilation: null!, ignoreAssemblyKey: false, CancellationToken.None);
            _ = reader.ReadFormatVersion();
            _ = reader.ReadString();
            return reader.Position;
        }

        public override readonly int GetHashCode()
        {
            var position = GetDataStartPosition(_symbolKeyData);

#if NETSTANDARD
            var hashCode = 0;
            foreach (var ch in _symbolKeyData[position..])
                hashCode = Hash.Combine(ch, hashCode);

            return hashCode;
#else
            return string.GetHashCode(_symbolKeyData.AsSpan(position));
#endif
        }

        public override readonly bool Equals(object? obj)
            => obj is SymbolKey symbolKey && this.Equals(symbolKey);

        public readonly bool Equals(SymbolKey other)
            => Equals(other, ignoreCase: false);

        private readonly bool Equals(SymbolKey other, bool ignoreCase)
        {
            var position1 = GetDataStartPosition(_symbolKeyData);
            var position2 = GetDataStartPosition(other._symbolKeyData);

            var keySpan1 = _symbolKeyData.AsSpan(position1);
            var keySpan2 = other._symbolKeyData.AsSpan(position2);

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return keySpan1.Equals(keySpan2, comparison);
        }
    }
}

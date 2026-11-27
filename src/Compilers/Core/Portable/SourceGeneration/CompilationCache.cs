// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Caches the compilation produced by the pre-compilation phase, keyed on the inputs that
    /// determine its content. When a subsequent run produces the same inputs the cached
    /// <see cref="Compilation"/> reference is reused, which preserves reference-equality on
    /// <c>SharedInputNodes.Compilation</c> and therefore keeps every generator's
    /// <c>CompilationProvider</c>-derived caching valid across runs.
    /// </summary>
    /// <remarks>
    /// Always present on <see cref="GeneratorDriverState"/> (initially <see cref="Empty"/>).
    /// Driver code accumulates the run's inputs into a <see cref="Builder"/> obtained via
    /// <see cref="ToBuilder"/>, then calls <see cref="Builder.ToImmutableAndFree"/> to obtain
    /// either this same instance (cache hit) or a new one (cache miss / no caching needed).
    /// The result's <see cref="Compilation"/> is always the compilation the driver should pass
    /// to standard-phase generators.
    /// </remarks>
    internal sealed class CompilationCache
    {
        /// <summary>
        /// Initial sentinel used as the seed before any driver run has populated the cache.
        /// Its <see cref="Compilation"/> is <c>null</c> and is never read by the driver, since
        /// every run replaces the seed via <see cref="Builder.ToImmutableAndFree"/> before reading.
        /// </summary>
        public static readonly CompilationCache Empty = new();

        private readonly Compilation? _compilation;
        private readonly Compilation? _inputCompilation;
        private readonly ImmutableArray<SyntaxTree> _postInitTrees;
        private readonly ImmutableArray<PreCompCacheKey> _preCompKeys;

        private CompilationCache() { }

        private CompilationCache(
            Compilation compilation,
            Compilation inputCompilation,
            ImmutableArray<SyntaxTree> postInitTrees,
            ImmutableArray<PreCompCacheKey> preCompKeys)
        {
            _compilation = compilation;
            _inputCompilation = inputCompilation;
            _postInitTrees = postInitTrees;
            _preCompKeys = preCompKeys;
        }

        /// <summary>
        /// The compilation the driver should pass to standard-phase generators. Only valid to
        /// read after a <see cref="Builder.ToImmutableAndFree"/> call has produced this cache;
        /// reading it on the <see cref="Empty"/> sentinel will assert.
        /// </summary>
        public Compilation Compilation
        {
            get
            {
                Debug.Assert(_compilation is not null, $"{nameof(Compilation)} read before any builder produced this cache.");
                return _compilation;
            }
        }

        /// <summary>
        /// Starts accumulating inputs for the current run. The builder takes the input
        /// compilation reference (used for cache identity) and the compilation after post-init
        /// trees have been added (used as the base for any pre-compilation augmentation).
        /// </summary>
        public Builder ToBuilder(Compilation inputCompilation, Compilation compilationWithPostInit)
            => new Builder(this, inputCompilation, compilationWithPostInit);

        internal sealed class Builder
        {
            private readonly CompilationCache _previous;
            private readonly Compilation _inputCompilation;
            private readonly Compilation _compilationWithPostInit;
            private readonly ArrayBuilder<SyntaxTree> _postInitTrees = ArrayBuilder<SyntaxTree>.GetInstance();
            private readonly ArrayBuilder<PreCompCacheKey> _preCompKeys = ArrayBuilder<PreCompCacheKey>.GetInstance();
            private readonly ArrayBuilder<SyntaxTree> _preCompTreesToAdd = ArrayBuilder<SyntaxTree>.GetInstance();

            internal Builder(CompilationCache previous, Compilation inputCompilation, Compilation compilationWithPostInit)
            {
                _previous = previous;
                _inputCompilation = inputCompilation;
                _compilationWithPostInit = compilationWithPostInit;
            }

            /// <summary>
            /// Records a post-init <see cref="SyntaxTree"/> in driver order. The flat list of
            /// post-init trees participates in the cache key.
            /// </summary>
            public void AddPostInitTree(SyntaxTree tree) => _postInitTrees.Add(tree);

            /// <summary>
            /// Records a pre-compilation tree: contributes to the cache key (via generator
            /// index, hint name, text reference, and parse options) and to the set of trees
            /// appended to <c>compilationWithPostInit</c> on a cache miss.
            /// </summary>
            public void AddPreCompTree(int generatorIndex, GeneratedSyntaxTree tree)
            {
                _preCompKeys.Add(new PreCompCacheKey(generatorIndex, tree.HintName, tree.Text, tree.Tree.Options));
                _preCompTreesToAdd.Add(tree.Tree);
            }

            /// <summary>
            /// Decides between reusing the previous run's cache (when its inputs match) or
            /// producing a new entry. Always returns a cache whose <see cref="CompilationCache.Compilation"/>
            /// is the compilation the driver should use; reference equality with the source
            /// <see cref="CompilationCache"/> indicates the cache state is unchanged.
            /// </summary>
            public CompilationCache ToImmutableAndFree()
            {
                var preCompKeys = _preCompKeys.ToImmutableAndFree();
                var postInitTrees = _postInitTrees.ToImmutableAndFree();
                var preCompTreesToAdd = _preCompTreesToAdd.ToImmutableAndFree();

                if (preCompKeys.IsEmpty)
                {
                    // No pre-compilation contributions this run. Return a fresh cache holding
                    // just compilationWithPostInit; we deliberately don't attempt to reuse
                    // _previous here because that compilation reference is regenerated every
                    // run (post-init AddSyntaxTrees) and standard-phase consumers expect to
                    // see that fresh reference to preserve their own re-execution semantics.
                    return new CompilationCache(_compilationWithPostInit, _inputCompilation, ImmutableArray<SyntaxTree>.Empty, ImmutableArray<PreCompCacheKey>.Empty);
                }

                if (_previous._compilation is not null
                    && ReferenceEquals(_previous._inputCompilation, _inputCompilation)
                    && ReferenceSequenceEqual(_previous._postInitTrees, postInitTrees)
                    && _previous._preCompKeys.AsSpan().SequenceEqual(preCompKeys.AsSpan()))
                {
                    // Cache hit -- reuse the previous run's compilation reference. This makes
                    // SharedInputNodes.Compilation report Cached for every generator, which is
                    // the whole point of this cache.
                    return _previous;
                }

                var newCompilation = preCompTreesToAdd.IsEmpty
                    ? _compilationWithPostInit
                    : _compilationWithPostInit.AddSyntaxTrees(preCompTreesToAdd);
                return new CompilationCache(newCompilation, _inputCompilation, postInitTrees, preCompKeys);
            }

            // Reference-equality SequenceEqual for ImmutableArray<T> where T is a class.
            // ReferenceEqualityComparer is IEqualityComparer<object?>, so it can't be passed to
            // ImmutableArray<T>.SequenceEqual's IEqualityComparer<T> parameter directly; this
            // walks the underlying buffers via spans for an allocation-free comparison.
            private static bool ReferenceSequenceEqual<T>(ImmutableArray<T> a, ImmutableArray<T> b) where T : class
            {
                if (a.Length != b.Length)
                {
                    return false;
                }
                var aSpan = a.AsSpan();
                var bSpan = b.AsSpan();
                for (int i = 0; i < aSpan.Length; i++)
                {
                    if (!ReferenceEquals(aSpan[i], bSpan[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Identity for a single pre-compilation source as fed into the compilation cache.
    /// Equality is reference-based on <see cref="Text"/> and <see cref="Options"/>; this matches
    /// what <c>AbstractSourceOutputNode</c> already does — when the pre-compilation output's
    /// inputs are unchanged it returns the previous <see cref="GeneratedSourceText"/> with the
    /// same <see cref="SourceText"/> reference, so reference equality is the right level here.
    /// </summary>
    internal readonly struct PreCompCacheKey : IEquatable<PreCompCacheKey>
    {
        public PreCompCacheKey(int generatorIndex, string hintName, SourceText text, ParseOptions options)
        {
            GeneratorIndex = generatorIndex;
            HintName = hintName;
            Text = text;
            Options = options;
        }

        public int GeneratorIndex { get; }
        public string HintName { get; }
        public SourceText Text { get; }
        public ParseOptions Options { get; }

        public bool Equals(PreCompCacheKey other) =>
            GeneratorIndex == other.GeneratorIndex
            && string.Equals(HintName, other.HintName, StringComparison.OrdinalIgnoreCase)
            && ReferenceEquals(Text, other.Text)
            && ReferenceEquals(Options, other.Options);

        public override bool Equals(object? obj) => obj is PreCompCacheKey k && Equals(k);

        public override int GetHashCode()
            => Roslyn.Utilities.Hash.Combine(
                GeneratorIndex,
                Roslyn.Utilities.Hash.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(HintName),
                    Roslyn.Utilities.Hash.Combine(
                        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Text),
                        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Options))));
    }
}

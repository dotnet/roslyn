// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

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
        private readonly ImmutableArray<SyntaxTree> _preCompTrees;

        private CompilationCache() { }

        private CompilationCache(
            Compilation compilation,
            Compilation inputCompilation,
            ImmutableArray<SyntaxTree> postInitTrees,
            ImmutableArray<SyntaxTree> preCompTrees)
        {
            _compilation = compilation;
            _inputCompilation = inputCompilation;
            _postInitTrees = postInitTrees;
            _preCompTrees = preCompTrees;
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
        /// compilation reference (used for cache identity) and the compilation after every
        /// generator's current post-init trees have been added.
        /// </summary>
        public Builder ToBuilder(Compilation inputCompilation, Compilation compilationWithPostInit)
            => new Builder(this, inputCompilation, compilationWithPostInit);

        internal sealed class Builder
        {
            private readonly CompilationCache _previous;
            private readonly Compilation _inputCompilation;
            private readonly Compilation _compilationWithPostInit;
            private readonly ArrayBuilder<SyntaxTree> _postInitTrees = ArrayBuilder<SyntaxTree>.GetInstance();
            private readonly ArrayBuilder<SyntaxTree> _preCompTreesToAdd = ArrayBuilder<SyntaxTree>.GetInstance();

            internal Builder(CompilationCache previous, Compilation inputCompilation, Compilation compilationWithPostInit)
            {
                _previous = previous;
                _inputCompilation = inputCompilation;
                _compilationWithPostInit = compilationWithPostInit;
            }

            /// <summary>
            /// Records a post-init <see cref="SyntaxTree"/> in driver order. The flat list of
            /// post-init trees participates in the cache key and matches the trees already added
            /// to the compilation supplied to the builder.
            /// </summary>
            public void AddPostInitTree(SyntaxTree tree) => _postInitTrees.Add(tree);

            /// <summary>
            /// Records a pre-compilation tree in driver order. Its reference participates in the
            /// cache key and it is added after post-init trees on a cache miss.
            /// </summary>
            public void AddPreCompTree(SyntaxTree tree) => _preCompTreesToAdd.Add(tree);

            /// <summary>
            /// Decides between reusing the previous run's cache (when its inputs match) or
            /// producing a new entry. Always returns a cache whose <see cref="CompilationCache.Compilation"/>
            /// is the compilation the driver should use; reference equality with the source
            /// <see cref="CompilationCache"/> indicates the cache state is unchanged.
            /// </summary>
            public CompilationCache ToImmutableAndFree()
            {
                var postInitTrees = _postInitTrees.ToImmutableAndFree();
                var preCompTreesToAdd = _preCompTreesToAdd.ToImmutableAndFree();

                if (preCompTreesToAdd.IsEmpty)
                {
                    // No pre-compilation contributions this run. Return a fresh cache holding
                    // just compilationWithPostInit; we deliberately don't attempt to reuse
                    // _previous here because that compilation reference is regenerated every
                    // run (post-init AddSyntaxTrees) and standard-phase consumers expect to
                    // see that fresh reference to preserve their own re-execution semantics.
                    return new CompilationCache(_compilationWithPostInit, _inputCompilation, ImmutableArray<SyntaxTree>.Empty, ImmutableArray<SyntaxTree>.Empty);
                }

                if (_previous._compilation is not null
                    && ReferenceEquals(_previous._inputCompilation, _inputCompilation)
                    && ReferenceSequenceEqual(_previous._postInitTrees, postInitTrees)
                    && ReferenceSequenceEqual(_previous._preCompTrees, preCompTreesToAdd))
                {
                    // Cache hit -- reuse the previous run's compilation reference. This makes
                    // SharedInputNodes.Compilation report Cached for every generator, which is
                    // the whole point of this cache.
                    return _previous;
                }

                var newCompilation = _compilationWithPostInit.AddSyntaxTrees(preCompTreesToAdd);
                return new CompilationCache(newCompilation, _inputCompilation, postInitTrees, preCompTreesToAdd);
            }

            // Compare ordered tree identity without allocating an enumerator.
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

}

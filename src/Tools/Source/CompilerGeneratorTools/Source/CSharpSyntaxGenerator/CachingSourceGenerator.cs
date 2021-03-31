// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We only build the Source Generator in the netstandard target
#if NETSTANDARD

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CSharpSyntaxGenerator
{
    public abstract class CachingSourceGenerator : ISourceGenerator
    {
        /// <summary>
        /// ⚠ This value may be accessed by multiple threads.
        /// </summary>
        private static readonly WeakReference<CachedSourceGeneratorResult> s_cachedResult = new(null);

        protected abstract bool TryGetRelevantInput(in GeneratorExecutionContext context, out AdditionalText? input, out SourceText? inputText);

        protected abstract bool TryGenerateSources(
            AdditionalText input,
            SourceText inputText,
            out ImmutableArray<(string hintName, SourceText sourceText)> sources,
            out ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken);

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!TryGetRelevantInput(in context, out var input, out var inputText))
            {
                return;
            }

            // Get the current input checksum, which will either be used for verifying the current cache or updating it
            // with the new results.
            var currentChecksum = inputText.GetChecksum();

            // Read the current cached result once to avoid race conditions
            if (s_cachedResult.TryGetTarget(out var cachedResult)
                && cachedResult.Checksum.SequenceEqual(currentChecksum))
            {
                // Add the previously-cached sources, and leave the cache as it was
                AddSources(in context, sources: cachedResult.Sources);
                return;
            }

            if (TryGenerateSources(input, inputText, out var sources, out var diagnostics, context.CancellationToken))
            {
                AddSources(in context, sources);

                if (diagnostics.IsEmpty)
                {
                    var result = new CachedSourceGeneratorResult(currentChecksum, sources);

                    // Default Large Object Heap size threshold
                    // https://github.com/dotnet/runtime/blob/c9d69e38d0e54bea5d188593ef6c3b30139f3ab1/src/coreclr/src/gc/gc.h#L111
                    const int Threshold = 85000;

                    // Overwrite the cached result with the new result. This is an opportunistic cache, so as long as
                    // the write is atomic (which it is for SetTarget) synchronization is unnecessary. We allocate an
                    // array on the Large Object Heap (which is always part of Generation 2) and give it a reference to
                    // the cached object to ensure this weak reference is not reclaimed prior to a full GC pass.
                    var largeArray = new CachedSourceGeneratorResult[Threshold / Unsafe.SizeOf<CachedSourceGeneratorResult>()];
                    Debug.Assert(GC.GetGeneration(largeArray) >= 2);
                    largeArray[0] = result;
                    s_cachedResult.SetTarget(result);
                    GC.KeepAlive(largeArray);
                }
                else
                {
                    // Invalidate the cache since we cannot currently cache diagnostics
                    s_cachedResult.SetTarget(null);
                }
            }
            else
            {
                // Invalidate the cache since generation failed
                s_cachedResult.SetTarget(null);
            }

            // Always report the diagnostics (if any)
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AddSources(
            in GeneratorExecutionContext context,
            ImmutableArray<(string hintName, SourceText sourceText)> sources)
        {
            foreach (var (hintName, sourceText) in sources)
            {
                context.AddSource(hintName, sourceText);
            }
        }

        private sealed record CachedSourceGeneratorResult(
            ImmutableArray<byte> Checksum,
            ImmutableArray<(string hintName, SourceText sourceText)> Sources);
    }
}

#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for diagnostic analyzers.
    /// </summary>
    public abstract class DiagnosticAnalyzer
    {
        /// <summary>
        /// This field caches the result of <see cref="AnalysisContext.ConfigureGeneratedCodeAnalysis"/> for the first
        /// initialization containing at least one registered callback.
        /// </summary>
        /// <value>
        /// The value is a <see cref="GeneratedCodeAnalysisFlags"/> stored as an integer, or -1 if the value has not
        /// been assigned.
        /// </value>
        private int _generatedCodeAnalysisFlags = -1;

        /// <summary>
        /// This collection is typically <see langword="null"/>. For cases where code analysis flags for a compilation
        /// differ from <see cref="_generatedCodeAnalysisFlags"/> (only occurs if an analyzer does not consistently
        /// report this flag), this collection is created to hold the differing values.
        /// </summary>
        private ConditionalWeakTable<Compilation, StrongBox<GeneratedCodeAnalysisFlags>>? _generatedCodeAnalysisFlagsOverride;

        /// <summary>
        /// Returns a set of descriptors for the diagnostics that this analyzer is capable of producing.
        /// </summary>
        public abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        /// <summary>
        /// Called once at session start to register actions in the analysis context.
        /// </summary>
        /// <param name="context"></param>
        public abstract void Initialize(AnalysisContext context);

        internal void SetGeneratedCodeAnalysisFlags(Compilation compilation, GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
        {
            // Set _generatedCodeAnalysisFlags to the new value if it does not already have a value. The resulting
            // value can be:
            //
            //  -1: The value was not initialized prior to this call, but is now initialized and matches the current flags.
            //  generatedCodeAnalysisFlags: The value was already initialized prior to this call, and matches the current flags.
            //  other: The value was initialized prior to this call, and does not match the current flags. An override is required.
            var commonFlags = Interlocked.CompareExchange(ref _generatedCodeAnalysisFlags, (int)generatedCodeAnalysisFlags, -1);
            if (commonFlags == -1 || commonFlags == (int)generatedCodeAnalysisFlags)
            {
                return;
            }

            RoslynLazyInitializer.EnsureInitialized(ref _generatedCodeAnalysisFlagsOverride, static () => new ConditionalWeakTable<Compilation, StrongBox<GeneratedCodeAnalysisFlags>>());

            // SetGeneratedCodeAnalysisFlags is only allowed to be called once for a given compilation.
            _generatedCodeAnalysisFlagsOverride.Add(compilation, new StrongBox<GeneratedCodeAnalysisFlags>(generatedCodeAnalysisFlags));
        }

        internal GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags(Compilation compilation)
        {
            if (_generatedCodeAnalysisFlagsOverride is not null
                && _generatedCodeAnalysisFlagsOverride.TryGetValue(compilation, out var flags))
            {
                return flags.Value;
            }

            if (_generatedCodeAnalysisFlags == -1)
            {
                Debug.Fail("Expected the flags to be set for a compilation before checking them.");
                return AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags;
            }

            return (GeneratedCodeAnalysisFlags)_generatedCodeAnalysisFlags;
        }

        public sealed override bool Equals(object? obj)
        {
            return (object?)this == obj;
        }

        public sealed override int GetHashCode()
        {
            return ReferenceEqualityComparer.GetHashCode(this);
        }

        public sealed override string ToString()
        {
            return this.GetType().ToString();
        }
    }
}

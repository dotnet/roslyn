// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the results of running a generation pass over a set of <see cref="ISourceGenerator"/>s.
    /// </summary>
    public class GeneratorDriverRunResult
    {
        private ImmutableArray<Diagnostic> _lazyDiagnostics;

        private ImmutableArray<SyntaxTree> _lazyGeneratedTrees;

        internal GeneratorDriverRunResult(ImmutableArray<GeneratorRunResult> results, TimeSpan elapsedTime)
        {
            this.Results = results;
            ElapsedTime = elapsedTime;
        }

        /// <summary>
        /// The individual result of each <see cref="ISourceGenerator"/> that was run in this generator pass, one per generator.
        /// </summary>
        public ImmutableArray<GeneratorRunResult> Results { get; }

        /// <summary>
        /// The wall clock time that this generator pass took to execute.
        /// </summary>
        internal TimeSpan ElapsedTime { get; }

        /// <summary>
        /// The <see cref="Diagnostic"/>s produced by all generators run during this generation pass.
        /// </summary>
        /// <remarks>
        /// This is equivalent to the union of all <see cref="GeneratorRunResult.Diagnostics"/> in <see cref="Results"/>.
        /// </remarks>
        public ImmutableArray<Diagnostic> Diagnostics
        {
            get
            {
                if (_lazyDiagnostics.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDiagnostics, Results.Where(r => !r.Diagnostics.IsDefaultOrEmpty).SelectMany(r => r.Diagnostics).ToImmutableArray());
                }
                return _lazyDiagnostics;
            }
        }

        /// <summary>
        /// The <see cref="SyntaxTree"/>s produced during this generation pass by parsing each <see cref="SourceText"/> added by each generator.
        /// </summary>
        /// <remarks>
        /// This is equivalent to the union of all <see cref="GeneratedSourceResult.SyntaxTree"/>s in each <see cref="GeneratorRunResult.GeneratedSources"/> in each <see cref="Results"/>
        /// </remarks>
        public ImmutableArray<SyntaxTree> GeneratedTrees
        {
            get
            {
                if (_lazyGeneratedTrees.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyGeneratedTrees, Results.Where(r => !r.GeneratedSources.IsDefaultOrEmpty).SelectMany(r => r.GeneratedSources.Select(g => g.SyntaxTree)).ToImmutableArray());
                }
                return _lazyGeneratedTrees;
            }
        }
    }

    /// <summary>
    /// Represents the results of a single <see cref="ISourceGenerator"/> generation pass.
    /// </summary>
    public readonly struct GeneratorRunResult
    {
        internal GeneratorRunResult(
            ISourceGenerator generator,
            ImmutableArray<GeneratedSourceResult> generatedSources,
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> namedSteps,
            ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps,
            ImmutableArray<(string Key, string Value)> hostOutputs,
            Exception? exception,
            TimeSpan elapsedTime)
        {
            Debug.Assert(exception is null || (generatedSources.IsEmpty && diagnostics.Length == 1));

            this.Generator = generator;
            this.GeneratedSources = generatedSources;
            this.Diagnostics = diagnostics;
            this.TrackedSteps = namedSteps;
            this.TrackedOutputSteps = outputSteps;
            this.HostOutputs = hostOutputs;
            this.Exception = exception;
            this.ElapsedTime = elapsedTime;
        }

        /// <summary>
        /// The <see cref="ISourceGenerator"/> that produced this result.
        /// </summary>
        public ISourceGenerator Generator { get; }

        /// <summary>
        /// The sources that were added by <see cref="Generator"/> during the generation pass this result represents.
        /// </summary>
        public ImmutableArray<GeneratedSourceResult> GeneratedSources { get; }

        /// <summary>
        /// A collection of <see cref="Diagnostic"/>s reported by <see cref="Generator"/> 
        /// </summary>
        /// <remarks>
        /// When generation fails due to an <see cref="Exception"/> being thrown, a single diagnostic is added
        /// to represent the failure. Any generator reported diagnostics up to the failure point are not included.
        /// </remarks>
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        internal ImmutableArray<(string Key, string Value)> HostOutputs { get; }

        /// <summary>
        /// An <see cref="System.Exception"/> instance that was thrown by the generator, or <c>null</c> if the generator completed without error.
        /// </summary>
        /// <remarks>
        /// When this property has a value, <see cref="GeneratedSources"/> property is guaranteed to be empty, and the <see cref="Diagnostics"/>
        /// collection will contain a single diagnostic indicating that the generator failed.
        /// </remarks>
        public Exception? Exception { get; }

        /// <summary>
        /// The wall clock time that elapsed while this generator was running.
        /// </summary>
        internal TimeSpan ElapsedTime { get; }

        /// <summary>
        /// A collection of the named incremental steps (both intermediate and final output ones)
        /// executed during the generator pass this result represents.
        /// </summary>
        /// <remarks>
        /// Steps can be named by extension method WithTrackingName.
        /// </remarks>
        public ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> TrackedSteps { get; }

        /// <summary>
        /// A collection of the named output steps executed during the generator pass this result represents.
        /// </summary>
        /// <remarks>
        /// Steps can be named by extension method WithTrackingName.
        /// </remarks>
        public ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> TrackedOutputSteps { get; }
    }

    /// <summary>
    /// Represents the results of an <see cref="ISourceGenerator"/> calling <see cref="GeneratorExecutionContext.AddSource(string, SourceText)"/>.
    /// </summary>
    /// <remarks>
    /// This contains the original <see cref="SourceText"/> added by the generator, along with the parsed representation of that text in <see cref="SyntaxTree"/>.
    /// </remarks>
    public readonly struct GeneratedSourceResult
    {
        internal GeneratedSourceResult(SyntaxTree tree, SourceText text, string hintName)
        {
            this.SyntaxTree = tree;
            this.SourceText = text;
            this.HintName = hintName;
        }

        /// <summary>
        /// The <see cref="SyntaxTree"/> that was produced from parsing the <see cref="GeneratedSourceResult.SourceText"/>.
        /// </summary>
        public SyntaxTree SyntaxTree { get; }

        /// <summary>
        /// The <see cref="SourceText"/> that was added by the generator.
        /// </summary>
        public SourceText SourceText { get; }

        /// <summary>
        /// An identifier provided by the generator that identifies the added <see cref="SourceText"/>.
        /// </summary>
        public string HintName { get; }
    }

    /// <summary>
    /// Contains timing information for a full generation pass.
    /// </summary>
    public readonly struct GeneratorDriverTimingInfo
    {
        internal GeneratorDriverTimingInfo(TimeSpan elapsedTime, ImmutableArray<GeneratorTimingInfo> generatorTimes)
        {
            ElapsedTime = elapsedTime;
            GeneratorTimes = generatorTimes;
        }

        /// <summary>
        /// The wall clock time that the entire generation pass took.
        /// </summary>
        /// <remarks>
        /// This can be more than the sum of times in <see cref="GeneratorTimes"/> as it includes other costs such as setup.
        /// </remarks>
        public TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Individual timings per generator.
        /// </summary>
        public ImmutableArray<GeneratorTimingInfo> GeneratorTimes { get; }
    }

    /// <summary>
    /// Contains timing information for a single generator.
    /// </summary>
    public readonly struct GeneratorTimingInfo
    {
        internal GeneratorTimingInfo(ISourceGenerator generator, TimeSpan elapsedTime)
        {
            Generator = generator;
            ElapsedTime = elapsedTime;
        }

        /// <summary>
        /// The <see cref="ISourceGenerator"/> that was running during the recorded time.
        /// </summary>
        public ISourceGenerator Generator { get; }

        /// <summary>
        /// The wall clock time the generator spent running.
        /// </summary>
        public TimeSpan ElapsedTime { get; }
    }
}

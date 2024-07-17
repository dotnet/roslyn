// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.AnalyzerDriver;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class CompilationWithAnalyzers
    {
        private readonly Compilation _compilation;
        private readonly AnalysisScope _compilationAnalysisScope;
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly ImmutableArray<DiagnosticSuppressor> _suppressors;
        private readonly CompilationWithAnalyzersOptions _analysisOptions;

        /// <summary>
        /// Builder for storing current, possibly partial, analysis results:
        /// 1. Diagnostics reported by analyzers.
        /// 2. AnalyzerTelemetryInfo.
        /// </summary>
        private readonly AnalysisResultBuilder _analysisResultBuilder;

        /// <summary>
        /// Set of exception diagnostics reported for exceptions thrown by the analyzers.
        /// </summary>
        private readonly ConcurrentSet<Diagnostic> _exceptionDiagnostics = new ConcurrentSet<Diagnostic>();

        private static readonly AsyncQueue<CompilationEvent> s_EmptyEventQueue = new AsyncQueue<CompilationEvent>();

        /// <summary>
        /// Underlying <see cref="Compilation"/> with a non-null <see cref="Compilation.EventQueue"/>, used to drive analyzer execution.
        /// </summary>
        public Compilation Compilation => _compilation;

        /// <summary>
        /// Analyzers to execute on the compilation.
        /// </summary>
        public ImmutableArray<DiagnosticAnalyzer> Analyzers => _analyzers;

        /// <summary>
        /// Options to configure analyzer execution.
        /// </summary>
        public CompilationWithAnalyzersOptions AnalysisOptions => _analysisOptions;

        /// <summary>
        /// An optional cancellation token which can be used to cancel analysis.
        /// Note: This token is only used if the API invoked to get diagnostics doesn't provide a cancellation token.
        /// </summary>
        [Obsolete("This CancellationToken is always 'None'", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CancellationToken CancellationToken => CancellationToken.None;

        /// <inheritdoc cref="CompilationWithAnalyzers(Compilation, ImmutableArray{DiagnosticAnalyzer}, AnalyzerOptions?)"/>
        [Obsolete("Use constructor without a cancellation token")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions? options, CancellationToken cancellationToken)
            : this(compilation, analyzers, options)
        {
        }

        /// <summary>
        /// Creates a new compilation by attaching diagnostic analyzers to an existing compilation.
        /// </summary>
        /// <param name="compilation">The original compilation.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions? options)
            : this(compilation, analyzers, new CompilationWithAnalyzersOptions(options, onAnalyzerException: null, analyzerExceptionFilter: null, concurrentAnalysis: true, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: false))
        {
        }

        /// <summary>
        /// Creates a new compilation by attaching diagnostic analyzers to an existing compilation.
        /// </summary>
        /// <param name="compilation">The original compilation.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="analysisOptions">Options to configure analyzer execution.</param>
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions)
        {
            VerifyArguments(compilation, analyzers, analysisOptions);

            compilation = compilation
                .WithOptions(compilation.Options.WithReportSuppressedDiagnostics(analysisOptions.ReportSuppressedDiagnostics))
                .WithSemanticModelProvider(new CachingSemanticModelProvider())
                .WithEventQueue(new AsyncQueue<CompilationEvent>());
            _compilation = compilation;
            _analyzers = analyzers;
            _suppressors = analyzers.OfType<DiagnosticSuppressor>().ToImmutableArrayOrEmpty();
            _analysisOptions = analysisOptions;

            _analysisResultBuilder = new AnalysisResultBuilder(analysisOptions.LogAnalyzerExecutionTime, analyzers, _analysisOptions.Options?.AdditionalFiles ?? ImmutableArray<AdditionalText>.Empty);
            _compilationAnalysisScope = AnalysisScope.Create(_compilation, _analyzers, this);
        }

        #region Helper methods for public API argument validation

        private static void VerifyArguments(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (analysisOptions == null)
            {
                throw new ArgumentNullException(nameof(analysisOptions));
            }

            VerifyAnalyzersArgumentForStaticApis(analyzers);
        }

        private static void VerifyAnalyzersArgumentForStaticApis(ImmutableArray<DiagnosticAnalyzer> analyzers, bool allowDefaultOrEmpty = false)
        {
            if (analyzers.IsDefaultOrEmpty)
            {
                if (allowDefaultOrEmpty)
                {
                    return;
                }

                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(analyzers));
            }

            if (analyzers.Any(static a => a == null))
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentElementCannotBeNull, nameof(analyzers));
            }

            if (analyzers.HasDuplicates())
            {
                // Has duplicate analyzer instances.
                throw new ArgumentException(CodeAnalysisResources.DuplicateAnalyzerInstances, nameof(analyzers));
            }
        }

        private void VerifyAnalyzerArgument(DiagnosticAnalyzer analyzer)
        {
            VerifyAnalyzerArgumentForStaticApis(analyzer);

            if (!_analyzers.Contains(analyzer))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(analyzer));
            }
        }

        private static void VerifyAnalyzerArgumentForStaticApis(DiagnosticAnalyzer analyzer)
        {
            if (analyzer == null)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(analyzer));
            }
        }

        private void VerifyExistingAnalyzersArgument(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            VerifyAnalyzersArgumentForStaticApis(analyzers);

            if (analyzers.Any(predicate: static (a, self) => !self._analyzers.Contains(a), arg: this))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(_analyzers));
            }

            if (analyzers.HasDuplicates())
            {
                // Has duplicate analyzer instances.
                throw new ArgumentException(CodeAnalysisResources.DuplicateAnalyzerInstances, nameof(analyzers));
            }
        }

        private void VerifyModel(SemanticModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!_compilation.ContainsSyntaxTree(model.SyntaxTree))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidTree, nameof(model));
            }
        }

        private void VerifyTree(SyntaxTree tree)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            if (!_compilation.ContainsSyntaxTree(tree))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidTree, nameof(tree));
            }
        }

        private void VerifyAdditionalFile(AdditionalText file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!AdditionalFiles.Contains(file))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidAdditionalFile, nameof(file));
            }
        }

        #endregion

        private ImmutableArray<AdditionalText> AdditionalFiles => _analysisOptions.Options?.AdditionalFiles ?? ImmutableArray<AdditionalText>.Empty;

        /// <summary>
        /// Returns diagnostics produced by all <see cref="Analyzers"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync()
        {
            return GetAnalyzerDiagnosticsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns diagnostics produced by all <see cref="Analyzers"/>.
        /// </summary>
#pragma warning disable RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
        {
            return await GetAnalyzerDiagnosticsCoreAsync(Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns diagnostics produced by given <paramref name="analyzers"/>.
        /// </summary>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerDiagnosticsCoreAsync(analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes all <see cref="Analyzers"/> and returns the corresponding <see cref="AnalysisResult"/> with all diagnostics and telemetry info.
        /// </summary>
        public async Task<AnalysisResult> GetAnalysisResultAsync(CancellationToken cancellationToken)
        {
            return await GetAnalysisResultCoreAsync(Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the given <paramref name="analyzers"/> and returns the corresponding <see cref="AnalysisResult"/> with all diagnostics and telemetry info.
        /// </summary>
        /// <param name="analyzers">Analyzers whose analysis results are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalysisResultCoreAsync(analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns all diagnostics produced by compilation and by all <see cref="Analyzers"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync()
        {
            return GetAllDiagnosticsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Returns all diagnostics produced by compilation and by all <see cref="Analyzers"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            var diagnostics = await getAllDiagnosticsWithoutStateTrackingAsync(Analyzers, cancellationToken: cancellationToken).ConfigureAwait(false);
            return diagnostics.AddRange(_exceptionDiagnostics);

            // NOTE: We have a specialized implementation for computing diagnostics in this method
            //       as we need to compute and return both compiler and analyzer diagnostics.
            //       Rest of the public APIs in this type only compute analyzer diagnostics,
            //       and all of them have a shared implementation in
            //       'ComputeAnalyzerDiagnosticsAsync(AnalysisScope, CancellationToken)'.
            async Task<ImmutableArray<Diagnostic>> getAllDiagnosticsWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
            {
                // Clone the compilation with new event queue.
                var compilation = _compilation.WithEventQueue(new AsyncQueue<CompilationEvent>());

                // Create and attach the driver to compilation.
                var analysisScope = AnalysisScope.Create(compilation, analyzers, this);
                using var driver = await CreateAndInitializeDriverAsync(compilation, _analysisOptions, analysisScope, _suppressors, categorizeDiagnostics: false, cancellationToken).ConfigureAwait(false);
                driver.AttachQueueAndStartProcessingEvents(compilation.EventQueue!, analysisScope, usingPrePopulatedEventQueue: false, cancellationToken);

                // Force compilation diagnostics and wait for analyzer execution to complete.
                var compDiags = compilation.GetDiagnostics(cancellationToken);
                var analyzerDiags = await driver.GetDiagnosticsAsync(compilation, cancellationToken).ConfigureAwait(false);
                var reportedDiagnostics = compDiags.AddRange(analyzerDiags);
                return driver.ApplyProgrammaticSuppressionsAndFilterDiagnostics(reportedDiagnostics, compilation, cancellationToken);
            }
        }

        /// <summary>
        /// Returns diagnostics produced by compilation actions of all <see cref="Analyzers"/>.
        /// </summary>
        [Obsolete("This API was found to have performance issues and hence has been deprecated. Instead, invoke the API 'GetAnalysisResultAsync' and access the property 'CompilationDiagnostics' on the returned 'AnalysisResult' to fetch the compilation diagnostics.")]
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsAsync(CancellationToken cancellationToken)
        {
            return await GetAnalyzerCompilationDiagnosticsCoreAsync(Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns diagnostics produced by compilation actions of given <paramref name="analyzers"/>.
        /// </summary>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Obsolete("This API was found to have performance issues and hence has been deprecated. Instead, invoke the API 'GetAnalysisResultAsync' and access the property 'CompilationDiagnostics' on the returned 'AnalysisResult' to fetch the compilation diagnostics.")]
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerCompilationDiagnosticsCoreAsync(analyzers, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsCoreAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = _compilationAnalysisScope.WithAnalyzers(analyzers, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: false, getNonLocalDiagnostics: true);
        }

        private async Task<AnalysisResult> GetAnalysisResultCoreAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = _compilationAnalysisScope.WithAnalyzers(analyzers, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.ToAnalysisResult(analyzers, analysisScope, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsCoreAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = _compilationAnalysisScope.WithAnalyzers(analyzers, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: true);
        }

        private static async Task<AnalyzerDriver> CreateAndInitializeDriverAsync(
            Compilation compilation,
            CompilationWithAnalyzersOptions analysisOptions,
            AnalysisScope analysisScope,
            ImmutableArray<DiagnosticSuppressor> suppressors,
            bool categorizeDiagnostics,
            CancellationToken cancellationToken)
        {
            var analyzers = analysisScope.Analyzers;
            if (!suppressors.IsEmpty)
            {
                // Always provide all the diagnostic suppressors to the driver.
                // We also need to ensure we are not passing any duplicate suppressor instances.
                var suppressorsInAnalysisScope = analysisScope.Analyzers.OfType<DiagnosticSuppressor>().ToImmutableHashSet();
                analyzers = analyzers.AddRange(suppressors.Where(suppressor => !suppressorsInAnalysisScope.Contains(suppressor)));
            }

            var driver = compilation.CreateAnalyzerDriver(analyzers, new AnalyzerManager(analyzers), severityFilter: SeverityFilter.None);
            driver.Initialize(compilation, analysisOptions, new CompilationData(compilation), analysisScope, categorizeDiagnostics, trackSuppressedDiagnosticIds: false, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await driver.WhenInitializedTask.ConfigureAwait(false);
            return driver;
        }

        /// <summary>
        /// Returns syntax diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, CancellationToken cancellationToken)
        {
            VerifyTree(tree);

            return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, Analyzers, filterSpan: null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns syntax diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="tree"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree or filter span,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="filterSpan">Optional filter span to analyze within the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyTree(tree);

            return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, Analyzers, filterSpan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns syntax diagnostics produced by given <paramref name="analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyTree(tree);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, analyzers, filterSpan: null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns syntax diagnostics produced by given <paramref name="analyzers"/> from analyzing the given <paramref name="tree"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree or filter span,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="filterSpan">Optional filter span to analyze within the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyTree(tree);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, analyzers, filterSpan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SyntaxDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SyntaxTree tree, CancellationToken cancellationToken)
        {
            VerifyTree(tree);

            return GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(tree), Analyzers, filterSpan: null, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SyntaxDiagnostics"/> produced by all <see cref="Analyzers"/>
        /// from analyzing the given <paramref name="tree"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="filterSpan">Optional filter span to analyze within the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SyntaxTree tree, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyTree(tree);

            return GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(tree), Analyzers, filterSpan, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SyntaxDiagnostics"/> produced by given <paramref name="analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyTree(tree);
            VerifyExistingAnalyzersArgument(analyzers);

            return GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(tree), analyzers, filterSpan: null, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SyntaxDiagnostics"/> produced by given <paramref name="analyzers"/>
        /// from analyzing the given <paramref name="tree"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="filterSpan">Optional filter span to analyze within the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SyntaxTree tree, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyTree(tree);
            VerifyExistingAnalyzersArgument(analyzers);

            return GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(tree), analyzers, filterSpan, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.AdditionalFileDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given additional <paramref name="file"/>.
        /// The given <paramref name="file"/> must be part of <see cref="AnalyzerOptions.AdditionalFiles"/> for the <see cref="AnalysisOptions"/> for this CompilationWithAnalyzers instance.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the file by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(AdditionalText file, CancellationToken cancellationToken)
        {
            VerifyAdditionalFile(file);

            return await GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(file), Analyzers, filterSpan: null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.AdditionalFileDiagnostics"/> produced by given <paramref name="analyzers"/> from analyzing the given additional <paramref name="file"/>.
        /// The given <paramref name="file"/> must be part of <see cref="AnalyzerOptions.AdditionalFiles"/> for the <see cref="AnalysisOptions"/> for this CompilationWithAnalyzers instance.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the file by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(AdditionalText file, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyAdditionalFile(file);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(file), analyzers, filterSpan: null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.AdditionalFileDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given additional <paramref name="file"/>,
        /// optionally scoped to a <paramref name="filterSpan"/>.
        /// The given <paramref name="file"/> must be part of <see cref="AnalyzerOptions.AdditionalFiles"/> for the <see cref="AnalysisOptions"/> for this CompilationWithAnalyzers instance.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the file by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="filterSpan">Optional filter span to analyze within the <paramref name="file"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(AdditionalText file, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyAdditionalFile(file);

            return await GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(file), Analyzers, filterSpan, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.AdditionalFileDiagnostics"/> produced by given <paramref name="analyzers"/> from analyzing the given additional <paramref name="file"/>,
        /// optionally scoped to a <paramref name="filterSpan"/>.
        /// The given <paramref name="file"/> must be part of <see cref="AnalyzerOptions.AdditionalFiles"/> for the <see cref="AnalysisOptions"/> for this CompilationWithAnalyzers instance.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the file by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="filterSpan">Optional filter span to analyze within the <paramref name="file"/>.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(AdditionalText file, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyAdditionalFile(file);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(file), analyzers, filterSpan, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AnalysisResult> GetAnalysisResultCoreAsync(SourceOrAdditionalFile file, ImmutableArray<DiagnosticAnalyzer> analyzers, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            var analysisScope = AnalysisScope.Create(analyzers, file, filterSpan, isSyntacticSingleFileAnalysis: true, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.ToAnalysisResult(analyzers, analysisScope, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsCoreAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            var analysisScope = AnalysisScope.Create(analyzers, new SourceOrAdditionalFile(tree), filterSpan, isSyntacticSingleFileAnalysis: true, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: false);
        }

        /// <summary>
        /// Returns semantic diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyModel(model);

            return await GetAnalyzerSemanticDiagnosticsCoreAsync(model, filterSpan, Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns semantic diagnostics produced by the given <paramref name="analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyModel(model);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerSemanticDiagnosticsCoreAsync(model, filterSpan, analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SemanticDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SemanticModel model, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyModel(model);

            return GetAnalysisResultCoreAsync(model, filterSpan, this.Analyzers, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SemanticDiagnostics"/> produced by the given <paramref name="analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyModel(model);
            VerifyExistingAnalyzersArgument(analyzers);

            return GetAnalysisResultCoreAsync(model, filterSpan, analyzers, cancellationToken);
        }

        private async Task<AnalysisResult> GetAnalysisResultCoreAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = AnalysisScope.Create(analyzers, new SourceOrAdditionalFile(model.SyntaxTree), filterSpan, isSyntacticSingleFileAnalysis: false, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.ToAnalysisResult(analyzers, analysisScope, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsCoreAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = AnalysisScope.Create(analyzers, new SourceOrAdditionalFile(model.SyntaxTree), filterSpan, isSyntacticSingleFileAnalysis: false, this);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: false);
        }

        /// <summary>
        /// Core method to compute analyzer diagnostics for the given <paramref name="analysisScope"/>.
        /// This method is used to compute diagnostics for the entire compilation or a specific file.
        /// It executes the required analyzers and stores the reported analyzer diagnostics into
        /// <see cref="_analysisResultBuilder"/>.
        /// </summary>
        /// <remarks>
        /// PERF: We re-use the underlying <see cref="_compilation"/> for the below cases:
        ///     1. If the given analysis scope only includes the <see cref="CompilerDiagnosticAnalyzer"/>.
        ///     2. If we are only computing syntax diagnostics.
        /// For rest of the cases, we always fork the underlying <see cref="_compilation"/> with a
        /// new compilation event queue, execute the analyzers on this forked compilation and then
        /// discard this compilation. Using a forked compilation allows us to avoid performing expensive
        /// partial analysis state tracking for analyzer execution. It is the responsibility of the
        /// CompilationWithAnalyzers host to club the analyzer diagnostics requests into minimal number of
        /// calls into CompilationWithAnalyzers to get the optimum performance by minimize compilation forking.
        /// See https://github.com/dotnet/roslyn/issues/66714 for more details.
        /// </remarks>
        private async Task ComputeAnalyzerDiagnosticsAsync(AnalysisScope? analysisScope, CancellationToken cancellationToken)
        {
            // Implementation Note: We compute the analyzer diagnostics for the given scope by following the below steps:
            //  1. First, we invoke GetPendingAnalysisScope to filter out the analyzers that have already
            //     executed on the given analysis scope.
            //  2. We fetch the compilation and initialized analyzer driver to use for analyzer execution.
            //     We may re-use the root '_compilation' stored as a field on this CompilationWithAnalyzers instance
            //     OR use a new cloned compilation with a new event queue.
            //  3. We compute the registered analyzer action counts for the analyzers to be executed. We also
            //     compute whether there are any registered analyzer actions that need us to generate compilation
            //     events to drive analyzer execution.
            //  4. We perform analyzer execution with different execution models based on whether the analysis scope
            //     is full compilation or scoped to a specific file/span:
            //     a. For full compilation scope, we use the execution model similar to batch compilation.
            //        We first attach the driver to compilation event queue, then force generation of all
            //        compilation events by invoking 'Compilation.GetDiagnostics(optionalSpan),
            //        while concurrently executing analyzers by processing the generated events.
            //     b. For file/span partial analysis scope, we first generate the compilation events for this scope
            //        by invoking 'SemanticModel.GetDiagnostics(optionalSpan)'. Then we populate an event queue with
            //        these events and attach this event queue to the driver and request it to process these pre-generated
            //        events to drive analysis.
            //  5. Finally, we save the computed analyzer diagnostics onto the '_analysisResultBuilder' field. This
            //     analysis result builder also tracks the set of analyzers that have fully executed for the compilation
            //     and/or specific trees and additional files. This enables us to skip executing these analyzers for future
            //     diagnostic queries on the same analysis scope.

            Debug.Assert(analysisScope != null);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                analysisScope = GetPendingAnalysisScope(analysisScope);
                if (analysisScope == null)
                    return;

                // Get the compilation to execute analysis.
                // PERF: We reuse the root "_compilation" when computing single-file diagnostics only for the compiler analyzer.
                // For rest of the cases, we use a cloned compilation with a new event queue and semantic model provider
                // to allow us to execute analyzers on this compilation without any partial state tracking and
                // subsequently discard this compilation.
                var compilation = analysisScope.IsSingleFileAnalysisForCompilerAnalyzer
                    ? _compilation
                    : _compilation.WithSemanticModelProvider(new CachingSemanticModelProvider()).WithEventQueue(new AsyncQueue<CompilationEvent>());

                // Get the analyzer driver to execute analysis.
                using var driver = await CreateAndInitializeDriverAsync(compilation, _analysisOptions, analysisScope, _suppressors, categorizeDiagnostics: true, cancellationToken).ConfigureAwait(false);

                // Driver must have been initialized.
                Debug.Assert(driver.IsInitialized);
                Debug.Assert(!driver.WhenInitializedTask.IsCanceled);

                (var analyzerActionCounts, var hasAnyActionsRequiringCompilationEvents) = await getAnalyzerActionCountsAsync(
                    driver, compilation, analysisScope, cancellationToken).ConfigureAwait(false);
                Func<DiagnosticAnalyzer, AnalyzerActionCounts> getAnalyzerActionCounts = analyzer => analyzerActionCounts[analyzer];

                // We use different analyzer execution models based on whether we are analyzing the full compilation or not.
                if (!analysisScope.IsSingleFileAnalysis)
                {
                    // We are performing full compilation analysis.
                    // PERF: For improved performance, we first attach the event queue to the driver and then
                    //       invoke compilation.GetDiagnostics() to ensure that we generate and process the
                    //       compilation events concurrently.

                    // Attach the driver to compilation and start processing events.
                    // If we do not have any analyzer actions that are driven by compilation events,
                    // we are only going to enqueue a CompilationStartedEvent, so ensure we
                    // pass 'usingPrePopulatedEventQueue = true' for that case.
                    driver.AttachQueueAndStartProcessingEvents(compilation.EventQueue!, analysisScope, usingPrePopulatedEventQueue: !hasAnyActionsRequiringCompilationEvents, cancellationToken);

                    // If we have any analyzer actions that are driven by compilation events, we need to
                    // force compilation diagnostics to populate the compilation event queue and force it to be completed.
                    if (hasAnyActionsRequiringCompilationEvents)
                        _ = compilation.GetDiagnostics(cancellationToken);

                    // Wait for analyzer execution to complete.
                    await driver.WhenCompletedTask.ConfigureAwait(false);

                    _analysisResultBuilder.ApplySuppressionsAndStoreAnalysisResult(analysisScope, driver, compilation, getAnalyzerActionCounts, cancellationToken);
                }
                else
                {
                    // We are performing analysis for a single file in the compilation, with or without a filter span.
                    // First fetch the compilation events to drive this partial analysis.
                    var compilationEvents = GetCompilationEventsForSingleFileAnalysis(compilation, analysisScope, AdditionalFiles, hasAnyActionsRequiringCompilationEvents, cancellationToken);

                    var builder = ArrayBuilder<(AnalysisScope, ImmutableArray<CompilationEvent>)>.GetInstance();
                    builder.Add((analysisScope, compilationEvents));

                    // If the analysis scope has symbol declared events, and we have any analyzers with SymbolStart/End actions,
                    // then we need to analyze entire symbol declaration, including partial declarations, to compute the
                    // required SymbolEnd diagnostics for such symbolStartAnalyzers.
                    if (compilationEvents.Any(e => e is SymbolDeclaredCompilationEvent) &&
                        driver.HasSymbolStartedActions(analysisScope))
                    {
                        Debug.Assert(!analysisScope.IsSyntacticSingleFileAnalysis);
                        Debug.Assert(hasAnyActionsRequiringCompilationEvents);

                        var (symbolStartAnalyzers, otherAnalyzers) = getSymbolStartAnalyzers(analysisScope.Analyzers, analyzerActionCounts);
                        Debug.Assert(!symbolStartAnalyzers.IsEmpty);

                        // We separate out the execution of symbol start analyzers and rest of the analyzers.
                        // This is due to the fact that symbol start analyzers need to execute on the entire
                        // symbol declarations, not just the filter span, and hence have different analysis scope.
                        builder.Clear();
                        if (!otherAnalyzers.IsEmpty)
                        {
                            var otherAnalyzersAnalysisScope = analysisScope.WithAnalyzers(otherAnalyzers, this);
                            builder.Add((otherAnalyzersAnalysisScope, compilationEvents));
                        }

                        processSymbolStartAnalyzers(analysisScope.FilterFileOpt!.Value, analysisScope.FilterSpanOpt, compilationEvents, symbolStartAnalyzers, compilation,
                            _analysisResultBuilder, builder, AdditionalFiles, cancellationToken);
                    }

                    await attachQueueAndProcessAllEventsAsync(builder, driver, cancellationToken).ConfigureAwait(false);

                    // Update the diagnostic results based on the diagnostics reported on the driver.
                    foreach (var (scope, _) in builder)
                    {
                        _analysisResultBuilder.ApplySuppressionsAndStoreAnalysisResult(scope, driver, compilation, getAnalyzerActionCounts, cancellationToken);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }

            return;

            static async Task attachQueueAndProcessAllEventsAsync(
                ArrayBuilder<(AnalysisScope, ImmutableArray<CompilationEvent>)> builder,
                AnalyzerDriver driver,
                CancellationToken cancellationToken)
            {
                foreach (var (analysisScope, compilationEvents) in builder)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var eventQueue = CreateEventsQueue(compilationEvents);

                    // Perform analysis to compute new diagnostics.
                    await driver.AttachQueueAndProcessAllEventsAsync(eventQueue, analysisScope, cancellationToken).ConfigureAwait(false);
                }
            }

            static async Task<(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerActionCounts> analyzerActionCounts, bool hasAnyActionsRequiringCompilationEvents)> getAnalyzerActionCountsAsync(
                AnalyzerDriver driver,
                Compilation compilation,
                AnalysisScope analysisScope,
                CancellationToken cancellationToken)
            {
                Debug.Assert(driver.IsInitialized);

                // Get analyzer action counts.
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerActionCounts>();
                var hasAnyActionsRequiringCompilationEvents = false;
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    var actionCounts = await driver.GetAnalyzerActionCountsAsync(analyzer, compilation.Options, analysisScope, cancellationToken).ConfigureAwait(false);
                    builder.Add(analyzer, actionCounts);

                    if (actionCounts.HasAnyActionsRequiringCompilationEvents)
                        hasAnyActionsRequiringCompilationEvents = true;
                }

                return (builder.ToImmutable(), hasAnyActionsRequiringCompilationEvents);
            }

            static (ImmutableArray<DiagnosticAnalyzer> symbolStartAnalyzers, ImmutableArray<DiagnosticAnalyzer> otherAnalyzers) getSymbolStartAnalyzers(
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                ImmutableDictionary<DiagnosticAnalyzer, AnalyzerActionCounts> analyzerActionCounts)
            {
                var symbolStartAnalyzersBuilder = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();
                var otherAnalyzersBuilder = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();

                foreach (var analyzer in analyzers)
                {
                    if (analyzerActionCounts[analyzer].SymbolStartActionsCount > 0)
                    {
                        symbolStartAnalyzersBuilder.Add(analyzer);
                    }
                    else
                    {
                        otherAnalyzersBuilder.Add(analyzer);
                    }
                }

                return (symbolStartAnalyzersBuilder.ToImmutableAndFree(), otherAnalyzersBuilder.ToImmutableAndFree());
            }

            void processSymbolStartAnalyzers(
                SourceOrAdditionalFile originalFile,
                TextSpan? originalSpan,
                ImmutableArray<CompilationEvent> compilationEventsForTree,
                ImmutableArray<DiagnosticAnalyzer> symbolStartAnalyzers,
                Compilation compilation,
                AnalysisResultBuilder analysisResultBuilder,
                ArrayBuilder<(AnalysisScope, ImmutableArray<CompilationEvent>)> builder,
                ImmutableArray<AdditionalText> additionalFiles,
                CancellationToken cancellationToken)
            {
                // This method processes all the compilation events generated for the tree in the
                // original requested analysis scope to identify symbol declared events whose symbol
                // declarations span across different trees. For the given symbolStartAnalyzers to
                // report the correct set of diagnostics for the original tree/span, we need to
                // execute them on all the partial declarations of the symbols across these different trees,
                // followed by the SymbolEnd action at the end.
                // This method computes these set of trees with partial declarations, and adds
                // analysis scopes to the 'builder' for each of these trees, along with the corresponding
                // compilation events generated for each tree.

                var partialTrees = PooledHashSet<SyntaxTree>.GetInstance();
                var tree = originalFile.SourceTree!;
                partialTrees.Add(tree);

                try
                {
                    // Gather all trees with symbol declarations events in original analysis scope, except namespace symbols.
                    foreach (var compilationEvent in compilationEventsForTree)
                    {
                        if (compilationEvent is SymbolDeclaredCompilationEvent symbolDeclaredEvent &&
                            symbolDeclaredEvent.Symbol.Kind != SymbolKind.Namespace)
                        {
                            foreach (var location in symbolDeclaredEvent.Symbol.Locations)
                            {
                                if (location.SourceTree != null)
                                {
                                    partialTrees.Add(location.SourceTree);
                                }
                            }
                        }
                    }

                    // Next, generate compilation events for each of the partial trees
                    // and add the (analysisScope, compilationEvents) tuple for each tree to the builder.
                    // Process all trees sequentially: this is required to ensure the appropriate
                    // compilation events are mapped to the tree for which they are generated.
                    foreach (var partialTree in partialTrees)
                    {
                        if (tryProcessTree(partialTree, out var analysisScopeAndEvents))
                        {
                            builder.Add((analysisScopeAndEvents.Value.scope, analysisScopeAndEvents.Value.events));
                        }
                    }
                }
                finally
                {
                    partialTrees.Free();
                }

                bool tryProcessTree(SyntaxTree partialTree, [NotNullWhen(true)] out (AnalysisScope scope, ImmutableArray<CompilationEvent> events)? scopeAndEvents)
                {
                    scopeAndEvents = null;

                    var file = new SourceOrAdditionalFile(partialTree);
                    var analysisScope = AnalysisScope.Create(symbolStartAnalyzers, file, filterSpan: null,
                        originalFile, originalSpan, isSyntacticSingleFileAnalysis: false, this);

                    analysisScope = GetPendingAnalysisScope(analysisScope);
                    if (analysisScope == null)
                        return false;

                    var compilationEvents = GetCompilationEventsForSingleFileAnalysis(compilation, analysisScope, additionalFiles, hasAnyActionsRequiringCompilationEvents: true, cancellationToken);

                    // Include the already generated compilations events for the primary tree.
                    if (partialTree == tree)
                    {
                        compilationEvents = compilationEventsForTree.AddRange(compilationEvents);

                        // Filter out synthesized compilation unit completed event that was generated for span analysis
                        // as we are now doing full tree analysis, GetCompilationEventsForSingleFileAnalysis call above should
                        // have already generated a CompilationUnitCompletedEvent without any filter span.
                        compilationEvents = compilationEvents.WhereAsArray(e => e is not CompilationUnitCompletedEvent c || !c.FilterSpan.HasValue);
                        Debug.Assert(compilationEvents.Count(e => e is CompilationUnitCompletedEvent c && !c.FilterSpan.HasValue) == 1);

                        // We shouldn't have any duplicate events.
                        Debug.Assert(!compilationEvents.HasDuplicates());
                    }

                    scopeAndEvents = (analysisScope, compilationEvents);
                    return true;
                }
            }
        }

        private AnalysisScope? GetPendingAnalysisScope(AnalysisScope analysisScope)
        {
            (SourceOrAdditionalFile file, bool syntax)? filterScope = analysisScope.FilterFileOpt.HasValue ?
                (analysisScope.FilterFileOpt.Value, analysisScope.IsSyntacticSingleFileAnalysis) :
                null;
            var pendingAnalyzers = _analysisResultBuilder.GetPendingAnalyzers(analysisScope.Analyzers, filterScope);
            if (pendingAnalyzers.IsEmpty)
            {
                // All analyzers have already executed on the requested scope.
                return null;
            }

            return pendingAnalyzers.Length < analysisScope.Analyzers.Length ?
                analysisScope.WithAnalyzers(pendingAnalyzers, this) :
                analysisScope;
        }

        private static ImmutableArray<CompilationEvent> GetCompilationEventsForSingleFileAnalysis(
            Compilation compilation,
            AnalysisScope analysisScope,
            ImmutableArray<AdditionalText> additionalFiles,
            bool hasAnyActionsRequiringCompilationEvents,
            CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.IsSingleFileAnalysis);

            if (analysisScope.IsSyntacticSingleFileAnalysis || !hasAnyActionsRequiringCompilationEvents)
            {
                return ImmutableArray<CompilationEvent>.Empty;
            }

            if (analysisScope.IsSemanticSingleFileAnalysisForCompilerAnalyzer)
            {
                // PERF: We are computing only compiler semantic diagnostics for this file via the CompilerDiagnosticAnalyzer.
                //       CompilerDiagnosticAnalyzer reports semantic diagnostics for each tree in SemanticModelAction callbacks,
                //       which are driven with CompilationUnitCompleted events.
                //       We just need CompilationStarted event and CompilationUnitCompleted event for the tree to drive all analysis.
                var compilationStartedEvent = new CompilationStartedEvent(compilation);
                if (!additionalFiles.IsEmpty)
                {
                    compilationStartedEvent = compilationStartedEvent.WithAdditionalFiles(additionalFiles);
                }

                var compilationUnitCompletedEvent = new CompilationUnitCompletedEvent(compilation, analysisScope.FilterFileOpt!.Value.SourceTree!, analysisScope.FilterSpanOpt);
                return ImmutableArray.Create<CompilationEvent>(compilationStartedEvent, compilationUnitCompletedEvent);
            }

            generateCompilationEvents(compilation, analysisScope, cancellationToken);

            return dequeueAndFilterCompilationEvents(compilation, analysisScope, additionalFiles, cancellationToken);

            static void generateCompilationEvents(Compilation compilation, AnalysisScope analysisScope, CancellationToken cancellationToken)
            {
                // Invoke GetDiagnostics to populate CompilationEvent queue for the given analysis scope.
                // Discard the returned diagnostics.
                if (analysisScope.FilterFileOpt == null)
                {
                    _ = compilation.GetDiagnostics(cancellationToken);
                }
                else if (!analysisScope.IsSyntacticSingleFileAnalysis)
                {
                    // Get the mapped model and invoke GetDiagnostics for the given filter span, if any.
                    // Limiting the GetDiagnostics scope to the filter span ensures we only generate compilation events
                    // for the required symbols whose declaration intersects with this span, instead of all symbols in the tree.
                    var mappedModel = compilation.GetSemanticModel(analysisScope.FilterFileOpt!.Value.SourceTree!);
                    _ = mappedModel.GetDiagnostics(analysisScope.FilterSpanOpt, cancellationToken);
                }
            }

            static ImmutableArray<CompilationEvent> dequeueAndFilterCompilationEvents(
                Compilation compilation,
                AnalysisScope analysisScope,
                ImmutableArray<AdditionalText> additionalFiles,
                CancellationToken cancellationToken)
            {
                Debug.Assert(analysisScope.IsSingleFileAnalysis);
                Debug.Assert(!analysisScope.IsSyntacticSingleFileAnalysis);

                var eventQueue = compilation.EventQueue!;
                if (eventQueue.Count == 0)
                {
                    return ImmutableArray<CompilationEvent>.Empty;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // We synthesize a span-based CompilationUnitCompletedEvent for improved performance for computing semantic diagnostics
                // of compiler diagnostic analyzer. See https://github.com/dotnet/roslyn/issues/56843 for more details.
                var needsSpanBasedCompilationUnitCompletedEvent = analysisScope.FilterSpanOpt.HasValue;

                var tree = analysisScope.FilterFileOpt!.Value.SourceTree!;
                var builder = ArrayBuilder<CompilationEvent>.GetInstance();
                while (eventQueue.TryDequeue(out CompilationEvent compilationEvent))
                {
                    switch (compilationEvent)
                    {
                        case CompilationStartedEvent compilationStartedEvent:
                            if (!additionalFiles.IsEmpty)
                            {
                                compilationEvent = compilationStartedEvent.WithAdditionalFiles(additionalFiles);
                            }

                            break;

                        case CompilationCompletedEvent:
                            break;

                        case CompilationUnitCompletedEvent compilationUnitCompletedEvent:
                            if (tree != compilationUnitCompletedEvent.CompilationUnit)
                                continue;

                            // We don't need to synthesize a span-based CompilationUnitCompletedEvent if the event queue already
                            // has a CompilationUnitCompletedEvent for the entire source tree.
                            needsSpanBasedCompilationUnitCompletedEvent = false;
                            break;

                        case SymbolDeclaredCompilationEvent symbolDeclaredCompilationEvent:
                            if (!shouldIncludeSymbol(symbolDeclaredCompilationEvent.SymbolInternal, tree, cancellationToken))
                                continue;

                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(compilationEvent.GetType().ToString());
                    }

                    builder.Add(compilationEvent);
                }

                if (needsSpanBasedCompilationUnitCompletedEvent)
                {
                    builder.Add(new CompilationUnitCompletedEvent(compilation, tree, analysisScope.FilterSpanOpt));
                }

                return builder.ToImmutableAndFree();

                static bool shouldIncludeSymbol(ISymbolInternal symbol, SyntaxTree tree, CancellationToken cancellationToken)
                {
                    if (symbol.IsDefinedInSourceTree(tree, definedWithinSpan: null, cancellationToken))
                        return true;

                    // Always include both parts of partial in analysis if any one part is defined in the tree.
                    if (symbol is IMethodSymbolInternal methodSymbol)
                    {
                        if (methodSymbol.PartialDefinitionPart?.IsDefinedInSourceTree(tree, definedWithinSpan: null, cancellationToken) == true
                            || methodSymbol.PartialImplementationPart?.IsDefinedInSourceTree(tree, definedWithinSpan: null, cancellationToken) == true)
                        {
                            return true;
                        }
                    }

                    // https://github.com/dotnet/roslyn/issues/73772: should we also check IPropertySymbol?
                    // there is no interface IPropertySymbolInternal
                    // where are tests for this?

                    return false;
                }
            }
        }

        private static AsyncQueue<CompilationEvent> CreateEventsQueue(ImmutableArray<CompilationEvent> compilationEvents)
        {
            if (compilationEvents.IsEmpty)
            {
                return s_EmptyEventQueue;
            }

            var eventQueue = new AsyncQueue<CompilationEvent>();
            foreach (var compilationEvent in compilationEvents)
            {
                eventQueue.TryEnqueue(compilationEvent);
            }

            return eventQueue;
        }

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation)
            => GetEffectiveDiagnostics(diagnostics.AsImmutableOrNull(), compilation);

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics.IsDefault)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return GetEffectiveDiagnosticsImpl(diagnostics, compilation);
        }

        private static IEnumerable<Diagnostic> GetEffectiveDiagnosticsImpl(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics.IsEmpty)
            {
                yield break;
            }

            if (compilation.SemanticModelProvider == null)
            {
                compilation = compilation.WithSemanticModelProvider(new CachingSemanticModelProvider());
            }

            var suppressMessageState = new SuppressMessageAttributeState(compilation);
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic != null)
                {
                    var effectiveDiagnostic = compilation.Options.FilterDiagnostic(diagnostic, CancellationToken.None);
                    if (effectiveDiagnostic != null)
                    {
                        yield return suppressMessageState.ApplySourceSuppressions(effectiveDiagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        /// <param name="analyzer">Analyzer to be checked for suppression.</param>
        /// <param name="options">Compilation options.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        [Obsolete("This API is no longer supported. See https://github.com/dotnet/roslyn/issues/67592 for details")]
        public static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException = null)
        {
            VerifyAnalyzerArgumentForStaticApis(analyzer);

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var analyzerManager = new AnalyzerManager(analyzer);

            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> wrappedOnAnalyzerException =
                (ex, analyzer, diagnostic, _) => onAnalyzerException?.Invoke(ex, analyzer, diagnostic);

            Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getSupportedDiagnosticDescriptors = analyzer =>
            {
                try
                {
                    return analyzer.SupportedDiagnostics;
                }
                catch (Exception ex) when (AnalyzerExecutor.HandleAnalyzerException(ex, analyzer, info: null, wrappedOnAnalyzerException, analyzerExceptionFilter: null, CancellationToken.None))
                {
                    return ImmutableArray<DiagnosticDescriptor>.Empty;
                }
            };

            Func<DiagnosticSuppressor, ImmutableArray<SuppressionDescriptor>> getSupportedSuppressionDescriptors = suppressor =>
            {
                try
                {
                    return suppressor.SupportedSuppressions;
                }
                catch (Exception ex) when (AnalyzerExecutor.HandleAnalyzerException(ex, suppressor, info: null, wrappedOnAnalyzerException, analyzerExceptionFilter: null, CancellationToken.None))
                {
                    return ImmutableArray<SuppressionDescriptor>.Empty;
                }
            };

            return AnalyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, IsCompilerAnalyzer, severityFilter: SeverityFilter.None,
                isEnabledWithAnalyzerConfigOptions: _ => false, getSupportedDiagnosticDescriptors, getSupportedSuppressionDescriptors, CancellationToken.None); ;
        }

        /// <summary>
        /// This method should be invoked when the analyzer host is disposing off the given <paramref name="analyzers"/>.
        /// It clears the cached internal state (supported descriptors, registered actions, exception handlers, etc.) for analyzers.
        /// </summary>
        /// <param name="analyzers">Analyzers whose state needs to be cleared.</param>
        [Obsolete("This API is no longer required to be invoked. Analyzer state is automatically cleaned up when CompilationWithAnalyzers instance is released.")]
        public static void ClearAnalyzerState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
        }

        /// <summary>
        /// Gets telemetry info for the given analyzer, such as count of registered actions, the total execution time (if <see cref="CompilationWithAnalyzersOptions.LogAnalyzerExecutionTime"/> is true), etc.
        /// </summary>
        public async Task<AnalyzerTelemetryInfo> GetAnalyzerTelemetryInfoAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            VerifyAnalyzerArgument(analyzer);

            try
            {
                var actionCounts = await GetAnalyzerActionCountsAsync(analyzer, cancellationToken).ConfigureAwait(false);
                var suppressionActionCounts = analyzer is DiagnosticSuppressor ? 1 : 0;
                var executionTime = GetAnalyzerExecutionTime(analyzer);
                return new AnalyzerTelemetryInfo(actionCounts, suppressionActionCounts, executionTime);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Gets the count of registered actions for the analyzer.
        /// </summary>
        private async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            var analysisScope = _compilationAnalysisScope.WithAnalyzers(ImmutableArray.Create(analyzer), this);
            using var driver = await CreateAndInitializeDriverAsync(_compilation, _analysisOptions, analysisScope, _suppressors, categorizeDiagnostics: true, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await driver.GetAnalyzerActionCountsAsync(analyzer, _compilation.Options, analysisScope, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the execution time for the given analyzer.
        /// </summary>
        private TimeSpan GetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer)
        {
            if (!_analysisOptions.LogAnalyzerExecutionTime)
            {
                return default;
            }

            return _analysisResultBuilder.GetAnalyzerExecutionTime(analyzer);
        }
    }
}

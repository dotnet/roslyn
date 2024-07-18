// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal abstract partial class BaseDiagnosticAndGeneratorItemSource : IAttachedCollectionSource
{
    private static readonly DiagnosticDescriptorComparer s_comparer = new();

    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
    private readonly IAsynchronousOperationListener _listener;
    private readonly BulkObservableCollection<BaseItem> _items = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AsyncBatchingWorkQueue _workQueue;

    private ReportDiagnostic _generalDiagnosticOption;
    private ImmutableDictionary<string, ReportDiagnostic>? _specificDiagnosticOptions;
    private AnalyzerConfigData? _analyzerConfigOptions;

    /// <summary>
    /// The analyzer reference that has been found. Once it's been assigned a non-null value, it'll never be assigned
    /// <see langword="null"/> again.
    /// </summary>
    private AnalyzerReference? _analyzerReference_DoNotAccessDirectly;

    public BaseDiagnosticAndGeneratorItemSource(
        Workspace workspace,
        ProjectId projectId,
        IAnalyzersCommandHandler commandHandler,
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        Workspace = workspace;
        ProjectId = projectId;
        CommandHandler = commandHandler;
        _diagnosticAnalyzerService = diagnosticAnalyzerService;

        _listener = listenerProvider.GetListener(FeatureAttribute.SourceGenerators);
        _workQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Idle,
            ProcessQueueAsync,
            _listener,
            _cancellationTokenSource.Token);
    }

    public AnalyzerReference? AnalyzerReference
    {
        get => _analyzerReference_DoNotAccessDirectly;
        protected set
        {
            Contract.ThrowIfTrue(_analyzerReference_DoNotAccessDirectly != null);
            if (value is null)
                return;

            _analyzerReference_DoNotAccessDirectly = value;

            // Listen for changes that would affect the set of analyzers/generators in this reference, and kick off work
            // to now get the items for this source.
            Workspace.WorkspaceChanged += OnWorkspaceChanged;
            _workQueue.AddWork();
        }
    }

    public Workspace Workspace { get; }
    public ProjectId ProjectId { get; }
    protected IAnalyzersCommandHandler CommandHandler { get; }

    public abstract object SourceItem { get; }

    [MemberNotNullWhen(true, nameof(AnalyzerReference))]
    // Defer actual determination and computation of the items until later.
    public bool HasItems => this.AnalyzerReference != null;

    public IEnumerable Items => _items;

    private async ValueTask ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var analyzerReference = this.AnalyzerReference;

        // If we haven't even determined which analyzer reference we're for, there's nothing to do.
        if (analyzerReference is null)
            return;

        // If the project went away, or no longer contains this analyzer.  Shut ourselves down.
        var project = this.Workspace.CurrentSolution.GetProject(this.ProjectId);
        if (project is null || !project.AnalyzerReferences.Contains(analyzerReference))
        {
            _cancellationTokenSource.Cancel();
            _items.Clear();

            _generalDiagnosticOption = default;
            _specificDiagnosticOptions = null;
            _analyzerConfigOptions = null;
            return;
        }

        var newDiagnosticItems = GenerateDiagnosticItems(project, analyzerReference);
        var newSourceGeneratorItems = await GenerateSourceGeneratorItemsAsync(
            project, analyzerReference).ConfigureAwait(false);

        if (!_items.SequenceEqual([.. newDiagnosticItems, .. newSourceGeneratorItems]))
        {
            _items.BeginBulkOperation();
            try
            {
                _items.AddRange(newDiagnosticItems);
                _items.AddRange(newSourceGeneratorItems);
            }
            finally
            {
                _items.EndBulkOperation();
            }
        }

        UpdateEffectiveSeverity();
        return;

        ImmutableArray<DiagnosticItem> GenerateDiagnosticItems(
            Project project,
            AnalyzerReference analyzerReference)
        {
            return analyzerReference.GetAnalyzers(project.Language)
                .SelectMany(a => _diagnosticAnalyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(a))
                .GroupBy(d => d.Id)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .SelectAsArray(g =>
                {
                    var selectedDiagnostic = g.OrderBy(d => d, s_comparer).First();
                    var effectiveSeverity = selectedDiagnostic.GetEffectiveSeverity(project.CompilationOptions!, _analyzerConfigOptions?.ConfigOptions, _analyzerConfigOptions?.TreeOptions);
                    return new DiagnosticItem(project.Id, analyzerReference, selectedDiagnostic, effectiveSeverity, CommandHandler);
                });
        }

        async Task<ImmutableArray<SourceGeneratorItem>> GenerateSourceGeneratorItemsAsync(
            Project project,
            AnalyzerReference analyzerReference)
        {
            var client = await RemoteHostClient.TryGetClientAsync(this.Workspace, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return [];

            if (analyzerReference is not AnalyzerFileReference analyzerFileReference)
                return [];

            var result = await client.TryInvokeAsync<IRemoteSourceGenerationService, ImmutableArray<SourceGeneratorIdentity>>(
                project,
                (service, solutionChecksum, cancellationToken) => service.GetSourceGeneratorIdentitiesAsync(
                    solutionChecksum, project.Id, analyzerFileReference.FullPath, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            // If the call fails, the OOP substrate will have already reported an error
            if (!result.HasValue)
                return [];

            return result.Value.SelectAsArray(
                identity => new SourceGeneratorItem(project.Id, identity, analyzerFileReference.FullPath));
        }

        void UpdateEffectiveSeverity()
        {
            var newGeneralDiagnosticOption = project.CompilationOptions!.GeneralDiagnosticOption;
            var newSpecificDiagnosticOptions = project.CompilationOptions!.SpecificDiagnosticOptions;
            var newAnalyzerConfigOptions = project.GetAnalyzerConfigOptions();

            if (newGeneralDiagnosticOption != _generalDiagnosticOption ||
                !object.ReferenceEquals(newSpecificDiagnosticOptions, _specificDiagnosticOptions) ||
                !object.ReferenceEquals(newAnalyzerConfigOptions?.TreeOptions, _analyzerConfigOptions?.TreeOptions) ||
                !object.ReferenceEquals(newAnalyzerConfigOptions?.ConfigOptions, _analyzerConfigOptions?.ConfigOptions))
            {
                _generalDiagnosticOption = newGeneralDiagnosticOption;
                _specificDiagnosticOptions = newSpecificDiagnosticOptions;
                _analyzerConfigOptions = newAnalyzerConfigOptions;

                foreach (var item in _items.OfType<DiagnosticItem>())
                {
                    var effectiveSeverity = item.Descriptor.GetEffectiveSeverity(project.CompilationOptions, newAnalyzerConfigOptions?.ConfigOptions, newAnalyzerConfigOptions?.TreeOptions);
                    item.UpdateEffectiveSeverity(effectiveSeverity);
                }
            }
        }
    }

    private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        if (e.Kind is WorkspaceChangeKind.SolutionCleared or
                      WorkspaceChangeKind.SolutionReloaded or
                      WorkspaceChangeKind.SolutionRemoved)
        {
            // Solution is going away or being reloaded. The work queue will detect this and clean up accordingly.
            _workQueue.AddWork();
        }
        else if (e.ProjectId == ProjectId)
        {
            if (e.Kind is WorkspaceChangeKind.ProjectRemoved)
            {
                // The project itself is being removed.  The work queue will detect this and clean up accordingly.
                _workQueue.AddWork();
            }
            else if (e.Kind == WorkspaceChangeKind.ProjectChanged)
            {
                _workQueue.AddWork();
            }
            else if (e.DocumentId != null)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                        _workQueue.AddWork();
                        break;
                }
            }
        }
    }
}

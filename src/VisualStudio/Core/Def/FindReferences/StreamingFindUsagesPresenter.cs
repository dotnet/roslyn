// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

[Export(typeof(IStreamingFindUsagesPresenter)), Shared]
internal sealed partial class StreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
{
    public const string RoslynFindUsagesTableDataSourceIdentifier =
        nameof(RoslynFindUsagesTableDataSourceIdentifier);

    public const string RoslynFindUsagesTableDataSourceSourceTypeIdentifier =
        nameof(RoslynFindUsagesTableDataSourceSourceTypeIdentifier);

    private readonly IServiceProvider _serviceProvider;

    public readonly ClassificationTypeMap TypeMap;
    public readonly IEditorFormatMapService FormatMapService;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly Lazy<IClassificationFormatMap> _lazyClassificationFormatMap;

    private readonly Workspace _workspace;
    private readonly IThreadingContext ThreadingContext;
    private readonly IGlobalOptionService _globalOptions;

    private readonly HashSet<AbstractTableDataSourceFindUsagesContext> _currentContexts = [];
    private readonly ImmutableArray<ITableColumnDefinition> _customColumns;

    /// <summary>
    /// Optional info bar that can be shown in the FindRefs window.  Useful, for example, for letting the user know
    /// if the results are incomplete because the solution is loading.  Only valid to read/write on the UI thread.
    /// </summary>
    private IVsInfoBarUIElement? _infoBar;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public StreamingFindUsagesPresenter(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IGlobalOptionService globalOptions,
        Shell.SVsServiceProvider serviceProvider,
        ClassificationTypeMap typeMap,
        IEditorFormatMapService formatMapService,
        IClassificationFormatMapService classificationFormatMapService,
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        [ImportMany] IEnumerable<Lazy<ITableColumnDefinition, NameMetadata>> columns)
        : this(workspace,
               threadingContext,
               serviceProvider,
               globalOptions,
               typeMap,
               formatMapService,
               classificationFormatMapService,
               GetCustomColumns(columns),
               asynchronousOperationListenerProvider)
    {
    }

    // Test only
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
    public StreamingFindUsagesPresenter(
        Workspace workspace,
        ExportProvider exportProvider)
        : this(workspace,
              exportProvider.GetExportedValue<IThreadingContext>(),
              exportProvider.GetExportedValue<Shell.SVsServiceProvider>(),
              exportProvider.GetExportedValue<IGlobalOptionService>(),
              exportProvider.GetExportedValue<ClassificationTypeMap>(),
              exportProvider.GetExportedValue<IEditorFormatMapService>(),
              exportProvider.GetExportedValue<IClassificationFormatMapService>(),
              exportProvider.GetExportedValues<ITableColumnDefinition>(),
              exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>())
    {
    }

    [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
    private StreamingFindUsagesPresenter(
        Workspace workspace,
        IThreadingContext threadingContext,
        Shell.SVsServiceProvider serviceProvider,
        IGlobalOptionService optionService,
        ClassificationTypeMap typeMap,
        IEditorFormatMapService formatMapService,
        IClassificationFormatMapService classificationFormatMapService,
        IEnumerable<ITableColumnDefinition> columns,
        IAsynchronousOperationListenerProvider asyncListenerProvider)
    {
        _workspace = workspace;
        ThreadingContext = threadingContext;
        _globalOptions = optionService;
        _serviceProvider = serviceProvider;
        TypeMap = typeMap;
        FormatMapService = formatMapService;
        _asyncListener = asyncListenerProvider.GetListener(FeatureAttribute.FindReferences);

        _lazyClassificationFormatMap = new Lazy<IClassificationFormatMap>(() =>
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            return classificationFormatMapService.GetClassificationFormatMap("tooltip");
        });

        _customColumns = columns.ToImmutableArray();
    }

    public IClassificationFormatMap ClassificationFormatMap
        => _lazyClassificationFormatMap.Value;

    private static IEnumerable<ITableColumnDefinition> GetCustomColumns(IEnumerable<Lazy<ITableColumnDefinition, NameMetadata>> columns)
    {
        foreach (var column in columns)
        {
            // PERF: Filter the columns by metadata name before selecting our custom columns.
            //       This is done to ensure that we do not load every single table control column
            //       that implements ITableColumnDefinition, which will cause unwanted assembly loads.
            switch (column.Metadata.Name)
            {
                case StandardTableKeyNames2.SymbolKind:
                case ContainingTypeColumnDefinition.ColumnName:
                case ContainingMemberColumnDefinition.ColumnName:
                case StandardTableKeyNames.Repository:
                case StandardTableKeyNames.ItemOrigin:
                    yield return column.Value;
                    break;
            }
        }
    }

    public void ClearAll()
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        foreach (var context in _currentContexts)
        {
            context.Clear();
        }
    }

    /// <summary>
    /// Start a search that may include Containing Type, Containing Member, or Kind information about the reference
    /// </summary>
    public (FindUsagesContext context, CancellationToken cancellationToken) StartSearch(string title, StreamingFindUsagesPresenterOptions options)
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        var vsFindAllReferencesService = (IFindAllReferencesService)_serviceProvider.GetService(typeof(SVsFindAllReferences));

        // Get the appropriate window for FAR results to go into.
        var window = vsFindAllReferencesService.StartSearch(title);

        // If there is an info bar on our tool window, clear it out.
        RemoveExistingInfoBar();

        // Keep track of the users preference for grouping by definition if we don't already know it.
        // We need this because we disable the Definition column when we're not showing references
        // (i.e. GoToImplementation/GoToDef).  However, we want to restore the user's choice if they
        // then do another FindAllReferences.
        var desiredGroupingPriority = _globalOptions.GetOption(FindUsagesPresentationOptionsStorage.DefinitionGroupingPriority);
        if (desiredGroupingPriority < 0)
        {
            StoreCurrentGroupingPriority(window);
        }

        var context = options.SupportsReferences
            ? StartSearchWithReferences(window, desiredGroupingPriority, options.IncludeContainingTypeAndMemberColumns, options.IncludeKindColumn)
            : StartSearchWithoutReferences(window, options.IncludeContainingTypeAndMemberColumns, options.IncludeKindColumn);

        // Keep track of this context object as long as it is being displayed in the UI.
        // That way we can Clear it out if requested by a client.  When the context is
        // no longer being displayed, VS will dispose it and it will remove itself from
        // this set.
        _currentContexts.Add(context);
        return (context, context.CancellationTokenSource!.Token);
    }

    private AbstractTableDataSourceFindUsagesContext StartSearchWithReferences(
        IFindAllReferencesWindow window, int desiredGroupingPriority, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
    {
        // Ensure that the window's definition-grouping reflects what the user wants.
        // i.e. we may have disabled this column for a previous GoToImplementation call. 
        // We want to still show the column as long as the user has not disabled it themselves.
        var definitionColumn = window.GetDefinitionColumn();
        if (definitionColumn.GroupingPriority != desiredGroupingPriority)
        {
            SetDefinitionGroupingPriority(window, desiredGroupingPriority);
        }

        // If the user changes the grouping, then store their current preference.
        var tableControl = (IWpfTableControl2)window.TableControl;
        tableControl.GroupingsChanged += (s, e) => StoreCurrentGroupingPriority(window);

        return new WithReferencesFindUsagesContext(
            this, window, _customColumns, _globalOptions, includeContainingTypeAndMemberColumns, includeKindColumn, ThreadingContext);
    }

    private AbstractTableDataSourceFindUsagesContext StartSearchWithoutReferences(
        IFindAllReferencesWindow window, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
    {
        // If we're not showing references, then disable grouping by definition, as that will
        // just lead to a poor experience.  i.e. we'll have the definition entry buckets, 
        // with the same items showing underneath them.
        SetDefinitionGroupingPriority(window, 0);
        return new WithoutReferencesFindUsagesContext(
            this, window, _customColumns, _globalOptions, includeContainingTypeAndMemberColumns, includeKindColumn, ThreadingContext);
    }

    private void StoreCurrentGroupingPriority(IFindAllReferencesWindow window)
    {
        _globalOptions.SetGlobalOption(FindUsagesPresentationOptionsStorage.DefinitionGroupingPriority, window.GetDefinitionColumn().GroupingPriority);
    }

    private void SetDefinitionGroupingPriority(IFindAllReferencesWindow window, int priority)
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        using var _ = ArrayBuilder<ColumnState>.GetInstance(out var newColumns);
        var tableControl = (IWpfTableControl2)window.TableControl;

        foreach (var columnState in window.TableControl.ColumnStates)
        {
            var columnState2 = columnState as ColumnState2;
            if (columnState2?.Name == StandardTableColumnDefinitions2.Definition)
            {
                newColumns.Add(new ColumnState2(
                    columnState2.Name,
                    isVisible: false,
                    width: columnState2.Width,
                    sortPriority: columnState2.SortPriority,
                    descendingSort: columnState2.DescendingSort,
                    groupingPriority: priority));
            }
            else
            {
                newColumns.Add(columnState);
            }
        }

        tableControl.SetColumnStates(newColumns);
    }

    private static (Guid, string projectName) GetGuidAndProjectName(Document document)
    {
        // The FAR system needs to know the guid for the project that a def/reference is 
        // from (to support features like filtering).  Normally that would mean we could
        // only support this from a VisualStudioWorkspace.  However, we want till work 
        // in cases like Any-Code (which does not use a VSWorkspace).  So we are tolerant
        // when we have another type of workspace.  This means we will show results, but
        // certain features (like filtering) may not work in that context.
        var vsWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspace;

        var (projectName, _) = document.Project.State.NameAndFlavor;
        projectName ??= document.Project.Name;

        var guid = vsWorkspace?.GetProjectGuid(document.Project.Id) ?? Guid.Empty;

        return (guid, projectName);
    }

    private void RemoveExistingInfoBar()
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        var infoBar = _infoBar;
        _infoBar = null;
        if (infoBar != null)
        {
            var infoBarHost = GetInfoBarHost();
            infoBarHost?.RemoveInfoBar(infoBar);
        }
    }

    private IVsInfoBarHost? GetInfoBarHost()
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        // Guid of the FindRefs window.  Defined here:
        // https://devdiv.visualstudio.com/DevDiv/_git/VS?path=/src/env/ErrorList/Pkg/Guids.cs&version=GBmain&line=24
        var guid = new Guid("a80febb4-e7e0-4147-b476-21aaf2453969");

        if (_serviceProvider.GetService(typeof(SVsUIShell)) is not IVsUIShell uiShell ||
            uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref guid, out var windowFrame) != VSConstants.S_OK ||
            windowFrame == null ||
            windowFrame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out var infoBarHostObj) != VSConstants.S_OK ||
            infoBarHostObj is not IVsInfoBarHost infoBarHost)
        {
            return null;
        }

        return infoBarHost;
    }

    private async Task ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken)
    {
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        RemoveExistingInfoBar();

        if (_serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) is not IVsInfoBarUIFactory factory)
            return;

        var infoBarHost = GetInfoBarHost();
        if (infoBarHost == null)
            return;

        _infoBar = factory.CreateInfoBar(new InfoBarModel(
            message,
            severity switch
            {
                NotificationSeverity.Information => KnownMonikers.StatusInformation,
                NotificationSeverity.Error => KnownMonikers.StatusError,
                NotificationSeverity.Warning => KnownMonikers.StatusWarning,
                _ => throw ExceptionUtilities.UnexpectedValue(severity),
            },
            isCloseButtonVisible: false));

        infoBarHost.AddInfoBar(_infoBar);
    }
}

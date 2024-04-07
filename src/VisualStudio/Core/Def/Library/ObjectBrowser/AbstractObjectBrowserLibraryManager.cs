// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using IServiceProvider = System.IServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

internal abstract partial class AbstractObjectBrowserLibraryManager : AbstractLibraryManager, IDisposable
{
    internal readonly VisualStudioWorkspace Workspace;

    internal ILibraryService LibraryService => _libraryService.Value;

    private readonly Lazy<ILibraryService> _libraryService;
    private readonly string _languageName;

    private uint _classVersion;
    private uint _membersVersion;
    private uint _packageVersion;

    private ObjectListItem _activeListItem;
    private AbstractListItemFactory _listItemFactory;
    private readonly object _classMemberGate = new();

    protected AbstractObjectBrowserLibraryManager(
        string languageName,
        Guid libraryGuid,
        IServiceProvider serviceProvider,
        IComponentModel componentModel,
        VisualStudioWorkspace workspace)
        : base(libraryGuid, componentModel, serviceProvider)
    {
        _languageName = languageName;

        Workspace = workspace;
        Workspace.WorkspaceChanged += OnWorkspaceChanged;

        _libraryService = new Lazy<ILibraryService>(() => Workspace.Services.GetLanguageServices(_languageName).GetService<ILibraryService>());
    }

    internal abstract AbstractDescriptionBuilder CreateDescriptionBuilder(
        IVsObjectBrowserDescription3 description,
        ObjectListItem listItem,
        Project project);

    internal abstract AbstractListItemFactory CreateListItemFactory();

    private AbstractListItemFactory GetListItemFactory()
    {
        _listItemFactory ??= CreateListItemFactory();

        return _listItemFactory;
    }

    public void Dispose()
        => this.Workspace.WorkspaceChanged -= OnWorkspaceChanged;

    private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        switch (e.Kind)
        {
            case WorkspaceChangeKind.DocumentChanged:
                // Ensure the text version actually changed. This is necessary to not
                // cause Class View to update simply when a document is opened.
                var oldDocument = e.OldSolution.GetDocument(e.DocumentId);
                var newDocument = e.NewSolution.GetDocument(e.DocumentId);

                UpdateDocument(oldDocument, newDocument);
                break;

            case WorkspaceChangeKind.ProjectAdded:
            case WorkspaceChangeKind.ProjectChanged:
            case WorkspaceChangeKind.ProjectReloaded:
            case WorkspaceChangeKind.ProjectRemoved:
                UpdatePackageVersion();
                break;

            case WorkspaceChangeKind.SolutionAdded:
            case WorkspaceChangeKind.SolutionChanged:
            case WorkspaceChangeKind.SolutionCleared:
            case WorkspaceChangeKind.SolutionReloaded:
            case WorkspaceChangeKind.SolutionRemoved:
                UpdatePackageVersion();
                break;
        }
    }

    private void UpdateDocument(Document oldDocument, Document newDocument)
    {
        try
        {
            // If the versions are the same, avoid updating the object browser. However, avoid
            // loading the document to determine the version because it can cause extreme memory
            // pressure during batch changes.
            if (oldDocument.TryGetTextVersion(out var oldTextVersion)
                && newDocument.TryGetTextVersion(out var newTextVersion)
                && oldTextVersion == newTextVersion)
            {
                return;
            }

            UpdateClassAndMemberVersions();
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Diagnostic))
        {
        }
    }

    internal uint ClassVersion
    {
        get
        {
            lock (_classMemberGate)
            {
                return _classVersion;
            }
        }
    }

    internal uint MembersVersion
    {
        get
        {
            lock (_classMemberGate)
            {
                return _membersVersion;
            }
        }
    }

    internal uint PackageVersion
    {
        get { return _packageVersion; }
    }

    internal void UpdateClassAndMemberVersions()
    {
        lock (_classMemberGate)
        {
            UpdateClassVersion();
            UpdateMembersVersion();
        }
    }

    private void UpdateClassVersion()
        => _classVersion = unchecked(_classVersion + 1);

    private void UpdateMembersVersion()
        => _membersVersion = unchecked(_membersVersion + 1);

    internal void UpdatePackageVersion()
        => _packageVersion = unchecked(_packageVersion + 1);

    internal void SetActiveListItem(ObjectListItem listItem)
        => _activeListItem = listItem;

    private bool IsFindAllReferencesSupported()
    {
        if (_activeListItem == null)
        {
            return false;
        }

        return _activeListItem.SupportsFindAllReferences;
    }

    internal Project GetProject(ProjectId projectId)
        => this.Workspace.CurrentSolution.GetProject(projectId);

    internal Project GetProject(ObjectListItem listItem)
    {
        var projectId = listItem.ProjectId;
        if (projectId == null)
        {
            return null;
        }

        return this.GetProject(projectId);
    }

    internal Compilation GetCompilation(ProjectId projectId)
    {
        var project = GetProject(projectId);
        if (project == null)
        {
            return null;
        }

        return project
            .GetCompilationAsync(CancellationToken.None)
            .WaitAndGetResult_ObjectBrowser(CancellationToken.None);
    }

    public override uint GetLibraryFlags()
    {
        // Note: the legacy C# code also included LF_SUPPORTSLISTREFERENCES,
        // but that should be handled now by the FindResults LibraryManager.
        return
            (uint)_LIB_FLAGS.LF_PROJECT |
            (uint)_LIB_FLAGS.LF_EXPANDABLE |
            (uint)_LIB_FLAGS2.LF_SUPPORTSFILTERING |
            (uint)_LIB_FLAGS2.LF_SUPPORTSBASETYPES |
            (uint)_LIB_FLAGS2.LF_SUPPORTSINHERITEDMEMBERS |
            (uint)_LIB_FLAGS2.LF_SUPPORTSPRIVATEMEMBERS |
            (uint)_LIB_FLAGS2.LF_SUPPORTSPROJECTREFERENCES |
            (uint)_LIB_FLAGS2.LF_SUPPORTSCLASSDESIGNER;
    }

    protected override uint GetSupportedCategoryFields(uint category)
    {
        switch (category)
        {
            case (uint)LIB_CATEGORY.LC_MEMBERTYPE:
                return
                    (uint)_LIBCAT_MEMBERTYPE.LCMT_METHOD |
                    (uint)_LIBCAT_MEMBERTYPE.LCMT_FIELD |
                    (uint)_LIBCAT_MEMBERTYPE.LCMT_PROPERTY;

            case (uint)LIB_CATEGORY.LC_MEMBERACCESS:
                return
                    (uint)_LIBCAT_MEMBERACCESS.LCMA_PUBLIC |
                    (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE |
                    (uint)_LIBCAT_MEMBERACCESS.LCMA_PROTECTED |
                    (uint)_LIBCAT_MEMBERACCESS.LCMA_PACKAGE |
                    (uint)_LIBCAT_MEMBERACCESS.LCMA_SEALED;

            case (uint)_LIB_CATEGORY2.LC_MEMBERINHERITANCE:
                return
                    (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_IMMEDIATE |
                    (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_INHERITED;

            case (uint)LIB_CATEGORY.LC_CLASSACCESS:
                return
                    (uint)_LIBCAT_CLASSACCESS.LCCA_PUBLIC |
                    (uint)_LIBCAT_CLASSACCESS.LCCA_PROTECTED |
                    (uint)_LIBCAT_CLASSACCESS.LCCA_PACKAGE |
                    (uint)_LIBCAT_CLASSACCESS.LCCA_PRIVATE |
                    (uint)_LIBCAT_CLASSACCESS.LCCA_SEALED;

            case (uint)LIB_CATEGORY.LC_CLASSTYPE:
                return
                    (uint)_LIBCAT_CLASSTYPE.LCCT_CLASS |
                    (uint)_LIBCAT_CLASSTYPE.LCCT_INTERFACE |
                    (uint)_LIBCAT_CLASSTYPE.LCCT_ENUM |
                    (uint)_LIBCAT_CLASSTYPE.LCCT_STRUCT |
                    (uint)_LIBCAT_CLASSTYPE.LCCT_UNION |
                    (uint)_LIBCAT_CLASSTYPE.LCCT_DELEGATE |
                    (uint)_LIBCAT_CLASSTYPE.LCCT_MODULE;

            case (uint)LIB_CATEGORY.LC_ACTIVEPROJECT:
                return (uint)_LIBCAT_ACTIVEPROJECT.LCAP_SHOWALWAYS;

            case (uint)LIB_CATEGORY.LC_LISTTYPE:
                return
                    (uint)_LIB_LISTTYPE.LLT_CLASSES |
                    (uint)_LIB_LISTTYPE.LLT_NAMESPACES |
                    (uint)_LIB_LISTTYPE.LLT_MEMBERS |
                    (uint)_LIB_LISTTYPE.LLT_HIERARCHY |
                    (uint)_LIB_LISTTYPE.LLT_PACKAGE;

            case (uint)LIB_CATEGORY.LC_VISIBILITY:
                return
                    (uint)_LIBCAT_VISIBILITY.LCV_VISIBLE |
                    (uint)_LIBCAT_VISIBILITY.LCV_HIDDEN;

            case (uint)LIB_CATEGORY.LC_MODIFIER:
                return
                    (uint)_LIBCAT_MODIFIERTYPE.LCMDT_FINAL |
                    (uint)_LIBCAT_MODIFIERTYPE.LCMDT_STATIC;

            case (uint)_LIB_CATEGORY2.LC_HIERARCHYTYPE:
                return
                    (uint)_LIBCAT_HIERARCHYTYPE.LCHT_BASESANDINTERFACES |
                    (uint)_LIBCAT_HIERARCHYTYPE.LCHT_PROJECTREFERENCES;

            case (uint)_LIB_CATEGORY2.LC_PHYSICALCONTAINERTYPE:
                return
                    (uint)_LIBCAT_PHYSICALCONTAINERTYPE.LCPT_GLOBAL |
                    (uint)_LIBCAT_PHYSICALCONTAINERTYPE.LCPT_PROJECT |
                    (uint)_LIBCAT_PHYSICALCONTAINERTYPE.LCPT_PROJECTREFERENCE;
        }

        Debug.Fail("Unknown category: " + category.ToString());
        return 0;
    }

    protected override IVsSimpleObjectList2 GetList(uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch)
    {
        var listKind = Helpers.ListTypeToObjectListKind(listType);

        if (Helpers.IsFindSymbol(flags))
        {
            var projectAndAssemblySet = this.GetAssemblySet(this.Workspace.CurrentSolution, _languageName, CancellationToken.None);
            return GetSearchList(listKind, flags, pobSrch, projectAndAssemblySet);
        }

        if (listKind == ObjectListKind.Hierarchy)
        {
            return null;
        }

        Debug.Assert(listKind == ObjectListKind.Projects);

        return new ObjectList(ObjectListKind.Projects, flags, this, this.GetProjectListItems(this.Workspace.CurrentSolution, _languageName, flags));
    }

    protected override uint GetUpdateCounter()
        => _packageVersion;

    protected override int CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo)
    {
        Debug.Assert(rgSymbolNodes != null || ulcNodes > 0, "Invalid input parameters into CreateNavInfo");

        ppNavInfo = null;

        var count = 0;
        string referenceOwnerName = null;

        string libraryName;
        if (rgSymbolNodes[0].dwType != (uint)_LIB_LISTTYPE.LLT_PACKAGE)
        {
            Debug.Fail("Symbol description should always contain LLT_PACKAGE node as first node");
            return VSConstants.E_INVALIDARG;
        }
        else
        {
            count++;

            // If second node is also a package node, the below is the inference Node for
            // which NavInfo is generated is a 'referenced' node in CV
            // First package node ---> project item under which referenced node is displayed
            // Second package node ---> actual lib item node i.e., referenced assembly
            if (ulcNodes > 1 && rgSymbolNodes[1].dwType == (uint)_LIB_LISTTYPE.LLT_PACKAGE)
            {
                count++;

                referenceOwnerName = rgSymbolNodes[0].pszName;
                libraryName = rgSymbolNodes[1].pszName;
            }
            else
            {
                libraryName = rgSymbolNodes[0].pszName;
            }
        }

        var namespaceName = SharedPools.Default<StringBuilder>().AllocateAndClear();
        var className = SharedPools.Default<StringBuilder>().AllocateAndClear();
        var memberName = string.Empty;

        // Populate namespace, class and member names
        // Generate flattened names for nested namespaces and classes
        for (; count < ulcNodes; count++)
        {
            switch (rgSymbolNodes[count].dwType)
            {
                case (uint)_LIB_LISTTYPE.LLT_NAMESPACES:
                    if (namespaceName.Length > 0)
                    {
                        namespaceName.Append('.');
                    }

                    namespaceName.Append(rgSymbolNodes[count].pszName);
                    break;

                case (uint)_LIB_LISTTYPE.LLT_CLASSES:
                    if (className.Length > 0)
                    {
                        className.Append('.');
                    }

                    className.Append(rgSymbolNodes[count].pszName);
                    break;

                case (uint)_LIB_LISTTYPE.LLT_MEMBERS:
                    if (memberName.Length > 0)
                    {
                        Debug.Fail("Symbol description cannot contain more than one LLT_MEMBERS node.");
                    }

                    memberName = rgSymbolNodes[count].pszName;
                    break;
            }
        }

        // TODO: Make sure we pass the right value for Visual Basic.
        ppNavInfo = this.LibraryService.NavInfoFactory.Create(libraryName, referenceOwnerName, namespaceName.ToString(), className.ToString(), memberName);

        SharedPools.Default<StringBuilder>().ClearAndFree(namespaceName);
        SharedPools.Default<StringBuilder>().ClearAndFree(className);

        return VSConstants.S_OK;
    }

    internal IVsNavInfo GetNavInfo(SymbolListItem symbolListItem, bool useExpandedHierarchy)
    {
        var project = GetProject(symbolListItem);
        if (project == null)
        {
            return null;
        }

        var compilation = symbolListItem.GetCompilation(this.Workspace);
        if (compilation == null)
        {
            return null;
        }

        var symbol = symbolListItem.ResolveSymbol(compilation);
        if (symbol == null)
        {
            return null;
        }

        if (symbolListItem is MemberListItem)
        {
            return this.LibraryService.NavInfoFactory.CreateForMember(symbol, project, compilation, useExpandedHierarchy);
        }
        else if (symbolListItem is TypeListItem)
        {
            return this.LibraryService.NavInfoFactory.CreateForType((INamedTypeSymbol)symbol, project, compilation, useExpandedHierarchy);
        }
        else if (symbolListItem is NamespaceListItem)
        {
            return this.LibraryService.NavInfoFactory.CreateForNamespace((INamespaceSymbol)symbol, project, compilation, useExpandedHierarchy);
        }

        return this.LibraryService.NavInfoFactory.CreateForProject(project);
    }

    protected override bool TryQueryStatus(Guid commandGroup, uint commandId, ref OLECMDF commandFlags)
    {
        if (commandGroup == VsMenus.guidStandardCommandSet97)
        {
            switch (commandId)
            {
                case (uint)VSConstants.VSStd97CmdID.FindReferences:
                    if (IsFindAllReferencesSupported())
                    {
                        commandFlags = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED;
                    }
                    else
                    {
                        commandFlags = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE;
                    }

                    return true;
            }
        }

        return false;
    }

    protected override bool TryExec(Guid commandGroup, uint commandId)
    {
        if (commandGroup == VsMenus.guidStandardCommandSet97)
        {
            switch (commandId)
            {
                case (uint)VSConstants.VSStd97CmdID.FindReferences:
                    var symbolListItem = _activeListItem as SymbolListItem;
                    if (symbolListItem?.ProjectId != null)
                    {
                        var project = this.Workspace.CurrentSolution.GetProject(symbolListItem.ProjectId);
                        if (project != null)
                        {
                            // Note: we kick of FindReferencesAsync in a 'fire and forget' manner. We don't want to
                            // block the UI thread while we compute the references, and the references will be
                            // asynchronously added to the FindReferences window as they are computed.  The user
                            // also knows something is happening as the window, with the progress-banner will pop up
                            // immediately.
                            var streamingPresenter = ComponentModel.GetService<IStreamingFindUsagesPresenter>();
                            var asynchronousOperationListener = ComponentModel.GetService<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.LibraryManager);
                            var globalOptions = ComponentModel.GetService<IGlobalOptionService>();

                            var asyncToken = asynchronousOperationListener.BeginAsyncOperation(nameof(AbstractObjectBrowserLibraryManager) + "." + nameof(TryExec));
                            FindReferencesAsync(streamingPresenter, symbolListItem, project, globalOptions.GetClassificationOptionsProvider()).CompletesAsyncOperation(asyncToken);
                            return true;
                        }
                    }

                    break;
            }
        }

        return false;
    }

    private static async Task FindReferencesAsync(
        IStreamingFindUsagesPresenter presenter, SymbolListItem symbolListItem, Project project, OptionsProvider<ClassificationOptions> classificationOptions)
    {
        try
        {
            // Let the presented know we're starting a search.  It will give us back the context object that the FAR
            // service will push results into.  Because we kicked off this work in a fire and forget fashion,
            // the presenter owns canceling this work (i.e. if it's closed or if another FAR request is made).
            var (context, cancellationToken) = presenter.StartSearch(EditorFeaturesResources.Find_References, new StreamingFindUsagesPresenterOptions { SupportsReferences = true });

            try
            {
                // Kick off the work to do the actual finding on a BG thread.  That way we don'
                // t block the calling (UI) thread too long if we happen to do our work on this
                // thread.
                await Task.Run(
                    () => FindReferencesAsync(symbolListItem, project, context, classificationOptions, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Critical))
        {
        }
    }

    private static async Task FindReferencesAsync(
        SymbolListItem symbolListItem,
        Project project,
        FindUsagesContext context,
        OptionsProvider<ClassificationOptions> classificationOptions,
        CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var symbol = symbolListItem.ResolveSymbol(compilation);
        if (symbol != null)
            await AbstractFindUsagesService.FindSymbolReferencesAsync(context, symbol, project, classificationOptions, cancellationToken).ConfigureAwait(false);
    }
}

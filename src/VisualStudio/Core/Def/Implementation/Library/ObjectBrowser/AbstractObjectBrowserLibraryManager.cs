﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using IServiceProvider = System.IServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractObjectBrowserLibraryManager : AbstractLibraryManager, IDisposable
    {
        internal readonly VisualStudioWorkspace Workspace;
        internal readonly ILibraryService LibraryService;

        private readonly string _languageName;
        private readonly __SymbolToolLanguage _preferredLanguage;

        private uint _classVersion;
        private uint _membersVersion;
        private uint _packageVersion;

        private ObjectListItem _activeListItem;
        private AbstractListItemFactory _listItemFactory;
        private object _classMemberGate = new object();

        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;

        protected AbstractObjectBrowserLibraryManager(
            string languageName, 
            Guid libraryGuid, 
            __SymbolToolLanguage preferredLanguage, 
            IServiceProvider serviceProvider)
            : base(libraryGuid, serviceProvider)
        {
            _languageName = languageName;
            _preferredLanguage = preferredLanguage;

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            this.Workspace = componentModel.GetService<VisualStudioWorkspace>();
            this.LibraryService = this.Workspace.Services.GetLanguageServices(languageName).GetService<ILibraryService>();
            this.Workspace.WorkspaceChanged += OnWorkspaceChanged;

            this._streamingPresenters = componentModel.DefaultExportProvider.GetExports<IStreamingFindUsagesPresenter>();
        }

        internal abstract AbstractDescriptionBuilder CreateDescriptionBuilder(
            IVsObjectBrowserDescription3 description,
            ObjectListItem listItem,
            Project project);

        internal abstract AbstractListItemFactory CreateListItemFactory();

        private AbstractListItemFactory GetListItemFactory()
        {
            if (_listItemFactory == null)
            {
                _listItemFactory = CreateListItemFactory();
            }

            return _listItemFactory;
        }

        public void Dispose()
        {
            this.Workspace.WorkspaceChanged -= OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.DocumentChanged:
                    // Ensure the text version actually changed. This is necessary to not
                    // cause Class View to update simply when a document is opened.
                    var oldDocument = e.OldSolution.GetDocument(e.DocumentId);
                    var newDocument = e.NewSolution.GetDocument(e.DocumentId);

                    // make sure we do this in background thread. we don't care about ordering of events
                    // we just need to refresh OB at some point if it ever needs to be updated
                    // link to the bug tracking root cause  - https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=169649&_a=edit
                    Task.Run(() => DocumentChangedAsync(oldDocument, newDocument));
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

        private async Task DocumentChangedAsync(Document oldDocument, Document newDocument)
        {
            try
            {
                var oldTextVersion = await oldDocument.GetTextVersionAsync(CancellationToken.None).ConfigureAwait(false);
                var newTextVersion = await newDocument.GetTextVersionAsync(CancellationToken.None).ConfigureAwait(false);

                if (oldTextVersion != newTextVersion)
                {
                    UpdateClassAndMemberVersions();
                }
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                // make it crash VS on any exception
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
        {
            _classVersion = unchecked(_classVersion + 1);
        }

        private void UpdateMembersVersion()
        {
            _membersVersion = unchecked(_membersVersion + 1);
        }

        internal void UpdatePackageVersion()
        {
            _packageVersion = unchecked(_packageVersion + 1);
        }

        internal void SetActiveListItem(ObjectListItem listItem)
        {
            _activeListItem = listItem;
        }

        private bool IsFindAllReferencesSupported()
        {
            if (_activeListItem == null)
            {
                return false;
            }

            return _activeListItem.SupportsFindAllReferences;
        }

        internal Project GetProject(ProjectId projectId)
        {
            return this.Workspace.CurrentSolution.GetProject(projectId);
        }

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

            return new ObjectList(ObjectListKind.Projects, flags, this, this.GetProjectListItems(this.Workspace.CurrentSolution, _languageName, flags, CancellationToken.None));
        }

        protected override uint GetUpdateCounter()
        {
            return _packageVersion;
        }

        protected override int CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo)
        {
            Debug.Assert(rgSymbolNodes != null || ulcNodes > 0, "Invalid input parameters into CreateNavInfo");

            ppNavInfo = null;

            var count = 0;
            string libraryName = null;
            string referenceOwnerName = null;

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
                            namespaceName.Append(".");
                        }

                        namespaceName.Append(rgSymbolNodes[count].pszName);
                        break;

                    case (uint)_LIB_LISTTYPE.LLT_CLASSES:
                        if (className.Length > 0)
                        {
                            className.Append(".");
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
                        var streamingPresenter = _streamingPresenters.FirstOrDefault()?.Value;
                        var symbolListItem = _activeListItem as SymbolListItem;

                        if (streamingPresenter != null && symbolListItem?.ProjectId != null)
                        {
                            var project = this.Workspace.CurrentSolution.GetProject(symbolListItem.ProjectId);
                            if (project != null)
                            {
                                // Note: we kick of FindReferencesAsync in a 'fire and forget' manner.
                                // We don't want to block the UI thread while we compute the references,
                                // and the references will be asynchronously added to the FindReferences
                                // window as they are computed.  The user also knows something is happening
                                // as the window, with the progress-banner will pop up immediately.
                                var task = FindReferencesAsync(streamingPresenter, symbolListItem, project);
                                return true;
                            }
                        }

                        break;
                }
            }

            return false;
        }

        private async Task FindReferencesAsync(
            IStreamingFindUsagesPresenter presenter, SymbolListItem symbolListItem, Project project)
        {
            try
            {
                // Let the presented know we're starting a search.  It will give us back
                // the context object that the FAR service will push results into.
                var context = presenter.StartSearch(
                    EditorFeaturesResources.Find_References, supportsReferences: true);

                var cancellationToken = context.CancellationToken;

                // Kick off the work to do the actual finding on a BG thread.  That way we don'
                // t block the calling (UI) thread too long if we happen to do our work on this
                // thread.
                await Task.Run(async () =>
                {
                    await FindReferencesAsync(symbolListItem, project, context, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);

                // Note: we don't need to put this in a finally.  The only time we might not hit
                // this is if cancellation or another error gets thrown.  In the former case,
                // that means that a new search has started.  We don't care about telling the
                // context it has completed.  In the latter case something wrong has happened
                // and we don't want to run any more code in this particular context.
                await context.OnCompletedAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }
        }

        private static async Task FindReferencesAsync(SymbolListItem symbolListItem, Project project, CodeAnalysis.FindUsages.FindUsagesContext context, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = symbolListItem.ResolveSymbol(compilation);
            if (symbol != null)
            {
                await AbstractFindUsagesService.FindSymbolReferencesAsync(
                    context, symbol, project, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

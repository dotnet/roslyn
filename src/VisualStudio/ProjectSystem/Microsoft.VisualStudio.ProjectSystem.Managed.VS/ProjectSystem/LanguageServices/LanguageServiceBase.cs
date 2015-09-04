// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Designers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.OLE.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides the base <see langword="abstract"/> for all services that integrate the language service with the project system.
    /// </summary>
    internal abstract class LanguageServiceBase :
        IVsIntellisenseProjectHost,
        IDisposable,
        ICodeModelProvider,
        IProjectWithIntellisense
    {
        /// <summary>
        /// The names of the rules that represent items that are interesting to the language service.
        /// </summary>
        private static readonly ImmutableHashSet<string> WatchedEvaluationRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(CSharp.SchemaName)
            .Add(ProjectReference.SchemaName);

        /// <summary>
        /// The names of rules that represent resolved references.
        /// </summary>
        private static readonly ImmutableHashSet<string> WatchedDesignTimeBuildRules = Empty.OrdinalIgnoreCaseStringSet
            .Add(ResolvedAssemblyReference.SchemaName)
            .Add(ResolvedCOMReference.SchemaName)
            .Add(ResolvedProjectReference.SchemaName);

        /// <summary>
        /// The intellisense project itself.
        /// </summary>
        private IVsIntellisenseProject _intellisenseEngine;

        /// <summary>
        /// The link that represents the design-time build subscription.
        /// </summary>
        private IDisposable _designTimeBuildSubscriptionLink;

        /// <summary>
        /// The link that represents the project evaluation subscription.
        /// </summary>
        private IDisposable _evaluationSubscriptionLink;

        /// <summary>
        /// A map of the full paths to the projects referenced from this project,
        /// and their current state as being referenced via their intellisense project or their file output.
        /// </summary>
        private ImmutableDictionary<string, ProjectReferenceState> _projectReferenceFullPaths = ImmutableDictionary<string, ProjectReferenceState>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        protected LanguageServiceBase(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));

            _projectVsServices = projectVsServices;
        }

        IVsIntellisenseProject IProjectWithIntellisense.IntellisenseProject
        {
            get { return this._intellisenseEngine; }
        }

        /// <summary>
        /// Gets the unconfigured project.
        /// </summary>
        [Import]
        public UnconfiguredProject UnconfiguredProject
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the GUID of the Intellisense provider to create.
        /// </summary>
        protected abstract Guid ProviderGuid
        {
            get;
        }

        /// <summary>
        /// Gets or sets exports from the active configured project.
        /// </summary>
        [Import]
        private ActiveConfiguredProject<ActiveConfiguredProjectHelper> ActiveConfigurationExports
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Visual Studio IServiceProvider.
        /// </summary>
        [Import]
        private SVsServiceProvider ServiceProvider
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the project tree service.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]
        private IProjectTreeService ProjectTreeService
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the project subscription service.
        /// </summary>
        [Import]
        private IActiveConfiguredProjectSubscriptionService ActiveConfiguredProjectSubscriptionService
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the project fault handler service.
        /// </summary>
        [Import]
        private IProjectFaultHandlerService ProjectFaultHandlerService
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the asynchronous task service.
        /// </summary>
        [Import(ExportContractNames.Scopes.UnconfiguredProject)]
        private IProjectAsynchronousTasksService ProjectAsynchronousTasksService
        {
            get;
            set;
        }

        /// <summary>
        /// The thread handling service.
        /// </summary>
        [Import]
        private IThreadHandling ThreadHandling
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the physical tree provider.
        /// </summary>
        [Import(ExportContractNames.ProjectTreeProviders.PhysicalViewTree)]
        private Lazy<IProjectTreeProvider> PhysicalProjectTreeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the service where we register our language service projects.
        /// </summary>
        [Import]
        private ILanguageServiceRegister LanguageServiceRegister
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the project async load dashboard.
        /// </summary>
        [Import]
        private IProjectAsyncLoadDashboard ProjectAsyncLoadDashboard
        {
            get;
            set;
        }

        /// <summary>
        /// Invoked when the UnconfiguredProject is first loaded to initialize language services.
        /// </summary>
        protected async Task InitializeAsync()
        {
            this.ProjectAsynchronousTasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

            // Don't start until the project has been loaded as far as the IDE is concerned.
            await this.ProjectAsyncLoadDashboard.ProjectLoadedInHost;

            // Defer this work until VS has idle time.  Otherwise we'll block the UI thread to load the MSBuild project evaluation
            // during synchronous project load time.
            await ThreadHelper.JoinableTaskFactory.RunAsync(
                VsTaskRunContext.UIThreadBackgroundPriority,
                async delegate
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await Task.Yield();

                    using (this.ProjectAsynchronousTasksService.LoadedProject())
                    {
                        // hack to ensure webproj package is properly sited, by forcing it to load with a QueryService call for
                        // one of the services it implements.
                        var tmpObj = Package.GetGlobalService(typeof(SWebApplicationCtxSvc)) as IWebApplicationCtxSvc;
                        Report.IfNotPresent(tmpObj);
                        if (tmpObj == null)
                        {
                            return;
                        }

                        // Create the Intellisense engine for C#
                        var registry = this.ServiceProvider.GetService(typeof(SLocalRegistry)) as ILocalRegistry3;
                        Assumes.Present(registry);
                        IntPtr pIntellisenseEngine = IntPtr.Zero;
                        try
                        {
                            Marshal.ThrowExceptionForHR(registry.CreateInstance(
                                this.ProviderGuid,
                                null,
                                typeof(IVsIntellisenseProject).GUID,
                                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                                out pIntellisenseEngine));

                            this._intellisenseEngine = Marshal.GetObjectForIUnknown(pIntellisenseEngine) as IVsIntellisenseProject;
                        }
                        finally
                        {
                            if (pIntellisenseEngine != IntPtr.Zero)
                            {
                                Marshal.Release(pIntellisenseEngine);
                            }
                        }

                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.Init(this));
                        await this.LanguageServiceRegister.RegisterProjectAsync(this);
                    }
                });

            // The rest of this can execute on a worker thread.
            await TaskScheduler.Default;
            using (this.ProjectAsynchronousTasksService.LoadedProject())
            {
                var designTimeBuildBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    this.ProjectBuildRuleBlock_Changed);
                this._designTimeBuildSubscriptionLink = this.ActiveConfiguredProjectSubscriptionService.JointRuleBlock.LinkTo(
                    designTimeBuildBlock,
                    ruleNames: WatchedEvaluationRules.Union(WatchedDesignTimeBuildRules));

                var evaluationBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                    this.ProjectRuleBlock_ChangedAsync);
                this._evaluationSubscriptionLink = this.ActiveConfiguredProjectSubscriptionService.ProjectRuleBlock.LinkTo(
                    evaluationBlock,
                    ruleNames: WatchedEvaluationRules);
            }
        }

        #region IVsIntellisenseProjectHost Members

        /// <summary>
        /// See IVsIntellisenseProjectHost
        /// </summary>
        int IVsIntellisenseProjectHost.CreateFileCodeModel(string pszFilename, out object ppCodeModel)
        {
            EnvDTE.ProjectItem projectItem = this.GetDTEProjectItemForFile(pszFilename);
            if (projectItem != null)
            {
                ppCodeModel = projectItem.FileCodeModel;
                return (ppCodeModel != null) ? VSConstants.S_OK : VSConstants.E_NOTIMPL;
            }
            else
            {
                ppCodeModel = null;
                return VSConstants.E_UNEXPECTED;
            }
        }

        /// <summary>
        /// See IVsIntellisenseProjectHost
        /// </summary>
        int IVsIntellisenseProjectHost.GetCompilerOptions(out string pbstrOptions)
        {
            pbstrOptions = string.Empty;
            return VSConstants.E_NOTIMPL;
        }

        /// <summary>
        /// See IVsIntellisenseProjectHost
        /// </summary>
        int IVsIntellisenseProjectHost.GetHostProperty(uint dwPropID, out object pvarParam)
        {
            const uint HOSTPROPID_SUPPRESSSHADOWWARNINGS = unchecked((uint)-1);
            const uint HOSTPROPID_TARGETFRAMEWORKMONIKER = unchecked((uint)-2);
            const uint HOSTPROPID_P2PREFERENCENAME = unchecked((uint)-4);

            object pvar = null;

            HResult hr = this.HrInvoke(async delegate
            {
                var generalProperties = await this.ActiveConfigurationExports.Value.Properties.Value.GetConfigurationGeneralPropertiesAsync();

                switch (dwPropID)
                {
                    case (uint)HOSTPROPID.HOSTPROPID_HIERARCHY:
                        pvar = _projectVsServices.Hierarchy;
                        break;
                    case (uint)HOSTPROPID.HOSTPROPID_PROJECTNAME:
                        pvar = this.UnconfiguredProject.FullPath;
                        break;
                    case (uint)HOSTPROPID.HOSTPROPID_INTELLISENSECACHE_FILENAME:
                        string intDir = await generalProperties.IntDir.GetEvaluatedValueAtEndAsync();
                        string cacheFile = Path.Combine(intDir, Path.GetFileNameWithoutExtension(this.UnconfiguredProject.FullPath) + ".cachedata");
                        pvar = cacheFile;
                        break;
                    case (uint)HOSTPROPID.HOSTPROPID_RELURL:
                        pvar = this.UnconfiguredProject.FullPath;
                        break;
                    case HOSTPROPID_TARGETFRAMEWORKMONIKER:
                        pvar = await generalProperties.TargetFrameworkMoniker.GetEvaluatedValueAtEndAsync();
                        break;
                    case HOSTPROPID_SUPPRESSSHADOWWARNINGS:
                        pvar = false;
                        break;
                    case HOSTPROPID_P2PREFERENCENAME:
                        // REVIEW: The only other implementation of this property seems to return a relative
                        // path rather than just the leaf filename without extension. It also makes a note
                        // that it caches the path returned to ensure it always returns that path even
                        // after a project rename, which seems odd.
                        pvar = Path.GetFileNameWithoutExtension(this.UnconfiguredProject.FullPath);
                        break;
                    default:
                        pvar = null;
                        return HResult.InvalidArg;
                }

                return HResult.OK;
            });

            pvarParam = pvar;
            return hr;
        }

        /// <summary>
        /// See IVsIntellisenseProjectHost
        /// </summary>
        int IVsIntellisenseProjectHost.GetOutputAssembly(out string pbstrOutputAssemblyParam)
        {
            string pbstrOutputAssembly = null;
            HResult hr = this.HrInvoke(async delegate
            {
                var generalProperties = await this.ActiveConfigurationExports.Value.Properties.Value.GetConfigurationGeneralPropertiesAsync();
                pbstrOutputAssembly = await generalProperties.TargetPath.GetEvaluatedValueAtEndAsync();
            });

            pbstrOutputAssemblyParam = pbstrOutputAssembly;
            return hr;
        }

        #endregion

        #region ICodeModelProvider Members

        /// <summary>
        /// See ICodeModelProvider
        /// </summary>
        EnvDTE.FileCodeModel ICodeModelProvider.GetFileCodeModel(ProjectItem fileItem)
        {
            object result;
            Marshal.ThrowExceptionForHR(this._intellisenseEngine.GetFileCodeModel(_projectVsServices.Hierarchy, fileItem, out result));
            return (EnvDTE.FileCodeModel)result;
        }

        #endregion

        #region LanguageServiceRegister.IProjectWithIntellisense Methods

        /// <inheritdoc/>
        async Task IProjectWithIntellisense.OnProjectAddedAsync(UnconfiguredProject unconfiguredProject, IVsIntellisenseProject intellisenseProject)
        {
            ProjectReferenceState state;
            if (this._projectReferenceFullPaths.TryGetValue(unconfiguredProject.FullPath, out state))
            {
                await this.ThreadHandling.AsyncPump.SwitchToMainThreadAsync();
                if (state.ResolvedPath != null)
                {
                    Marshal.ThrowExceptionForHR(this._intellisenseEngine.RemoveAssemblyReference(state.ResolvedPath));
                }

                Marshal.ThrowExceptionForHR(this._intellisenseEngine.AddP2PReference(intellisenseProject));
                state.AsProjectReference = true;

                Marshal.ThrowExceptionForHR(this._intellisenseEngine.StartIntellisenseEngine());
            }
        }

        /// <inheritdoc/>
        async Task IProjectWithIntellisense.OnProjectRemovedAsync(UnconfiguredProject unconfiguredProject, IVsIntellisenseProject intellisenseProject)
        {
            ProjectReferenceState state;
            if (this._projectReferenceFullPaths.TryGetValue(unconfiguredProject.FullPath, out state))
            {
                await this.ThreadHandling.AsyncPump.SwitchToMainThreadAsync();
                Marshal.ThrowExceptionForHR(this._intellisenseEngine.RemoveP2PReference(intellisenseProject));
                state.AsProjectReference = false;

                if (state.ResolvedPath != null)
                {
                    Marshal.ThrowExceptionForHR(this._intellisenseEngine.AddAssemblyReference(state.ResolvedPath));
                }

                Marshal.ThrowExceptionForHR(this._intellisenseEngine.StartIntellisenseEngine());
            }
        }

        #endregion

        /// <summary>
        /// Disposes the resources of this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Following standard framework guideline dispose pattern
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources..</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ThreadHandling.AsyncPump.Run(async delegate
                {
                    await this.LanguageServiceRegister.UnregisterAsync(this);
                    if (this._intellisenseEngine != null)
                    {
                        await this.ThreadHandling.AsyncPump.SwitchToMainThreadAsync();
                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.StopIntellisenseEngine());
                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.Close());
                        this._intellisenseEngine = null;
                    }

                    this._designTimeBuildSubscriptionLink?.Dispose();
                    this._evaluationSubscriptionLink?.Dispose();
                });
            }
        }

        /// <summary>
        /// Obtains the absolute path and item id for a given project-relative source file path and project tree.
        /// </summary>
        /// <param name="sourceFile">The project-relative path to a source file.</param>
        /// <param name="tree">The tree to use to obtain the item id.</param>
        /// <returns>A tuple, where the first item is the absolute path to the source file and the second item is the item id.</returns>
        private Tuple<string, uint> SourceFileToLanguageServiceUnit(string sourceFile, IProjectTree tree)
        {
            string absolutePath = this.UnconfiguredProject.MakeRooted(sourceFile);
            IProjectTree sourceFileNode = this.PhysicalProjectTreeProvider.Value.FindByPath(tree, absolutePath);
            if (sourceFileNode != null)
            {
                uint itemid = unchecked((uint)sourceFileNode.Identity.ToInt32());
                return new Tuple<string, uint>(absolutePath, itemid);
            }

            return null;
        }

        /// <summary>
        /// Returns EnvDTE.ProjectItem object for the given filename.
        /// Returns null if file is not in the project or the method fails.
        /// </summary>
        private EnvDTE.ProjectItem GetDTEProjectItemForFile(string mkDocument)
        {
            this.ThreadHandling.VerifyOnUIThread();

            int isFound = 0;
            uint itemid = VSConstants.VSITEMID_NIL;
            VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];

            int hr = _projectVsServices.Project.IsDocumentInProject(mkDocument, out isFound, priority, out itemid);

            if (ErrorHandler.Succeeded(hr) && (isFound != 0) && (itemid != VSConstants.VSITEMID_NIL))
            {
                object extObject;
                if (ErrorHandler.Succeeded(_projectVsServices.Hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
                {
                    return extObject as EnvDTE.ProjectItem;
                }
            }

            return null;
        }

        /// <summary>
        /// Executes a delegate, catching any thrown exceptions and converting them to HRESULTs.
        /// </summary>
        private HResult HrInvoke(Func<Task<HResult>> asyncAction, bool registerProjectFaultHandlerService = false)
        {
            return HResult.Invoke(
                delegate
                {
                    return this.ThreadHandling.ExecuteSynchronously(asyncAction);
                },
                Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                registerProjectFaultHandlerService ? this.ProjectFaultHandlerService : null,
                this.UnconfiguredProject);
        }

        /// <summary>
        /// Executes a delegate, catching any thrown exceptions and converting them to HRESULTs.
        /// </summary>
        internal HResult HrInvoke(Action action)
        {
            return HResult.Invoke(action, Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider);
        }

        /// <summary>
        /// Handles the <see cref="IActiveConfiguredProjectSubscriptionService"/> callback event on the active configured project's
        /// change notification service.
        /// </summary>
        private async System.Threading.Tasks.Task ProjectRuleBlock_ChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            await this.ThreadHandling.SwitchToUIThread();
            await this.ProjectAsynchronousTasksService.LoadedProjectAsync(async delegate
            {
                var sourceFiles = e.Value.ProjectChanges[CSharp.SchemaName];
                var projectReferences = e.Value.ProjectChanges[ProjectReference.SchemaName];
                var treeUpdate = await this.ProjectTreeService.PublishLatestTreeAsync(blockDuringLoadingTree: true);
                var tree = treeUpdate.Tree;

                foreach (var sourceUnit in sourceFiles.Difference.AddedItems.Select(item => this.SourceFileToLanguageServiceUnit(item, tree)).Where(u => u != null))
                {
                    Marshal.ThrowExceptionForHR(this._intellisenseEngine.AddFile(sourceUnit.Item1, sourceUnit.Item2));
                }

                foreach (var sourceUnit in sourceFiles.Difference.RemovedItems.Select(item => this.SourceFileToLanguageServiceUnit(item, tree)).Where(u => u != null))
                {
                    Marshal.ThrowExceptionForHR(this._intellisenseEngine.RemoveFile(sourceUnit.Item1, sourceUnit.Item2));
                }

                foreach (KeyValuePair<string, string> sourceFileNames in sourceFiles.Difference.RenamedItems)
                {
                    var newSourceUnit = this.SourceFileToLanguageServiceUnit(sourceFileNames.Value, tree);
                    if (newSourceUnit != null)
                    {
                        string beforeAbsolutePath = this.UnconfiguredProject.MakeRooted(sourceFileNames.Key);
                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.RenameFile(beforeAbsolutePath, newSourceUnit.Item1, newSourceUnit.Item2));
                    }
                }

                foreach (string projectReferencePath in projectReferences.Difference.AddedItems)
                {
                    string projectReferenceFullPath = this.UnconfiguredProject.MakeRooted(projectReferencePath);
                    ProjectReferenceState state;
                    if (!this._projectReferenceFullPaths.TryGetValue(projectReferenceFullPath, out state))
                    {
                        this._projectReferenceFullPaths = this._projectReferenceFullPaths.Add(projectReferenceFullPath, state = new ProjectReferenceState());
                    }

                    IVsIntellisenseProject intellisenseProject;
                    if (this.LanguageServiceRegister.TryGetIntellisenseProject(projectReferenceFullPath, out intellisenseProject))
                    {
                        if (state.ResolvedPath != null && !state.AsProjectReference)
                        {
                            Marshal.ThrowExceptionForHR(this._intellisenseEngine.RemoveAssemblyReference(state.ResolvedPath));
                        }

                        state.AsProjectReference = true;
                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.AddP2PReference(intellisenseProject));
                    }
                }

                foreach (string projectReferencePath in projectReferences.Difference.RemovedItems)
                {
                    string projectReferenceFullPath = this.UnconfiguredProject.MakeRooted(projectReferencePath);
                    this._projectReferenceFullPaths = this._projectReferenceFullPaths.Remove(projectReferenceFullPath);

                    IVsIntellisenseProject intellisenseProject;
                    if (this.LanguageServiceRegister.TryGetIntellisenseProject(projectReferencePath, out intellisenseProject))
                    {
                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.RemoveP2PReference(this._intellisenseEngine));
                    }

                    ProjectReferenceState state;
                    if (this._projectReferenceFullPaths.TryGetValue(projectReferenceFullPath, out state))
                    {
                        state.AsProjectReference = false;
                    }
                }

                Marshal.ThrowExceptionForHR(this._intellisenseEngine.StartIntellisenseEngine());
            });
        }

        /// <summary>
        /// Handles the <see cref="IActiveConfiguredProjectSubscriptionService"/> callback on the active configured project's
        /// design-time build change notification service.
        /// </summary>
        private async Task ProjectBuildRuleBlock_Changed(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            await this.ThreadHandling.SwitchToUIThread();
            using (this.ProjectAsynchronousTasksService.LoadedProject())
            {
                foreach (var resolvedReferenceChange in e.Value.ProjectChanges.Values)
                {
                    if (!WatchedDesignTimeBuildRules.Contains(resolvedReferenceChange.After.RuleName))
                    {
                        // This is an evaluation rule.
                        continue;
                    }

                    foreach (string resolvedReferencePath in resolvedReferenceChange.Difference.AddedItems)
                    {
                        // If this is a resolved project reference, we need to treat it specially.
                        string originalItemSpec = resolvedReferenceChange.After.Items[resolvedReferencePath]["OriginalItemSpec"];
                        if (!string.IsNullOrEmpty(originalItemSpec))
                        {
                            if (e.Value.CurrentState[ProjectReference.SchemaName].Items.ContainsKey(originalItemSpec))
                            {
                                string originalFullPath = this.UnconfiguredProject.MakeRooted(originalItemSpec);
                                ProjectReferenceState state;
                                if (!this._projectReferenceFullPaths.TryGetValue(originalFullPath, out state))
                                {
                                    this._projectReferenceFullPaths = this._projectReferenceFullPaths.Add(originalFullPath, state = new ProjectReferenceState());
                                }

                                state.ResolvedPath = resolvedReferencePath;

                                // Be careful to not add assembly references that overlap with project references.
                                if (state.AsProjectReference)
                                {
                                    continue;
                                }
                            }
                        }

                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.AddAssemblyReference(resolvedReferencePath));
                    }

                    foreach (string resolvedReferencePath in resolvedReferenceChange.Difference.RemovedItems)
                    {
                        Marshal.ThrowExceptionForHR(this._intellisenseEngine.RemoveAssemblyReference(resolvedReferencePath));
                    }

                    Marshal.ThrowExceptionForHR(this._intellisenseEngine.StartIntellisenseEngine());
                }
            }
        }

        /// <summary>
        /// A class that has ConfiguredProject-level imports.
        /// </summary>
        [Export]
        private class ActiveConfiguredProjectHelper
        {
            /// <summary>
            /// Gets the configured project instance.
            /// </summary>
            [Import]
            internal ConfiguredProject ConfiguredProject
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the project properties rule accessors.
            /// </summary>
            [Import]
            internal Lazy<ProjectProperties> Properties
            {
                get;
                set;
            }
        }

        /// <summary>
        /// A description of the state of how a project reference has been added to our Intellisense project.
        /// </summary>
        private class ProjectReferenceState
        {
            /// <summary>
            /// Gets or sets the resolved path for the referenced project.
            /// </summary>
            /// <value>An absolute path, or <c>null</c> if the resolved path is not (yet) available.</value>
            public string ResolvedPath
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a value indicating whether the reference has been added to Intellisense as a project reference.
            /// </summary>
            /// <value>
            /// <c>true</c> if the reference is a P2P reference in Intellisense.
            /// <c>false</c> if the reference is a file reference in Intellisense.
            /// </value>
            public bool AsProjectReference
            {
                get;
                set;
            }
        }
    }
}

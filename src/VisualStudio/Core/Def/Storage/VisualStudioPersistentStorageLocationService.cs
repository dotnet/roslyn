// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), ServiceLayer.Host), Shared]
    internal class VisualStudioPersistentStorageLocationService : ForegroundThreadAffinitizedObject, IPersistentStorageLocationService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly object _gate = new object();
        private SolutionId _currentSolutionId = null;
        private string _currentWorkingFolderPath = null;
        private bool _solutionEventsAdvised = false;

        /// <remarks>
        /// In Visual Studio, this event will be raised on the UI thread.
        /// </remarks>
        public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioPersistentStorageLocationService(IThreadingContext threadingContext, [Import] SVsServiceProvider serviceProvider)
            : base(threadingContext, assertIsForeground: false)
        {
            _serviceProvider = serviceProvider;
        }

        public bool IsSupported(Workspace workspace)
            => workspace is VisualStudioWorkspaceImpl;

        public string TryGetStorageLocation(SolutionId solutionId)
        {
            lock (_gate)
            {
                if (solutionId == _currentSolutionId)
                {
                    return _currentWorkingFolderPath;
                }
            }

            return null;
        }

        internal void UpdateForVisualStudioWorkspace(Workspace visualStudioWorkspace)
        {
            AssertIsForeground();

            Contract.ThrowIfFalse(IsSupported(visualStudioWorkspace));

            lock (_gate)
            {
                if (visualStudioWorkspace.CurrentSolution.Id == _currentSolutionId && _currentWorkingFolderPath != null)
                {
                    return;
                }

                var solution = _serviceProvider.GetService<SVsSolution, IVsSolution>();

                if (!_solutionEventsAdvised)
                {
                    solution.AdviseSolutionEvents(new EventSink(this, visualStudioWorkspace), out var cookie);
                    _solutionEventsAdvised = true;
                }

                try
                {
                    var solutionWorkingFolder = (IVsSolutionWorkingFolders)solution;
                    solutionWorkingFolder.GetFolder(
                        (uint)__SolutionWorkingFolder.SlnWF_StatePersistence, Guid.Empty, fVersionSpecific: true, fEnsureCreated: true,
                        pfIsTemporary: out var temporary, pszBstrFullPath: out var workingFolderPath);

                    if (!temporary && !string.IsNullOrWhiteSpace(workingFolderPath))
                    {
                        OnWorkingFolderChanging_NoLock(
                            new PersistentStorageLocationChangingEventArgs(
                                visualStudioWorkspace.CurrentSolution.Id,
                                workingFolderPath,
                                mustUseNewStorageLocationImmediately: false));
                    }
                }
                catch
                {
                    // don't crash just because solution having problem getting working folder information
                }
            }
        }

        private void OnWorkingFolderChanging_NoLock(PersistentStorageLocationChangingEventArgs eventArgs)
        {
            AssertIsForeground();

            StorageLocationChanging?.Invoke(this, eventArgs);

            _currentSolutionId = eventArgs.SolutionId;
            _currentWorkingFolderPath = eventArgs.NewStorageLocation;
        }

        private void ProcessChangeToIVsSolutionChange(Workspace visualStudioWorkspace)
        {
            AssertIsForeground();

            lock (_gate)
            {
                if (_currentSolutionId == visualStudioWorkspace.CurrentSolution.Id)
                {
                    // We want to make sure everybody synchronously detaches
                    OnWorkingFolderChanging_NoLock(
                        new PersistentStorageLocationChangingEventArgs(
                            _currentSolutionId,
                            newStorageLocation: null,
                            mustUseNewStorageLocationImmediately: true));
                }
            }
        }

        private class EventSink : IVsSolutionEvents, IVsSolutionWorkingFoldersEvents
        {
            private readonly VisualStudioPersistentStorageLocationService _service;
            private readonly Workspace _visualStudioWorkspace;

            public EventSink(VisualStudioPersistentStorageLocationService service, Workspace visualStudioWorkspace)
            {
                _service = service;
                _visualStudioWorkspace = visualStudioWorkspace;
            }

            void IVsSolutionWorkingFoldersEvents.OnQueryLocationChange(uint location, out bool pfCanMoveContent)
            {
                pfCanMoveContent = true;

                if ((__SolutionWorkingFolder)location == __SolutionWorkingFolder.SlnWF_StatePersistence)
                {
                    _service.ProcessChangeToIVsSolutionChange(_visualStudioWorkspace);
                }
            }

            void IVsSolutionWorkingFoldersEvents.OnAfterLocationChange(uint location, bool contentMoved)
            {
                _service.UpdateForVisualStudioWorkspace(_visualStudioWorkspace);
            }

            int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
            {
                _service.ProcessChangeToIVsSolutionChange(_visualStudioWorkspace);
                return VSConstants.S_OK;
            }

            int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
            {
                return VSConstants.E_NOTIMPL;
            }
        }
    }
}

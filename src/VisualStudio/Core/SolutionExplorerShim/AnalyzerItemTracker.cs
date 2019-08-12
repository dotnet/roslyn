// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// This class listens to selection change events, and tracks which, if any, of our
    /// <see cref="AnalyzerItem"/> or <see cref="AnalyzersFolderItem"/> is selected.
    /// </summary>
    [Export]
    internal class AnalyzerItemsTracker : IVsSelectionEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private IVsMonitorSelection _vsMonitorSelection = null;
        private uint _selectionEventsCookie = 0;

        public event EventHandler SelectedHierarchyItemChanged;

        [ImportingConstructor]
        public AnalyzerItemsTracker(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Register()
        {
            var vsMonitorSelection = GetMonitorSelection();

            if (vsMonitorSelection != null)
            {
                vsMonitorSelection.AdviseSelectionEvents(this, out _selectionEventsCookie);
            }
        }

        public void Unregister()
        {
            var vsMonitorSelection = GetMonitorSelection();

            if (vsMonitorSelection != null)
            {
                vsMonitorSelection.UnadviseSelectionEvents(_selectionEventsCookie);
            }
        }

        public IVsHierarchy SelectedHierarchy { get; private set; }
        public uint SelectedItemId { get; private set; } = VSConstants.VSITEMID_NIL;
        public AnalyzersFolderItem SelectedFolder { get; private set; }
        public ImmutableArray<AnalyzerItem> SelectedAnalyzerItems { get; private set; } = ImmutableArray<AnalyzerItem>.Empty;
        public ImmutableArray<BaseDiagnosticItem> SelectedDiagnosticItems { get; private set; } = ImmutableArray<BaseDiagnosticItem>.Empty;

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnSelectionChanged(
            IVsHierarchy pHierOld,
            uint itemidOld,
            IVsMultiItemSelect pMISOld,
            ISelectionContainer pSCOld,
            IVsHierarchy pHierNew,
            uint itemidNew,
            IVsMultiItemSelect pMISNew,
            ISelectionContainer pSCNew)
        {
            var oldSelectedHierarchy = this.SelectedHierarchy;
            var oldSelectedItemId = this.SelectedItemId;

            this.SelectedHierarchy = pHierNew;
            this.SelectedItemId = itemidNew;

            var selectedObjects = GetSelectedObjects(pSCNew);

            this.SelectedAnalyzerItems = selectedObjects
                                         .OfType<AnalyzerItem.BrowseObject>()
                                         .Select(b => b.AnalyzerItem)
                                         .ToImmutableArray();

            this.SelectedFolder = selectedObjects
                                  .OfType<AnalyzersFolderItem.BrowseObject>()
                                  .Select(b => b.Folder)
                                  .FirstOrDefault();

            this.SelectedDiagnosticItems = selectedObjects
                                           .OfType<BaseDiagnosticItem.BrowseObject>()
                                           .Select(b => b.DiagnosticItem)
                                           .ToImmutableArray();

            if (!object.ReferenceEquals(oldSelectedHierarchy, this.SelectedHierarchy) ||
                oldSelectedItemId != this.SelectedItemId)
            {
                this.SelectedHierarchyItemChanged?.Invoke(this, EventArgs.Empty);
            }

            return VSConstants.S_OK;
        }

        private object[] GetSelectedObjects(ISelectionContainer selectionContainer)
        {
            if (selectionContainer == null)
            {
                return Array.Empty<object>();
            }

            if (selectionContainer.CountObjects((uint)Constants.GETOBJS_SELECTED, out var selectedObjectCount) < 0 || selectedObjectCount == 0)
            {
                return Array.Empty<object>();
            }

            var selectedObjects = new object[selectedObjectCount];
            if (selectionContainer.GetObjects((uint)Constants.GETOBJS_SELECTED, selectedObjectCount, selectedObjects) < 0)
            {
                return Array.Empty<object>();
            }

            return selectedObjects;
        }

        private IVsMonitorSelection GetMonitorSelection()
        {
            if (_vsMonitorSelection == null)
            {
                _vsMonitorSelection = _serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            }

            return _vsMonitorSelection;
        }
    }
}

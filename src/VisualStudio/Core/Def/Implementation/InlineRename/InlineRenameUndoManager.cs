// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InlineRename
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [ExportWorkspaceServiceFactory(typeof(IInlineRenameUndoManager), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioInlineRenameUndoManagerServiceFactory : IWorkspaceServiceFactory
    {
        private readonly InlineRenameService _inlineRenameService;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        [ImportingConstructor]
        public VisualStudioInlineRenameUndoManagerServiceFactory(
            InlineRenameService inlineRenameService,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
        {
            _inlineRenameService = inlineRenameService;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new InlineRenameUndoManager(_inlineRenameService, _editorAdaptersFactoryService);
        }

        internal class InlineRenameUndoManager : AbstractInlineRenameUndoManager<InlineRenameUndoManager.BufferUndoState>, IInlineRenameUndoManager
        {
            private class RenameUndoPrimitive : IOleUndoUnit
            {
                private readonly string _description;

                public RenameUndoPrimitive(string description)
                {
                    _description = description;
                }

                public virtual void Do(IOleUndoManager pUndoManager)
                {
                }

                public void GetDescription(out string pBstr)
                {
                    pBstr = _description;
                }

                public void GetUnitType(out Guid pClsid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")]out int plID)
                {
                    throw new NotImplementedException();
                }

                public void OnNextAdd()
                {
                }
            }

            private class RedoPrimitive : RenameUndoPrimitive
            {
                private readonly IOleUndoManager _undoManager;

                public RedoPrimitive(IOleUndoManager undoManager, string replacementText) : base(replacementText)
                {
                    _undoManager = undoManager;
                }

                // Undoing this instance simply adds it to the Redo stack.
                public override void Do(IOleUndoManager pUndoManager)
                {
                    _undoManager.Add(this);
                }
            }

            internal class BufferUndoState
            {
                public IOleUndoManager UndoManager { get; set; }
                public ITextUndoHistory TextUndoHistory { get; set; }
                public IOleUndoUnit StartRenameSessionUndoPrimitive { get; set; }
                public IOleUndoUnit ConflictResolutionRenameUndoPrimitive { get; set; }
                public ITextBuffer UndoHistoryBuffer { get; set; }
            }

            private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

            public InlineRenameUndoManager(InlineRenameService inlineRenameService, IVsEditorAdaptersFactoryService editorAdaptersFactoryService) : base(inlineRenameService)
            {
                _editorAdaptersFactoryService = editorAdaptersFactoryService;
            }

            public void CreateStartRenameUndoTransaction(Workspace workspace, ITextBuffer subjectBuffer, InlineRenameSession inlineRenameSession)
            {
                var startRenameUndoPrimitive = new RenameUndoPrimitive(EditorFeaturesResources.Start_Rename);
                var textUndoHistoryService = workspace.Services.GetService<ITextUndoHistoryWorkspaceService>();
                Contract.ThrowIfFalse(textUndoHistoryService.TryGetTextUndoHistory(workspace, subjectBuffer, out var undoHistory));
                Contract.ThrowIfFalse(undoHistory.Properties.TryGetProperty(typeof(ITextBuffer), out ITextBuffer primaryBuffer));
                var undoManager = GetUndoManager(primaryBuffer);

                UndoManagers[subjectBuffer] = new BufferUndoState() { UndoManager = undoManager, TextUndoHistory = undoHistory, StartRenameSessionUndoPrimitive = startRenameUndoPrimitive, UndoHistoryBuffer = primaryBuffer };
                undoManager.Add(startRenameUndoPrimitive);
            }

            public void CreateConflictResolutionUndoTransaction(ITextBuffer subjectBuffer, Action applyEdit)
            {
                // Replace the StartRenameSession undo entry with an identically named entry that also includes
                // the conflict resolution edits.
                var undoManager = this.UndoManagers[subjectBuffer].UndoManager;
                undoManager.UndoTo(this.UndoManagers[subjectBuffer].StartRenameSessionUndoPrimitive);

                var adapter = _editorAdaptersFactoryService.GetBufferAdapter(this.UndoManagers[subjectBuffer].UndoHistoryBuffer);
                var compoundAction = adapter as IVsCompoundAction;
                compoundAction.OpenCompoundAction(EditorFeaturesResources.Start_Rename);
                applyEdit();
                compoundAction.CloseCompoundAction();

                this.UndoManagers[subjectBuffer].ConflictResolutionRenameUndoPrimitive = GetUndoUnits(undoManager).Last();
            }

            public void UndoTemporaryEdits(ITextBuffer subjectBuffer, bool disconnect)
            {
                UndoTemporaryEdits(subjectBuffer, disconnect, true);
            }

            protected override void UndoTemporaryEdits(ITextBuffer subjectBuffer, bool disconnect, bool undoConflictResolution)
            {
                // There are crashes from Windows Error Reporting that indicate the BufferUndoState
                // may be being unavailable here when inline rename has been dismissed due to an
                // external workspace change. See bug #1167415.
                if (!this.UndoManagers.TryGetValue(subjectBuffer, out var bufferUndoState))
                {
                    return;
                }

                var undoManager = bufferUndoState.UndoManager;
                var startRenameUndoPrimitive = bufferUndoState.StartRenameSessionUndoPrimitive;
                var markerPrimitive = bufferUndoState.ConflictResolutionRenameUndoPrimitive ?? startRenameUndoPrimitive;

                // If we're not undoing conflict resolution, we need to undo the next unit after our startRenameUndoPrimitive
                var count = GetUndoUnits(undoManager).SkipWhile(u => u != markerPrimitive).Count() + (undoConflictResolution ? 0 : -1);
                for (var i = 0; i < count; i++)
                {
                    undoManager.UndoTo(null);
                }

                if (disconnect)
                {
                    // destroy the redo stack.
                    undoManager.Add(startRenameUndoPrimitive);
                    undoManager.UndoTo(null);
                }
                else if (undoConflictResolution)
                {
                    // If we undid conflict resolution, then we need to put back our start rename transaction
                    undoManager.Add(startRenameUndoPrimitive);
                    bufferUndoState.ConflictResolutionRenameUndoPrimitive = null;
                }
            }

            public void ApplyCurrentState(ITextBuffer subjectBuffer, object propagateSpansEditTag, IEnumerable<ITrackingSpan> spans)
            {
                // There are crash dumps that indicate the BufferUndoState may be being unavailable
                // here when inline rename has been dismissed due to an external workspace change.
                // See bug https://github.com/dotnet/roslyn/issues/31883.
                if (!this.UndoManagers.TryGetValue(subjectBuffer, out var bufferUndoState))
                {
                    return;
                }

                ApplyReplacementText(subjectBuffer, bufferUndoState.TextUndoHistory, propagateSpansEditTag, spans, this.currentState.ReplacementText);

                // Here we create the descriptions for the redo list dropdown.
                var undoManager = bufferUndoState.UndoManager;
                foreach (var state in this.RedoStack.Reverse())
                {
                    undoManager.Add(new RedoPrimitive(undoManager, GetUndoTransactionDescription(state.ReplacementText)));
                }

                foreach (var state in this.RedoStack)
                {
                    undoManager.UndoTo(null);
                }
            }

            private IOleUndoManager GetUndoManager(ITextBuffer subjectBuffer)
            {
                var adapter = _editorAdaptersFactoryService.GetBufferAdapter(subjectBuffer);
                if (adapter != null)
                {
                    if (ErrorHandler.Succeeded(adapter.GetUndoManager(out var manager)))
                    {
                        return manager;
                    }
                }

                return null;
            }

            private IEnumerable<IOleUndoUnit> GetUndoUnits(IOleUndoManager undoManager)
            {
                IEnumOleUndoUnits undoUnitEnumerator;
                try
                {
                    // Unfortunately, EnumUndoable returns the units in oldest-first order.
                    undoManager.EnumUndoable(out undoUnitEnumerator);
                }
                catch (COMException)
                {
                    yield break;
                }

                const int BatchSize = 100;
                var fetchedUndoUnits = new IOleUndoUnit[BatchSize];

                while (true)
                {
                    undoUnitEnumerator.Next(BatchSize, fetchedUndoUnits, out var fetchedCount);
                    for (var i = 0; i < fetchedCount; i++)
                    {
                        yield return fetchedUndoUnits[i];
                    }

                    if (fetchedCount < BatchSize)
                    {
                        break;
                    }
                }
            }
        }
    }
}

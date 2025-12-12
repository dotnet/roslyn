// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InlineRename;

using Workspace = Microsoft.CodeAnalysis.Workspace;

[ExportWorkspaceServiceFactory(typeof(IInlineRenameUndoManager), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioInlineRenameUndoManagerServiceFactory(
    InlineRenameService inlineRenameService,
    IVsEditorAdaptersFactoryService editorAdaptersFactoryService) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new InlineRenameUndoManager(inlineRenameService, editorAdaptersFactoryService);

    internal sealed class InlineRenameUndoManager(InlineRenameService inlineRenameService, IVsEditorAdaptersFactoryService editorAdaptersFactoryService) : AbstractInlineRenameUndoManager<InlineRenameUndoManager.BufferUndoState>(inlineRenameService), IInlineRenameUndoManager
    {
        private class RenameUndoPrimitive : IOleUndoUnit
        {
            private readonly string _description;

            public RenameUndoPrimitive(string description)
                => _description = description;

            public virtual void Do(IOleUndoManager pUndoManager)
            {
            }

            public void GetDescription(out string pBstr)
                => pBstr = _description;

            public void GetUnitType(out Guid pClsid, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.LONG")] out int plID)
                => throw new NotImplementedException();

            public void OnNextAdd()
            {
            }
        }

        private sealed class RedoPrimitive : RenameUndoPrimitive
        {
            private readonly IOleUndoManager _undoManager;

            public RedoPrimitive(IOleUndoManager undoManager, string replacementText) : base(replacementText)
                => _undoManager = undoManager;

            // Undoing this instance simply adds it to the Redo stack.
            public override void Do(IOleUndoManager pUndoManager)
                => _undoManager.Add(this);
        }

        internal sealed class BufferUndoState
        {
            public IOleUndoManager UndoManager { get; set; }
            public ITextUndoHistory TextUndoHistory { get; set; }
            public IOleUndoUnit StartRenameSessionUndoPrimitive { get; set; }
            public IOleUndoUnit ConflictResolutionRenameUndoPrimitive { get; set; }
            public ITextBuffer UndoHistoryBuffer { get; set; }
        }

        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService = editorAdaptersFactoryService;

        public void CreateStartRenameUndoTransaction(Workspace workspace, ITextBuffer subjectBuffer, IInlineRenameSession inlineRenameSession)
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

            try
            {
                // Replace the StartRenameSession undo entry with an identically named entry that also includes
                // the conflict resolution edits.
                undoManager.UndoTo(this.UndoManagers[subjectBuffer].StartRenameSessionUndoPrimitive);
            }
            catch (COMException ex) when (ex.ErrorCode == VSConstants.E_UNEXPECTED && FatalError.ReportAndCatch(ex))
            {
                // According to the documentation, E_UNEXPECTED (0x8000FFFF) is raised when the UndoManager is disabled.
                // https://docs.microsoft.com/en-us/windows/win32/api/ocidl/nf-ocidl-ioleundomanager-undoto#return-value
                // Report a non-fatal error so we can learn more about this scenario.
            }

            var adapter = _editorAdaptersFactoryService.GetBufferAdapter(this.UndoManagers[subjectBuffer].UndoHistoryBuffer);
            var compoundAction = adapter as IVsCompoundAction;
            compoundAction.OpenCompoundAction(EditorFeaturesResources.Start_Rename);
            applyEdit();
            compoundAction.CloseCompoundAction();

            this.UndoManagers[subjectBuffer].ConflictResolutionRenameUndoPrimitive = GetUndoUnits(undoManager).Last();
        }

        public void UndoTemporaryEdits(ITextBuffer subjectBuffer, bool disconnect)
            => UndoTemporaryEdits(subjectBuffer, disconnect, true);

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

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var isCaseSensitive = document?.GetLanguageService<ISyntaxFactsService>()?.IsCaseSensitive ?? true;

            // This is where we apply the replacement text to each inline preview in the buffer.
            // Needs to remove the "Attribute" suffix, since the inline preview does not include the "Attribute" suffix in the replacement span,
            // so that the user does not see the suffix twice.
            ApplyReplacementText(subjectBuffer, bufferUndoState.TextUndoHistory, propagateSpansEditTag, spans,
                currentState.ReplacementText.GetWithoutAttributeSuffix(isCaseSensitive) ?? currentState.ReplacementText);

            // Here we create the descriptions for the redo list dropdown.
            var undoManager = bufferUndoState.UndoManager;
            foreach (var state in this.RedoStack.Reverse())
            {
                undoManager.Add(new RedoPrimitive(undoManager, GetUndoTransactionDescription(state.ReplacementText)));
            }

            foreach (var _ in this.RedoStack)
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

        private static IEnumerable<IOleUndoUnit> GetUndoUnits(IOleUndoManager undoManager)
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

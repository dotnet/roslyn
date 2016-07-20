// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [ExportWorkspaceServiceFactory(typeof(IInlineRenameUndoManager), ServiceLayer.Default), Shared]
    internal class UndoManagerServiceFactory : IWorkspaceServiceFactory
    {
        private readonly InlineRenameService _inlineRenameService;

        [ImportingConstructor]
        public UndoManagerServiceFactory(InlineRenameService inlineRenameService)
        {
            _inlineRenameService = inlineRenameService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new InlineRenameUndoManager(_inlineRenameService);
        }

        internal class InlineRenameUndoManager : AbstractInlineRenameUndoManager<InlineRenameUndoManager.BufferUndoState>, IInlineRenameUndoManager
        {
            internal class BufferUndoState
            {
                public ITextUndoHistory TextUndoHistory { get; set; }
                public ITextUndoTransaction StartRenameSessionUndoTransaction { get; set; }
                public ITextUndoTransaction ConflictResolutionUndoTransaction { get; set; }
            }

            public InlineRenameUndoManager(InlineRenameService inlineRenameService) : base(inlineRenameService)
            {
            }

            public void CreateStartRenameUndoTransaction(Workspace workspace, ITextBuffer subjectBuffer, InlineRenameSession inlineRenameSession)
            {
                var textUndoHistoryService = workspace.Services.GetService<ITextUndoHistoryWorkspaceService>();
                ITextUndoHistory undoHistory;
                Contract.ThrowIfFalse(textUndoHistoryService.TryGetTextUndoHistory(workspace, subjectBuffer, out undoHistory));
                UndoManagers[subjectBuffer] = new BufferUndoState() { TextUndoHistory = undoHistory };
                CreateStartRenameUndoTransaction(subjectBuffer);
            }

            public void CreateStartRenameUndoTransaction(ITextBuffer subjectBuffer)
            {
                var undoHistory = this.UndoManagers[subjectBuffer].TextUndoHistory;

                // Create an undo transaction to mark the starting point of the rename session in this buffer
                using (var undoTransaction = undoHistory.CreateTransaction(EditorFeaturesResources.Start_Rename))
                {
                    undoTransaction.Complete();
                    this.UndoManagers[subjectBuffer].StartRenameSessionUndoTransaction = undoTransaction;
                    this.UndoManagers[subjectBuffer].ConflictResolutionUndoTransaction = null;
                }
            }

            public void CreateConflictResolutionUndoTransaction(ITextBuffer subjectBuffer, Action applyEdit)
            {
                var undoHistory = this.UndoManagers[subjectBuffer].TextUndoHistory;
                while (true)
                {
                    if (undoHistory.UndoStack.First() == this.UndoManagers[subjectBuffer].StartRenameSessionUndoTransaction)
                    {
                        undoHistory.Undo(1);
                        break;
                    }

                    undoHistory.Undo(1);
                }

                using (var undoTransaction = undoHistory.CreateTransaction(EditorFeaturesResources.Start_Rename))
                {
                    applyEdit();
                    undoTransaction.Complete();
                    UndoManagers[subjectBuffer].ConflictResolutionUndoTransaction = undoTransaction;
                }
            }

            public void UndoTemporaryEdits(ITextBuffer subjectBuffer, bool disconnect)
            {
                UndoTemporaryEdits(subjectBuffer, disconnect, true);
            }

            protected override void UndoTemporaryEdits(ITextBuffer subjectBuffer, bool disconnect, bool undoConflictResolution)
            {
                BufferUndoState bufferUndoState;
                if (!this.UndoManagers.TryGetValue(subjectBuffer, out bufferUndoState))
                {
                    return;
                }

                var undoHistory = bufferUndoState.TextUndoHistory;
                var targetTransaction = this.UndoManagers[subjectBuffer].ConflictResolutionUndoTransaction ?? this.UndoManagers[subjectBuffer].StartRenameSessionUndoTransaction;
                while (undoHistory.UndoStack.First() != targetTransaction)
                {
                    undoHistory.Undo(1);
                }

                if (undoConflictResolution)
                {
                    undoHistory.Undo(1);
                    if (disconnect)
                    {
                        return;
                    }

                    CreateStartRenameUndoTransaction(subjectBuffer);
                }
            }

            public void ApplyCurrentState(ITextBuffer subjectBuffer, object propagateSpansEditTag, IEnumerable<ITrackingSpan> spans)
            {
                ApplyReplacementText(subjectBuffer, this.UndoManagers[subjectBuffer].TextUndoHistory, propagateSpansEditTag, spans, this.currentState.ReplacementText);

                // Here we create the descriptions for the redo list dropdown.
                var undoHistory = this.UndoManagers[subjectBuffer].TextUndoHistory;
                foreach (var state in this.RedoStack.Reverse())
                {
                    using (var transaction = undoHistory.CreateTransaction(GetUndoTransactionDescription(state.ReplacementText)))
                    {
                        transaction.Complete();
                    }
                }

                if (this.RedoStack.Any())
                {
                    undoHistory.Undo(this.RedoStack.Count);
                }
            }
        }
    }
}

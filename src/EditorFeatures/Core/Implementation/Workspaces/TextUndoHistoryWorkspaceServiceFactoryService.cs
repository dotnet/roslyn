// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(ITextUndoHistoryWorkspaceService), ServiceLayer.Default), Shared]
    internal class TextUndoHistoryWorkspaceServiceFactoryService : IWorkspaceServiceFactory
    {
        private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TextUndoHistoryWorkspaceServiceFactoryService(ITextUndoHistoryRegistry textUndoHistoryRegistry)
            => _textUndoHistoryRegistry = textUndoHistoryRegistry;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new TextUndoHistoryWorkspaceService(_textUndoHistoryRegistry);

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

            public TextUndoHistoryWorkspaceService(ITextUndoHistoryRegistry textUndoHistoryRegistry)
                => _textUndoHistoryRegistry = textUndoHistoryRegistry;

            public bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, out ITextUndoHistory undoHistory)
                => _textUndoHistoryRegistry.TryGetHistory(textBuffer, out undoHistory);
        }
    }
}

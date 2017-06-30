﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public TextUndoHistoryWorkspaceServiceFactoryService(ITextUndoHistoryRegistry textUndoHistoryRegistry)
        {
            _textUndoHistoryRegistry = textUndoHistoryRegistry;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new TextUndoHistoryWorkspaceService(_textUndoHistoryRegistry);
        }

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

            public TextUndoHistoryWorkspaceService(ITextUndoHistoryRegistry textUndoHistoryRegistry)
            {
                _textUndoHistoryRegistry = textUndoHistoryRegistry;
            }

            public bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, out ITextUndoHistory undoHistory)
            {
                return _textUndoHistoryRegistry.TryGetHistory(textBuffer, out undoHistory);
            }
        }
    }
}

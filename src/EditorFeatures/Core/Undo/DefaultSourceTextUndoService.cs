// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    [ExportWorkspaceService(typeof(ISourceTextUndoService), ServiceLayer.Default), Shared]
    internal sealed class DefaultSourceTextUndoService : ISourceTextUndoService
    {
        [ImportingConstructor]
        public DefaultSourceTextUndoService()
        {
        }

        public ISourceTextUndoTransaction RegisterUndoTransaction(SourceText sourceText, string description)
        {
            return null;
        }

        public bool BeginUndoTransaction(ITextSnapshot snapshot)
        {
            return false;
        }

        public bool EndUndoTransaction(ISourceTextUndoTransaction transaction)
        {
            return false;
        }
    }
}

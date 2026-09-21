// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor;

internal interface ITextUndoHistoryWorkspaceService : IWorkspaceService
{
    bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, [NotNullWhen(true)] out ITextUndoHistory? undoHistory);
}

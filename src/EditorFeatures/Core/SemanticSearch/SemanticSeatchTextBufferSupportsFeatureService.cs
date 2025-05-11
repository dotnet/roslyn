// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch;

[ExportWorkspaceService(typeof(ITextBufferSupportsFeatureService), WorkspaceKind.SemanticSearch), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchTextBufferSupportsFeatureService() : ITextBufferSupportsFeatureService
{
    public bool SupportsCodeFixes(ITextBuffer textBuffer)
        => true;

    public bool SupportsRefactorings(ITextBuffer textBuffer)
        => true;

    public bool SupportsRename(ITextBuffer textBuffer)
        => true;

    public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer)
        => true;
}

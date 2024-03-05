// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared;

internal interface ITextBufferSupportsFeatureService : IWorkspaceService
{
    bool SupportsCodeFixes(ITextBuffer textBuffer);
    bool SupportsRefactorings(ITextBuffer textBuffer);
    bool SupportsRename(ITextBuffer textBuffer);
    bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer);
}

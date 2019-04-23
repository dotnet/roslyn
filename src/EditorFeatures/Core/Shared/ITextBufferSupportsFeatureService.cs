// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    internal interface ITextBufferSupportsFeatureService : IWorkspaceService
    {
        bool SupportsCodeFixes(ITextBuffer textBuffer);
        bool SupportsRefactorings(ITextBuffer textBuffer);
        bool SupportsRename(ITextBuffer textBuffer);
        bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer);
    }
}

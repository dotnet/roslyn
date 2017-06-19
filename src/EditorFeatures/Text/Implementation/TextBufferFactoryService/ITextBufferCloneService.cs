// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Text
{
    internal interface ITextBufferCloneService : IWorkspaceService
    {
        ITextBuffer Clone(SnapshotSpan span);
        ITextBuffer Clone(ITextImage textImage);
    }
}
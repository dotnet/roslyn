// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    internal interface IImageMonikerService : IWorkspaceService
    {
        ImageMoniker GetImageMoniker(Glyph glyph);
    }
}

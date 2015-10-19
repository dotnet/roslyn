﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IPreviewPaneService : IWorkspaceService
    {
        object GetPreviewPane(DiagnosticData diagnostic, string language, string projectType, object previewContent); 
    }
}

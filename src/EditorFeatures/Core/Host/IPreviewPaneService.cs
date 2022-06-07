﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IPreviewPaneService : IWorkspaceService
    {
        object GetPreviewPane(DiagnosticData diagnostic, IReadOnlyList<object> previewContent);
    }
}

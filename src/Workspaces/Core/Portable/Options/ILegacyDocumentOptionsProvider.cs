// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Enables legacy APIs in VS Mac to provide options for a specified project and document path.
/// </summary>
internal interface ILegacyDocumentOptionsProvider : IWorkspaceService
{
    AnalyzerConfigOptions GetOptions(ProjectId projectId, string documentPath);
}

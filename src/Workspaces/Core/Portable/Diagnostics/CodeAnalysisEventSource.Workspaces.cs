// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace Microsoft.CodeAnalysis;

[EventSource(Name = "Microsoft-CodeAnalysis-Workspaces")]
internal sealed partial class CodeAnalysisEventSource
{
    public static readonly CodeAnalysisEventSource Log = new();

    [Event(20, Message = "Project '{0}' created with file path '{1}'", Level = EventLevel.Informational)]
    internal void ProjectCreated(string projectSystemName, string? filePath) => WriteEvent(20, projectSystemName, filePath ?? string.Empty);
}

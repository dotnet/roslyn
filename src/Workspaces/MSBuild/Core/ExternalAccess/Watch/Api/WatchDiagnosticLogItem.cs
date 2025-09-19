// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

internal readonly struct WatchDiagnosticLogItem
{
    internal DiagnosticLogItem UnderlyingObject { get; }

    internal WatchDiagnosticLogItem(DiagnosticLogItem underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public WatchDiagnosticLogItem(WatchDiagnosticLogItemKind kind, string message, string projectFilePath)
        : this(new DiagnosticLogItem((DiagnosticLogItemKind)kind, message, projectFilePath))
    {
    }

    public WatchDiagnosticLogItemKind Kind => (WatchDiagnosticLogItemKind)UnderlyingObject.Kind;

    public string Message => UnderlyingObject.Message;

    public string ProjectFilePath => UnderlyingObject.ProjectFilePath;

    public override string ToString() => Message;
}

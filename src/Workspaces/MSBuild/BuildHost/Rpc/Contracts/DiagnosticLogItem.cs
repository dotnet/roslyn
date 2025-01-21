// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

internal enum DiagnosticLogItemKind
{
    Error,
    Warning,
}

[DataContract]
internal class DiagnosticLogItem(DiagnosticLogItemKind kind, string message, string projectFilePath)
{
    [DataMember(Order = 0)]
    public DiagnosticLogItemKind Kind { get; } = kind;

    [DataMember(Order = 1)]
    public string Message { get; } = message;

    [DataMember(Order = 2)]
    public string ProjectFilePath { get; } = projectFilePath;

    public override string ToString() => Message;
}

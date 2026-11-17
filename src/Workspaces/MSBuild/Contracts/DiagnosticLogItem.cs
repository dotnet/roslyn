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
#if NETFRAMEWORK
[System.Serializable] // We need to this to be able to serialize across the AppDomain boundary
#endif
internal record DiagnosticLogItem(
    [property: DataMember(Order = 0)] DiagnosticLogItemKind Kind,
    [property: DataMember(Order = 1)] string Message,
    [property: DataMember(Order = 2)] string ProjectFilePath,
    [property: DataMember(Order = 3)] string? FileName = null,
    [property: DataMember(Order = 4)] int? LineNumber = null,
    [property: DataMember(Order = 5)] int? ColumnNumber = null)
{
    // LineNumber and ColumnNumber are either both present or both absent.
    public override string ToString()
        => FileName is null
            ? Message
            : LineNumber is { } line
                ? $"{FileName}: ({line}, {ColumnNumber}): {Message}"
                : $"{FileName}: {Message}";
}

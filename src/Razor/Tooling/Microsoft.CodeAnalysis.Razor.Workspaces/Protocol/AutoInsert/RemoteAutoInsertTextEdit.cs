// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Protocol.AutoInsert;

[DataContract]
internal readonly record struct RemoteAutoInsertTextEdit(
    [property: DataMember(Order = 0)] LinePositionSpan LinePositionSpan,
    [property: DataMember(Order = 1)] string NewText,
    [property: DataMember(Order = 2)] InsertTextFormat InsertTextFormat)
{
    public static RemoteAutoInsertTextEdit FromLspInsertTextEdit(VSInternalDocumentOnAutoInsertResponseItem edit)
        => new(
            edit.TextEdit.Range.ToLinePositionSpan(),
            edit.TextEdit.NewText,
            edit.TextEditFormat);

    public static VSInternalDocumentOnAutoInsertResponseItem ToLspInsertTextEdit(RemoteAutoInsertTextEdit edit)
        => new()
        {
            TextEdit = LspFactory.CreateTextEdit(edit.LinePositionSpan, edit.NewText),
            TextEditFormat = edit.InsertTextFormat,
        };

    public override string ToString()
    {
        return $"({LinePositionSpan}), '{NewText}', {InsertTextFormat}";
    }
}

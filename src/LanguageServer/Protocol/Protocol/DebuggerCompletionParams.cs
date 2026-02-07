// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Parameters for the roslyn/debuggerCompletion request for completion in Watch/QuickWatch/Immediate window.
/// </summary>
internal sealed class DebuggerCompletionParams : ITextDocumentParams
{
    /// <summary>
    /// The source document corresponding to the stopped stack frame.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// The "current statement" range in the source document.
    /// Server uses statementRange.end as the context/anchor point.
    /// Can be a zero-width range (point) if only IP position is known.
    /// </summary>
    [JsonPropertyName("statementRange")]
    [JsonRequired]
    public Range StatementRange { get; set; }

    /// <summary>
    /// The debugger expression input .
    /// </summary>
    [JsonPropertyName("expression")]
    [JsonRequired]
    public string Expression { get; set; }

    /// <summary>
    /// Caret offset within <see cref="Expression"/>, in UTF-16 code units.
    /// </summary>
    [JsonPropertyName("cursorOffset")]
    [JsonRequired]
    public int CursorOffset { get; set; }

    /// <summary>
    /// Optional: standard LSP completion context (trigger kind/character).
    /// </summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionContext? Context { get; set; }
}

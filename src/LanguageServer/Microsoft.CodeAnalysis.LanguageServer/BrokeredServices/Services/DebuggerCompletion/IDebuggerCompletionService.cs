// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.DebuggerCompletion;

/// <summary>
/// Brokered service interface for debugger expression completion.
/// Provides completion items for expressions evaluated in debugger windows
/// (Watch, QuickWatch, Immediate) by splicing the expression into the source
/// document and running the Roslyn completion engine.
/// </summary>
internal interface IDebuggerCompletionService
{
    /// <summary>
    /// Gets completion items for a debugger expression at the given document location.
    /// </summary>
    /// <param name="documentFilePath">The absolute file path of the source document at the current stack frame.</param>
    /// <param name="statementEndLine">The 0-based line number of the current statement's end position.</param>
    /// <param name="statementEndCharacter">The 0-based character offset of the current statement's end position.</param>
    /// <param name="expression">The debugger expression to complete (e.g., "myVar.").</param>
    /// <param name="cursorOffset">The 0-based UTF-16 offset within the expression where completions are requested.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of completion items, or null if completion is not available.</returns>
    Task<DebuggerCompletionResult?> GetDebuggerCompletionsAsync(
        string documentFilePath,
        int statementEndLine,
        int statementEndCharacter,
        string expression,
        int cursorOffset,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result of a debugger completion request.
/// </summary>
internal sealed class DebuggerCompletionResult
{
    public required DebuggerCompletionResultItem[] Items { get; set; }
}

/// <summary>
/// A single completion item from a debugger completion request.
/// </summary>
internal sealed class DebuggerCompletionResultItem
{
    public required string Label { get; set; }
    public int Kind { get; set; }
    public string? SortText { get; set; }
    public string? FilterText { get; set; }
    public string? InsertText { get; set; }
}

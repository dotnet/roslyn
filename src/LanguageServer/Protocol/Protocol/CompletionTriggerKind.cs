// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Enum which represents the various ways in which completion can be triggered.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionTriggerKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal enum CompletionTriggerKind
{
    /// <summary>
    /// Completion was triggered by typing an identifier, manual invocation (e.g Ctrl+Space) or via API.
    /// </summary>
    Invoked = 1,

    /// <summary>
    /// Completion was triggered by a trigger character specified in <see cref="CompletionOptions.TriggerCharacters"/>
    /// </summary>
    TriggerCharacter = 2,

    /// <summary>
    /// Completion was re-triggered as the current completion list is incomplete.
    /// </summary>
    TriggerForIncompleteCompletions = 3,
}

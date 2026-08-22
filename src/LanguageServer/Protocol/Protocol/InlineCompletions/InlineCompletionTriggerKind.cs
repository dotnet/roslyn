// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Describes how an inline completion request was triggered.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineCompletionTriggerKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal enum InlineCompletionTriggerKind
{
    /// <summary>
    /// Completion was triggered explicitly by a user gesture.
    /// </summary>
    Invoked = 1,

    /// <summary>
    /// Completion was triggered automatically while editing.
    /// </summary>
    Automatic = 2,
}

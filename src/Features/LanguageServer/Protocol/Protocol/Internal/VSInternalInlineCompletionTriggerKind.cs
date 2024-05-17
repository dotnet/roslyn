// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// How the inline completion request was triggered.
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L58.
    /// </summary>
    internal enum VSInternalInlineCompletionTriggerKind
    {
        /// <summary>
        /// Inline completions were triggered automatically while editing.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Completion was triggered explicitly by a user gesture.
        /// </summary>
        Explicit = 1,
    }
}

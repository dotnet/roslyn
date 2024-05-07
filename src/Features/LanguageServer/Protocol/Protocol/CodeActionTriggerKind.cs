// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum which represents the various reason why code actions were requested.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionTriggerKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal enum CodeActionTriggerKind
    {
        /// <summary>
        /// Code actions were explicitly requested by the user or by an extension.
        /// </summary>
        Invoked = 1,

        /// <summary>
        /// Code actions were requested automatically.
        /// This typically happens when current selection in a file changes, but can also be triggered when file content changes.
        /// </summary>
        Automatic = 2,
    }
}
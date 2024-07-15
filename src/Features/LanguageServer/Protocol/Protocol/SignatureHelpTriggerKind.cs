// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum which represents the various ways in which completion can be triggered.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpTriggerKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal enum SignatureHelpTriggerKind
    {
        /// <summary>
        /// Signature help was invoked manually by the user or a command.
        /// </summary>
        Invoked = 1,

        /// <summary>
        /// Signature help was triggered by a trigger character.
        /// </summary>
        TriggerCharacter = 2,

        /// <summary>
        /// Signature help was triggered by the cursor moving or by the document content changing.
        /// </summary>
        ContentChange = 3,
    }
}
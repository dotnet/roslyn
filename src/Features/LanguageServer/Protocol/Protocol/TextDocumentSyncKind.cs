// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum which represents the various ways to sync text documents.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSyncKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal enum TextDocumentSyncKind
    {
        /// <summary>
        /// Documents should not be synced at all.
        /// </summary>
        None = 0,

        /// <summary>
        /// Documents are synced by always sending the full text.
        /// </summary>
        Full = 1,

        /// <summary>
        /// Documents are synced by sending only incremental updates.
        /// </summary>
        Incremental = 2,
    }
}

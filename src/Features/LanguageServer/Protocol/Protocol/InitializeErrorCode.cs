// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum representing the possible reasons for an initialization error.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeErrorCodes">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal enum InitializeErrorCode
    {
        /// <summary>
        /// Protocol version can't be handled by the server.
        /// </summary>
        UnknownProtocolVersion = 1,
    }
}

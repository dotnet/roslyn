// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Message type enum.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#messageType">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum MessageType
    {
        /// <summary>
        /// Error message.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Warning message.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Info message.
        /// </summary>
        Info = 3,

        /// <summary>
        /// Log message.
        /// </summary>
        Log = 4,
    }
}

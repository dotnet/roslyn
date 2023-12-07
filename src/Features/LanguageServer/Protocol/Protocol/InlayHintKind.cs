// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum values for inlay hint kinds.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal enum InlayHintKind
    {
        /// <summary>
        /// Type.
        /// </summary>
        Type = 1,

        /// <summary>
        /// Parameter.
        /// </summary>
        Parameter = 2,
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents the parameter that is sent with workspace/didChangeWatchedFiles message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeWatchedFilesParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DidChangeWatchedFilesParams
    {
        /// <summary>
        /// Gets or sets of the collection of file change events.
        /// </summary>
        [DataMember(Name = "changes")]
        public FileEvent[] Changes
        {
            get;
            set;
        }
    }
}

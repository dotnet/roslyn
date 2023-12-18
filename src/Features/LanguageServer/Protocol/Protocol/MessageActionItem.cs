// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represent an action the user performs after a window/showMessageRequest request is sent.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#messageActionItem">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class MessageActionItem
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [DataMember(Name = "title")]
        public string Title
        {
            get;
            set;
        }
    }
}
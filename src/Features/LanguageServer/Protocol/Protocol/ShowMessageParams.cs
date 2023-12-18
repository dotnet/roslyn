﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents parameter sent with window/showMessage requests.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showMessage">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ShowMessageParams
    {
        /// <summary>
        /// Gets or sets the type of message.
        /// </summary>
        [DataMember(Name = "type")]
        public MessageType MessageType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [DataMember(Name = "message")]
        public string Message
        {
            get;
            set;
        }
    }
}

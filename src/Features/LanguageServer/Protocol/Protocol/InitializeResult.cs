﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents the result returned by the initialize request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeResult">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InitializeResult
    {
        /// <summary>
        /// Gets or sets the server capabilities.
        /// </summary>
        [DataMember(Name = "capabilities")]
        public ServerCapabilities Capabilities
        {
            get;
            set;
        }
    }
}

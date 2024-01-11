﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing the error type sent when the initialize request fails.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeError">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InitializeError
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to retry.
        /// </summary>
        [DataMember(Name = "retry")]
        public bool Retry
        {
            get;
            set;
        }
    }
}

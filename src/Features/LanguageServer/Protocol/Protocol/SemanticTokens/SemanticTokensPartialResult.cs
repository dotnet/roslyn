﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing response to semantic tokens messages.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensPartialResult">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SemanticTokensPartialResult
    {
        /// <summary>
        /// Gets or sets and array containing encoded semantic tokens data.
        /// </summary>
        [DataMember(Name = "data", IsRequired = true)]
        public int[] Data { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Legend used to encode semantic token types in <see cref="SemanticTokens.Data"/>.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensLegend">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SemanticTokensLegend
    {
        /// <summary>
        /// Gets or sets an array of token types that can be encoded in semantic tokens responses.
        /// </summary>
        [DataMember(Name = "tokenTypes")]
        public string[] TokenTypes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an array of token modfiers that can be encoded in semantic tokens responses.
        /// </summary>
        [DataMember(Name = "tokenModifiers")]
        public string[] TokenModifiers
        {
            get;
            set;
        }
    }
}

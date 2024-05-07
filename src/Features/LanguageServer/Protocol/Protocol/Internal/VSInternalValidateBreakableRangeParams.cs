// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing the parameters sent for the textDocument/validateBreakableRange request.
    /// </summary>
    [DataContract]
    internal class VSInternalValidateBreakableRangeParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the <see cref="TextDocumentIdentifier"/> for the request.
        /// </summary>
        [DataMember(Name = "_vs_textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Range"/> at which the request was sent.
        /// </summary>
        [DataMember(Name = "_vs_range")]
        public Range Range { get; set; }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents the parameter that is sent with textDocument/rangeFormatting message.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentRangeFormattingParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentRangeFormattingParams : ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the identifier for the text document to be formatted.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the selection range to be formatted.
        /// </summary>
        [DataMember(Name = "range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the formatting options.
        /// </summary>
        [DataMember(Name = "options")]
        public FormattingOptions Options
        {
            get;
            set;
        }
    }
}

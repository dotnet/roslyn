// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/_ms_onAutoInsert request.
    /// </summary>
    [DataContract]
    internal class VSInternalDocumentOnAutoInsertParams : ITextDocumentPositionParams
    {
        /// <summary>
        /// Gets or sets the <see cref="TextDocumentIdentifier"/> representing the document to format.
        /// </summary>
        [DataMember(Name = "_vs_textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="Position"/> at which the request was sent.
        /// </summary>
        [DataMember(Name = "_vs_position")]
        public Position Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the character that was typed.
        /// </summary>
        [DataMember(Name = "_vs_ch")]
        public string Character
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="FormattingOptions"/> for the request.
        /// </summary>
        [DataMember(Name = "_vs_options")]
        public FormattingOptions Options
        {
            get;
            set;
        }
    }
}

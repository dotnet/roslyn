﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing a set of changes to a single text document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentEdit">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class TextDocumentEdit
    {
        /// <summary>
        /// Gets or sets a document identifier indication which document to apply the edits to.
        /// </summary>
        [DataMember(Name = "textDocument", IsRequired = true)]
        public OptionalVersionedTextDocumentIdentifier TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the array of edits to be applied to the document.
        /// </summary>
        [DataMember(Name = "edits", IsRequired = true)]
        public TextEdit[] Edits
        {
            get;
            set;
        }
    }
}

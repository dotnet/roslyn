// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

// TODO - Move this to LSP extensions or elsewhere.
// Temporary internal copy so we can implement these custom extensions
// until these types are available elsewhere.
namespace Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol
{
    /// <summary>
    /// Class which represents a document reference
    /// </summary>
    [DataContract]
    internal class TextDocumentParams
    {
        /// <summary>
        /// Gets or sets the value which identifies the external document.
        /// </summary>
        [DataMember(Name = "textDocument")]
        public TextDocumentIdentifier TextDocument
        {
            get;
            set;
        }
    }
}

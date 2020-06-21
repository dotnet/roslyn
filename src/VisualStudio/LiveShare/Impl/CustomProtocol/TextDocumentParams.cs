// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol
{
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    /// <summary>
    /// Class which represents a document reference
    /// </summary>
    [DataContract]
    public class TextDocumentParams
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

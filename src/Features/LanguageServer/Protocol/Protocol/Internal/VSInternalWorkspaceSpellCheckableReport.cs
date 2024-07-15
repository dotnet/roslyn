// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// Report for workspace spell checkable range request.
    /// </summary>
    [DataContract]
    internal class VSInternalWorkspaceSpellCheckableReport : VSInternalSpellCheckableRangeReport, ITextDocumentParams
    {
        /// <summary>
        /// Gets or sets the document for which the spell checkable ranges are returned.
        /// </summary>
        [DataMember(Name = "_vs_textDocument", IsRequired = true)]
        public TextDocumentIdentifier TextDocument { get; set; }
    }
}

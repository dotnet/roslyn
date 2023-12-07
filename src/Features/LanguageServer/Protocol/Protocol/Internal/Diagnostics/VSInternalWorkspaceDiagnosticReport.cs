// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing a diagnostic pull request result for all documents.
    /// </summary>
    [DataContract]
    internal class VSInternalWorkspaceDiagnosticReport : VSInternalDiagnosticReport
    {
        /// <summary>
        /// Gets or sets the document for which diagnostics is returned.
        /// </summary>
        [DataMember(Name = "_vs_textDocument", IsRequired = true)]
        public TextDocumentIdentifier? TextDocument { get; set; }
    }
}

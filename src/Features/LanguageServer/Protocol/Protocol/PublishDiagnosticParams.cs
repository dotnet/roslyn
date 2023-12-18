// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents the parameter that's sent with 'textDocument/publishDiagnostics' messages.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#publishDiagnosticsParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class PublishDiagnosticParams
    {
        /// <summary>
        /// Gets or sets the URI of the text document.
        /// </summary>
        [DataMember(Name = "uri")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of diagnostics.
        /// </summary>
        [DataMember(Name = "diagnostics")]
        public Diagnostic[] Diagnostics
        {
            get;
            set;
        }
    }
}

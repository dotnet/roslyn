//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol
{
    /// <summary>
    /// Extends <see cref="ServerCapabilities"/> to support LSP 4.0. 
    /// </summary>
    [DataContract]
    public class ServerCapabilities_v40 : ServerCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether the server provides go to implementation support.
        /// </summary>
        [DataMember(Name = "implementationProvider")]
        public bool ImplementationProvider { get; set; }
    }
}

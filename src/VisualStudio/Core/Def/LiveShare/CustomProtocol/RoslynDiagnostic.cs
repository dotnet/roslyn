// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol
{
    // TODO - Migrate to regular diagnostic once more tags are supported.
    [DataContract]
    internal class RoslynDiagnostic : Diagnostic
    {
        /// <summary>
        /// Custom tags on diagnostics - used by analyzers for things like marking a location as unnecessary.
        /// </summary>
        [DataMember(Name = "tags")]
        public new string[] Tags { get; set; }
    }
}

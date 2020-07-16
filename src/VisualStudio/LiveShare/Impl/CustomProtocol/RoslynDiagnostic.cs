// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

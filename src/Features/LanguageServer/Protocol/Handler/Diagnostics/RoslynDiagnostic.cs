// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [DataContract]
    internal class RoslynDiagnostic : VSDiagnostic
    {
        [DataMember(Name = "_ms_client", IsRequired = false)]
        public string? Client { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal interface IRazorLanguageServerFactoryWrapper
    {
        [Obsolete("Use the overload that takes a IRazorTestCapabilitiesProvider")]
        IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, IRazorCapabilitiesProvider capabilitiesProvider, HostServices hostServices);

        IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, IRazorTestCapabilitiesProvider capabilitiesProvider, HostServices hostServices);

        DocumentInfo CreateDocumentInfo(
            DocumentId id,
            string name,
            IReadOnlyList<string>? folders = null,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            TextLoader? loader = null,
            string? filePath = null,
            bool isGenerated = false,
            bool designTimeOnly = false,
            IRazorDocumentServiceProvider? razorDocumentServiceProvider = null);
    }
}

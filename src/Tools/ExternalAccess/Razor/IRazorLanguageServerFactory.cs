// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal interface IRazorLanguageServerFactoryWrapper
    {
        IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, IRazorCapabilitiesProvider capabilitiesProvider);

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

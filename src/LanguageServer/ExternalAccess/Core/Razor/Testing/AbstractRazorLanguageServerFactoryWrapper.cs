// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.CodeAnalysis.Host;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// NOTE: For Razor test usage only
    /// </summary>
    internal abstract class AbstractRazorLanguageServerFactoryWrapper
    {
        internal abstract IRazorLanguageServerTarget CreateLanguageServer(JsonRpc jsonRpc, JsonSerializerOptions options, IRazorTestCapabilitiesProvider capabilitiesProvider, HostServices hostServices);

        internal abstract DocumentInfo CreateDocumentInfo(
            DocumentId id,
            string name,
            IReadOnlyList<string>? folders = null,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            TextLoader? loader = null,
            string? filePath = null,
            bool isGenerated = false,
            bool designTimeOnly = false,
            IRazorDocumentServiceProvider? razorDocumentServiceProvider = null);

        /// <summary>
        /// Supports the creation of a Roslyn LSP server for functional tests
        /// </summary>
        internal abstract void AddJsonConverters(JsonSerializerOptions options);
    }
}

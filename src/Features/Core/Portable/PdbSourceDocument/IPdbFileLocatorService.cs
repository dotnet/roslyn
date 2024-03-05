// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

internal interface IPdbFileLocatorService
{
    Task<DocumentDebugInfoReader?> GetDocumentDebugInfoReaderAsync(string dllPath, bool useDefaultSymbolServers, TelemetryMessage telemetry, CancellationToken cancellationToken);
}

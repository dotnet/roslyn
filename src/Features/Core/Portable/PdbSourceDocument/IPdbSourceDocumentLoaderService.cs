// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

internal interface IPdbSourceDocumentLoaderService
{
    Task<SourceFileInfo?> LoadSourceDocumentAsync(string tempFilePath, SourceDocument sourceDocument, Encoding encoding, TelemetryMessage telemetry, bool useExtendedTimeout, CancellationToken cancellationToken);
}

/// <param name="FilePath">The path to the source file on disk</param>
/// <param name="SourceDescription">Localized description of where the file came from, for the document tab, eg. Source Link, Embedded, On Disk</param>
/// <param name="Loader">The text loader to use</param>
/// <param name="ChecksumAlgorithm">Algorithm to use for content checksum.</param>
/// <param name="FromRemoteLocation">Whether the source files came from a remote location, and therefore their existence should be used to indicate that future requests can wait longer</param>
internal sealed record SourceFileInfo(string FilePath, string SourceDescription, TextLoader Loader, SourceHashAlgorithm ChecksumAlgorithm, bool FromRemoteLocation);

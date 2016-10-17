// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(ITextFactoryService), ServiceLayer.Desktop), Shared]
    internal class DesktopTextFactoryService : ITextFactoryService
    {
        public SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return EncodedStringText.Create(stream, defaultEncoding);
        }

        public SourceText CreateText(TextReader reader, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var temporaryStorageReader = reader as TemporaryStorageServiceFactory.DirectMemoryAccessStreamReader;
            if (temporaryStorageReader != null)
            {
                return SourceText.From(temporaryStorageReader, temporaryStorageReader.Length, encoding);
            }

            return SourceText.From(reader.ReadToEnd(), encoding);
        }
    }
}


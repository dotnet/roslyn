// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(ITextFactoryService), ServiceLayer.Editor)]
    [Shared]
    internal class TextFactoryFactory : IWorkspaceServiceFactory
    {
        private readonly ITextFactoryService _singleton;

        [ImportingConstructor]
        public TextFactoryFactory(
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService)
        {
            _singleton = new TextBufferTextFactory(textBufferFactoryService, contentTypeRegistryService);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }

        internal partial class TextBufferTextFactory : ITextFactoryService
        {
            private readonly ITextBufferFactoryService _textBufferFactory;
            private readonly IContentType _unknownContentType;

            public TextBufferTextFactory(
                ITextBufferFactoryService textBufferFactoryService,
                IContentTypeRegistryService contentTypeRegistryService)
            {
                _textBufferFactory = textBufferFactoryService;
                _unknownContentType = contentTypeRegistryService.UnknownContentType;
            }

            public SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken = default(CancellationToken))
            {
                var encoding = EncodedStringText.TryReadByteOrderMark(stream)
                    ?? defaultEncoding
                    ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

                // Close the stream here since we might throw an exception trying to determine the encoding
                using (stream)
                {
                    return CreateTextInternal(stream, encoding, cancellationToken)
                        ?? CreateTextInternal(stream, Encoding.Default, cancellationToken);
                }
            }

            private SourceText CreateTextInternal(Stream stream, Encoding encoding, CancellationToken cancellationToken)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                    {
                        var buffer = _textBufferFactory.CreateTextBuffer(reader, _unknownContentType);
                        return buffer.CurrentSnapshot.AsRoslynText(reader.CurrentEncoding);
                    }
                }
                catch (DecoderFallbackException) when(encoding != Encoding.Default)
                {
                    return null;
                }
                }
            }
        }
    }

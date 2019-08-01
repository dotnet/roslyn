// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceService(typeof(ITextFactoryService), ServiceLayer.Editor), Shared]
    internal class EditorTextFactoryService : ITextFactoryService
    {
        private readonly ITextBufferCloneService _textBufferCloneService;
        private readonly ITextBufferFactoryService _textBufferFactory;
        private readonly IContentType _unknownContentType;

        [ImportingConstructor]
        public EditorTextFactoryService(
            ITextBufferCloneService textBufferCloneService,
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService)
        {
            _textBufferCloneService = textBufferCloneService;
            _textBufferFactory = textBufferFactoryService;
            _unknownContentType = contentTypeRegistryService.UnknownContentType;
        }

        private static readonly Encoding s_throwingUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken = default)
        {
            // this API is for a case where user wants us to figure out encoding from the given stream.
            // if defaultEncoding is given, we will use it if we couldn't figure out encoding used in the stream ourselves.
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanSeek);
            Debug.Assert(stream.CanRead);

            if (defaultEncoding == null)
            {
                // Try UTF-8
                try
                {
                    return CreateTextInternal(stream, s_throwingUtf8Encoding, cancellationToken);
                }
                catch (DecoderFallbackException)
                {
                    // Try Encoding.Default
                    defaultEncoding = Encoding.Default;
                }
            }

            try
            {
                return CreateTextInternal(stream, defaultEncoding, cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }

        public SourceText CreateText(TextReader reader, Encoding encoding, CancellationToken cancellationToken = default)
        {
            // this API is for a case where user just wants to create a source text with explicit encoding.
            var buffer = CreateTextBuffer(reader, cancellationToken);

            // use the given encoding as it is.
            return buffer.CurrentSnapshot.AsRoslynText(_textBufferCloneService, encoding);
        }

        private ITextBuffer CreateTextBuffer(TextReader reader, CancellationToken cancellationToken = default)
        {
            return _textBufferFactory.CreateTextBuffer(reader, _unknownContentType);
        }

        private SourceText CreateTextInternal(Stream stream, Encoding encoding, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

            var buffer = CreateTextBuffer(reader, cancellationToken);
            return buffer.CurrentSnapshot.AsRoslynText(_textBufferCloneService, reader.CurrentEncoding ?? Encoding.UTF8);
        }
    }
}


// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
        private readonly ITextBufferFactoryService _textBufferFactory;
        private readonly IContentType _unknownContentType;

        [ImportingConstructor]
        public EditorTextFactoryService(
                ITextBufferFactoryService textBufferFactoryService,
                IContentTypeRegistryService contentTypeRegistryService)
        {
            _textBufferFactory = textBufferFactoryService;
            _unknownContentType = contentTypeRegistryService.UnknownContentType;
        }

        private static readonly Encoding ThrowingUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanSeek);
            Debug.Assert(stream.CanRead);

            if (defaultEncoding == null)
            {
                // Try UTF-8
                try
                {
                    return CreateTextInternal(stream, ThrowingUtf8Encoding, cancellationToken);
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

        private ITextBuffer CreateTextBuffer(TextReader reader, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _textBufferFactory.CreateTextBuffer(reader, _unknownContentType);
        }

        private SourceText CreateTextInternal(Stream stream, Encoding encoding, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Seek(0, SeekOrigin.Begin);

            // Detect text coming from temporary storage
            var accessor = stream as ISupportDirectMemoryAccess;
            if (accessor != null)
            {
                return CreateTextFromTemporaryStorage(accessor, (int)stream.Length, encoding, cancellationToken);
            }

            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                var buffer = CreateTextBuffer(reader, cancellationToken);
                return buffer.CurrentSnapshot.AsRoslynText(reader.CurrentEncoding ?? Encoding.UTF8);
            }
        }

        private unsafe SourceText CreateTextFromTemporaryStorage(ISupportDirectMemoryAccess accessor, int streamLength, Encoding encoding, CancellationToken cancellationToken)
        {
            char* src = (char*)accessor.GetPointer();
            Debug.Assert(*src == 0xFEFF); // BOM: Unicode, little endian
            // Skip the BOM when creating the reader
            using (var reader = new DirectMemoryAccessStreamReader(src + 1, streamLength / sizeof(char) - 1))
            {
                var buffer = CreateTextBuffer(reader, cancellationToken);
                return buffer.CurrentSnapshot.AsRoslynText(encoding ?? Encoding.Unicode);
            }
        }

        private unsafe class DirectMemoryAccessStreamReader : TextReader
        {
            private char* _position;
            private readonly char* _end;

            public DirectMemoryAccessStreamReader(char* src, int length)
            {
                Debug.Assert(src != null);
                Debug.Assert(length >= 0);
                _position = src;
                _end = _position + length;
            }

            public override int Read()
            {
                if(_position >= _end)
                {
                    return -1;
                }

                return *_position++;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (index < 0 || index >= buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (count < 0 || (index + count) > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                count = Math.Min(count, (int)(_end - _position));
                if (count > 0)
                {
                    Marshal.Copy((IntPtr)_position, buffer, index, count);
                    _position += count;
                }
                return count;
            }
        }
    }
}

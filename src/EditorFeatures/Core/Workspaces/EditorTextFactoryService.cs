// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceService(typeof(ITextFactoryService), ServiceLayer.Editor), Shared]
    [method: ImportingConstructor]
    [method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    internal sealed class EditorTextFactoryService(
        ITextBufferCloneService textBufferCloneService,
        ITextBufferFactoryService textBufferFactoryService,
        IContentTypeRegistryService contentTypeRegistryService) : ITextFactoryService
    {
        private readonly ITextBufferCloneService _textBufferCloneService = textBufferCloneService;
        private readonly ITextBufferFactoryService _textBufferFactory = textBufferFactoryService;
        private readonly IContentType _unknownContentType = contentTypeRegistryService.UnknownContentType;
        private static readonly Encoding s_throwingUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public SourceText CreateText(Stream stream, Encoding? defaultEncoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        {
            // this API is for a case where user wants us to figure out encoding from the given stream.
            // if defaultEncoding is given, we will use it if we couldn't figure out encoding used in the stream ourselves.
            RoslynDebug.Assert(stream != null);
            RoslynDebug.Assert(stream.CanSeek);
            RoslynDebug.Assert(stream.CanRead);

            if (defaultEncoding == null)
            {
                // Try UTF-8
                try
                {
                    return CreateTextInternal(stream, s_throwingUtf8Encoding, checksumAlgorithm, cancellationToken);
                }
                catch (DecoderFallbackException)
                {
                    // Try Encoding.Default
                    defaultEncoding = Encoding.Default;
                }
            }

            try
            {
                return CreateTextInternal(stream, defaultEncoding, checksumAlgorithm, cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                // TODO: the callers do not expect null (https://github.com/dotnet/roslyn/issues/43040)
                return null!;
            }
        }

        public SourceText CreateText(TextReader reader, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        {
            // this API is for a case where user just wants to create a source text with explicit encoding.
            var buffer = CreateTextBuffer(reader);

            // use the given encoding as it is.
            return buffer.CurrentSnapshot.AsRoslynText(_textBufferCloneService, encoding, checksumAlgorithm);
        }

        private ITextBuffer CreateTextBuffer(TextReader reader)
            => _textBufferFactory.CreateTextBuffer(reader, _unknownContentType);

        private SourceText CreateTextInternal(Stream stream, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

            var buffer = CreateTextBuffer(reader);
            return buffer.CurrentSnapshot.AsRoslynText(_textBufferCloneService, reader.CurrentEncoding ?? Encoding.UTF8, checksumAlgorithm);
        }
    }
}


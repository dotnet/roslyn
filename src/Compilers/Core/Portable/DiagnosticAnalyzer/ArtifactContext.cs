// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Context object that can be used to create non-source-code streams that are saved to disk during a normal
    /// compilation.  An <see cref="ArtifactContext"/> can be retrieved by calling <see
    /// cref="AnalysisContext.TryGetArtifactContext"/>.  However, this call will only succeed if the caller has the <see
    /// cref="ArtifactProducerAttribute"/> set on them and it is being called in a context where artifact production is
    /// supported.  In general that will only be when a compiler is invoked with the <c>generatedartifactsout</c>
    /// argument.
    /// </summary>
    public sealed class ArtifactContext
    {
        /// <summary>
        /// Callback the compiler can pass into us to actually generate artifacts.
        /// </summary>
        private readonly Func<string, Stream> _createArtifactStream;

        internal ArtifactContext(Func<string, Stream> createArtifactStream)
            => _createArtifactStream = createArtifactStream;

        /// <summary>
        /// Writes out an artifact with the contents of <paramref name="source"/>.  The artifact will be written out
        /// with utf8 encoding.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// <c>generatedartifactsout</c> path provided to the compiler.</param>
        /// <param name="source">The text to generate</param>
        public void WriteArtifact(string fileName, string source)
        {
            WriteArtifact(fileName, stream =>
            {
                using var writer = CreateStreamWriter(stream, Encoding.UTF8);
                writer.Write(source);
            });
        }

        /// <summary>
        /// Generates an artifact with the contents of <paramref name="builder"/>.  The artifact will be written out
        /// with utf8 encoding.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// <c>generatedartifactsout</c> path provided to the compiler.</param>
        /// <param name="builder">The string builder containing the contents to generate</param>
        public void WriteArtifact(string fileName, StringBuilder builder)
        {
            WriteArtifact(fileName, stream =>
            {
                using var writer = CreateStreamWriter(stream, Encoding.UTF8);
                writer.Write(builder);
            });
        }

        /// <summary>
        /// Generates an artifact with the contents and encoding specified by <paramref name="sourceText"/>.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// <c>generatedartifactsout</c> path provided to the compiler.</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to generate</param>
        public void WriteArtifact(string fileName, SourceText sourceText)
        {
            WriteArtifact(fileName, stream =>
            {
                using var writer = CreateStreamWriter(stream, sourceText.Encoding ?? Encoding.UTF8);
                sourceText.Write(writer);
            });
        }

        private static StreamWriter CreateStreamWriter(Stream stream, Encoding encoding)
        {
#if NETCOREAPP
            return new StreamWriter(stream, encoding, leaveOpen: true);
#else
            // From: https://github.com/microsoft/referencesource/blob/f461f1986ca4027720656a0c77bede9963e20b7e/mscorlib/system/io/streamwriter.cs#L48
            const int DefaultBufferSize = 1024;

            return new StreamWriter(stream, encoding, bufferSize: DefaultBufferSize, leaveOpen: true);
#endif
        }

        /// <summary>
        /// Requests a fresh stream associated with the given <paramref name="fileName"/> to write artifact data into.
        /// The callback should not call <see cref="Stream.Close"/>, <see cref="Stream.Flush"/>, or <see
        /// cref="Stream.Dispose()"/>.  The stream will be automatically flushed and closed after <paramref
        /// name="writeStream"/> is invoked.  This overload is useful if there is a large amount of data to write, or if
        /// there is binary data to write.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// <c>generatedartifactsout</c> path provided to the compiler.</param>
        /// <param name="writeStream">A callback that will be passed the stream to write into.</param>
        public void WriteArtifact(string fileName, Action<Stream> writeStream)
        {
            using var stream = CreateArtifactStream(fileName);
            writeStream(stream);
            stream.Flush();
        }

        /// <summary>
        /// Requests a fresh stream associated with the given <paramref name="fileName"/> to write artifact data into.
        /// After the compilation pass is done, all streams created by this will be flushed and disposed.  Calling <see
        /// cref="Stream.Flush"/> or <see cref="Stream.Dispose()"/> will not cause any problems. This overload is useful
        /// if there is a large amount of data to write, or if there is binary data to write.
        /// </summary>
        /// <remarks>
        /// There is no locking or ordering guaranteed around this stream.  If a client wants to write to this stream
        /// from multiple callbacks, it will need to coordinate that work itself internally.
        /// </remarks>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
        public Stream CreateArtifactStream(string fileName)
            => _createArtifactStream(fileName);
    }
}

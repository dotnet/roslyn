// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal class ArtifactGeneratorCallback
    {
        /// <summary>
        /// From: https://github.com/microsoft/referencesource/blob/f461f1986ca4027720656a0c77bede9963e20b7e/mscorlib/system/io/streamwriter.cs#L48
        /// </summary>
        private const int DefaultBufferSize = 1024;

        private readonly Action<(string hintPath, Action<Stream> callback)> _createArtifactStream;

        public ArtifactGeneratorCallback(Action<(string hintPath, Action<Stream> callback)> createArtifactStream)
        {
            _createArtifactStream = createArtifactStream;
        }

        /// <summary>
        /// Generates an artifact with the contents of <paramref name="source"/>.  The artifact will be written out with utf8 encoding.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this analyzer</param>
        /// <param name="source">The text to generate</param>
        public void GenerateArtifact(string hintName, string source)
        {
            GenerateArtifact(hintName, stream =>
            {
#if NETCOREAPP
                using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
#else
                using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: DefaultBufferSize, leaveOpen: true);
#endif
                writer.Write(source);
            });
        }

        /// <summary>
        /// Generates an artifact with the contents of <paramref name="builder"/>.  The artifact will be written out with utf8 encoding.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this analyzer</param>
        /// <param name="builder">The string builder containing the contents to generate</param>
        public void GenerateArtifact(string hintName, StringBuilder builder)
        {
            GenerateArtifact(hintName, stream =>
            {
#if NETCOREAPP
                using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
#else
                using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: DefaultBufferSize, leaveOpen: true);
#endif
                writer.Write(builder);
            });
        }

        /// <summary>
        /// Generates an artifact with the contents and encoding specified by <paramref name="sourceText"/>.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this analyzer</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to generate</param>
        public void GenerateArtifact(string hintName, SourceText sourceText)
        {
            GenerateArtifact(hintName, stream =>
            {
#if NETCOREAPP
                using var writer = new StreamWriter(stream, sourceText.Encoding ?? Encoding.UTF8, leaveOpen: true);
#else
                using var writer = new StreamWriter(stream, sourceText.Encoding ?? Encoding.UTF8, bufferSize: DefaultBufferSize, leaveOpen: true);
#endif
                sourceText.Write(writer);
            });
        }

        /// <summary>
        /// Requests a fresh stream associated with the given <paramref name="hintName"/> to write artifact data into.
        /// The callback does should not call <see cref="Stream.Close"/>, <see cref="Stream.Flush"/>, or <see
        /// cref="Stream.Dispose()"/>.  This overload is useful if there is a large amount of data to write, or if there
        /// is binary data to write.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this analyzer</param>
        /// <param name="writeStream">A callback that will be passed the stream to write into.</param>
        public void GenerateArtifact(string hintName, Action<Stream> writeStream)
            => _createArtifactStream((hintName, writeStream));
    }
}

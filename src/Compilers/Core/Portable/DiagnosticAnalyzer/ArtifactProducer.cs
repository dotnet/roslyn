// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for artifact producers that can programmatically generate additional non-code files during a
    /// compilation.  Artifact generators only run when a compiler is invoked with the <c>generatedfilesout</c>
    /// parameter.
    /// </summary>
    /// <remarks>
    /// Normally, an artifact generator will not need to report diagnostics.  However, it may sometimes be necessary if
    /// errors or other issues arise during generation.  If diagnostic reporting is needed, then the same mechanism are
    /// available as with normal analyzers.  Specifically, <see cref="SupportedDiagnostics"/> should be overridden to
    /// state which diagnostics may be produced, and the various context `ReportDiagnostic` calls should be used to
    /// report them.
    /// </remarks>
    public abstract class ArtifactProducer : DiagnosticAnalyzer
    {
#if !NETCOREAPP
        /// <summary>
        /// From: https://github.com/microsoft/referencesource/blob/f461f1986ca4027720656a0c77bede9963e20b7e/mscorlib/system/io/streamwriter.cs#L48
        /// </summary>
        private const int DefaultBufferSize = 1024;
#endif

        /// <summary>
        /// Callback the compiler can pass into us to actually generate artifacts.  This is safe to hold as a mutable
        /// value as this is only set once during batch compile as that's the only scenario where /generatedartifactsout
        /// can be provided.
        /// </summary>
        internal Action<string, Action<Stream>>? CreateArtifactStream;

        // By default artifact generators don't report diagnostics.  However, they are still allowed to if they run into
        // any issues.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray<DiagnosticDescriptor>.Empty;

        /// <summary>
        /// Writes out an artifact with the contents of <paramref name="source"/>.  The artifact will be written out with utf8 encoding.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
        /// <param name="source">The text to generate</param>
        protected void WriteArtifact(string fileName, string source)
        {
            WriteArtifact(fileName, stream =>
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
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
        /// <param name="builder">The string builder containing the contents to generate</param>
        protected void WriteArtifact(string fileName, StringBuilder builder)
        {
            WriteArtifact(fileName, stream =>
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
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to generate</param>
        protected void WriteArtifact(string fileName, SourceText sourceText)
        {
            WriteArtifact(fileName, stream =>
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
        /// Requests a fresh stream associated with the given <paramref name="fileName"/> to write artifact data into.
        /// The callback does should not call <see cref="Stream.Close"/>, <see cref="Stream.Flush"/>, or <see
        /// cref="Stream.Dispose()"/>.  This overload is useful if there is a large amount of data to write, or if there
        /// is binary data to write.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
        /// <param name="writeStream">A callback that will be passed the stream to write into.</param>
        protected void WriteArtifact(string fileName, Action<Stream> writeStream)
            => CreateArtifactStream!(fileName, writeStream);
    }
}

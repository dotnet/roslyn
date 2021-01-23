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
    /// The base interface for artifact producers that can programmatically generate additional non-code files during a
    /// compilation.  Artifact generators only run when a compiler is invoked with the <c>generatedartifactsout</c>
    /// parameter.
    /// </summary>
    /// <remarks>
    /// Normally, an artifact generator will not need to report diagnostics.  However, it may sometimes be necessary if
    /// errors or other issues arise during generation.  If diagnostic reporting is needed, then the same mechanism are
    /// available as with normal analyzers.  Specifically, <see cref="SupportedDiagnostics"/> should be overridden to
    /// state which diagnostics may be produced, and the various context `ReportDiagnostic` calls should be used to
    /// report them.
    /// </remarks>
    public interface IArtifactProducer
    {
        /// <summary>
        /// Returns a set of descriptors for the diagnostics that this artifact producer is capable of producing.
        /// </summary>
        ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        /// <summary>
        /// Called once at session start to register actions in the analysis context.  <paramref
        /// name="artifactContext"/> can be used to create or write to streams that will be written out as artifacts.
        /// </summary>
        void Initialize(AnalysisContext analysisContext, ArtifactContext artifactContext);
    }

    public struct ArtifactContext
    {
        /// <summary>
        /// Callback the compiler can pass into us to actually generate artifacts.  This is safe to hold as a mutable
        /// value as this is only set once during batch compile as that's the only scenario where /generatedartifactsout
        /// can be provided.
        /// </summary>
        private readonly Func<string, Stream> _createArtifactStream;

        internal ArtifactContext(Func<string, Stream> createArtifactStream)
        {
            _createArtifactStream = createArtifactStream;
        }

        /// <summary>
        /// Writes out an artifact with the contents of <paramref name="source"/>.  The artifact will be written out with utf8 encoding.
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
        /// Generates an artifact with the contents of <paramref name="builder"/>.  The artifact will be written out with utf8 encoding.
        /// </summary>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
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
        /// generatedartifactsout path provided to the compiler.</param>
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
        /// generatedartifactsout path provided to the compiler.</param>
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
        /// There is no locking or ordering guaranteed around this stream.  If an <see cref="IArtifactProducer"/> wants
        /// to write to this stream from multiple callbacks, it will need to coordinate that work itself internally.
        /// </remarks>
        /// <param name="fileName">The file name to generate this artifact into.  Will be concatenated with the
        /// generatedartifactsout path provided to the compiler.</param>
        public Stream CreateArtifactStream(string fileName)
            => _createArtifactStream(fileName);
    }

    /// <summary>
    /// Wrapper type to allow an IArtifactProducer to flow through the normal DiagnosticAnalyzer codepaths.
    /// </summary>
    internal class ArtifactProducerDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public readonly IArtifactProducer ArtifactProducer;

        public ArtifactProducerDiagnosticAnalyzer(IArtifactProducer artifactProducer)
        {
            ArtifactProducer = artifactProducer;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ArtifactProducer.SupportedDiagnostics;

#pragma warning disable RS1025 // Configure generated code analysis
#pragma warning disable RS1026 // Enable concurrent execution
        public override void Initialize(AnalysisContext context)
        {
            throw new InvalidOperationException("Caller should call initialize method on ArtifactProducerInstead");
        }
#pragma warning restore RS1025 // Configure generated code analysis
#pragma warning restore RS1026 // Enable concurrent execution
    }
}

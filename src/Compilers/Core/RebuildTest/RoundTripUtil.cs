// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
using Microsoft.Metadata.Tools;
using Roslyn.Utilities;
using Xunit;
using Roslyn.Test.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    internal static class RoundTripUtil
    {
        public static void VerifyRoundTrip(
            MemoryStream peStream,
            MemoryStream? pdbStream,
            string assemblyFileName,
            IRebuildArtifactResolver rebuildArtifactResolver,
            CancellationToken cancellationToken = default)
        {
            using var peReader = new PEReader(peStream);
            var embeddedPdbReader = peReader.GetEmbeddedPdbMetadataReader();
            var portablePdbReader = pdbStream is not null ? MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader() : null;
            Assert.True(embeddedPdbReader == null ^ portablePdbReader == null);

            var pdbReader = embeddedPdbReader ?? portablePdbReader ?? throw ExceptionUtilities.Unreachable();
            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");
            var optionsReader = new CompilationOptionsReader(logger, pdbReader, peReader);
            var compilationFactory = CompilationFactory.Create(
                assemblyFileName,
                optionsReader);
            using var rebuildPeStream = new MemoryStream();
            using var rebuildPdbStream = optionsReader.HasEmbeddedPdb ? null : new MemoryStream();
            var emitResult = compilationFactory.Emit(
                rebuildPeStream,
                rebuildPdbStream,
                rebuildArtifactResolver,
                cancellationToken);

            Assert.True(emitResult.Success);
            Assert.Equal(peStream.ToArray(), rebuildPeStream.ToArray());
            Assert.Equal(pdbStream?.ToArray(), rebuildPdbStream?.ToArray());
        }

        private record EmitInfo(ImmutableArray<byte> PEBytes, PEReader PEReader, ImmutableArray<byte> PdbBytes, MetadataReader PdbReader) : IDisposable
        {
            public void Dispose() => PEReader.Dispose();
        }

        public static unsafe void VerifyRoundTrip<TCompilation>(TCompilation original, EmitOptions? emitOptions = null)
            where TCompilation : Compilation
        {
            Assert.True(original.SyntaxTrees.All(x => !string.IsNullOrEmpty(x.FilePath)));
            Assert.True(original.Options.Deterministic);

            original.VerifyDiagnostics();
            emitOptions ??= new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
            using var originalEmit = emitOriginal();
            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");
            var optionsReader = new CompilationOptionsReader(logger, originalEmit.PdbReader, originalEmit.PEReader);
            var assemblyFileName = original.AssemblyName!;
            if (typeof(TCompilation) == typeof(CSharpCompilation))
            {
                var assemblyFileExtension = original.Options.OutputKind switch
                {
                    OutputKind.DynamicallyLinkedLibrary => ".dll",
                    OutputKind.ConsoleApplication => ".exe",
                    _ => throw new InvalidOperationException(),
                };
                assemblyFileName += assemblyFileExtension;
            }

            var compilationFactory = CompilationFactory.Create(assemblyFileName, optionsReader);
            var rebuild = compilationFactory.CreateCompilation(
                original.SyntaxTrees.SelectAsArray(x => compilationFactory.CreateSyntaxTree(x.FilePath, x.GetText())),
                original.References.ToImmutableArray());

            Assert.IsType<TCompilation>(rebuild);
            VerifyCompilationOptions(original.Options, rebuild.Options);

            using var rebuildStream = new MemoryStream();
            using var rebuildPdbStream = optionsReader.HasEmbeddedPdb ? null : new MemoryStream();
            var result = compilationFactory.Emit(
                rebuildStream,
                rebuildPdbStream,
                rebuild,
                embeddedTexts: ImmutableArray<EmbeddedText>.Empty,
                CancellationToken.None);
            Assert.Empty(result.Diagnostics);
            Assert.True(result.Success);

            var rebuildBytes = rebuildStream.ToImmutable();
            using var rebuildReader = new PEReader(rebuildBytes);

            ImmutableArray<byte> rebuildPdbBytes;
            MetadataReader rebuildPdbReader;
            if (rebuildPdbStream is null)
            {
                rebuildPdbReader = rebuildReader.GetEmbeddedPdbMetadataReader() ?? throw new InvalidOperationException();
                rebuildPdbBytes = new ReadOnlySpan<byte>(rebuildPdbReader.MetadataPointer, rebuildPdbReader.MetadataLength).ToArray().ToImmutableArray();
            }
            else
            {
                rebuildPdbBytes = rebuildPdbStream.ToImmutable();
                rebuildPdbReader = MetadataReaderProvider.FromPortablePdbImage(rebuildPdbBytes).GetMetadataReader();
            }

            var rebuildEmit = new EmitInfo(rebuildBytes, rebuildReader, rebuildPdbBytes, rebuildPdbReader);

            // https://github.com/dotnet/roslyn/issues/52327
            // This should be replaced with a CompilationDiff-based helper in MS.CA.RB which writes diffs
            // out to the test output, not necessarily entire visualizations.
            AssertImagesEqual(originalEmit, rebuildEmit);

            unsafe EmitInfo emitOriginal()
            {
                switch (emitOptions.DebugInformationFormat)
                {
                    case DebugInformationFormat.Embedded:
                        {
                            var originalBytes = original.EmitToArray(emitOptions);
                            var originalReader = new PEReader(originalBytes);
                            var originalPdbReader = originalReader.GetEmbeddedPdbMetadataReader();
                            Assert.NotNull(originalPdbReader);

                            var pdbSpan = new ReadOnlySpan<byte>(originalPdbReader!.MetadataPointer, originalPdbReader.MetadataLength);
                            return new EmitInfo(originalBytes, originalReader, pdbSpan.ToArray().ToImmutableArray(), originalPdbReader);
                        }
                    case DebugInformationFormat.PortablePdb:
                        {
                            using var pdbStream = new MemoryStream();
                            var originalBytes = original.EmitToArray(emitOptions, pdbStream: pdbStream);
                            var originalReader = new PEReader(originalBytes);

                            var pdbBytes = pdbStream.ToImmutable();
                            var originalPdbReader = MetadataReaderProvider.FromPortablePdbImage(pdbBytes).GetMetadataReader();
                            return new EmitInfo(originalBytes, originalReader, pdbBytes, originalPdbReader);
                        }
                    default:
                        throw new ArgumentException("Unsupported DebugInformationFormat: " + emitOptions.DebugInformationFormat);
                }
            }
        }

        private static void AssertImagesEqual(
            EmitInfo originalEmit,
            EmitInfo rebuildEmit)
        {
            if (!originalEmit.PEBytes.SequenceEqual(rebuildEmit.PEBytes))
            {
                var originalMdv = getMdv(originalEmit.PEReader.GetMetadataReader());
                var rebuildMdv = getMdv(rebuildEmit.PEReader.GetMetadataReader());

                // At this point the bytes were not equal, so the MDV output should also not be equal.
                Assert.NotEqual(originalMdv, rebuildMdv);

                // https://github.com/dotnet/roslyn/issues/52327
                // this is not all that useful without manual copy/pasting. Can we diff this during the test and show the differences?
                Assert.True(false, $@"
Expected:
{originalMdv}

Actual:
{rebuildMdv}
");
            }

            if (!originalEmit.PdbBytes.SequenceEqual(rebuildEmit.PdbBytes))
            {
                var originalMdv = getMdv(originalEmit.PdbReader);
                var rebuildMdv = getMdv(rebuildEmit.PdbReader);

                Assert.True(false, $@"
Expected:
{originalMdv}

Actual:
{rebuildMdv}
");
            }

            static string getMdv(MetadataReader metadataReader)
            {
                using var stream = new MemoryStream();
                var writer = new StreamWriter(stream);

                var visualizer = new MetadataVisualizer(metadataReader, writer);
                visualizer.Visualize();
                writer.Flush();

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

#pragma warning disable 612 // 'CompilationOptions.Features' is obsolete

        public static void VerifyCompilationOptions(CompilationOptions originalOptions, CompilationOptions rebuildOptions)
        {
            var type = originalOptions.GetType();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                switch (propertyInfo.Name)
                {
                    case nameof(CompilationOptions.GeneralDiagnosticOption):
                    case nameof(CompilationOptions.Features):
                    case nameof(CompilationOptions.ModuleName):
                    case nameof(CompilationOptions.MainTypeName):
                    case nameof(CompilationOptions.ConcurrentBuild):
                    case nameof(CompilationOptions.WarningLevel):
                    case nameof(CompilationOptions.SyntaxTreeOptionsProvider):
                    case nameof(CompilationOptions.MetadataReferenceResolver):
                    case nameof(CompilationOptions.XmlReferenceResolver):
                    case nameof(CompilationOptions.SourceReferenceResolver):
                    case nameof(CompilationOptions.StrongNameProvider):
                    case nameof(CompilationOptions.AssemblyIdentityComparer):
                        // Can be different and are special cased
                        break;
                    case nameof(VisualBasicCompilationOptions.ParseOptions):
                        {
                            var originalValue = propertyInfo.GetValue(originalOptions)!;
                            var rebuildValue = propertyInfo.GetValue(rebuildOptions)!;
                            VerifyParseOptions((ParseOptions)originalValue, (ParseOptions)rebuildValue);
                        }
                        break;
                    default:
                        {
                            var originalValue = propertyInfo.GetValue(originalOptions);
                            var rebuildValue = propertyInfo.GetValue(rebuildOptions);
                            Assert.Equal(originalValue, rebuildValue);
                        }
                        break;
                }
            }
        }
#pragma warning restore 612

        private static void VerifyParseOptions(ParseOptions originalOptions, ParseOptions rebuildOptions)
        {
            var type = originalOptions.GetType();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // Several options are expected to be different and they are special cased here.
                if (propertyInfo.Name == nameof(VisualBasicParseOptions.SpecifiedLanguageVersion))
                {
                    continue;
                }

                var originalValue = propertyInfo.GetValue(originalOptions);
                var rebuildValue = propertyInfo.GetValue(rebuildOptions);

                Assert.Equal(originalValue, rebuildValue);
            }
        }
    }
}

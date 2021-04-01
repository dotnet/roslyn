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

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    internal static class RoundTripUtil
    {
        public static void VerifyRoundTrip(
            MemoryStream peStream,
            MemoryStream? pdbStream,
            string assemblyFileName,
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences,
            CancellationToken cancellationToken = default)
        {
            // TODO: introduce CommonCompiler-based test which exercises this code path.
            using var peReader = new PEReader(peStream);
            var embeddedPdbReader = peReader.GetEmbeddedPdbMetadataReader();
            var nonEmbeddedPdbReader = pdbStream is not null ? MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader() : null;
            Assert.True(embeddedPdbReader == null ^ nonEmbeddedPdbReader == null);

            var pdbReader = embeddedPdbReader ?? nonEmbeddedPdbReader ?? throw ExceptionUtilities.Unreachable;
            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");
            var optionsReader = new CompilationOptionsReader(logger, pdbReader, peReader);
            var compilationFactory = CompilationFactory.Create(
                assemblyFileName,
                optionsReader);
            using var rebuildPeStream = new MemoryStream();
            using var rebuildPdbStream = optionsReader.HasEmbeddedPdb ? null : new MemoryStream();
            var emitResult = compilationFactory.Emit(rebuildPeStream, rebuildPdbStream, syntaxTrees, metadataReferences, cancellationToken);
            Assert.True(emitResult.Success);

            Assert.True(peStream.ToArray().SequenceEqual(rebuildPeStream.ToArray()));
        }

        public static void VerifyRoundTrip<TCompilation>(TCompilation original, EmitOptions? emitOptions = null)
            where TCompilation : Compilation
        {
            Assert.True(original.SyntaxTrees.All(x => !string.IsNullOrEmpty(x.FilePath)));
            Assert.True(original.Options.Deterministic);

            original.VerifyDiagnostics();
            emitOptions ??= new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
            var (originalBytes, originalReader, originalPdbReader) = emitOriginal();

            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");
            var optionsReader = new CompilationOptionsReader(logger, originalPdbReader, originalReader);
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
            var rebuildReader = new PEReader(rebuildBytes);

            // https://github.com/dotnet/roslyn/issues/52327
            // This should be replaced with a CompilationDiff-based helper in MS.CA.RB which writes diffs
            // out to the test output, not necessarily entire visualizations.
            AssertImagesEqual(originalBytes, originalReader, rebuildBytes, rebuildReader);

            (ImmutableArray<byte> originalBytes, PEReader originalReader, MetadataReader originalPdbReader) emitOriginal()
            {
                switch (emitOptions.DebugInformationFormat)
                {
                    case DebugInformationFormat.Embedded:
                    {
                        var originalBytes = original.EmitToArray(emitOptions);
                        var originalReader = new PEReader(originalBytes);
                        var originalPdbReader = originalReader.GetEmbeddedPdbMetadataReader();
                        Assert.NotNull(originalPdbReader);
                        return (originalBytes, originalReader, originalPdbReader!);
                    }
                    case DebugInformationFormat.PortablePdb:
                    {
                        using var pdbStream = new MemoryStream();
                        var originalBytes = original.EmitToArray(emitOptions, pdbStream: pdbStream);
                        var originalReader = new PEReader(originalBytes);

                        pdbStream.Position = 0;
                        var originalPdbReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
                        return (originalBytes, originalReader, originalPdbReader);
                    }
                    default:
                        throw new ArgumentException("Unsupported DebugInformationFormat: " + emitOptions.DebugInformationFormat);
                }
            }
        }

        private static void AssertImagesEqual(
            ImmutableArray<byte> originalBytes,
            PEReader originalReader,
            ImmutableArray<byte> rebuildBytes,
            PEReader rebuildReader)
        {
            if (originalBytes.SequenceEqual(rebuildBytes))
            {
                return;
            }

            var originalMdv = GetMdv(originalReader);
            var rebuildMdv = GetMdv(rebuildReader);

            // At this point the bytes were not equal, so the MDV output should also not be equal.
            Assert.NotEqual(originalMdv, rebuildMdv);

            // TODO: this is not all that useful without manual copy/pasting. Can we diff this during the test and show the differences.
            Assert.True(false, $@"
Expected:
{originalMdv}

Actual:
{rebuildMdv}
");

            static string GetMdv(PEReader peReader)
            {
                using var stream = new MemoryStream();
                var writer = new StreamWriter(stream);

                var visualizer = new MetadataVisualizer(peReader.GetMetadataReader(), writer);
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

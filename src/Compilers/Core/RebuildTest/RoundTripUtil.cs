// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
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
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences,
            CancellationToken cancellationToken = default)
        {
            // https://github.com/dotnet/roslyn/issues/51890
            // Will be null until we add support for non-embedded PDBs. When this assert fires 
            // the below logic needs to be updated to support non-embedded PDBs. Should be straight
            // forward
            Assert.Null(pdbStream);
            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("RoundTripVerification");

            using var peReader = new PEReader(peStream);
            var optionsReader = new CompilationOptionsReader(logger, peReader.GetEmbeddedPdbMetadataReader(), peReader);
            VerifyMetadataReferenceInfo(optionsReader, metadataReferences);

            var compilationFactory = CompilationFactory.Create(
                assemblyFileName,
                optionsReader);
            using var rebuildPeStream = new MemoryStream();
            var emitResult = compilationFactory.Emit(rebuildPeStream, syntaxTrees, metadataReferences, cancellationToken);
            Assert.True(emitResult.Success);
            Assert.True(peStream.ToArray().SequenceEqual(rebuildPeStream.ToArray()));
        }

        public static void VerifyMetadataReferenceInfo(CompilationOptionsReader optionsReader, ImmutableArray<MetadataReference> metadataReferences)
        {
            var count = 0;
            foreach (var info in optionsReader.GetMetadataReferences())
            {
                count++;
                var metadataReference = metadataReferences.FirstOrDefault(x =>
                    info.Mvid == x.GetModuleVersionId() &&
                    info.ExternAlias == GetSingleAlias(x));
                AssertEx.NotNull(metadataReference);

                string? GetSingleAlias(MetadataReference metadataReference)
                {
                    Assert.True(metadataReference.Properties.Aliases.Length is 0 or 1);
                    return metadataReference.Properties.Aliases.Length == 1
                        ? metadataReference.Properties.Aliases[0]
                        : null;
                }
            }

            Assert.Equal(metadataReferences.Length, count);

        }

        public static void VerifyRoundTrip<TCompilation>(TCompilation original)
            where TCompilation : Compilation
        {
            Assert.True(original.SyntaxTrees.All(x => !string.IsNullOrEmpty(x.FilePath)));
            Assert.True(original.Options.Deterministic);

            original.VerifyDiagnostics();
            var originalBytes = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var originalReader = new PEReader(originalBytes);
            var originalPdbReader = originalReader.GetEmbeddedPdbMetadataReader();

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
            var result = compilationFactory.Emit(
                rebuildStream,
                rebuild,
                embeddedTexts: ImmutableArray<EmbeddedText>.Empty,
                CancellationToken.None);
            Assert.Empty(result.Diagnostics);
            Assert.True(result.Success);
            Assert.Equal(originalBytes.ToArray(), rebuildStream.ToArray());
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

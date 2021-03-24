// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace BuildValidator
{
    public abstract class CompilationFactory
    {
        public string AssemblyFileName { get; }
        public CompilationOptionsReader OptionsReader { get; }
        public ParseOptions ParseOptions => CommonParseOptions;
        public CompilationOptions CompilationOptions => CommonCompilationOptions;

        protected abstract ParseOptions CommonParseOptions { get; }
        protected abstract CompilationOptions CommonCompilationOptions { get; }

        protected CompilationFactory(string assemblyFileName, CompilationOptionsReader optionsReader)
        {
            AssemblyFileName = assemblyFileName;
            OptionsReader = optionsReader;
        }

        public static CompilationFactory Create(string assemblyFileName, CompilationOptionsReader optionsReader)
            => optionsReader.GetLanguageName() switch
            {
                LanguageNames.CSharp => CSharpCompilationFactory.Create(assemblyFileName, optionsReader),
                LanguageNames.VisualBasic => VisualBasicCompilationFactory.Create(assemblyFileName, optionsReader),
                var language => throw new InvalidDataException($"{assemblyFileName} has unsupported language {language}")
            };

        public abstract SyntaxTree CreateSyntaxTree(string filePath, SourceText sourceText);

        public abstract Compilation CreateCompilation(
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences);

        public EmitResult Emit(
            Stream rebuildPeStream,
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences,
            CancellationToken cancellationToken)
            => Emit(
                rebuildPeStream,
                CreateCompilation(syntaxTrees, metadataReferences),
                cancellationToken);

        public EmitResult Emit(
            Stream rebuildPeStream,
            Compilation rebuildCompilation,
            CancellationToken cancellationToken)
        {
            var embeddedTexts = rebuildCompilation.SyntaxTrees
                    .Select(st => (path: st.FilePath, text: st.GetText()))
                    .Where(pair => pair.text.CanBeEmbedded)
                    .Select(pair => EmbeddedText.FromSource(pair.path, pair.text))
                    .ToImmutableArray();

            return Emit(
                rebuildPeStream,
                rebuildCompilation,
                embeddedTexts,
                cancellationToken);
        }

        public unsafe EmitResult Emit(
            Stream rebuildPeStream,
            Compilation rebuildCompilation,
            ImmutableArray<EmbeddedText> embeddedTexts,
            CancellationToken cancellationToken)
        {
            var peHeader = OptionsReader.PeReader.PEHeaders.PEHeader!;
            var win32Resources = OptionsReader.PeReader.GetSectionData(peHeader.ResourceTableDirectory.RelativeVirtualAddress);
            using var win32ResourceStream = win32Resources.Pointer != null
                ? new UnmanagedMemoryStream(win32Resources.Pointer, win32Resources.Length)
                : null;

            var sourceLink = OptionsReader.GetSourceLinkUTF8();

            var debugEntryPoint = getDebugEntryPoint();

            var emitResult = rebuildCompilation.Emit(
                peStream: rebuildPeStream,
                pdbStream: null,
                xmlDocumentationStream: null,
                win32Resources: win32ResourceStream,
                useRawWin32Resources: true,
                manifestResources: OptionsReader.GetManifestResources(),
                options: new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.Embedded,
                    highEntropyVirtualAddressSpace: (peHeader.DllCharacteristics & DllCharacteristics.HighEntropyVirtualAddressSpace) != 0,
                    subsystemVersion: SubsystemVersion.Create(peHeader.MajorSubsystemVersion, peHeader.MinorSubsystemVersion)),
                debugEntryPoint: debugEntryPoint,
                metadataPEStream: null,
                pdbOptionsBlobReader: OptionsReader.GetMetadataCompilationOptionsBlobReader(),
                sourceLinkStream: sourceLink != null ? new MemoryStream(sourceLink) : null,
                embeddedTexts: embeddedTexts,
                cancellationToken: cancellationToken);

            return emitResult;

            IMethodSymbol? getDebugEntryPoint()
            {
                if (OptionsReader.GetMainMethodInfo() is (string mainTypeName, string mainMethodName))
                {
                    var typeSymbol = rebuildCompilation.GetTypeByMetadataName(mainTypeName);
                    if (typeSymbol is object)
                    {
                        var methodSymbols = typeSymbol
                            .GetMembers(mainMethodName)
                            .OfType<IMethodSymbol>();
                        return methodSymbols.FirstOrDefault();
                    }
                }

                return null;
            }
        }

        protected static (OptimizationLevel, bool) GetOptimizationLevel(string? optimizationLevel)
            => optimizationLevel switch
            {
                null or "debug" => (OptimizationLevel.Debug, false),
                "debug-plus" => (OptimizationLevel.Debug, true),
                "release" => (OptimizationLevel.Release, false),
                _ => throw new InvalidDataException($"Optimization \"{optimizationLevel}\" level not recognized")
            };

        protected static Platform GetPlatform(string? platform)
            => platform is null
                ? Platform.AnyCpu
                : (Platform)Enum.Parse(typeof(Platform), platform);
    }
}

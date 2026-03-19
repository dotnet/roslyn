// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rebuild
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

        public Compilation CreateCompilation(IRebuildArtifactResolver resolver)
        {
            var tuple = OptionsReader.ResolveArtifacts(resolver, CreateSyntaxTree);
            return CreateCompilation(tuple.SyntaxTrees, tuple.MetadataReferences);
        }

        public abstract Compilation CreateCompilation(
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences);

        public EmitResult Emit(
            Stream rebuildPeStream,
            Stream? rebuildPdbStream,
            IRebuildArtifactResolver rebuildArtifactResolver,
            CancellationToken cancellationToken)
            => Emit(
                rebuildPeStream,
                rebuildPdbStream,
                CreateCompilation(rebuildArtifactResolver),
                cancellationToken);

        public EmitResult Emit(
            Stream rebuildPeStream,
            Stream? rebuildPdbStream,
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> metadataReferences,
            CancellationToken cancellationToken)
            => Emit(
                rebuildPeStream,
                rebuildPdbStream,
                CreateCompilation(syntaxTrees, metadataReferences),
                cancellationToken);

        public EmitResult Emit(
            Stream rebuildPeStream,
            Stream? rebuildPdbStream,
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
                rebuildPdbStream,
                rebuildCompilation,
                embeddedTexts,
                cancellationToken);
        }

        public unsafe EmitResult Emit(
            Stream rebuildPeStream,
            Stream? rebuildPdbStream,
            Compilation rebuildCompilation,
            ImmutableArray<EmbeddedText> embeddedTexts,
            CancellationToken cancellationToken)
        {
            var peHeader = OptionsReader.PeReader.PEHeaders.PEHeader!;
            var win32Resources = OptionsReader.PeReader.GetSectionData(peHeader.ResourceTableDirectory.RelativeVirtualAddress);
            using var win32ResourceStream = win32Resources.Pointer != null
                ? new UnmanagedMemoryStream(win32Resources.Pointer, win32Resources.Length)
                : null;

            var sourceLink = OptionsReader.GetSourceLinkUtf8();

            var debugEntryPoint = getDebugEntryPoint();
            string? pdbFilePath;
            DebugInformationFormat debugInformationFormat;
            if (OptionsReader.HasEmbeddedPdb)
            {
                if (rebuildPdbStream is object)
                {
                    throw new ArgumentException(RebuildResources.PDB_stream_must_be_null_because_the_compilation_has_an_embedded_PDB, nameof(rebuildPdbStream));
                }

                debugInformationFormat = DebugInformationFormat.Embedded;
                pdbFilePath = null;
            }
            else
            {
                if (rebuildPdbStream is null)
                {
                    throw new ArgumentException(RebuildResources.A_non_null_PDB_stream_must_be_provided_because_the_compilation_does_not_have_an_embedded_PDB, nameof(rebuildPdbStream));
                }

                debugInformationFormat = DebugInformationFormat.PortablePdb;
                var codeViewEntry = OptionsReader.PeReader.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.CodeView);
                var codeView = OptionsReader.PeReader.ReadCodeViewDebugDirectoryData(codeViewEntry);
                pdbFilePath = codeView.Path ?? throw new InvalidOperationException(RebuildResources.Could_not_get_PDB_file_path);
            }

            var rebuildData = new RebuildData(
                OptionsReader.GetMetadataCompilationOptionsBlobReader(),
                getNonSourceFileDocumentNames(OptionsReader.PdbReader, OptionsReader.GetSourceFileCount()));
            var emitResult = rebuildCompilation.Emit(
                peStream: rebuildPeStream,
                pdbStream: rebuildPdbStream,
                xmlDocumentationStream: null,
                win32Resources: win32ResourceStream,
                manifestResources: OptionsReader.GetManifestResources(),
                options: new EmitOptions(
                    debugInformationFormat: debugInformationFormat,
                    pdbFilePath: pdbFilePath,
                    highEntropyVirtualAddressSpace: (peHeader.DllCharacteristics & DllCharacteristics.HighEntropyVirtualAddressSpace) != 0,
                    subsystemVersion: SubsystemVersion.Create(peHeader.MajorSubsystemVersion, peHeader.MinorSubsystemVersion)),
                debugEntryPoint: debugEntryPoint,
                metadataPEStream: null,
                rebuildData: rebuildData,
                sourceLinkStream: sourceLink != null ? new MemoryStream(sourceLink) : null,
                embeddedTexts: embeddedTexts,
                cancellationToken: cancellationToken);

            return emitResult;

            static ImmutableArray<string> getNonSourceFileDocumentNames(MetadataReader pdbReader, int sourceFileCount)
            {
                var count = pdbReader.Documents.Count - sourceFileCount;
                var builder = ArrayBuilder<string>.GetInstance(count);
                foreach (var documentHandle in pdbReader.Documents.Skip(sourceFileCount))
                {
                    var document = pdbReader.GetDocument(documentHandle);
                    var name = pdbReader.GetString(document.Name);
                    builder.Add(name);
                }
                return builder.ToImmutableAndFree();
            }

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

        protected static (OptimizationLevel OptimizationLevel, bool DebugPlus) GetOptimizationLevel(string? value)
        {
            if (value is null)
            {
                return OptimizationLevelFacts.DefaultValues;
            }

            if (!OptimizationLevelFacts.TryParsePdbSerializedString(value, out OptimizationLevel optimizationLevel, out bool debugPlus))
            {
                throw new InvalidOperationException();
            }

            return (optimizationLevel, debugPlus);
        }

        protected static Platform GetPlatform(string? platform)
            => platform is null
                ? Platform.AnyCpu
                : (Platform)Enum.Parse(typeof(Platform), platform);
    }
}

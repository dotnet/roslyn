// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.DiaSymReader.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Metadata.Tools;
using Roslyn.Utilities;

namespace BuildValidator
{
    internal enum RebuildResult
    {
        Success,
        MissingReferences,
        CompilationError,
        BinaryDifference,
        MiscError
    }

    internal sealed class CompilationDiff
    {
        public record BuildInfo(
            byte[] AssemblyBytes,
            PEReader AssemblyReader,
            MetadataReader PdbMetadataReader)
        {
            public MetadataReader AssemblyMetadataReader { get; } = AssemblyReader.GetMetadataReader();
        }

        public record BuildDataFiles(
            string AssemblyMdvFilePath,
            string PdbMdvFilePath,
            string PdbXmlFilePath,
            string ILFilePath,
            string CustomDataFilePath);

        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly byte[]? _originalPortableExecutableBytes;
        private readonly MetadataReader? _originalPdbReader;
        private readonly byte[]? _rebuildPortableExecutableBytes;
        private readonly MetadataReader? _rebuildPdbReader;
        private readonly Compilation? _rebuildCompilation;
        private readonly ImmutableArray<MetadataReferenceInfo> _references;
        private readonly LocalReferenceResolver? _localReferenceResolver;
        private readonly string? _message;

        public AssemblyInfo AssemblyInfo { get; }
        public RebuildResult Result { get; }

        public bool Succeeded => Result == RebuildResult.Success;

        public ImmutableArray<Diagnostic> Diagnostics
        {
            get
            {
                EnsureRebuildResult(RebuildResult.CompilationError);
                return _diagnostics;
            }
        }

        public string MiscErrorMessage
        {
            get
            {
                EnsureRebuildResult(RebuildResult.MiscError);
                Debug.Assert(_message is object);
                return _message;
            }
        }

        private CompilationDiff(
            AssemblyInfo assemblyInfo,
            RebuildResult outcome,
            ImmutableArray<Diagnostic> diagnostics = default,
            byte[]? originalPortableExecutableBytes = null,
            MetadataReader? originalPdbReader = null,
            byte[]? rebuildPortableExecutableBytes = null,
            MetadataReader? rebuildPdbReader = null,
            Compilation? rebuildCompilation = null,
            LocalReferenceResolver? localReferenceResolver = null,
            ImmutableArray<MetadataReferenceInfo> references = default,
            string? message = null)
        {
            AssemblyInfo = assemblyInfo;
            Result = outcome;
            _diagnostics = diagnostics;
            _originalPortableExecutableBytes = originalPortableExecutableBytes;
            _originalPdbReader = originalPdbReader;
            _rebuildPortableExecutableBytes = rebuildPortableExecutableBytes;
            _rebuildPdbReader = rebuildPdbReader;
            _rebuildCompilation = rebuildCompilation;
            _references = references;
            _localReferenceResolver = localReferenceResolver;
            _message = message;
        }

        public static CompilationDiff CreateMiscError(
            AssemblyInfo assemblyInfo,
            string message)
            => new CompilationDiff(
                assemblyInfo,
                RebuildResult.MiscError,
                message: message);

        public static unsafe CompilationDiff CreateMissingReferences(
            AssemblyInfo assemblyInfo,
            LocalReferenceResolver resolver,
            ImmutableArray<MetadataReferenceInfo> references)
            => new CompilationDiff(
                assemblyInfo,
                RebuildResult.MissingReferences,
                localReferenceResolver: resolver,
                references: references);

        public static unsafe CompilationDiff Create(
            AssemblyInfo assemblyInfo,
            CompilationFactory compilationFactory,
            RebuildArtifactResolver artifactResolver,
            ILogger logger)
        {
            using var rebuildPeStream = new MemoryStream();
            var hasEmbeddedPdb = compilationFactory.OptionsReader.HasEmbeddedPdb;
            var rebuildPdbStream = hasEmbeddedPdb ? null : new MemoryStream();
            var rebuildCompilation = compilationFactory.CreateCompilation(artifactResolver);
            var emitResult = compilationFactory.Emit(
                rebuildPeStream,
                rebuildPdbStream,
                rebuildCompilation,
                CancellationToken.None);

            if (!emitResult.Success)
            {
                using var diagsScope = logger.BeginScope($"Diagnostics");
                foreach (var diag in emitResult.Diagnostics)
                {
                    logger.LogError(diag.ToString());
                }

                return new CompilationDiff(
                    assemblyInfo,
                    RebuildResult.CompilationError,
                    diagnostics: emitResult.Diagnostics);
            }
            else
            {
                var originalBytes = File.ReadAllBytes(assemblyInfo.FilePath);
                var rebuildBytes = rebuildPeStream.ToArray();
                if (originalBytes.SequenceEqual(rebuildBytes))
                {
                    return new CompilationDiff(assemblyInfo, RebuildResult.Success);
                }
                else
                {
                    return new CompilationDiff(
                        assemblyInfo,
                        RebuildResult.BinaryDifference,
                        originalPortableExecutableBytes: originalBytes,
                        originalPdbReader: compilationFactory.OptionsReader.PdbReader,
                        rebuildPortableExecutableBytes: rebuildBytes,
                        rebuildPdbReader: getRebuildPdbReader(),
                        rebuildCompilation: rebuildCompilation);
                }

                MetadataReader getRebuildPdbReader()
                {
                    if (hasEmbeddedPdb)
                    {
                        var peReader = new PEReader(rebuildBytes.ToImmutableArray());
                        return peReader.GetEmbeddedPdbMetadataReader() ?? throw ExceptionUtilities.Unreachable();
                    }
                    else
                    {
                        rebuildPdbStream!.Position = 0;
                        return MetadataReaderProvider.FromPortablePdbStream(rebuildPdbStream).GetMetadataReader();
                    }
                }
            }
        }

        private void EnsureRebuildResult(RebuildResult result)
        {
            if (Result != result)
            {
                throw new InvalidOperationException();
            }
        }

        public unsafe void WriteArtifacts(string debugPath, ILogger logger)
        {
            if (Result == RebuildResult.Success)
            {
                return;
            }

            Directory.CreateDirectory(debugPath);
            switch (Result)
            {
                case RebuildResult.BinaryDifference:
                    writeBinaryDiffArtifacts();
                    break;
                case RebuildResult.CompilationError:
                    writeDiagnostics(Diagnostics);
                    break;
                case RebuildResult.MissingReferences:
                    writeMissingReferences();
                    break;
                case RebuildResult.MiscError:
                    File.WriteAllText(Path.Combine(debugPath, "error.txt"), MiscErrorMessage);
                    break;
                default:
                    throw new Exception($"Unexpected value {Result}");
            }

            void writeDiagnostics(ImmutableArray<Diagnostic> diagnostics)
            {
                using var writer = new StreamWriter(Path.Combine(debugPath, "diagnostics.txt"), append: false);
                foreach (var diagnostic in diagnostics)
                {
                    writer.WriteLine(diagnostic);
                }
            }

            void writeMissingReferences()
            {
                Debug.Assert(_localReferenceResolver is object);
                using var writer = new StreamWriter(Path.Combine(debugPath, "references.txt"), append: false);
                foreach (var info in _references)
                {
                    if (_localReferenceResolver.TryGetCachedAssemblyInfo(info.ModuleVersionId, out var assemblyInfo))
                    {
                        writer.WriteLine($"Found: {info.ModuleVersionId} {info.FileName} at {assemblyInfo.FilePath}");
                    }
                    else
                    {
                        writer.WriteLine($"Missing: {info.ModuleVersionId} {info.FileName}");
                        foreach (var cachedInfo in _localReferenceResolver.GetCachedAssemblyInfos(info.FileName))
                        {
                            writer.WriteLine($"\t{cachedInfo.Mvid} {cachedInfo.FilePath}");
                        }
                    }
                }
            }

            void writeBinaryDiffArtifacts()
            {
                Debug.Assert(Result == RebuildResult.BinaryDifference);
                Debug.Assert(_originalPortableExecutableBytes is object);
                Debug.Assert(_originalPdbReader is object);
                Debug.Assert(_rebuildPortableExecutableBytes is object);
                Debug.Assert(_rebuildPdbReader is object);
                Debug.Assert(_rebuildCompilation is object);

                var originalPeReader = new PEReader(_originalPortableExecutableBytes.ToImmutableArray());
                var rebuildPeReader = new PEReader(_rebuildPortableExecutableBytes.ToImmutableArray());
                var originalInfo = new BuildInfo(
                    AssemblyBytes: _originalPortableExecutableBytes,
                    AssemblyReader: originalPeReader,
                    PdbMetadataReader: _originalPdbReader);

                var rebuildInfo = new BuildInfo(
                    AssemblyBytes: _rebuildPortableExecutableBytes,
                    AssemblyReader: rebuildPeReader,
                    PdbMetadataReader: _rebuildPdbReader);

                createDiffArtifacts(debugPath, AssemblyInfo.FileName, originalInfo, rebuildInfo, _rebuildCompilation);
                SearchForKnownIssues(logger, originalInfo, rebuildInfo);
            }

            static void createDiffArtifacts(string debugPath, string assemblyFileName, BuildInfo originalInfo, BuildInfo rebuildInfo, Compilation compilation)
            {
                var originalDataFiles = createBuildArtifacts(Path.Combine(debugPath, "original"), assemblyFileName, originalInfo);
                var rebuildDataFiles = createBuildArtifacts(Path.Combine(debugPath, "rebuild"), assemblyFileName, rebuildInfo);

                createDiffScript("compare-pe.mdv.ps1", originalDataFiles.AssemblyMdvFilePath, rebuildDataFiles.AssemblyMdvFilePath);
                createDiffScript("compare-pdb.mdv.ps1", originalDataFiles.PdbMdvFilePath, rebuildDataFiles.PdbMdvFilePath);
                createDiffScript("compare-pdb.xml.ps1", originalDataFiles.PdbXmlFilePath, rebuildDataFiles.PdbXmlFilePath);
                createDiffScript("compare-il.ps1", originalDataFiles.ILFilePath, rebuildDataFiles.ILFilePath);

                void createDiffScript(string scriptName, string originalFilePath, string rebuildFilePath)
                {
                    originalFilePath = getRelativePath(originalFilePath);
                    rebuildFilePath = getRelativePath(rebuildFilePath);

                    File.WriteAllText(Path.Combine(debugPath, scriptName), $@"code --diff (Join-Path $PSScriptRoot ""{originalFilePath}"") (Join-Path $PSScriptRoot ""{rebuildFilePath}"")");
                    string getRelativePath(string dataFilePath) => dataFilePath.Substring(debugPath.Length);
                }

                var sourcesPath = Path.Combine(debugPath, "sources");
                Directory.CreateDirectory(sourcesPath);

                // TODO: output source files should include the entire relative path instead of just the file name.
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var sourceFilePath = Path.Combine(sourcesPath, Path.GetFileName(tree.FilePath));
                    using var file = File.OpenWrite(sourceFilePath);
                    var writer = new StreamWriter(file);
                    tree.GetText().Write(writer);
                    writer.Flush();
                }
            }

            static BuildDataFiles createBuildArtifacts(string outputPath, string assemblyFileName, BuildInfo buildInfo)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyFileName);
                var assemblyFilePath = Path.Combine(outputPath, assemblyFileName);
                var buildDataFiles = new BuildDataFiles(
                    AssemblyMdvFilePath: Path.Combine(outputPath, assemblyName + ".mdv"),
                    PdbMdvFilePath: Path.Combine(outputPath, assemblyName + ".pdb.mdv"),
                    ILFilePath: Path.Combine(outputPath, assemblyName + ".il"),
                    PdbXmlFilePath: Path.Combine(outputPath, assemblyName + ".pdb.xml"),
                    CustomDataFilePath: Path.Combine(outputPath, "custom-data.txt"));

                Directory.CreateDirectory(outputPath);
                File.WriteAllBytes(assemblyFilePath, buildInfo.AssemblyBytes);

                // This is deliberately named .extracted.pdb instead of .pdb. A number of tools will look
                // for a PDB with the name assemblyName.pdb. Want to make explicitly sure that does not 
                // happen and such tools always correctly fall back to the embedded PDB. 
                var pdbFilePath = Path.Combine(outputPath, assemblyName + ".extracted.pdb");
                writeAllBytes(pdbFilePath, new Span<byte>(buildInfo.PdbMetadataReader.MetadataPointer, buildInfo.PdbMetadataReader.MetadataLength));

                createMetadataVisualization(buildDataFiles.AssemblyMdvFilePath, buildInfo.AssemblyMetadataReader);
                createMetadataVisualization(buildDataFiles.PdbMdvFilePath, buildInfo.PdbMetadataReader);
                createDataFile(buildDataFiles.CustomDataFilePath, buildInfo.AssemblyReader, buildInfo.PdbMetadataReader);

                var pdbToXmlOptions = PdbToXmlOptions.ResolveTokens
                    | PdbToXmlOptions.ThrowOnError
                    | PdbToXmlOptions.ExcludeScopes
                    | PdbToXmlOptions.IncludeSourceServerInformation
                    | PdbToXmlOptions.IncludeEmbeddedSources
                    | PdbToXmlOptions.IncludeTokens
                    | PdbToXmlOptions.IncludeMethodSpans;

                using var pdbXmlStream = File.Create(buildDataFiles.PdbXmlFilePath);
                PdbToXmlConverter.ToXml(
                    new StreamWriter(pdbXmlStream),
                    pdbStream: new UnmanagedMemoryStream(buildInfo.PdbMetadataReader.MetadataPointer, buildInfo.PdbMetadataReader.MetadataLength),
                    peStream: new MemoryStream(buildInfo.AssemblyBytes),
                    options: pdbToXmlOptions,
                    methodName: null);

                Process.Start(new ProcessStartInfo
                {
                    FileName = IldasmUtilities.IldasmPath,
                    Arguments = $@"{assemblyFilePath} /all /out={buildDataFiles.ILFilePath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!.WaitForExit();

                return buildDataFiles;
            }

            static void writeAllBytes(string filePath, Span<byte> span)
            {
                using var tempFile = File.OpenWrite(filePath);
                tempFile.Write(span);
            }

            static void createMetadataVisualization(string outputFilePath, MetadataReader metadataReader)
            {
                using var writer = new StreamWriter(outputFilePath, append: false);
                var visualizer = new MetadataVisualizer(metadataReader, writer);
                visualizer.Visualize();
                writer.Flush();
            }

            // Used to write any data that could be interesting for debugging purposes
            static void createDataFile(string outputFilePath, PEReader peReader, MetadataReader pdbMetadataReader)
            {
                using var writer = new StreamWriter(outputFilePath, append: false);
                var peMetadataReader = peReader.GetMetadataReader();

                writeDebugDirectory();
                writeEmbeddedFileInfo();

                void writeDebugDirectory()
                {
                    writer.WriteLine("Debug Directory");
                    foreach (var debugDirectory in peReader.ReadDebugDirectory())
                    {
                        writer.WriteLine($"\ttype:{debugDirectory.Type} dataSize:{debugDirectory.DataSize} dataPointer:{debugDirectory.DataPointer} dataRelativeVirtualAddress:{debugDirectory.DataRelativeVirtualAddress}");
                    }
                }

                void writeEmbeddedFileInfo()
                {
                    writer.WriteLine("Embedded File Info");
                    var optionsReader = new CompilationOptionsReader(EmptyLogger.Instance, pdbMetadataReader, peReader);
                    foreach (var info in optionsReader.GetEmbeddedSourceTextInfo())
                    {
                        if (!info.CompressedHash.IsDefaultOrEmpty)
                        {
                            var hashString = BitConverter.ToString(info.CompressedHash.ToArray()).Replace("-", "");
                            writer.WriteLine($@"\t""{Path.GetFileName(info.SourceTextInfo.OriginalSourceFilePath)}"" - {hashString}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given two builds which are not identical this will look for known issues that could be 
        /// causing the difference.
        /// </summary>
        private static unsafe bool SearchForKnownIssues(ILogger logger, BuildInfo originalInfo, BuildInfo rebuildInfo)
        {
            return hasPdbCompressionDifferences();

            bool hasPdbCompressionDifferences()
            {
                var originalEntry = originalInfo.AssemblyReader.ReadDebugDirectory().SingleOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                var rebuildEntry = rebuildInfo.AssemblyReader.ReadDebugDirectory().SingleOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                if (originalEntry.Type == DebugDirectoryEntryType.Unknown && rebuildEntry.Type == DebugDirectoryEntryType.Unknown)
                {
                    return false;
                }

                var originalMissingEmbeddedPdb = originalEntry.Type == DebugDirectoryEntryType.Unknown;
                if (originalMissingEmbeddedPdb || rebuildEntry.Type == DebugDirectoryEntryType.Unknown)
                {
                    var (hasEmbedded, doesntHaveEmbedded) = originalMissingEmbeddedPdb ? ("rebuild", "original") : ("original", "rebuild");
                    logger.LogError($"Known issue: {hasEmbedded} has an embedded PDB but {doesntHaveEmbedded} does not have an embedded PDB");
                    return true;
                }

                if (originalEntry.DataSize != rebuildEntry.DataSize)
                {
                    var originalPdbSpan = new Span<byte>(originalInfo.PdbMetadataReader.MetadataPointer, originalInfo.PdbMetadataReader.MetadataLength);
                    var rebuildPdbSpan = new Span<byte>(rebuildInfo.PdbMetadataReader.MetadataPointer, rebuildInfo.PdbMetadataReader.MetadataLength);
                    if (originalPdbSpan.SequenceEqual(rebuildPdbSpan))
                    {
                        logger.LogError($"Known issue: different compression used for embedded portable pdb debug directory entry");
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DiaSymReader.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Metadata.Tools;

namespace BuildValidator
{
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

        public bool? AreEqual { get; }
        public string OriginalPath { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        private CompilationDiff(string originalPath, bool? areEqual)
        {
            AreEqual = areEqual;
            OriginalPath = originalPath;
        }

        private CompilationDiff(ImmutableArray<Diagnostic> diagnostics, string originalPath)
        {
            Diagnostics = diagnostics;
            OriginalPath = originalPath;
        }

        public static CompilationDiff CreatePlaceholder(FileInfo originalBinaryPath, bool isError)
        {
            return new CompilationDiff(originalBinaryPath.FullName, areEqual: isError ? false : null);
        }

        public static unsafe CompilationDiff Create(
            FileInfo originalBinaryPath,
            CompilationOptionsReader optionsReader,
            Compilation producedCompilation,
            IMethodSymbol? debugEntryPoint,
            ILogger logger,
            Options options)
        {
            using var rebuildPeStream = new MemoryStream();

            var peHeader = optionsReader.PeReader.PEHeaders.PEHeader!;
            var win32Resources = optionsReader.PeReader.GetSectionData(peHeader.ResourceTableDirectory.RelativeVirtualAddress);
            using var win32ResourceStream = new UnmanagedMemoryStream(win32Resources.Pointer, win32Resources.Length);

            var sourceLink = optionsReader.GetSourceLinkUTF8();

            var embeddedTexts = producedCompilation.SyntaxTrees
                    .Select(st => (path: st.FilePath, text: st.GetText()))
                    .Where(pair => pair.text.CanBeEmbedded)
                    .Select(pair => EmbeddedText.FromSource(pair.path, pair.text))
                    .ToImmutableArray();

            var emitResult = producedCompilation.Emit(
                peStream: rebuildPeStream,
                pdbStream: null,
                xmlDocumentationStream: null,
                win32Resources: win32ResourceStream,
                useRawWin32Resources: true,
                manifestResources: optionsReader.GetManifestResources(),
                options: new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.Embedded,
                    highEntropyVirtualAddressSpace: (peHeader.DllCharacteristics & DllCharacteristics.HighEntropyVirtualAddressSpace) != 0,
                    subsystemVersion: SubsystemVersion.Create(peHeader.MajorSubsystemVersion, peHeader.MinorSubsystemVersion)),
                debugEntryPoint: debugEntryPoint,
                metadataPEStream: null,
                pdbOptionsBlobReader: optionsReader.GetMetadataCompilationOptionsBlobReader(),
                sourceLinkStream: sourceLink != null ? new MemoryStream(sourceLink) : null,
                embeddedTexts: embeddedTexts,
                cancellationToken: CancellationToken.None);

            if (!emitResult.Success)
            {
                using var diagsScope = logger.BeginScope($"Diagnostics");
                foreach (var diag in emitResult.Diagnostics)
                {
                    logger.LogError(diag.ToString());
                }

                return new CompilationDiff(emitResult.Diagnostics, originalBinaryPath.FullName);
            }
            else
            {
                var originalBytes = File.ReadAllBytes(originalBinaryPath.FullName);
                var rebuildBytes = rebuildPeStream.ToArray();

                var bytesEqual = originalBytes.SequenceEqual(rebuildBytes);
                if (!bytesEqual)
                {
                    logger.LogError($"Rebuild of {originalBinaryPath.Name} was not equivalent to the original.");
                    if (!options.Debug)
                    {
                        logger.LogInformation("Pass the --debug argument and re-run to write the visualization of the original and rebuild to disk.");
                    }
                    else
                    {
                        logger.LogInformation("Creating a diff...");

                        var debugPath = options.DebugPath;
                        logger.LogInformation($@"Writing diffs to ""{Path.GetFullPath(debugPath)}""");

                        fixed (byte* ptr = rebuildBytes)
                        {
                            using var rebuildPeReader = new PEReader(ptr, rebuildBytes.Length);
                            var originalInfo = new BuildInfo(
                                AssemblyBytes: originalBytes,
                                AssemblyReader: optionsReader.PeReader,
                                PdbMetadataReader: optionsReader.PdbReader);

                            var rebuildInfo = new BuildInfo(
                                AssemblyBytes: rebuildBytes,
                                AssemblyReader: rebuildPeReader,
                                PdbMetadataReader: rebuildPeReader.GetEmbeddedPdbMetadataReader());

                            createDiffArtifacts(debugPath, originalBinaryPath.Name, originalInfo, rebuildInfo, producedCompilation);
                            SearchForKnownIssues(logger, originalInfo, rebuildInfo);
                        }
                    }
                }

                return new CompilationDiff(originalBinaryPath.FullName, bytesEqual);
            }

            static void createDiffArtifacts(string debugPath, string assemblyFileName, BuildInfo originalInfo, BuildInfo rebuildInfo, Compilation compilation)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyFileName);
                var assemblyDebugPath = Path.Combine(debugPath, assemblyName);
                Directory.CreateDirectory(assemblyDebugPath);

                var originalDataFiles = createBuildArtifacts(Path.Combine(assemblyDebugPath, "original"), assemblyFileName, originalInfo);
                var rebuildDataFiles = createBuildArtifacts(Path.Combine(assemblyDebugPath, "rebuild"), assemblyFileName, rebuildInfo);

                createDiffScript("compare-pe.mdv.ps1", originalDataFiles.AssemblyMdvFilePath, rebuildDataFiles.AssemblyMdvFilePath);
                createDiffScript("compare-pdb.mdv.ps1", originalDataFiles.PdbMdvFilePath, rebuildDataFiles.PdbMdvFilePath);
                createDiffScript("compare-pdb.xml.ps1", originalDataFiles.PdbXmlFilePath, rebuildDataFiles.PdbXmlFilePath);
                createDiffScript("compare-il.ps1", originalDataFiles.ILFilePath, rebuildDataFiles.ILFilePath);

                void createDiffScript(string scriptName, string originalFilePath, string rebuildFilePath)
                {
                    originalFilePath = getRelativePath(originalFilePath);
                    rebuildFilePath = getRelativePath(rebuildFilePath);

                    File.WriteAllText(Path.Combine(assemblyDebugPath, scriptName), $@"code --diff (Join-Path $PSScriptRoot ""{originalFilePath}"") (Join-Path $PSScriptRoot ""{rebuildFilePath}"")");
                    string getRelativePath(string dataFilePath) => dataFilePath.Substring(assemblyDebugPath.Length);
                }

                var sourcesPath = Path.Combine(assemblyDebugPath, "sources");
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
                }).WaitForExit();

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
                    var sourceFileInfos = optionsReader.GetSourceFileInfos(optionsReader.GetEncoding());
                    foreach (var info in sourceFileInfos)
                    {
                        if (info.EmbeddedCompressedHash is { } hash)
                        {
                            var hashString = BitConverter.ToString(hash).Replace("-", "");
                            writer.WriteLine($@"\t""{Path.GetFileName(info.SourceFilePath)}"" - {hashString}");
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
                var originalEntry = originalInfo.AssemblyReader.ReadDebugDirectory().Single(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                var rebuildEntry = rebuildInfo.AssemblyReader.ReadDebugDirectory().Single(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
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

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

        private CompilationDiff(
            string originalPath,
            bool? areEqual)
        {
            AreEqual = areEqual;
            OriginalPath = originalPath;
        }

        private CompilationDiff(ImmutableArray<Diagnostic> diagnostics, string originalPath)
        {
            Diagnostics = diagnostics;
            OriginalPath = originalPath;
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

            // By default the Roslyn command line adds a resource that we need to replicate here.
            using var win32ResourceStream = producedCompilation.CreateDefaultWin32Resources(
                versionResource: true,
                noManifest: producedCompilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary,
                manifestContents: null,
                iconInIcoFormat: null);

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
                manifestResources: optionsReader.GetManifestResources(),
                options: new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.Embedded, highEntropyVirtualAddressSpace: true),
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

                            writeDiffInfo(debugPath, originalBinaryPath.Name, originalInfo, rebuildInfo, producedCompilation);
                        }
                    }
                }

                return new CompilationDiff(originalBinaryPath.FullName, bytesEqual);
            }

            static void writeDiffInfo(string debugPath, string assemblyFileName, BuildInfo originalInfo, BuildInfo rebuildInfo, Compilation compilation)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyFileName);
                var assemblyDebugPath = Path.Combine(debugPath, assemblyName);
                Directory.CreateDirectory(assemblyDebugPath);

                var originalDataFiles = writeBuildInfo(Path.Combine(assemblyDebugPath, "original"), assemblyFileName, originalInfo);
                var rebuildDataFiles = writeBuildInfo(Path.Combine(assemblyDebugPath, "rebuild"), assemblyFileName, rebuildInfo);

                writeDiffScript("compare-pe.mdv.ps1", originalDataFiles.AssemblyMdvFilePath, rebuildDataFiles.AssemblyMdvFilePath);
                writeDiffScript("compare-pdb.mdv.ps1", originalDataFiles.PdbMdvFilePath, rebuildDataFiles.PdbMdvFilePath);
                writeDiffScript("compare-pdb.xml.ps1", originalDataFiles.PdbXmlFilePath, rebuildDataFiles.PdbXmlFilePath);
                writeDiffScript("compare-il.ps1", originalDataFiles.ILFilePath, rebuildDataFiles.ILFilePath);

                void writeDiffScript(string scriptName, string originalFilePath, string rebuildFilePath)
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

            static BuildDataFiles writeBuildInfo(string outputPath, string assemblyFileName, BuildInfo buildInfo)
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

                writeMetadataVisualization(buildDataFiles.AssemblyMdvFilePath, buildInfo.AssemblyMetadataReader);
                writeMetadataVisualization(buildDataFiles.PdbMdvFilePath, buildInfo.PdbMetadataReader);
                writeDataFile(buildDataFiles.CustomDataFilePath, buildInfo.AssemblyReader, buildInfo.PdbMetadataReader);

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

                Process.Start(@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe", $@"{assemblyFilePath} /all /out={buildDataFiles.ILFilePath}").WaitForExit();

                return buildDataFiles;
            }

            static unsafe void writeAllBytes(string filePath, Span<byte> span)
            {
                using var tempFile = File.OpenWrite(filePath);
                tempFile.Write(span);
            }

            static void writeMetadataVisualization(string outputFilePath, MetadataReader metadataReader)
            {
                using var tempFile = File.OpenWrite(outputFilePath);
                var writer = new StreamWriter(tempFile);
                var visualizer = new MetadataVisualizer(metadataReader, writer);
                visualizer.Visualize();
                writer.Flush();
            }

            // Used to write any data that could be interesting for debugging purposes
            static void writeDataFile(string assemblyFilePath, PEReader peReader, MetadataReader pdbMetadataReader)
            {
                using var fileStream = File.OpenWrite(assemblyFilePath + ".data.txt");
                var writer = new StreamWriter(fileStream);
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
                            writer.WriteLine($@"\t""{info.SourceFilePath}"" - {hashString}");
                        }
                    }
                }
            }
        }
    }
}

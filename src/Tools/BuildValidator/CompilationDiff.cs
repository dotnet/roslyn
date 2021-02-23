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
    internal class CompilationDiff
    {
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

        public static CompilationDiff CreatePlaceholder(FileInfo originalBinaryPath)
        {
            return new CompilationDiff(originalBinaryPath.FullName, areEqual: null);
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

                        var assemblyName = Path.GetFileNameWithoutExtension(originalBinaryPath.Name);
                        var assemblyDebugPath = Path.Combine(debugPath, assemblyName);

                        var originalPath = Path.Combine(assemblyDebugPath, "original");
                        var rebuildPath = Path.Combine(assemblyDebugPath, "rebuild");
                        var sourcesPath = Path.Combine(assemblyDebugPath, "sources");

                        Directory.CreateDirectory(originalPath);
                        Directory.CreateDirectory(rebuildPath);
                        Directory.CreateDirectory(sourcesPath);

                        // TODO: output source files should include the entire relative path instead of just the file name.
                        foreach (var tree in producedCompilation.SyntaxTrees)
                        {
                            var sourceFilePath = Path.Combine(sourcesPath, Path.GetFileName(tree.FilePath));
                            using var file = File.OpenWrite(sourceFilePath);
                            var writer = new StreamWriter(file);
                            tree.GetText().Write(writer);
                            writer.Flush();
                        }

                        var originalAssemblyPath = Path.Combine(originalPath, originalBinaryPath.Name);
                        File.WriteAllBytes(originalAssemblyPath, originalBytes);

                        var rebuildAssemblyPath = Path.Combine(rebuildPath, originalBinaryPath.Name);
                        File.WriteAllBytes(rebuildAssemblyPath, rebuildBytes);

                        var originalPeMdvPath = Path.Combine(originalPath, assemblyName + ".pe.mdv");
                        var originalPdbMdvPath = Path.Combine(originalPath, assemblyName + ".pdb.mdv");
                        writeVisualization(originalPeMdvPath, optionsReader.PeReader.GetMetadataReader());
                        writeVisualization(originalPdbMdvPath, optionsReader.PdbReader);

                        var originalPdbXmlPath = Path.Combine(originalPath, assemblyName + ".pdb.xml");
                        using var originalPdbXml = File.Create(originalPdbXmlPath);

                        var rebuildPdbXmlPath = Path.Combine(rebuildPath, assemblyName + ".pdb.xml");

                        var pdbToXmlOptions = PdbToXmlOptions.ResolveTokens
                            | PdbToXmlOptions.ThrowOnError
                            | PdbToXmlOptions.ExcludeScopes
                            | PdbToXmlOptions.IncludeSourceServerInformation
                            | PdbToXmlOptions.IncludeEmbeddedSources
                            | PdbToXmlOptions.IncludeTokens
                            | PdbToXmlOptions.IncludeMethodSpans;

                        PdbToXmlConverter.ToXml(
                            new StreamWriter(originalPdbXml),
                            pdbStream: new UnmanagedMemoryStream(optionsReader.PdbReader.MetadataPointer, optionsReader.PdbReader.MetadataLength),
                            peStream: new MemoryStream(originalBytes),
                            options: pdbToXmlOptions,
                            methodName: null);

                        var rebuildPeMdvPath = Path.Combine(rebuildPath, assemblyName + ".pe.mdv");
                        var rebuildPdbMdvPath = Path.Combine(rebuildPath, assemblyName + ".pdb.mdv");
                        fixed (byte* ptr = rebuildBytes)
                        {
                            using var rebuildPeReader = new PEReader(ptr, rebuildBytes.Length);
                            writeVisualization(rebuildPeMdvPath, rebuildPeReader.GetMetadataReader());

                            if (rebuildPeReader.TryOpenAssociatedPortablePdb(
                                rebuildAssemblyPath,
                                path => File.Exists(path) ? File.OpenRead(path) : null,
                                out var provider,
                                out _) && provider is { })
                            {
                                var rebuildPdbReader = provider.GetMetadataReader(MetadataReaderOptions.Default);
                                writeVisualization(rebuildPdbMdvPath, rebuildPdbReader);

                                using var rebuildPdbXml = File.Create(rebuildPdbXmlPath);
                                PdbToXmlConverter.ToXml(
                                    new StreamWriter(rebuildPdbXml),
                                    pdbStream: new UnmanagedMemoryStream(rebuildPdbReader.MetadataPointer, rebuildPdbReader.MetadataLength),
                                    peStream: new MemoryStream(rebuildBytes),
                                    options: pdbToXmlOptions,
                                    methodName: null);

                                using (logger.BeginScope("Rebuild Embedded Texts raw SHAs"))
                                {
                                    var rebuildReader = new CompilationOptionsReader(logger, rebuildPdbReader, rebuildPeReader);
                                    var rebuildSourceFileInfos = rebuildReader.GetSourceFileInfos(rebuildReader.GetEncoding());
                                    foreach (var info in rebuildSourceFileInfos)
                                    {
                                        if (info.EmbeddedCompressedHash is { } hash)
                                        {
                                            var hashString = BitConverter.ToString(hash).Replace("-", "");
                                            logger.LogInformation($@"""{info.SourceFilePath}"" - {hashString}");
                                        }
                                    }
                                }
                            }
                        }

                        var ildasmOriginalOutputPath = Path.Combine(originalPath, assemblyName + ".il");
                        var ildasmRebuildOutputPath = Path.Combine(rebuildPath, assemblyName + ".il");

                        // TODO: can we bundle ildasm in with the utility?
                        Process.Start(@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe", $@"{originalBinaryPath.FullName} /all /out={ildasmOriginalOutputPath}").WaitForExit();
                        Process.Start(@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe", $@"{rebuildAssemblyPath} /all /out={ildasmRebuildOutputPath}").WaitForExit();

                        File.WriteAllText(Path.Combine(assemblyDebugPath, "compare-pe.mdv.ps1"), $@"code --diff (Join-Path $PSScriptRoot ""{originalPeMdvPath.Substring(assemblyDebugPath.Length)}"") (Join-Path $PSScriptRoot ""{rebuildPeMdvPath.Substring(assemblyDebugPath.Length)}"")");
                        File.WriteAllText(Path.Combine(assemblyDebugPath, "compare-pdb.mdv.ps1"), $@"code --diff (Join-Path $PSScriptRoot ""{originalPdbMdvPath.Substring(assemblyDebugPath.Length)}"") (Join-Path $PSScriptRoot ""{rebuildPdbMdvPath.Substring(assemblyDebugPath.Length)}"")");
                        File.WriteAllText(Path.Combine(assemblyDebugPath, "compare-pdb.xml.ps1"), $@"code --diff (Join-Path $PSScriptRoot ""{originalPdbXmlPath.Substring(assemblyDebugPath.Length)}"") (Join-Path $PSScriptRoot ""{rebuildPdbXmlPath.Substring(assemblyDebugPath.Length)}"")");
                        File.WriteAllText(Path.Combine(assemblyDebugPath, "compare-il.ps1"), $@"code --diff (Join-Path $PSScriptRoot ""{ildasmOriginalOutputPath.Substring(assemblyDebugPath.Length)}"") (Join-Path $PSScriptRoot ""{ildasmRebuildOutputPath.Substring(assemblyDebugPath.Length)}"")");
                    }
                }

                return new CompilationDiff(originalBinaryPath.FullName, bytesEqual);
            }

            void writeVisualization(string outPath, MetadataReader pdbReader)
            {
                using (var tempFile = File.OpenWrite(outPath))
                {
                    var writer = new StreamWriter(tempFile);
                    var visualizer = new MetadataVisualizer(pdbReader, writer);
                    visualizer.Visualize();
                    writer.Flush();
                }
            }
        }
    }
}

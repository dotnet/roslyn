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
using Microsoft.Metadata.Tools;

namespace BuildValidator
{
    internal class CompilationDiff
    {
        public bool? AreEqual { get; }
        public string OriginalPath { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public Exception? Exception { get; }

        private CompilationDiff(
            string originalPath,
            bool? areEqual)
        {
            AreEqual = areEqual;
            OriginalPath = originalPath;
        }

        private CompilationDiff(
            string originalPath,
            Exception exception)
        {
            OriginalPath = originalPath;
            Exception = exception;
        }

        private CompilationDiff(ImmutableArray<Diagnostic> diagnostics, string originalPath)
        {
            Diagnostics = diagnostics;
            OriginalPath = originalPath;
        }

        public static unsafe CompilationDiff Create(FileInfo originalBinaryPath, PEReader originalPeReader, MetadataReader originalPdbReader, Compilation producedCompilation, IMethodSymbol? debugEntryPoint)
        {
            using var rebuildPeStream = new MemoryStream();

            using var win32ResourceStream = producedCompilation.CreateDefaultWin32Resources(
                versionResource: true,
                noManifest: producedCompilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary,
                manifestContents: null,
                iconInIcoFormat: null);

            // TODO: clean up usages of options reader.
            // TODO: probably extract these emit bits into "BuildConstructor".
            var sourceLink = new CompilationOptionsReader(originalPdbReader, originalPeReader).GetSourceLinkUTF8();
            var emitResult = producedCompilation.Emit(
                peStream: rebuildPeStream,
                pdbStream: null,
                xmlDocumentationStream: null,
                win32Resources: win32ResourceStream,
                manifestResources: new CompilationOptionsReader(originalPdbReader, originalPeReader).GetManifestResources(),
                options: new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.Embedded, highEntropyVirtualAddressSpace: true),
                debugEntryPoint: debugEntryPoint,
                metadataPEStream: null,
                pdbOptionsBlobReader: new CompilationOptionsReader(originalPdbReader, originalPeReader).GetMetadataCompilationOptionsBlobReader(),
                sourceLinkStream: sourceLink != null ? new MemoryStream(sourceLink) : null,
                embeddedTexts: producedCompilation.SyntaxTrees
                    .Select(st => (path: st.FilePath, text: st.GetText()))
                    .Where(pair => pair.text.CanBeEmbedded)
                    .Select(pair => EmbeddedText.FromSource(pair.path, pair.text)),
                cancellationToken: CancellationToken.None);

            if (emitResult.Success)
            {
                var originalBytes = File.ReadAllBytes(originalBinaryPath.FullName);

                var rebuildBytes = rebuildPeStream.ToArray();
                var rebuildOutputPath = Path.GetTempFileName();
                File.WriteAllBytes(rebuildOutputPath, rebuildBytes);

                var bytesEqual = originalBytes.SequenceEqual(rebuildBytes);
                if (!bytesEqual)
                {
                    // TODO: how do we select which tool to use for validation, since they both appear to be needed in different scenarios.
                    var useMdv = true;
                    if (useMdv)
                    {
                        var originalTempPath = writeVisualizationToTempFile(originalPeReader, originalPdbReader);
                        string rebuildTempPath;
                        fixed (byte* ptr = rebuildBytes)
                        {
                            var rebuildPeReader = new PEReader(ptr, rebuildBytes.Length);
                            MetadataReader? rebuildPdbReader = null;
                            if (rebuildPeReader.TryOpenAssociatedPortablePdb(
                                rebuildOutputPath,
                                path => File.Exists(path) ? File.OpenRead(path) : null,
                                out var provider,
                                out _) && provider is { })
                            {
                                rebuildPdbReader = provider.GetMetadataReader(MetadataReaderOptions.Default);
                            }
                            rebuildTempPath = writeVisualizationToTempFile(rebuildPeReader, rebuildPdbReader);
                        }

                        Console.WriteLine("The rebuild was not equivalent to the original. Opening a diff...");
                        Process.Start(@"C:\Program Files\Microsoft VS Code\bin\code.cmd", $@"--diff ""{originalTempPath}"" ""{rebuildTempPath}""");
                    }

                    var ildasmOriginalOutputPath = Path.GetTempFileName();
                    var ildasmRebuildOutputPath = Path.GetTempFileName();

                    // TODO: can we bundle ildasm in with the utility?
                    Process.Start(@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe", $@"{originalBinaryPath.FullName} /out={ildasmOriginalOutputPath}").WaitForExit();
                    Process.Start(@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe", $@"{rebuildOutputPath} /out={ildasmRebuildOutputPath}").WaitForExit();
                    Process.Start(@"C:\Program Files\Microsoft VS Code\bin\code.cmd", $@"--diff ""{ildasmOriginalOutputPath}"" ""{ildasmRebuildOutputPath}""");
                }

                return new CompilationDiff(originalBinaryPath.FullName, bytesEqual);
            }
            else
            {
                return new CompilationDiff(emitResult.Diagnostics, originalBinaryPath.FullName);
            }

            string writeVisualizationToTempFile(PEReader peReader, MetadataReader? pdbReader)
            {
                var tempPath = Path.GetTempFileName();
                using (var tempFile = File.OpenWrite(tempPath))
                {
                    var writer = new StreamWriter(tempFile);
                    writer.WriteLine("======== PE VISUALIZATION =======");
                    var visualizer = new MetadataVisualizer(peReader.GetMetadataReader(), writer);
                    visualizer.VisualizeHeaders();

                    if (pdbReader is object)
                    {
                        writer.WriteLine("======== PDB VISUALIZATION =======");
                        var pdbVisualizer = new MetadataVisualizer(pdbReader, writer);
                        pdbVisualizer.Visualize();
                    }
                }

                return tempPath;
            }
        }

        public static CompilationDiff Create(FileInfo assemblyFile, Exception exception)
        {
            return new CompilationDiff(originalPath: assemblyFile.FullName, exception);
        }
    }
}

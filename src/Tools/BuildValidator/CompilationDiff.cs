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
using Microsoft.CodeAnalysis;
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

            var sourceLink = new CompilationOptionsReader(originalPdbReader, originalPeReader).GetSourceLinkUTF8();
            var emitResult = producedCompilation.Emit(
                peStream: rebuildPeStream,
                win32Resources: win32ResourceStream,
                debugEntryPoint: debugEntryPoint,
                options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded, highEntropyVirtualAddressSpace: true),
                sourceLinkStream: sourceLink != null ? new MemoryStream(sourceLink) : null);
                // TODO: embed only if the original was embedded
                //embeddedTexts: producedCompilation.SyntaxTrees.Select(st => EmbeddedText.FromSource(st.FilePath, st.GetText())));

            var rebuildOutputPath = Path.GetTempFileName();
            using (var rebuildWriter = File.OpenWrite(rebuildOutputPath))
            {
                rebuildPeStream.CopyTo(rebuildWriter);
            }

            if (emitResult.Success)
            {
                var originalBytes = File.ReadAllBytes(originalBinaryPath.FullName);
                var newBytes = rebuildPeStream.ToArray();

                var bytesEqual = originalBytes.SequenceEqual(newBytes);
                if (!bytesEqual)
                {
                    var originalTempPath = writeVisualizationToTempFile(originalPeReader, originalPdbReader);

                    string rebuildTempPath;
                    fixed (byte* ptr = newBytes)
                    {
                        var rebuildPeReader = new PEReader(ptr, newBytes.Length);
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
                    visualizer.Visualize();

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

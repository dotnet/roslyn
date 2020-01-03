// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Provides access to compilation outputs based only on the path of the output asssembly.
    /// If PDB path is known upfront use <see cref="CompilationOutputFiles"/> instead.
    /// </summary>
    internal sealed class CompilationOutputFilesWithImplicitPdbPath : CompilationOutputs
    {
        public string AssemblyFilePath { get; }

        public CompilationOutputFilesWithImplicitPdbPath(string assemblyFilePath = null)
        {
            if (assemblyFilePath != null)
            {
                CompilerPathUtilities.RequireAbsolutePath(assemblyFilePath, nameof(assemblyFilePath));
            }

            AssemblyFilePath = assemblyFilePath;
        }

        public override string AssemblyDisplayPath => AssemblyFilePath;

        // heuristic for error messages (determining the actual path requires opening the assembly):
        public override string PdbDisplayPath => Path.GetFileNameWithoutExtension(AssemblyFilePath) + ".pdb";

        protected override Stream OpenAssemblyStream()
            => TryOpenFileStream(AssemblyFilePath);

        // Not gonna be called since we override OpenPdb.
        protected override Stream OpenPdbStream()
            => throw ExceptionUtilities.Unreachable;

        public override DebugInformationReaderProvider OpenPdb()
        {
            var assemblyStream = OpenAssemblyStream();
            if (assemblyStream == null)
            {
                return null;
            }

            // find associated PDB
            string pdbPath;
            using (var peReader = new PEReader(assemblyStream))
            {
                var debugDirectory = peReader.ReadDebugDirectory();
                var embeddedPdbEntry = debugDirectory.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                if (embeddedPdbEntry.DataSize != 0)
                {
                    return DebugInformationReaderProvider.CreateFromMetadataReader(peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry));
                }

                var codeViewEntry = debugDirectory.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.CodeView);
                if (codeViewEntry.DataSize == 0)
                {
                    return null;
                }

                pdbPath = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry).Path;
            }

            // First try to use the full path as specified in the PDB, then look next to the assembly.
            var pdbStream =
                TryOpenFileStream(pdbPath) ??
                TryOpenFileStream(Path.Combine(Path.GetDirectoryName(AssemblyFilePath), PathUtilities.GetFileName(pdbPath)));

            return (pdbStream != null) ? DebugInformationReaderProvider.CreateFromStream(pdbStream) : null;
        }

        private static Stream TryOpenFileStream(string path)
        {
            if (path == null)
            {
                return null;
            }

            try
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return null;
            }
            catch (Exception e) when (!(e is IOException))
            {
                throw new IOException(e.Message, e);
            }
        }
    }
}

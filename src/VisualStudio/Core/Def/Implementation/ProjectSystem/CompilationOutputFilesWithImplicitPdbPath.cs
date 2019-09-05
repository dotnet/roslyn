// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
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
        public override string PdbDisplayPath => Path.GetFileNameWithoutExtension(AssemblyFilePath) + ".pdb";

        protected override Stream OpenAssemblyStream()
            => AssemblyFilePath != null ? FileUtilities.OpenRead(AssemblyFilePath) : null;

        protected override Stream OpenPdbStream()
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
                var pdbEntry = peReader.ReadDebugDirectory().FirstOrDefault(
                    e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb || e.Type == DebugDirectoryEntryType.CodeView);

                if (pdbEntry.Type != DebugDirectoryEntryType.CodeView)
                {
                    return null;
                }

                pdbPath = peReader.ReadCodeViewDebugDirectoryData(pdbEntry).Path;
            }

            // First try to use the full path as specified in the PDB, then look next to the assembly.
            Stream result;
            try
            {
                result = new FileStream(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                pdbPath = Path.Combine(Path.GetDirectoryName(AssemblyFilePath), PathUtilities.GetFileName(pdbPath));
                result = FileUtilities.OpenRead(pdbPath);
            }

            return result;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class CompilationOutputFiles : CompilationOutputs
    {
        internal static readonly CompilationOutputFiles None = new CompilationOutputFiles();

        public override string AssemblyDisplayPath => AssemblyFilePath;
        public override string PdbDisplayPath => PdbFilePath;

        public string PdbFilePath { get; }
        public string AssemblyFilePath { get; }

        public CompilationOutputFiles(string assemblyFilePath = null, string pdbFilePath = null)
        {
            if (assemblyFilePath != null)
            {
                CompilerPathUtilities.RequireAbsolutePath(assemblyFilePath, nameof(assemblyFilePath));
            }

            if (pdbFilePath != null)
            {
                CompilerPathUtilities.RequireAbsolutePath(pdbFilePath, nameof(pdbFilePath));
            }

            AssemblyFilePath = assemblyFilePath;
            PdbFilePath = pdbFilePath;
        }

        /// <summary>
        /// Opens an assembly file produced by the compiler (corresponds to OutputAssembly build task parameter).
        /// </summary>
        protected override Stream OpenAssemblyStream()
            => AssemblyFilePath != null ? FileUtilities.OpenRead(AssemblyFilePath) : null;

        /// <summary>
        /// Opens a PDB file produced by the compiler.
        /// Returns null if the compiler generated no PDB (the symbols might be embedded in the assembly).
        /// </summary>
        /// <remarks>
        /// The stream must be readable and seekable.
        /// </remarks>
        protected override Stream OpenPdbStream()
            => PdbFilePath != null ? FileUtilities.OpenRead(PdbFilePath) : null;
    }
}

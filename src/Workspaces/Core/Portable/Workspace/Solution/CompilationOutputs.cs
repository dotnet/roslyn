// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis
{
    public abstract class CompilationOutputs
    {
        /// <summary>
        /// Opens an assembly file produced by the compiler (corresponds to OutputAssembly build task parameter).
        /// </summary>
        public abstract Stream OpenOutputAssembly();

        /// <summary>
        /// Opens a reference assembly file produced by the compiler (corresponds to OutputRefAssembly build task parameter).
        /// </summary>
        public abstract Stream OpenOutputRefAssembly();

        /// <summary>
        /// Opens a PDB file produced by the compiler (corresponds to PdbFile build task parameter).
        /// </summary>
        public abstract Stream OpenPdbFile();

        /// <summary>
        /// Opens a documentation file produced by the compiler (corresponds to DocumentationFile build task parameter).
        /// </summary>
        public abstract Stream OpenDocumentationFile();

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
    }
}

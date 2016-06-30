// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    public abstract class SourceGeneratorContext 
    {
        public abstract Compilation Compilation { get; }

        public abstract void ReportDiagnostic(Diagnostic diagnostic);

        /// <summary>
        /// Add the generated source.
        /// </summary>
        /// <param name="name">
        /// Name of the generated source. This name must be unique across
        /// all source generated from this <see cref="SourceGenerator"/> and
        /// <see cref="Compilation"/>. If the host persists the source to disk,
        /// the file will have this name, with a location determined by the host.
        /// (<see cref="SyntaxTree.FilePath"/> is ignored.)
        /// </param>
        /// <param name="tree">Generated source.</param>
        public abstract void AddCompilationUnit(string name, SyntaxTree tree);
    }
}

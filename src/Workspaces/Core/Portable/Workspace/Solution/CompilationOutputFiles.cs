// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class CompilationOutputFiles : CompilationOutputs, IEquatable<CompilationOutputFiles>
    {
        internal static readonly CompilationOutputFiles None = new CompilationOutputFiles();

        public string OutputAssemblyPath { get; private set; }
        public string OutputRefAssemblyPath { get; private set; }
        public string PdbFilePath { get; private set; }
        public string DocumentationFilePath { get; private set; }

        public CompilationOutputFiles(
            string outputAssemblyPath = null,
            string outputRefAssemblyPath = null,
            string pdbFilePath = null,
            string documentationFilePath = null)
        {
            string requireAbsolutePath(string path, string parameterName)
            {
                if (path != null && PathUtilities.IsAbsolute(path))
                {
                    throw new ArgumentException(WorkspacesResources.Specified_path_must_be_absolute, parameterName);
                }

                return path;
            }

            OutputAssemblyPath = requireAbsolutePath(outputAssemblyPath, nameof(outputAssemblyPath));
            OutputRefAssemblyPath = requireAbsolutePath(outputRefAssemblyPath, nameof(outputRefAssemblyPath));
            PdbFilePath = requireAbsolutePath(pdbFilePath, nameof(pdbFilePath));
            DocumentationFilePath = requireAbsolutePath(documentationFilePath, nameof(documentationFilePath));
        }

        private CompilationOutputFiles(CompilationOutputFiles other)
        {
            OutputAssemblyPath = other.OutputAssemblyPath;
            OutputRefAssemblyPath = other.OutputRefAssemblyPath;
            PdbFilePath = other.PdbFilePath;
            DocumentationFilePath = other.DocumentationFilePath;
        }

        public CompilationOutputFiles WithOutputAssemblyPath(string path)
            => (OutputAssemblyPath == path) ? this : new CompilationOutputFiles(this) { OutputAssemblyPath = path };

        public CompilationOutputFiles WithOutputRefAssemblyPath(string path)
            => (OutputRefAssemblyPath == path) ? this : new CompilationOutputFiles(this) { OutputRefAssemblyPath = path };

        public CompilationOutputFiles WithPdbFilePath(string path)
            => (PdbFilePath == path) ? this : new CompilationOutputFiles(this) { PdbFilePath = path };

        public CompilationOutputFiles WithDocumentationFilePath(string path)
            => (DocumentationFilePath == path) ? this : new CompilationOutputFiles(this) { DocumentationFilePath = path };

        private Stream OpenStream(string filePathOpt)
            => (filePathOpt != null) ? File.OpenRead(filePathOpt) : null;

        public override Stream OpenOutputAssembly()
            => OpenStream(OutputAssemblyPath);

        public override Stream OpenOutputRefAssembly()
            => OpenStream(OutputRefAssemblyPath);

        public override Stream OpenPdbFile()
            => OpenStream(PdbFilePath);

        public override Stream OpenDocumentationFile()
            => OpenStream(DocumentationFilePath);

        public override bool Equals(object other)
            => other is CompilationOutputFiles provider && Equals(provider);

        public override int GetHashCode()
            => Hash.Combine(OutputAssemblyPath,
               Hash.Combine(OutputRefAssemblyPath,
               Hash.Combine(PdbFilePath,
               Hash.Combine(DocumentationFilePath, 0))));

        public bool Equals(CompilationOutputFiles other)
            => OutputAssemblyPath == other.OutputAssemblyPath &&
               OutputRefAssemblyPath == other.OutputRefAssemblyPath &&
               PdbFilePath == other.PdbFilePath &&
               DocumentationFilePath == other.DocumentationFilePath;
    }
}

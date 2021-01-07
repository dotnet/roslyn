// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

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

        public static CompilationDiff Create(FileInfo assemblyFile, Compilation producedCompilation)
        {
            using var peStream = new MemoryStream();

            var emitResult = producedCompilation.Emit(peStream: peStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded, pdbChecksumAlgorithm: HashAlgorithmName.SHA256));

            using var peFileStream = File.Create(@"C:\Users\rikki\Desktop\scratch\compare-simple\right\simple-rebuild.dll");
            peStream.WriteTo(peFileStream);

            if (emitResult.Success)
            {
                var originalBytes = File.ReadAllBytes(assemblyFile.FullName);
                var newBytes = peStream.ToArray();

                var bytesEqual = originalBytes.SequenceEqual(newBytes);
                return new CompilationDiff(assemblyFile.FullName, bytesEqual);
            }
            else
            {
                return new CompilationDiff(emitResult.Diagnostics, assemblyFile.FullName);
            }
        }

        public static CompilationDiff Create(FileInfo assemblyFile, Exception exception)
        {
            return new CompilationDiff(originalPath: assemblyFile.FullName, exception);
        }
    }
}

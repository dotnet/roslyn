// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using ILAssembler = Microsoft.DotNet.ILAssembler;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class IlasmUtilities
    {
        public static DisposableFile CreateTempAssembly(string declarations, bool prependDefaultHeader = true, bool tolerateErrors = false)
        {
            IlasmTempAssembly(declarations, prependDefaultHeader, includePdb: false, autoInherit: true, assemblyPath: out var assemblyPath, pdbPath: out var pdbPath, tolerateErrors: tolerateErrors);
            Assert.NotNull(assemblyPath);
            Assert.Null(pdbPath);
            return new DisposableFile(assemblyPath);
        }

        private static string GetArchitecture()
        {
#if NET8_0_OR_GREATER
            return RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
#else
            return "x64";
#endif
        }

        public static readonly string Architecture = GetArchitecture();

        public static void IlasmTempAssembly(string declarations, bool appendDefaultHeader, bool includePdb, bool autoInherit, out string assemblyPath, out string pdbPath, bool tolerateErrors = false)
        {
            if (declarations == null)
                throw new ArgumentNullException(nameof(declarations));

            using (var sourceFile = new DisposableFile(extension: ".il"))
            {
                string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile.Path);

                assemblyPath = Path.Combine(
                    TempRoot.Root,
                    Path.ChangeExtension(Path.GetFileName(sourceFile.Path), "dll"));

                string completeIL;
                if (appendDefaultHeader)
                {
                    const string corLibName = "mscorlib";
                    const string corLibVersion = "4:0:0:0";
                    const string corLibKey = "B7 7A 5C 56 19 34 E0 89";

                    completeIL =
$@".assembly '{sourceFileName}' {{}} 

.assembly extern {corLibName} 
{{
  .publickeytoken = ({corLibKey})
  .ver {corLibVersion}
}} 

{declarations}";
                }
                else
                {
                    completeIL = declarations.Replace("<<GeneratedFileName>>", sourceFileName);
                }

                var options = new ILAssembler.Options
                {
                    Dll = true,
                    NoAutoInherit = !autoInherit,
                    ErrorTolerant = tolerateErrors,
                    OutputFileName = Path.GetFileName(assemblyPath),
                };

                if (includePdb && !MonoHelpers.IsRunningOnMono())
                {
                    pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
                    options.Pdb = true;
                    options.PdbFilePath = pdbPath;
                }
                else
                {
                    pdbPath = null;
                }

                var sourceText = new ILAssembler.SourceText(completeIL, sourceFile.Path);
                var (diagnostics, result) = new ILAssembler.DocumentCompiler().Compile(
                    sourceText,
                    path => new ILAssembler.SourceText(File.ReadAllText(path), path),
                    path => File.Exists(path) ? File.ReadAllBytes(path) : null,
                    options);

                if (result == null || (!tolerateErrors && diagnostics.Any(diagnostic => diagnostic.Severity == ILAssembler.DiagnosticSeverity.Error)))
                {
                    var diagnosticText = string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Location}: {diagnostic.Severity} {diagnostic.Id}: {diagnostic.Message}"));
                    throw new ArgumentException(
                        "The provided IL cannot be compiled." + Environment.NewLine +
                        (diagnosticText.Length > 0 ? diagnosticText : "No compilation result was produced."),
                        nameof(declarations));
                }

                var imageBuilder = new BlobBuilder();
                result.Serialize(imageBuilder);
                using (var stream = File.Create(assemblyPath))
                {
                    imageBuilder.WriteContentTo(stream);
                }

                if (pdbPath != null)
                {
                    if (!result.PortablePdb.HasValue)
                    {
                        throw new InvalidOperationException("The IL assembler did not produce the requested PDB.");
                    }

                    File.WriteAllBytes(pdbPath, result.PortablePdb.Value.ToArray());
                }
            }
        }
    }
}

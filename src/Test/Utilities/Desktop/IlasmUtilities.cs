// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class IlasmUtilities
    {
        public static DisposableFile CreateTempAssembly(string declarations, bool appendDefaultHeader = true)
        {
            string assemblyPath;
            string pdbPath;
            IlasmTempAssembly(declarations, appendDefaultHeader, includePdb: false, assemblyPath: out assemblyPath, pdbPath: out pdbPath);
            Assert.NotNull(assemblyPath);
            Assert.Null(pdbPath);
            return new DisposableFile(assemblyPath);
        }

        public static void IlasmTempAssembly(string declarations, bool appendDefaultHeader, bool includePdb, out string assemblyPath, out string pdbPath)
        {
            if (declarations == null) throw new ArgumentNullException(nameof(declarations));

            using (var sourceFile = new DisposableFile(extension: ".il"))
            {
                string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile.Path);

                assemblyPath = Path.Combine(
                    TempRoot.Root,
                    Path.ChangeExtension(Path.GetFileName(sourceFile.Path), "dll"));

                string completeIL;
                if (appendDefaultHeader)
                {
                    completeIL = string.Format(
@".assembly '{0}' {{}} 

.assembly extern mscorlib 
{{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 4:0:0:0
}} 

{1}",
                        sourceFileName,
                        declarations);
                }
                else
                {
                    completeIL = declarations.Replace("<<GeneratedFileName>>", sourceFileName);
                }

                sourceFile.WriteAllText(completeIL);

                var ilasmPath = Path.Combine(
                    Path.GetDirectoryName(typeof(object).Assembly.Location),
                    "ilasm.exe");

                var arguments = string.Format(
                    "\"{0}\" /DLL /OUT=\"{1}\"",
                    sourceFile.Path,
                    assemblyPath);

                if (includePdb && !MonoHelpers.IsRunningOnMono())
                {
                    pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
                    arguments += string.Format(" /PDB=\"{0}\"", pdbPath);
                }
                else
                {
                    pdbPath = null;
                }

                var program = ilasmPath;
                if (MonoHelpers.IsRunningOnMono())
                {
                    arguments = string.Format("{0} {1}", ilasmPath, arguments);
                    arguments = arguments.Replace("\"", "");
                    arguments = arguments.Replace("=", ":");
                    program = "mono";
                }

                var result = ProcessUtilities.Run(program, arguments);

                if (result.ContainsErrors)
                {
                    throw new ArgumentException(
                        "The provided IL cannot be compiled." + Environment.NewLine +
                        program + " " + arguments + Environment.NewLine +
                        result,
                        nameof(declarations));
                }
            }
        }
    }
}

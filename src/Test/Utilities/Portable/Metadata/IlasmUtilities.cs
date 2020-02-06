﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class IlasmUtilities
    {
        public static DisposableFile CreateTempAssembly(string declarations, bool prependDefaultHeader = true)
        {
            IlasmTempAssembly(declarations, prependDefaultHeader, includePdb: false, assemblyPath: out var assemblyPath, pdbPath: out var pdbPath);
            Assert.NotNull(assemblyPath);
            Assert.Null(pdbPath);
            return new DisposableFile(assemblyPath);
        }

        private static string GetIlasmPath()
        {
            if (ExecutionConditionUtil.IsWindowsDesktop)
            {
                // The desktop ilasm is still necessary because a number of our tests depend on being able to 
                // emit PDB files for net modules. That feature is not available on coreclr ilasm.
                return Path.Combine(
                    Path.GetDirectoryName(RuntimeUtilities.GetAssemblyLocation(typeof(object))),
                    "ilasm.exe");
            }

            var ilasmExeName = PlatformInformation.IsWindows ? "ilasm.exe" : "ilasm";
            var directory = Path.GetDirectoryName(RuntimeUtilities.GetAssemblyLocation(typeof(RuntimeUtilities)));
            string ridName;
            if (ExecutionConditionUtil.IsWindows)
            {
                ridName = "win-x64";
            }
            else if (ExecutionConditionUtil.IsMacOS)
            {
                ridName = "osx-x64";
            }
            else if (ExecutionConditionUtil.IsLinux)
            {
                ridName = "linux-x64";
            }
            else
            {
                throw new PlatformNotSupportedException("Runtime platform not supported for testing");
            }

            return Path.Combine(directory, "runtimes", ridName, "native", ilasmExeName);
        }

        private static readonly string IlasmPath = GetIlasmPath();

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

                sourceFile.WriteAllText(completeIL);

                var arguments = $"\"{sourceFile.Path}\" -DLL -out=\"{assemblyPath}\"";

                if (includePdb && !MonoHelpers.IsRunningOnMono())
                {
                    pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
                    arguments += string.Format(" -PDB=\"{0}\"", pdbPath);
                }
                else
                {
                    pdbPath = null;
                }

                var result = ProcessUtilities.Run(IlasmPath, arguments);

                if (result.ContainsErrors)
                {
                    throw new ArgumentException(
                        "The provided IL cannot be compiled." + Environment.NewLine +
                        IlasmPath + " " + arguments + Environment.NewLine +
                        result,
                        nameof(declarations));
                }
            }
        }
    }
}

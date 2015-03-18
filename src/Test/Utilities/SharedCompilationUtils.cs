﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias PDB;
// Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using PDB::Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class SharedCompilationUtils
    {
        internal static CompilationTestData.MethodData GetMethodData(this CompilationTestData data, string qualifiedMethodName)
        {
            var methodData = default(CompilationTestData.MethodData);
            var map = data.Methods;

            if (!map.TryGetValue(qualifiedMethodName, out methodData))
            {
                // caller may not have specified parameter list, so try to match parameterless method
                if (!map.TryGetValue(qualifiedMethodName + "()", out methodData))
                {
                    // now try to match single method with any parameter list
                    var keys = map.Keys.Where(k => k.StartsWith(qualifiedMethodName + "(", StringComparison.Ordinal));
                    if (keys.Count() == 1)
                    {
                        methodData = map[keys.First()];
                    }
                    else if (keys.Count() > 1)
                    {
                        throw new AmbiguousMatchException(
                            "Could not determine best match for method named: " + qualifiedMethodName + Environment.NewLine +
                            String.Join(Environment.NewLine, keys.Select(s => "    " + s)) + Environment.NewLine);
                    }
                }
            }

            if (methodData.ILBuilder == null)
            {
                throw new KeyNotFoundException("Could not find ILBuilder matching method '" + qualifiedMethodName + "'. Existing methods:\r\n" + string.Join("\r\n", map.Keys));
            }

            return methodData;
        }

        internal static void VerifyIL(
            this CompilationTestData.MethodData method,
            string expectedIL,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            const string moduleNamePlaceholder = "{#Module#}";
            string actualIL = GetMethodIL(method);
            if (expectedIL.IndexOf(moduleNamePlaceholder) >= 0)
            {
                var module = method.Method.ContainingModule;
                var moduleName = Path.GetFileNameWithoutExtension(module.Name);
                expectedIL = expectedIL.Replace(moduleNamePlaceholder, moduleName);
            }
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: true, expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            string expectedPdb,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(compilation, "", expectedPdb, expectedValueSourceLine, expectedValueSourcePath);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            string qualifiedMethodName,
            string expectedPdb,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            string actualPdb = GetPdbXml(compilation, qualifiedMethodName);
            XmlElementDiff.AssertEqual(ParseExpectedPdbXml(expectedPdb), XElement.Parse(actualPdb), expectedValueSourcePath, expectedValueSourceLine, expectedIsXmlLiteral: false);
        }

        private static XElement ParseExpectedPdbXml(string str)
        {
            return XElement.Parse(string.IsNullOrWhiteSpace(str) ? "<symbols></symbols>" : str);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            XElement expectedPdb,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(compilation, "", expectedPdb, expectedValueSourceLine, expectedValueSourcePath);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            string qualifiedMethodName,
            XElement expectedPdb,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            XElement actualPdb = XElement.Parse(GetPdbXml(compilation, qualifiedMethodName));
            XmlElementDiff.AssertEqual(expectedPdb, actualPdb, expectedValueSourcePath, expectedValueSourceLine, expectedIsXmlLiteral: true);
        }

        internal static string GetPdbXml(Compilation compilation, string qualifiedMethodName = "")
        {
            string actual = null;
            using (var exebits = new MemoryStream())
            {
                using (var pdbbits = new MemoryStream())
                {
                    compilation.Emit(exebits, pdbbits);

                    pdbbits.Position = 0;
                    exebits.Position = 0;

                    actual = PdbToXmlConverter.ToXml(pdbbits, exebits, PdbToXmlOptions.ResolveTokens | PdbToXmlOptions.ThrowOnError, methodName: qualifiedMethodName);
                }
            }

            return actual;
        }

        internal static string GetMethodIL(this CompilationTestData.MethodData method)
        {
            return ILBuilderVisualizer.ILBuilderToString(method.ILBuilder);
        }

        internal static EditAndContinueMethodDebugInformation GetEncDebugInfo(this CompilationTestData.MethodData methodData)
        {
            // TODO:
            return new EditAndContinueMethodDebugInformation(
                0,
                Cci.MetadataWriter.GetLocalSlotDebugInfos(methodData.ILBuilder.LocalSlotManager.LocalsInOrder()),
                closures: ImmutableArray<ClosureDebugInfo>.Empty,
                lambdas: ImmutableArray<LambdaDebugInfo>.Empty);
        }

        internal static Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> EncDebugInfoProvider(this CompilationTestData.MethodData methodData)
        {
            return _ => methodData.GetEncDebugInfo();
        }

        public static DisposableFile IlasmTempAssembly(string declarations, bool appendDefaultHeader = true)
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
            if (declarations == null) throw new ArgumentNullException("declarations");

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

                if (includePdb)
                {
                    pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
                    arguments += string.Format(" /PDB=\"{0}\"", pdbPath);
                }
                else
                {
                    pdbPath = null;
                }

                var result = ProcessLauncher.Run(ilasmPath, arguments);

                if (result.ContainsErrors)
                {
                    throw new ArgumentException(
                        "The provided IL cannot be compiled." + Environment.NewLine +
                        ilasmPath + " " + arguments + Environment.NewLine +
                        result,
                        "declarations");
                }
            }
        }

#if OUT_OF_PROC_PEVERIFY
        /// <summary>
        /// Saves <paramref name="assembly"/> to a temp file and runs PEVerify out-of-proc.
        /// </summary>
        /// <returns>
        /// Return <c>null</c> if verification succeeds, return error messages otherwise.
        /// </returns>
        public static string RunPEVerify(byte[] assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            var pathToPEVerify = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\PEVerify.exe");

            using (var tempDll = new TempFile("*.dll"))
            {
                File.WriteAllBytes(tempDll.FileName, assembly);

                var result = ProcessLauncher.Run(pathToPEVerify, "\"" + tempDll.FileName + "\"");
                return result.ContainsErrors
                           ? result.ToString()
                           : null;
            }
        }
#endif
    }

    static public class SharedResourceHelpers
    {
        public static void CleanupAllGeneratedFiles(string filename)
        {
            // This will cleanup all files with same name but different extension
            // These are often used by command line tests which use temp files.
            // The temp file dispose method cleans up that specific temp file 
            // but anything that was generated from this will not be removed by dispose

            string directory = System.IO.Path.GetDirectoryName(filename);
            string filenamewithoutextension = System.IO.Path.GetFileNameWithoutExtension(filename);
            string searchfilename = filenamewithoutextension + ".*";
            foreach (string f in System.IO.Directory.GetFiles(directory, searchfilename))
            {
                if (System.IO.Path.GetFileName(f) != System.IO.Path.GetFileName(filename))
                {
                    try
                    {
                        System.IO.File.Delete(f);
                    }
                    catch
                    {
                        // Swallow any exceptions as the cleanup should not necessarily block the test
                    }
                }
            }
        }
    }
}

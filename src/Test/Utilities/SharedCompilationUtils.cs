// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias PDB;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
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
            AssertXml.Equal(ParseExpectedPdbXml(expectedPdb), XElement.Parse(actualPdb), expectedValueSourcePath, expectedValueSourceLine, expectedIsXmlLiteral: false);
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
            AssertXml.Equal(expectedPdb, actualPdb, expectedValueSourcePath, expectedValueSourceLine, expectedIsXmlLiteral: true);
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

                ValidateDebugDirectory(exebits, compilation.AssemblyName + ".pdb");
            }

            return actual;
        }

        public static void ValidateDebugDirectory(Stream peStream, string pdbPath)
        {
            peStream.Seek(0, SeekOrigin.Begin);
            PEReader peReader = new PEReader(peStream);

            var debugDirectory = peReader.PEHeaders.PEHeader.DebugTableDirectory;

            int position;
            Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(debugDirectory, out position));
            Assert.Equal(0x1c, debugDirectory.Size);

            byte[] buffer = new byte[debugDirectory.Size];
            peStream.Read(buffer, 0, buffer.Length);

            peStream.Position = position;
            var reader = new BinaryReader(peStream);

            int characteristics = reader.ReadInt32();
            Assert.Equal(0, characteristics);

            uint timeDateStamp = reader.ReadUInt32();

            uint version = reader.ReadUInt32();
            Assert.Equal(0u, version);

            int type = reader.ReadInt32();
            Assert.Equal(2, type);

            int sizeOfData = reader.ReadInt32();
            int rvaOfRawData = reader.ReadInt32();

            int section = peReader.PEHeaders.GetContainingSectionIndex(rvaOfRawData);
            var sectionHeader = peReader.PEHeaders.SectionHeaders[section];

            int pointerToRawData = reader.ReadInt32();
            Assert.Equal(pointerToRawData, sectionHeader.PointerToRawData + rvaOfRawData - sectionHeader.VirtualAddress);

            peStream.Position = pointerToRawData;

            Assert.Equal((byte)'R', reader.ReadByte());
            Assert.Equal((byte)'S', reader.ReadByte());
            Assert.Equal((byte)'D', reader.ReadByte());
            Assert.Equal((byte)'S', reader.ReadByte());

            byte[] guidBlob = new byte[16];
            reader.Read(guidBlob, 0, guidBlob.Length);

            Assert.Equal(1u, reader.ReadUInt32());

            byte[] pathBlob = new byte[sizeOfData - 24 - 1];
            reader.Read(pathBlob, 0, pathBlob.Length);
            var actualPath = Encoding.UTF8.GetString(pathBlob);
            Assert.Equal(pdbPath, actualPath);
            Assert.Equal(0, reader.ReadByte());
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

                if (includePdb && !CLRHelpers.IsRunningOnMono())
                {
                    pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
                    arguments += string.Format(" /PDB=\"{0}\"", pdbPath);
                }
                else
                {
                    pdbPath = null;
                }

                var program = ilasmPath;
                if (CLRHelpers.IsRunningOnMono())
                {
                    arguments = string.Format("{0} {1}", ilasmPath, arguments);
                    arguments = arguments.Replace("\"", "");
                    arguments = arguments.Replace("=", ":");
                    program = "mono";
                }

                var result = ProcessLauncher.Run(program, arguments);

                if (result.ContainsErrors)
                {
                    throw new ArgumentException(
                        "The provided IL cannot be compiled." + Environment.NewLine +
                        program + " " + arguments + Environment.NewLine +
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
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

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

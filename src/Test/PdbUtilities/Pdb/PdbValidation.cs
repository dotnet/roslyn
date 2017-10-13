// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.Tools;
using Microsoft.Metadata.Tools;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class PdbValidation
    {
        public static CompilationVerifier VerifyPdb(
            this CompilationVerifier verifier,
            XElement expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            verifier.Compilation.VerifyPdb(expectedPdb, embeddedTexts, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
            return verifier;
        }

        public static CompilationVerifier VerifyPdb(
            this CompilationVerifier verifier,
            string expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            verifier.Compilation.VerifyPdb(expectedPdb, embeddedTexts, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
            return verifier;
        }

        public static CompilationVerifier VerifyPdb(
            this CompilationVerifier verifier,
            string qualifiedMethodName,
            string expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            verifier.Compilation.VerifyPdb(qualifiedMethodName, expectedPdb, embeddedTexts, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
            return verifier;
        }

        public static CompilationVerifier VerifyPdb(
            this CompilationVerifier verifier,
            string qualifiedMethodName,
            XElement expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            verifier.Compilation.VerifyPdb(qualifiedMethodName, expectedPdb, embeddedTexts, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
            return verifier;
        }

        public static void VerifyPdb(this CompilationDifference diff, IEnumerable<MethodDefinitionHandle> methodHandles, string expectedPdb)
        {
            VerifyPdb(diff, methodHandles.Select(h => MetadataTokens.GetToken(h)), expectedPdb);
        }

        public static void VerifyPdb(this CompilationDifference diff, IEnumerable<MethodDefinitionHandle> methodHandles, XElement expectedPdb)
        {
            VerifyPdb(diff, methodHandles.Select(h => MetadataTokens.GetToken(h)), expectedPdb);
        }

        public static void VerifyPdb(
            this CompilationDifference diff,
            IEnumerable<int> methodTokens,
            string expectedPdb,
            DebugInformationFormat format = DebugInformationFormat.Pdb,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(diff, methodTokens, expectedPdb, format, expectedValueSourceLine, expectedValueSourcePath, expectedIsXmlLiteral: false);
        }

        public static void VerifyPdb(
            this CompilationDifference diff,
            IEnumerable<int> methodTokens,
            XElement expectedPdb,
            DebugInformationFormat format = DebugInformationFormat.Pdb,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(diff, methodTokens, expectedPdb.ToString(), format, expectedValueSourceLine, expectedValueSourcePath, expectedIsXmlLiteral: true);
        }

        private static void VerifyPdb(
            this CompilationDifference diff,
            IEnumerable<int> methodTokens,
            string expectedPdb,
            DebugInformationFormat format,
            int expectedValueSourceLine,
            string expectedValueSourcePath,
            bool expectedIsXmlLiteral)
        {
            Assert.NotEqual(default(DebugInformationFormat), format);
            Assert.NotEqual(DebugInformationFormat.Embedded, format);

            string actualPdb = PdbToXmlConverter.DeltaPdbToXml(new ImmutableMemoryStream(diff.PdbDelta), methodTokens);
            var (actual, expected) = AdjustToPdbFormat(actualPdb, expectedPdb, actualIsPortable: diff.NextGeneration.InitialBaseline.HasPortablePdb, actualIsConverted: false);

            AssertEx.AssertLinesEqual(
                expected, 
                actual, 
                $"PDB format: {format}{Environment.NewLine}",
                expectedValueSourcePath, 
                expectedValueSourceLine, 
                escapeQuotes: !expectedIsXmlLiteral);
        }

        public static void VerifyPdb(
            this Compilation compilation,
            string expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(compilation, "", expectedPdb, embeddedTexts, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
        }

        public static void VerifyPdb(
            this Compilation compilation,
            string qualifiedMethodName,
            string expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdbImpl(
                compilation,
                embeddedTexts,
                debugEntryPoint,
                qualifiedMethodName,
                string.IsNullOrWhiteSpace(expectedPdb) ? "<symbols></symbols>" : expectedPdb,
                format,
                options,
                expectedValueSourceLine,
                expectedValueSourcePath,
                expectedIsXmlLiteral: false);
        }

        public static void VerifyPdb(
            this Compilation compilation,
            XElement expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(compilation, "", expectedPdb, embeddedTexts, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
        }

        public static void VerifyPdb(
            this Compilation compilation,
            string qualifiedMethodName,
            XElement expectedPdb,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbValidationOptions options = PdbValidationOptions.Default,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdbImpl(
                compilation,
                embeddedTexts,
                debugEntryPoint,
                qualifiedMethodName,
                expectedPdb.ToString(),
                format,
                options,
                expectedValueSourceLine,
                expectedValueSourcePath,
                expectedIsXmlLiteral: true);
        }

        private static void VerifyPdbImpl(
            this Compilation compilation,
            IEnumerable<EmbeddedText> embeddedTexts,
            IMethodSymbol debugEntryPoint,
            string qualifiedMethodName,
            string expectedPdb,
            DebugInformationFormat format,
            PdbValidationOptions options,
            int expectedValueSourceLine,
            string expectedValueSourcePath,
            bool expectedIsXmlLiteral)
        {
            Assert.NotEqual(DebugInformationFormat.Embedded, format);

            bool testWindowsPdb = format == 0 || format == DebugInformationFormat.Pdb;
            bool testPortablePdb = format == 0 || format == DebugInformationFormat.PortablePdb;
            bool testConversion = (options & PdbValidationOptions.SkipConversionValidation) == 0;
            var pdbToXmlOptions = options.ToPdbToXmlOptions();

            if (testWindowsPdb)
            {
                Verify(isPortable: false, testOtherFormat: testPortablePdb);
            }

            if (testPortablePdb)
            {
                Verify(isPortable: true, testOtherFormat: testWindowsPdb);
            }

            void Verify(bool isPortable, bool testOtherFormat)
            {
                var peStream = new MemoryStream();
                var pdbStream = new MemoryStream();
                EmitWithPdb(peStream, pdbStream, compilation, debugEntryPoint, embeddedTexts, isPortable);

                VerifyPdbMatchesExpectedXml(peStream, pdbStream, qualifiedMethodName, pdbToXmlOptions, expectedPdb, expectedValueSourceLine, expectedValueSourcePath, expectedIsXmlLiteral, isPortable);

                if (testConversion && testOtherFormat)
                {
                    VerifyConvertedPdbMatchesExpectedXml(peStream, pdbStream, qualifiedMethodName, expectedPdb, pdbToXmlOptions, expectedIsXmlLiteral, isPortable);
                }
            }
        }

        private static void VerifyPdbMatchesExpectedXml(
            Stream peStream,
            Stream pdbStream,
            string qualifiedMethodName,
            PdbToXmlOptions pdbToXmlOptions,
            string expectedPdb,
            int expectedValueSourceLine,
            string expectedValueSourcePath,
            bool expectedIsXmlLiteral,
            bool isPortable)
        {
            peStream.Position = 0;
            pdbStream.Position = 0;
            var actualPdb = XElement.Parse(PdbToXmlConverter.ToXml(pdbStream, peStream, pdbToXmlOptions, methodName: qualifiedMethodName)).ToString();
            var (actual, expected) = AdjustToPdbFormat(actualPdb, expectedPdb, actualIsPortable: isPortable, actualIsConverted: false);

            AssertEx.AssertLinesEqual(
                expected, 
                actual, 
                $"PDB format: {(isPortable ? "Portable" : "Windows")}{Environment.NewLine}", 
                expectedValueSourcePath,
                expectedValueSourceLine, 
                escapeQuotes: !expectedIsXmlLiteral);
        }

        private static void VerifyConvertedPdbMatchesExpectedXml(
            Stream peStreamOriginal,
            Stream pdbStreamOriginal,
            string qualifiedMethodName,
            string expectedPdb,
            PdbToXmlOptions pdbToXmlOptions,
            bool expectedIsXmlLiteral,
            bool originalIsPortable)
        {
            var pdbStreamConverted = new MemoryStream();
            var converter = new PdbConverter(diagnostic => Assert.True(false, diagnostic.ToString()));

            peStreamOriginal.Position = 0;
            pdbStreamOriginal.Position = 0;

            if (originalIsPortable)
            {
                converter.ConvertPortableToWindows(peStreamOriginal, pdbStreamOriginal, pdbStreamConverted);
            }
            else
            {
                converter.ConvertWindowsToPortable(peStreamOriginal, pdbStreamOriginal, pdbStreamConverted);
            }

            pdbStreamConverted.Position = 0;
            peStreamOriginal.Position = 0;

            var actualConverted = AdjustForConversionArtifacts(XElement.Parse(PdbToXmlConverter.ToXml(pdbStreamConverted, peStreamOriginal, pdbToXmlOptions, methodName: qualifiedMethodName)).ToString());
            var adjustedExpected = AdjustForConversionArtifacts(expectedPdb);

            var (actual, expected) = AdjustToPdbFormat(actualConverted, adjustedExpected, actualIsPortable: !originalIsPortable, actualIsConverted: true);

            AssertEx.AssertLinesEqual(
                expected,
                actual,
                $"PDB format: {(originalIsPortable ? "Windows" : "Portable")} converted from {(originalIsPortable ? "Portable" : "Windows")}{Environment.NewLine}",
                expectedValueSourcePath: null,
                expectedValueSourceLine: 0,
                escapeQuotes: !expectedIsXmlLiteral);
        }

        private static string AdjustForConversionArtifacts(string pdb)
        {
            var xml = XElement.Parse(pdb);
            var pendingRemoval = new List<XElement>();
            foreach (var e in xml.DescendantsAndSelf())
            {
                if (e.Name == "constant")
                {
                    // only compare constant names; values and signatures might differ:
                    var name = e.Attribute("name");
                    e.RemoveAttributes();
                    e.Add(name);
                }
                else if (e.Name == "bucket" && e.Parent.Name == "dynamicLocals")
                {
                    // dynamic flags might be 0-padded differently

                    var flags = e.Attribute("flags");
                    flags.SetValue(flags.Value.TrimEnd('0'));
                }
                else if (e.Name == "defunct")
                {
                    pendingRemoval.Add(e);
                }
            }

            foreach (var e in pendingRemoval)
            {
                e.Remove();
            }

            RemoveEmptyScopes(xml);
            return xml.ToString();
        }

        internal static (string Actual, string Expected) AdjustToPdbFormat(string actualPdb, string expectedPdb, bool actualIsPortable, bool actualIsConverted)
        {
            var actualXml = XElement.Parse(actualPdb);
            var expectedXml = XElement.Parse(expectedPdb);

            if (actualIsPortable)
            {
                // Windows SymWriter doesn't serialize empty scopes.
                // In Portable PDB each method with a body (even with no locals) has a scope that points to the imports. Such scope appears as empty
                // in the current XML representation. 
                RemoveEmptyScopes(actualXml);

                // Remove elements that are never present in Portable PDB.
                RemoveNonPortableElements(expectedXml);
            }

            if (actualIsPortable || actualIsConverted)
            {
                RemoveElementsWithSpecifiedFormat(expectedXml, "windows");
            }

            if (!actualIsPortable || actualIsConverted)
            {
                RemoveElementsWithSpecifiedFormat(expectedXml, "portable");
            }

            RemoveEmptySequencePoints(expectedXml);
            RemoveEmptyScopes(expectedXml);
            RemoveEmptyCustomDebugInfo(expectedXml);
            RemoveEmptyMethods(expectedXml);
            RemoveFormatAttributes(expectedXml);

            return (actualXml.ToString(), expectedXml.ToString());
        }

        private static bool RemoveElements(IEnumerable<XElement> elements)
        {
            var array = elements.ToArray();

            foreach (var e in array)
            {
                e.Remove();
            }

            return array.Length > 0;
        }

        private static void RemoveEmptyCustomDebugInfo(XElement pdb)
        {
            RemoveElements(from e in pdb.DescendantsAndSelf()
                           where e.Name == "customDebugInfo" && !e.HasElements
                           select e);
        }

        private static void RemoveEmptyScopes(XElement pdb)
        {
            while (RemoveElements(from e in pdb.DescendantsAndSelf()
                                  where e.Name == "scope" && !e.HasElements
                                  select e));
        }

        private static void RemoveEmptySequencePoints(XElement pdb)
        {
            RemoveElements(from e in pdb.DescendantsAndSelf()
                           where e.Name == "sequencePoints" && !e.HasElements
                           select e);
        }

        private static void RemoveEmptyMethods(XElement pdb)
        {
            RemoveElements(from e in pdb.DescendantsAndSelf()
                           where e.Name == "method" && !e.HasElements
                           select e);
        }

        private static void RemoveNonPortableElements(XElement expectedNativePdb)
        {
            // The following elements are never presents in Portable PDB.
            RemoveElements(from e in expectedNativePdb.DescendantsAndSelf()
                           where e.Name == "forwardIterator" ||
                                 e.Name == "forwardToModule" ||
                                 e.Name == "forward" ||
                                 e.Name == "tupleElementNames" ||
                                 e.Name == "dynamicLocals" ||
                                 e.Name == "using" ||
                                 e.Name == "currentnamespace" ||
                                 e.Name == "defaultnamespace" ||
                                 e.Name == "importsforward" ||
                                 e.Name == "xmlnamespace" ||
                                 e.Name == "alias" ||
                                 e.Name == "namespace" ||
                                 e.Name == "type" ||
                                 e.Name == "extern" ||
                                 e.Name == "externinfo" ||
                                 e.Name == "defunct" ||
                                 e.Name == "local" && e.Attributes().Any(a => a.Name.LocalName == "name" && a.Value.StartsWith("$VB$ResumableLocal_"))
                           select e);
        }

        private static void RemoveElementsWithSpecifiedFormat(XElement expectedNativePdb, string format)
        {
            RemoveElements(from e in expectedNativePdb.DescendantsAndSelf()
                           where e.Attributes().Any(a => a.Name.LocalName == "format" && a.Value == format)
                           select e);
        }

        private static void RemoveFormatAttributes(XElement pdb)
        {
            foreach (var element in pdb.DescendantsAndSelf())
            {
                element.Attributes().FirstOrDefault(a => a.Name.LocalName == "format")?.Remove();
            }
        }

        internal static string GetPdbXml(
            Compilation compilation,
            IEnumerable<EmbeddedText> embeddedTexts = null,
            IMethodSymbol debugEntryPoint = null,
            PdbValidationOptions options = PdbValidationOptions.Default,
            string qualifiedMethodName = "",
            bool portable = false)
        {
            var peStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            EmitWithPdb(peStream, pdbStream, compilation, debugEntryPoint, embeddedTexts, portable);

            pdbStream.Position = 0;
            peStream.Position = 0;
            return PdbToXmlConverter.ToXml(pdbStream, peStream, options.ToPdbToXmlOptions(), methodName: qualifiedMethodName);
        }

        private static void EmitWithPdb(MemoryStream peStream, MemoryStream pdbStream, Compilation compilation, IMethodSymbol debugEntryPoint, IEnumerable<EmbeddedText> embeddedTexts, bool portable)
        {
            var result = compilation.Emit(
                peStream,
                pdbStream,
                debugEntryPoint: debugEntryPoint,
                options: EmitOptions.Default.WithDebugInformationFormat(portable ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb),
                embeddedTexts: embeddedTexts);

            result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Verify();
            ValidateDebugDirectory(peStream, portable ? pdbStream : null, compilation.AssemblyName + ".pdb", compilation.IsEmitDeterministic);
        }

        public unsafe static byte[] GetSourceLinkData(Stream pdbStream)
        {
            pdbStream.Position = 0;

            var symReader = SymReaderFactory.CreateReader(pdbStream);
            try
            {
                Marshal.ThrowExceptionForHR(symReader.GetSourceServerData(out byte* data, out int size));
                if (size == 0)
                {
                    return Array.Empty<byte>();
                }

                var result = new byte[size];
                Marshal.Copy((IntPtr)data, result, 0, result.Length);
                return result;
            }
            finally
            {
                symReader.Dispose();
            }
        }

        public static void ValidateDebugDirectory(Stream peStream, Stream portablePdbStreamOpt, string pdbPath, bool isDeterministic)
        {
            peStream.Position = 0;

            var peReader = new PEReader(peStream);
            var debugDirectory = peReader.PEHeaders.PEHeader.DebugTableDirectory;

            Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(debugDirectory, out var position));
            int entries = debugDirectory.Size / 0x1c;
            Assert.Equal(0, debugDirectory.Size % 0x1c);
            Assert.True(entries == 1 || entries == 2);
            bool hasDebug = entries == 2;

            peStream.Position = position;
            var reader = new BinaryReader(peStream);

            // first the IMAGE_DEBUG_TYPE_CODEVIEW entry
            int characteristics = reader.ReadInt32();
            Assert.Equal(0, characteristics);

            byte[] stamp = reader.ReadBytes(sizeof(int));

            uint version = reader.ReadUInt32();
            Assert.Equal((portablePdbStreamOpt != null) ? 0x504d0100u : 0, version);

            int type = reader.ReadInt32();
            Assert.Equal(2, type); // IMAGE_DEBUG_TYPE_CODEVIEW

            int sizeOfData = reader.ReadInt32();
            int rvaOfRawData = reader.ReadInt32();

            int section = peReader.PEHeaders.GetContainingSectionIndex(rvaOfRawData);
            var sectionHeader = peReader.PEHeaders.SectionHeaders[section];

            int pointerToRawData = reader.ReadInt32();
            Assert.Equal(pointerToRawData, sectionHeader.PointerToRawData + rvaOfRawData - sectionHeader.VirtualAddress);

            // optionally a IMAGE_DEBUG_TYPE_NO_TIMESTAMP entry indicating that timestamps are deterministic
            if (hasDebug)
            {
                int characteristics2 = reader.ReadInt32();
                Assert.Equal(0, characteristics2);

                byte[] stamp2 = reader.ReadBytes(sizeof(int));

                int version2 = reader.ReadInt32();
                Assert.Equal(0, version2);

                int type2 = reader.ReadInt32();
                Assert.Equal(16, type2); // IMAGE_DEBUG_TYPE_NO_TIMESTAMP

                int sizeOfData2 = reader.ReadInt32();
                int rvaOfRawData2 = reader.ReadInt32();
                int pointerToRawData2 = reader.ReadInt32();
                Assert.Equal(0, sizeOfData2 | rvaOfRawData2 | pointerToRawData2);
            }

            // Now verify the data pointed to by the IMAGE_DEBUG_TYPE_CODEVIEW entry
            peStream.Position = pointerToRawData;

            Assert.Equal((byte)'R', reader.ReadByte());
            Assert.Equal((byte)'S', reader.ReadByte());
            Assert.Equal((byte)'D', reader.ReadByte());
            Assert.Equal((byte)'S', reader.ReadByte());

            byte[] guidBlob = new byte[16];
            reader.Read(guidBlob, 0, guidBlob.Length);

            Assert.Equal(1u, reader.ReadUInt32());

            byte[] pathBlob = new byte[sizeOfData - 24];
            reader.Read(pathBlob, 0, pathBlob.Length);

            int terminator = Array.IndexOf(pathBlob, (byte)0);
            Assert.True(terminator >= 0, "Path should be NUL terminated");

            for (int i = terminator + 1; i < pathBlob.Length; i++)
            {
                Assert.Equal(0, pathBlob[i]);
            }

            if (isDeterministic)
            {
                Assert.Equal(pathBlob.Length - 1, terminator);
            }
            else
            {
                Assert.True(pathBlob.Length >= 260, "Path should be at least MAX_PATH long");
            }

            var actualPath = Encoding.UTF8.GetString(pathBlob, 0, terminator);
            Assert.Equal(pdbPath, actualPath);

            if (portablePdbStreamOpt != null)
            {
                ValidatePortablePdbId(portablePdbStreamOpt, stamp, guidBlob);
            }
        }

        private unsafe static void ValidatePortablePdbId(Stream pdbStream, byte[] stampInDebugDirectory, byte[] guidInDebugDirectory)
        {
            var expectedId = ImmutableArray.CreateRange(guidInDebugDirectory.Concat(stampInDebugDirectory));

            pdbStream.Position = 0;
            var buffer = new byte[pdbStream.Length];
            var bytesRead = pdbStream.TryReadAll(buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, bytesRead);

            fixed (byte* bufferPtr = buffer)
            {
                var id = new MetadataReader(bufferPtr, buffer.Length).DebugMetadataHeader.Id;
                Assert.Equal(id.ToArray(), expectedId);
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class PdbValidation
    {
        internal static void VerifyPdb(
            this Compilation compilation,
            string expectedPdb,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbToXmlOptions options = 0,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(compilation, "", expectedPdb, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            string qualifiedMethodName,
            string expectedPdb,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbToXmlOptions options = 0,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            var expectedPdbXml = XElement.Parse(string.IsNullOrWhiteSpace(expectedPdb) ? "<symbols></symbols>" : expectedPdb);

            VerifyPdbImpl(
                compilation,
                debugEntryPoint,
                qualifiedMethodName,
                expectedPdbXml,
                format,
                options,
                expectedValueSourceLine,
                expectedValueSourcePath,
                expectedIsXmlLiteral: false);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            XElement expectedPdb,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbToXmlOptions options = 0,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdb(compilation, "", expectedPdb, debugEntryPoint, format, options, expectedValueSourceLine, expectedValueSourcePath);
        }

        internal static void VerifyPdb(
            this Compilation compilation,
            string qualifiedMethodName,
            XElement expectedPdb,
            IMethodSymbol debugEntryPoint = null,
            DebugInformationFormat format = 0,
            PdbToXmlOptions options = 0,
            [CallerLineNumber]int expectedValueSourceLine = 0,
            [CallerFilePath]string expectedValueSourcePath = null)
        {
            VerifyPdbImpl(
                compilation,
                debugEntryPoint,
                qualifiedMethodName,
                expectedPdb,
                format,
                options,
                expectedValueSourceLine,
                expectedValueSourcePath,
                expectedIsXmlLiteral: true);
        }

        private static void VerifyPdbImpl(
            this Compilation compilation,
            IMethodSymbol debugEntryPoint,
            string qualifiedMethodName,
            XElement expectedPdb,
            DebugInformationFormat format,
            PdbToXmlOptions options,
            int expectedValueSourceLine,
            string expectedValueSourcePath,
            bool expectedIsXmlLiteral)
        {
            Assert.NotEqual(DebugInformationFormat.Embedded, format);
            
            if (format == 0 || format == DebugInformationFormat.Pdb)
            {
                XElement actualNativePdb = XElement.Parse(GetPdbXml(compilation, debugEntryPoint, options, qualifiedMethodName, portable: false));
                AssertXml.Equal(expectedPdb, actualNativePdb, expectedValueSourcePath, expectedValueSourceLine, expectedIsXmlLiteral);
            }

            if (format == 0 || format == DebugInformationFormat.PortablePdb)
            {
                XElement actualPortablePdb = XElement.Parse(GetPdbXml(compilation, debugEntryPoint, options, qualifiedMethodName, portable: true));

                // SymWriter doesn't create empty scopes. When the C# compiler uses forwarding CDI instead of a NamespaceScope
                // the scope is actually not empty - it logically contains the imports. Portable PDB does not used forwarding and thus
                // creates the scope. When generating PDB XML for testing the Portable DiaSymReader returns empty namespaces.
                RemoveEmptyScopes(actualPortablePdb);

                // sharing the same expected output with native PDB
                if (format == 0)
                {
                    RemoveNonPortablePdb(expectedPdb);

                    // TODO: remove
                    RemoveEmptySequencePoints(expectedPdb);

                    // remove scopes that only contained non-portable elements (namespace scopes)
                    RemoveEmptyScopes(expectedPdb);

                    RemoveEmptyMethods(expectedPdb);
                }

                AssertXml.Equal(expectedPdb, actualPortablePdb, expectedValueSourcePath, expectedValueSourceLine, expectedIsXmlLiteral);
            }
        }

        private static void RemoveEmptyScopes(XElement pdb)
        {
            var emptyScopes = from e in pdb.DescendantsAndSelf()
                              where e.Name == "scope" && !e.HasElements
                              select e;

            foreach (var e in emptyScopes.ToArray())
            {
                e.Remove();
            }
        }

        private static void RemoveEmptySequencePoints(XElement pdb)
        {
            var emptyScopes = from e in pdb.DescendantsAndSelf()
                              where e.Name == "sequencePoints" && !e.HasElements
                              select e;

            foreach (var e in emptyScopes.ToArray())
            {
                e.Remove();
            }
        }

        private static void RemoveEmptyMethods(XElement pdb)
        {
            var emptyScopes = from e in pdb.DescendantsAndSelf()
                              where e.Name == "method" && !e.HasElements
                              select e;

            foreach (var e in emptyScopes.ToArray())
            {
                e.Remove();
            }
        }

        private static void RemoveNonPortablePdb(XElement expectedNativePdb)
        {
            var nonPortableElements = from e in expectedNativePdb.DescendantsAndSelf()
                                      where e.Name == "customDebugInfo" ||
                                            e.Name == "currentnamespace" ||
                                            e.Name == "defaultnamespace" ||
                                            e.Name == "importsforward" ||
                                            e.Name == "xmlnamespace" ||
                                            e.Name == "alias" ||
                                            e.Name == "namespace" ||
                                            e.Name == "type" ||
                                            e.Name == "defunct" ||
                                            e.Name == "extern" ||
                                            e.Name == "externinfo"
                                      select e;

            foreach (var e in nonPortableElements.ToArray())
            {
                e.Remove();
            }
        }

        internal static string GetPdbXml(
            Compilation compilation,
            IMethodSymbol debugEntryPoint = null,
            PdbToXmlOptions options = 0,
            string qualifiedMethodName = "",
            bool portable = false)
        {
            string actual = null;
            using (var exebits = new MemoryStream())
            {
                using (var pdbbits = new MemoryStream())
                {
                    var result = compilation.Emit(
                        exebits,
                        pdbbits,
                        debugEntryPoint: debugEntryPoint,
                        options: EmitOptions.Default.WithDebugInformationFormat(portable ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb));

                    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

                    pdbbits.Position = 0;
                    exebits.Position = 0;

                    options |= PdbToXmlOptions.ResolveTokens | PdbToXmlOptions.ThrowOnError;
                    actual = PdbToXmlConverter.ToXml(pdbbits, exebits, options, methodName: qualifiedMethodName);

                    ValidateDebugDirectory(exebits, portable ? pdbbits : null, compilation.AssemblyName + ".pdb", compilation.IsEmitDeterministic);
                }
            }

            return actual;
        }

        public static void ValidateDebugDirectory(Stream peStream, Stream portablePdbStreamOpt, string pdbPath, bool isDeterministic)
        {
            peStream.Seek(0, SeekOrigin.Begin);
            PEReader peReader = new PEReader(peStream);

            var debugDirectory = peReader.PEHeaders.PEHeader.DebugTableDirectory;

            int position;
            Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(debugDirectory, out position));
            int entries = debugDirectory.Size / 0x1c;
            Assert.Equal(0, debugDirectory.Size % 0x1c);
            Assert.True(entries == 1 || entries == 2);
            bool hasDebug = entries == 2;

            byte[] buffer = new byte[debugDirectory.Size];
            peStream.Read(buffer, 0, buffer.Length); // TODO: this is not guaranteed to read buffer.Length of data

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

        public static void VerifyMetadataEqualModuloMvid(Stream peStream1, Stream peStream2)
        {
            peStream1.Position = 0;
            peStream2.Position = 0;

            var peReader1 = new PEReader(peStream1);
            var peReader2 = new PEReader(peStream2);

            var md1 = peReader1.GetMetadata().GetContent();
            var md2 = peReader2.GetMetadata().GetContent();

            var mdReader1 = peReader1.GetMetadataReader();
            var mdReader2 = peReader2.GetMetadataReader();

            var mvidIndex1 = mdReader1.GetModuleDefinition().Mvid;
            var mvidIndex2 = mdReader2.GetModuleDefinition().Mvid;

            var mvidOffset1 = mdReader1.GetHeapMetadataOffset(HeapIndex.Guid) + 16 * (MetadataTokens.GetHeapOffset(mvidIndex1) - 1);
            var mvidOffset2 = mdReader2.GetHeapMetadataOffset(HeapIndex.Guid) + 16 * (MetadataTokens.GetHeapOffset(mvidIndex2) - 1);

            if (!md1.RemoveRange(mvidOffset1, 16).SequenceEqual(md1.RemoveRange(mvidOffset2, 16)))
            {
                var mdw1 = new StringWriter();
                var mdw2 = new StringWriter();
                new MetadataVisualizer(mdReader1, mdw1).Visualize();
                new MetadataVisualizer(mdReader2, mdw2).Visualize();
                mdw1.Flush();
                mdw2.Flush();

                AssertEx.AssertResultsEqual(mdw1.ToString(), mdw2.ToString());
            }
        }

        public static Dictionary<int, string> GetMarkers(string pdbXml)
        {
            return ToDictionary<int, string, string>(EnumerateMarkers(pdbXml), (markers, marker) => markers + marker);
        }

        private static Dictionary<K, V> ToDictionary<K, V, I>(IEnumerable<KeyValuePair<K, I>> pairs, Func<V, I, V> aggregator)
        {
            var result = new Dictionary<K, V>();
            foreach (var pair in pairs)
            {
                V existing;
                if (result.TryGetValue(pair.Key, out existing))
                {
                    result[pair.Key] = aggregator(existing, pair.Value);
                }
                else
                {
                    result.Add(pair.Key, aggregator(default(V), pair.Value));
                }
            }

            return result;
        }

        public static IEnumerable<KeyValuePair<int, string>> EnumerateMarkers(string pdbXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(pdbXml);

            foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
            {
                foreach (XmlElement item in entry.ChildNodes)
                {
                    yield return KeyValuePair.Create(
                        Convert.ToInt32(item.GetAttribute("offset"), 16),
                        (item.GetAttribute("hidden") == "true") ? "~" : "-");
                }
            }

            foreach (XmlNode entry in doc.GetElementsByTagName("asyncInfo"))
            {
                foreach (XmlElement item in entry.ChildNodes)
                {
                    if (item.Name == "await")
                    {
                        yield return KeyValuePair.Create(Convert.ToInt32(item.GetAttribute("yield"), 16), "<");
                        yield return KeyValuePair.Create(Convert.ToInt32(item.GetAttribute("resume"), 16), ">");
                    }
                    else if (item.Name == "catchHandler")
                    {
                        yield return KeyValuePair.Create(Convert.ToInt32(item.GetAttribute("offset"), 16), "$");
                    }
                }
            }
        }
    }
}

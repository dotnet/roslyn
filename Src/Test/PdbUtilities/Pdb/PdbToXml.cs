// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.Samples.Debugging.CorSymbolStore;
using Microsoft.Samples.Debugging.SymbolStore;
using Microsoft.VisualStudio.SymReaderInterop;
using CDI = Microsoft.VisualStudio.SymReaderInterop.CustomDebugInfoReader;
using CDIC = Microsoft.Cci.CustomDebugInfoConstants;
using PooledStringBuilder = Microsoft.CodeAnalysis.Collections.PooledStringBuilder;
using System.Runtime.InteropServices;

namespace Roslyn.Test.PdbUtilities
{
    /// <summary>
    /// Class to write out XML for a PDB.
    /// </summary>
    public sealed class PdbToXmlConverter
    {
        // For printing integers in a standard hex format.
        private const string IntHexFormat = "0x{0:X}";

        private readonly MetadataReader metadataReader;
        private readonly TempPdbReader pdbReader;
        private readonly PdbToXmlOptions options;
        private readonly XmlWriter writer;

        // Maps files to ids. 
        private readonly Dictionary<string, int> m_fileMapping = new Dictionary<string, int>();

        private PdbToXmlConverter(XmlWriter writer, TempPdbReader pdbReader, MetadataReader metadataReader, PdbToXmlOptions options)
        {
            this.pdbReader = pdbReader;
            this.metadataReader = metadataReader;
            this.writer = writer;
            this.options = options;
        }

        public unsafe static string DeltaPdbToXml(Stream deltaPdb, IEnumerable<int> methodTokens)
        {
            var writer = new StringWriter();
            ToXml(
                writer, 
                deltaPdb, 
                metadataReaderOpt: null,
                options: PdbToXmlOptions.IncludeTokens, 
                methodHandles: methodTokens.Select(token => (MethodDefinitionHandle)MetadataTokens.Handle(token)));

            return writer.ToString();
        }

        public static string ToXml(Stream pdbStream, Stream peStream, PdbToXmlOptions options = PdbToXmlOptions.ResolveTokens, string methodName = null)
        {
            var writer = new StringWriter();
            ToXml(writer, pdbStream, peStream, options, methodName);
            return writer.ToString();
        }

        public static string ToXml(Stream pdbStream, byte[] peImage, PdbToXmlOptions options = PdbToXmlOptions.ResolveTokens, string methodName = null)
        {
            var writer = new StringWriter();
            ToXml(writer, pdbStream, new MemoryStream(peImage), options, methodName);
            return writer.ToString();
        }

        public unsafe static void ToXml(TextWriter xmlWriter, Stream pdbStream, Stream peStream, PdbToXmlOptions options = PdbToXmlOptions.Default, string methodName = null)
        {
            IEnumerable<MethodDefinitionHandle> methodHandles;
            var headers = new PEHeaders(peStream);
            byte[] metadata = new byte[headers.MetadataSize];
            peStream.Seek(headers.MetadataStartOffset, SeekOrigin.Begin);
            peStream.Read(metadata, 0, headers.MetadataSize);

            fixed (byte* metadataPtr = metadata)
            {
                var metadataReader = new MetadataReader(metadataPtr, metadata.Length);

                if (string.IsNullOrEmpty(methodName))
                {
                    methodHandles = metadataReader.MethodDefinitions;
                }
                else
                {
                    var matching = metadataReader.MethodDefinitions.
                        Where(methodHandle => GetQualifiedMethodName(metadataReader, methodHandle) == methodName).ToArray();

                    if (matching.Length == 0)
                    {
                        xmlWriter.WriteLine("<error>");
                        xmlWriter.WriteLine(string.Format("<message>No method '{0}' found in metadata.</message>", methodName));
                        xmlWriter.WriteLine("<available-methods>");

                        foreach (var methodHandle in metadataReader.MethodDefinitions)
                        {
                            xmlWriter.Write("<method><![CDATA[");
                            xmlWriter.Write(GetQualifiedMethodName(metadataReader, methodHandle));
                            xmlWriter.Write("]]></method>");
                            xmlWriter.WriteLine();
                        }

                        xmlWriter.WriteLine("</available-methods>");
                        xmlWriter.WriteLine("</error>");

                        return;
                    }

                    methodHandles = matching;
                }

                ToXml(xmlWriter, pdbStream, metadataReader, options, methodHandles);
            }
        }

        /// <summary>
        /// Load the PDB given the parameters at the ctor and spew it out to the XmlWriter specified
        /// at the ctor.
        /// </summary>
        private static void ToXml(TextWriter xmlWriter, Stream pdbStream, MetadataReader metadataReaderOpt, PdbToXmlOptions options, IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            Debug.Assert(pdbStream != null);
            Debug.Assert((options & PdbToXmlOptions.ResolveTokens) == 0 || metadataReaderOpt != null);

            XmlDocument doc = new XmlDocument();
            XmlWriter writer = doc.CreateNavigator().AppendChild();

            using (TempPdbReader pdbReader = TempPdbReader.Create(pdbStream))
            {
                if (pdbReader == null)
                {
                    Console.WriteLine("Error: No Symbol Reader could be initialized.");
                }

                var converter = new PdbToXmlConverter(writer, pdbReader, metadataReaderOpt, options);

                converter.WriteRoot(methodHandles ?? metadataReaderOpt.MethodDefinitions);
            }
                        
            writer.Close();

            // Save xml to disk
            doc.Save(xmlWriter);
        }

        private static byte[] GetImage(Stream stream)
        {
            MemoryStream memoryStream = stream as MemoryStream;
            if (memoryStream == null)
            {
                memoryStream = new MemoryStream((int)stream.Length);
                stream.Position = 0;
                stream.CopyTo(memoryStream);
            }

            return memoryStream.GetBuffer();
        }

        private void WriteRoot(IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            writer.WriteStartDocument();

            writer.WriteStartElement("symbols");

            WriteDocList();
            WriteEntryPoint();
            WriteAllMethods(methodHandles);

            if ((options & PdbToXmlOptions.IncludeMethodSpans) != 0)
            {
                WriteAllMethodSpans();
            }

            writer.WriteEndElement();
        }

        // Dump all of the methods in the given ISymbolReader to the XmlWriter provided in the ctor.
        private void WriteAllMethods(IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            writer.WriteStartElement("methods");

            foreach (var methodHandle in methodHandles)
            {
                WriteMethod(methodHandle);
            }

            writer.WriteEndElement();
        }

        private void WriteMethod(MethodDefinitionHandle methodHandle)
        {
            int token = metadataReader.GetToken(methodHandle);
            var symbolToken = new SymbolToken(token);

            byte[] bytes = pdbReader.RawSymbolReader.GetCustomDebugInfo(token, methodVersion: 0);
            ISymbolMethod methodSymbol = pdbReader.SymbolReader.GetMethod(symbolToken);
            if (bytes == null && methodSymbol == null)
            {
                // no debug info for the method
                return;
            }

            writer.WriteStartElement("method");
            WriteMethodAttributes(token, isReference: false);

            if (bytes != null)
            {
                WriteCustomDebugInfo(bytes);
            }

            if (methodSymbol != null)
            {
                WriteSequencePoints(methodSymbol);

                // TODO (tomat): Ideally this would be done in a separate test helper, not in PdbToXml.
                // verify ISymUnmanagedMethod APIs:
                ISymUnmanagedMethod rawMethod = pdbReader.RawSymbolReader.GetBaselineMethod(token);
                Debug.Assert(rawMethod != null, "How did we get an ISymbolMethod without a backing ISymUnmanagedMethod?");

                var expectedSlotNames = new Dictionary<int, ImmutableArray<string>>();
                WriteLocals(rawMethod, expectedSlotNames);

                var actualSlotNames = rawMethod.GetLocalVariableSlots();

                Debug.Assert(actualSlotNames.Length == (expectedSlotNames.Count == 0 ? 0 : expectedSlotNames.Keys.Max() + 1));

                int i = 0;
                foreach (var slotName in actualSlotNames)
                {
                    if (slotName == null)
                    {
                        Debug.Assert(!expectedSlotNames.ContainsKey(i));
                    }
                    else
                    {
                        Debug.Assert(expectedSlotNames[i].Contains(slotName));
                    }

                    i++;
                }

                ImmutableArray<ISymUnmanagedScope> children = rawMethod.GetRootScope().GetScopes();
                if (children.Length != 0)
                {
                    WriteScopes(children[0]);
                }

                WriteAsyncInfo(methodSymbol as ISymbolAsyncMethod);
            }

            writer.WriteEndElement(); // method
        }

        /// <summary>
        /// Given a byte array of custom debug info, parse the array and write out XML describing
        /// its structure and contents.
        /// </summary>
        private void WriteCustomDebugInfo(byte[] bytes)
        {
            int offset = 0;

            writer.WriteStartElement("customDebugInfo");

            byte globalVersion;
            byte globalCount;
            CDI.ReadGlobalHeader(bytes, ref offset, out globalVersion, out globalCount);

            writer.WriteAttributeString("version", globalVersion.ToString());
            writer.WriteAttributeString("count", globalCount.ToString());

            while (offset < bytes.Length)
            {
                byte version;
                CustomDebugInfoKind kind;
                int size;
                CDI.ReadRecordHeader(bytes, ref offset, out version, out kind, out size);

                if (version != CDIC.CdiVersion)
                {
                    WriteUnknownCustomDebugInfo(version, kind, size, bytes, ref offset);
                }
                else
                {
                    switch (kind)
                    {
                        case CustomDebugInfoKind.UsingInfo:
                            WriteUsingCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                        case CustomDebugInfoKind.ForwardInfo:
                            WriteForwardCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                        case CustomDebugInfoKind.ForwardToModuleInfo:
                            WriteForwardToModuleCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                        case CustomDebugInfoKind.StateMachineHoistedLocalScopes:
                            WriteStatemachineHoistedLocalScopesCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                        case CustomDebugInfoKind.ForwardIterator:
                            WriteForwardIteratorCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                        case CustomDebugInfoKind.DynamicLocals:
                            WriteDynamicLocalsCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                        case CustomDebugInfoKind.EditAndContinueLocalSlotMap:
                            WriteEditAndContinueLocalSlotMap(version, kind, size, bytes, ref offset);
                            break;
                        default:
                            WriteUnknownCustomDebugInfo(version, kind, size, bytes, ref offset);
                            break;
                    }
                }
            }

            writer.WriteEndElement(); //customDebugInfo
        }

        /// <summary>
        /// Write version, kind, and size attributes.
        /// </summary>
        private void WriteCustomDebugInfoRecordHeaderAttributes(byte version, CustomDebugInfoKind kind, int size)
        {
            writer.WriteAttributeString("version", version.ToString());
            writer.WriteAttributeString("kind", kind.ToString());
            writer.WriteAttributeString("size", size.ToString());
        }

        /// <summary>
        /// If the custom debug info is in a format that we don't understand, then we will
        /// just print a standard record header followed by the rest of the record as a
        /// single hex string.
        /// </summary>
        private void WriteUnknownCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            writer.WriteStartElement("unknown");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            ImmutableArray<byte> body;
            CDI.ReadRawRecordBody(bytes, ref offset, size, out body);

            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;
            foreach (byte b in body)
            {
                builder.AppendFormat("{0:X2}", b);
            }
            writer.WriteAttributeString("payload", pooled.ToStringAndFree());

            writer.WriteEndElement(); //unknown
        }

        /// <summary>
        /// For each namespace declaration enclosing a method (innermost-to-outermost), there is a count
        /// of the number of imports in that declaration.
        /// </summary>
        /// <remarks>
        /// There's always at least one entry (for the global namespace).
        /// </remarks>
        private void WriteUsingCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.UsingInfo);

            writer.WriteStartElement("using");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            ImmutableArray<short> counts;
            CDI.ReadUsingRecord(bytes, ref offset, size, out counts);

            writer.WriteAttributeString("namespaceCount", counts.Length.ToString());

            foreach (short importCount in counts)
            {
                writer.WriteStartElement("namespace");
                writer.WriteAttributeString("usingCount", importCount.ToString());
                writer.WriteEndElement(); //namespace
            }

            writer.WriteEndElement(); //using
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Emitting tokens makes tests more fragile.
        /// </remarks>
        private void WriteForwardCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.ForwardInfo);

            writer.WriteStartElement("forward");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            int token;
            CDI.ReadForwardRecord(bytes, ref offset, size, out token);
            WriteMethodAttributes(token, isReference: true);

            writer.WriteEndElement(); //forward
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when there are extern aliases and edit-and-continue is disabled.
        /// Emitting tokens makes tests more fragile.
        /// </remarks>
        private void WriteForwardToModuleCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.ForwardToModuleInfo);

            writer.WriteStartElement("forwardToModule");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            int token;
            CDI.ReadForwardRecord(bytes, ref offset, size, out token);
            WriteMethodAttributes(token, isReference: true);

            writer.WriteEndElement(); //forwardToModule
        }

        /// <summary>
        /// Appears when iterator locals have to lifted into fields.  Contains a list of buckets with
        /// start and end offsets (presumably, into IL).
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when there are locals in iterator methods.
        /// </remarks>
        private void WriteStatemachineHoistedLocalScopesCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.StateMachineHoistedLocalScopes);

            writer.WriteStartElement("hoistedLocalScopes");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            ImmutableArray<StateMachineHoistedLocalScope> scopes;
            CDI.ReadStateMachineHoistedLocalScopesRecord(bytes, ref offset, size, out scopes);

            writer.WriteAttributeString("count", scopes.Length.ToString());

            foreach (StateMachineHoistedLocalScope scope in scopes)
            {
                writer.WriteStartElement("slot");
                writer.WriteAttributeString("startOffset", AsILOffset(scope.StartOffset));
                writer.WriteAttributeString("endOffset", AsILOffset(scope.EndOffset));
                writer.WriteEndElement(); //bucket
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Contains a name string.
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when are iterator methods.
        /// </remarks>
        private void WriteForwardIteratorCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.ForwardIterator);

            writer.WriteStartElement("forwardIterator");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            string name;
            CDI.ReadForwardIteratorRecord(bytes, ref offset, size, out name);

            writer.WriteAttributeString("name", name);

            writer.WriteEndElement(); //forwardIterator
        }

        /// <summary>
        /// Contains a list of buckets, each of which contains a number of flags, a slot ID, and a name.
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when there are dynamic locals.
        /// </remarks>
        private void WriteDynamicLocalsCustomDebugInfo(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.DynamicLocals);

            writer.WriteStartElement("dynamicLocals");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            ImmutableArray<DynamicLocalBucket> buckets;
            CDI.ReadDynamicLocalsRecord(bytes, ref offset, size, out buckets);

            writer.WriteAttributeString("bucketCount", buckets.Length.ToString());

            foreach (DynamicLocalBucket bucket in buckets)
            {
                ulong flags = bucket.Flags;
                int flagCount = bucket.FlagCount;

                PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
                StringBuilder flagsBuilder = pooled.Builder;
                for (int f = 0; f < flagCount; f++)
                {
                    flagsBuilder.Append((flags >> f) & 1UL);
                }

                writer.WriteStartElement("bucket");
                writer.WriteAttributeString("flagCount", flagCount.ToString());
                writer.WriteAttributeString("flags", pooled.ToStringAndFree());
                writer.WriteAttributeString("slotId", bucket.SlotId.ToString());
                writer.WriteAttributeString("localName", bucket.Name);
                writer.WriteEndElement(); //bucket
            }

            writer.WriteEndElement(); //dynamicLocals
        }

        private unsafe void WriteEditAndContinueLocalSlotMap(byte version, CustomDebugInfoKind kind, int size, byte[] bytes, ref int offset)
        {
            Debug.Assert(kind == CustomDebugInfoKind.EditAndContinueLocalSlotMap);

            writer.WriteStartElement("encLocalSlotMap");

            WriteCustomDebugInfoRecordHeaderAttributes(version, kind, size);

            int bodySize = size - CDIC.CdiRecordHeaderSize;
            int syntaxOffsetBaseline = -1;

            fixed (byte* compressedSlotMapPtr = &bytes[offset])
            {
                var blobReader = new BlobReader(compressedSlotMapPtr, bodySize);

                while (blobReader.RemainingBytes > 0)
                {
                    byte b = blobReader.ReadByte();

                    if (b == 0xff)
                    {
                        break;
                    }

                    if (b == 0xfe)
                    {
                        syntaxOffsetBaseline = -blobReader.ReadCompressedInteger();
                        writer.WriteElementString("baseline", syntaxOffsetBaseline.ToString());
                        continue;
                    }

                    writer.WriteStartElement("slot");

                    if (b == 0)
                    {
                        // short-lived temp, no info
                        writer.WriteAttributeString("kind", "temp");
                    }
                    else
                    {
                        int synthesizedKind = (b & 0x3f) - 1;
                        bool hasOrdinal = (b & (1 << 7)) != 0;

                        int syntaxOffset;
                        bool badSyntaxOffset = !blobReader.TryReadCompressedInteger(out syntaxOffset);
                        syntaxOffset += syntaxOffsetBaseline;

                        int ordinal = 0;
                        bool badOrdinal = hasOrdinal && !blobReader.TryReadCompressedInteger(out ordinal);

                        writer.WriteAttributeString("kind", synthesizedKind.ToString());
                        writer.WriteAttributeString("offset", badSyntaxOffset ? "?" : syntaxOffset.ToString());

                        if (badOrdinal || hasOrdinal)
                        {
                            writer.WriteAttributeString("ordinal", badOrdinal ? "?" : ordinal.ToString());
                        }
                    }

                    writer.WriteEndElement();
                }
            }

            offset += bodySize;
            writer.WriteEndElement(); //encLocalSlotMap
        }

        private void WriteScopes(ISymUnmanagedScope scope)
        {
            writer.WriteStartElement("scope");
            {
                writer.WriteAttributeString("startOffset", AsILOffset(scope.GetStartOffset()));
                writer.WriteAttributeString("endOffset", AsILOffset(scope.GetEndOffset()));
                {
                    foreach (ISymUnmanagedNamespace @namespace in scope.GetNamespaces())
                    {
                        WriteNamespace(@namespace);
                    }

                    WriteLocalsHelper(scope, slotNames: null, includeChildScopes: false);
                }
                foreach (ISymUnmanagedScope child in scope.GetScopes())
                {
                    WriteScopes(child);
                }
            }
            writer.WriteEndElement(); // </scope>
        }

        private void WriteNamespace(ISymUnmanagedNamespace @namespace)
        {
            string rawName = @namespace.GetName();

            string alias;
            string externAlias;
            string target;
            ImportTargetKind kind;
            ImportScope scope;

            try
            {
                if (rawName.Length == 0)
                {
                    externAlias = null;
                    var parsingSucceeded = CDI.TryParseVisualBasicImportString(rawName, out alias, out target, out kind, out scope);
                    Debug.Assert(parsingSucceeded);
                }
                else
                {
                    switch (rawName[0])
                    {
                        case 'U':
                        case 'A':
                        case 'X':
                        case 'Z':
                        case 'E':
                        case 'T':
                            scope = ImportScope.Unspecified;
                            if (!CDI.TryParseCSharpImportString(rawName, out alias, out externAlias, out target, out kind))
                            {
                                throw new InvalidOperationException(string.Format("Invalid import '{0}'", rawName));
                            }
                            break;

                        default:
                            externAlias = null;
                            if (!CDI.TryParseVisualBasicImportString(rawName, out alias, out target, out kind, out scope))
                            {
                                throw new InvalidOperationException(string.Format("Invalid import '{0}'", rawName));
                            }
                            break;
                    }
                }
            }
            catch (ArgumentException) // TODO: filter
            {
                if ((options & PdbToXmlOptions.ThrowOnError) != 0)
                {
                    throw;
                }

                writer.WriteStartElement("invalid-custom-data");
                writer.WriteAttributeString("raw", rawName);
                writer.WriteEndElement();
                return;
            }

            switch (kind)
            {
                case ImportTargetKind.CurrentNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    writer.WriteStartElement("currentnamespace");
                    writer.WriteAttributeString("name", target);
                    writer.WriteEndElement(); // </currentnamespace>
                    break;
                case ImportTargetKind.DefaultNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    writer.WriteStartElement("defaultnamespace");
                    writer.WriteAttributeString("name", target);
                    writer.WriteEndElement(); // </defaultnamespace>
                    break;
                case ImportTargetKind.MethodToken:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    int token = Convert.ToInt32(target);
                    writer.WriteStartElement("importsforward");
                    WriteMethodAttributes(token, isReference: true);
                    writer.WriteEndElement(); // </importsforward>
                    break;
                case ImportTargetKind.XmlNamespace:
                    Debug.Assert(externAlias == null);
                    writer.WriteStartElement("xmlnamespace");
                    writer.WriteAttributeString("prefix", alias);
                    writer.WriteAttributeString("name", target);
                    WriteScopeAttribute(scope);
                    writer.WriteEndElement(); // </xmlnamespace>
                    break;
                case ImportTargetKind.NamespaceOrType:
                    Debug.Assert(externAlias == null);
                    writer.WriteStartElement("alias");
                    writer.WriteAttributeString("name", alias);
                    writer.WriteAttributeString("target", target);
                    writer.WriteAttributeString("kind", "namespace"); // Strange, but retaining to avoid breaking tests.
                    WriteScopeAttribute(scope);
                    writer.WriteEndElement(); // </alias>
                    break;
                case ImportTargetKind.Namespace:
                    if (alias != null)
                    {
                        writer.WriteStartElement("alias");
                        writer.WriteAttributeString("name", alias);
                        if (externAlias != null) writer.WriteAttributeString("qualifier", externAlias);
                        writer.WriteAttributeString("target", target);
                        writer.WriteAttributeString("kind", "namespace");
                        Debug.Assert(scope == ImportScope.Unspecified); // Only C# hits this case.
                        writer.WriteEndElement(); // </alias>
                    }
                    else
                    {
                        writer.WriteStartElement("namespace");
                        if (externAlias != null) writer.WriteAttributeString("qualifier", externAlias);
                        writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        writer.WriteEndElement(); // </namespace>
                    }
                    break;
                case ImportTargetKind.Type:
                    Debug.Assert(externAlias == null);
                    if (alias != null)
                    {
                        writer.WriteStartElement("alias");
                        writer.WriteAttributeString("name", alias);
                        writer.WriteAttributeString("target", target);
                        writer.WriteAttributeString("kind", "type");
                        Debug.Assert(scope == ImportScope.Unspecified); // Only C# hits this case.
                        writer.WriteEndElement(); // </alias>
                    }
                    else
                    {
                        writer.WriteStartElement("type");
                        writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        writer.WriteEndElement(); // </type>
                    }
                    break;
                case ImportTargetKind.Assembly:
                    Debug.Assert(alias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    if (target == null)
                    {
                        writer.WriteStartElement("extern");
                        writer.WriteAttributeString("alias", externAlias);
                        writer.WriteEndElement(); // </extern>
                    }
                    else
                    {
                        writer.WriteStartElement("externinfo");
                        writer.WriteAttributeString("alias", externAlias);
                        writer.WriteAttributeString("assembly", target);
                        writer.WriteEndElement(); // </externinfo>
                    }
                    break;
                case ImportTargetKind.Defunct:
                    Debug.Assert(alias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    writer.WriteStartElement("defunct");
                    writer.WriteAttributeString("name", rawName);
                    writer.WriteEndElement(); // </defunct>
                    break;
                default:
                    Debug.Assert(false, "Unexpected import kind '" + kind + "'");
                    writer.WriteStartElement("unknown");
                    writer.WriteAttributeString("name", rawName);
                    writer.WriteEndElement(); // </unknown>
                    break;
            }
        }

        private void WriteScopeAttribute(ImportScope scope)
        {
            if (scope == ImportScope.File)
            {
                writer.WriteAttributeString("importlevel", "file");
            }
            else if (scope == ImportScope.Project)
            {
                writer.WriteAttributeString("importlevel", "project");
            }
            else
            {
                Debug.Assert(scope == ImportScope.Unspecified, "Unexpected scope '" + scope + "'");
            }
        }

        private void WriteAsyncInfo(ISymbolAsyncMethod method)
        {
            if (method != null)
            {
                if (method.IsAsyncMethod)
                {
                    writer.WriteStartElement("async-info");

                    var catchOffset = method.CatchHandlerOffset;
                    if (catchOffset >= 0)
                    {
                        writer.WriteAttributeString("catch-IL-offset", AsILOffset(catchOffset));
                    }

                    writer.WriteStartElement("kickoff-method");
                    WriteMethodAttributes((int)method.KickoffMethod, isReference: true);
                    writer.WriteEndElement();

                    foreach (var info in method.GetAsyncStepInfos())
                    {
                        writer.WriteStartElement("await");
                        writer.WriteAttributeString("yield", AsILOffset(info.YieldOffset));
                        writer.WriteAttributeString("resume", AsILOffset(info.BreakpointOffset));
                        WriteMethodAttributes((int)info.BreakpointMethod, isReference: true);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }
        }

        private static ConstructorInfo ConstructorOfISymbolScope = null;
        private static ISymbolScope WrapRawScope(ISymUnmanagedScope rawScope)
        {
            if (ConstructorOfISymbolScope == null)
            {
                // NOTE: We have the sources for this type, so we can just make the constructor
                // public if reflection proves to be too expensive.
                var assembly = typeof(SymbolBinder).Assembly;
                var type = assembly.GetType("Microsoft.Samples.Debugging.CorSymbolStore.SymScope", throwOnError: true);
                var ctor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
                Interlocked.CompareExchange(ref ConstructorOfISymbolScope, ctor, null);
            }

            return (ISymbolScope)ConstructorOfISymbolScope.Invoke(new object[] { rawScope });
        }

        // Write all the locals in the given method out to an XML file.
        // Since the symbol store represents the locals in a recursive scope structure, we need to walk a tree.
        // Although the locals are technically a hierarchy (based off nested scopes), it's easiest for clients
        // if we present them as a linear list. We will provide the range for each local's scope so that somebody
        // could reconstruct an approximation of the scope tree. The reconstruction may not be exact.
        // (Note this would still break down if you had an empty scope nested in another scope.
        private void WriteLocals(ISymUnmanagedMethod method, Dictionary<int, ImmutableArray<string>> slotNames)
        {
            writer.WriteStartElement("locals");
            {
                // If there are no locals, then this element will just be empty.
                WriteLocalsHelper(method.GetRootScope(), slotNames, includeChildScopes: true);
            }
            writer.WriteEndElement();
        }

        // Helper method to write the local variables in the given scope.
        // Scopes match an IL range, and also have child scopes.
        private void WriteLocalsHelper(ISymUnmanagedScope rawScope, Dictionary<int, ImmutableArray<string>> slotNames, bool includeChildScopes)
        {
            WriteLocalsHelper(WrapRawScope(rawScope), slotNames, includeChildScopes);
        }

        private void WriteLocalsHelper(ISymbolScope scope, Dictionary<int, ImmutableArray<string>> slotNames, bool includeChildScopes)
        {
            foreach (ISymbolVariable l in scope.GetLocals())
            {
                writer.WriteStartElement("local");
                {
                    writer.WriteAttributeString("name", l.Name);

                    // Each local maps to a "IL Index" or "slot" number. 
                    // The index is not necessarily unique. Several locals may refer to the same slot. 
                    // It just means that the same local is known under different names inside the same or different scopes.
                    // This index is what you pass to ICorDebugILFrame::GetLocalVariable() to get
                    // a specific local variable. 
                    // NOTE: VB emits "fake" locals for resumable locals which are actually backed by fields.
                    //       These locals always map to the slot #0 which is just a valid number that is 
                    //       not used. Only scoping information is used by EE in this case.
                    Debug.Assert(l.AddressKind == SymAddressKind.ILOffset);
                    int slot = l.AddressField1;
                    writer.WriteAttributeString("il_index", CultureInvariantToString(slot));

                    bool reusingSlot = false;

                    // collect slot names so that we can verify ISymUnmanagedReader APIs
                    if (slotNames != null)
                    {
                        ImmutableArray<string> existingNames;
                        if (slotNames.TryGetValue(slot, out existingNames))
                        {
                            slotNames[slot] = existingNames.Add(l.Name);
                            reusingSlot = true;
                        }
                        else
                        {
                            slotNames.Add(slot, ImmutableArray.Create(l.Name));
                        }
                    }

                    // Provide scope range
                    writer.WriteAttributeString("il_start", AsILOffset(scope.StartOffset));
                    writer.WriteAttributeString("il_end", AsILOffset(scope.EndOffset));
                    writer.WriteAttributeString("attributes", l.Attributes.ToString());

                    if (reusingSlot)
                    {
                        writer.WriteAttributeString("reusingslot", reusingSlot.ToString(CultureInfo.InvariantCulture));
                    }
                }
                writer.WriteEndElement(); // </local>
            }

            foreach (ISymbolConstant c in ((ISymbolScope2)scope).GetConstants())
            {
                // Note: We can retrieve constant tokens by saving it into signature blob
                // in our implementation of IMetadataImport.GetSigFromToken.
                writer.WriteStartElement("constant");
                {
                    writer.WriteAttributeString("name", c.GetName());
                    
                    object value = c.GetValue();
                    string typeName = value.GetType().Name;

                    // certain Unicode characters will give Xml writers fits...in order to avoid this, we'll replace
                    // problematic characters/sequences with their hexadecimal equivalents, like U+0000, etc...
                    var chars = value as string;
                    if (chars != null)
                    {
                        PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
                        var valueWithPlaceholders = pooled.Builder;
                        foreach (var ch in chars)
                        {
                            // if we end up with more, we can add them here
                            if (0 == (int)ch)
                            {
                                valueWithPlaceholders.AppendFormat("U+{0:X4}", (int)ch);
                            }
                            else
                            {
                                valueWithPlaceholders.Append(ch);
                            }
                        }
                        if (valueWithPlaceholders.Length > chars.Length)
                        {
                            value = valueWithPlaceholders.ToString();
                        }
                        pooled.Free();
                    }

                    writer.WriteAttributeString("value", value.ToString());
                    writer.WriteAttributeString("type", typeName);
                }
                writer.WriteEndElement(); // </constant>
            }
            if (includeChildScopes)
            {
                foreach (ISymbolScope childScope in scope.GetChildren())
                {
                    WriteLocalsHelper(childScope, slotNames, includeChildScopes);
                }
            }
        }

        // Write the sequence points for the given method
        // Sequence points are the map between IL offsets and source lines.
        // A single method could span multiple files (use C#'s #line directive to see for yourself).        
        private void WriteSequencePoints(ISymbolMethod method)
        {
            writer.WriteStartElement("sequencepoints");

            int count = method.SequencePointCount;
            writer.WriteAttributeString("total", CultureInvariantToString(count));

            // Get the sequence points from the symbol store. 
            // We could cache these arrays and reuse them.
            int[] offsets = new int[count];
            ISymbolDocument[] docs = new ISymbolDocument[count];
            int[] startColumn = new int[count];
            int[] endColumn = new int[count];
            int[] startRow = new int[count];
            int[] endRow = new int[count];
            method.GetSequencePoints(offsets, docs, startRow, startColumn, endRow, endColumn);

            // Write out sequence points
            for (int i = 0; i < count; i++)
            {
                writer.WriteStartElement("entry");
                writer.WriteAttributeString("il_offset", AsILOffset(offsets[i]));

                // If it's a special 0xFeeFee sequence point (eg, "hidden"), 
                // place an attribute on it to make it very easy for tools to recognize.
                // See http://blogs.msdn.com/jmstall/archive/2005/06/19/FeeFee_SequencePoints.aspx
                if (startRow[i] == 0xFeeFee)
                {
                    writer.WriteAttributeString("hidden", XmlConvert.ToString(true));
                }

                writer.WriteAttributeString("start_row", CultureInvariantToString(startRow[i]));
                writer.WriteAttributeString("start_column", CultureInvariantToString(startColumn[i]));
                writer.WriteAttributeString("end_row", CultureInvariantToString(endRow[i]));
                writer.WriteAttributeString("end_column", CultureInvariantToString(endColumn[i]));
                //EDMAURER allow there to be PDBs generated for sources that don't have a name (document).
                int fileRefVal = -1;
                this.m_fileMapping.TryGetValue(docs[i].URL, out fileRefVal);
                writer.WriteAttributeString("file_ref", CultureInvariantToString(fileRefVal));

                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // sequencepoints
        }

        // Write all docs, and add to the m_fileMapping list.
        // Other references to docs will then just refer to this list.
        private void WriteDocList()
        {
            ISymbolDocument[] docs = pdbReader.SymbolReader.GetDocuments();
            if (docs.Length == 0)
            {
                return;
            }

            int id = 0;
            writer.WriteStartElement("files");
            foreach (ISymbolDocument doc in docs)
            {
                string url = doc.URL;

                // Symbol store may give out duplicate documents. We'll fold them here
                if (m_fileMapping.ContainsKey(url))
                {
                    writer.WriteComment("There is a duplicate entry for: " + url);
                    continue;
                }

                id++;
                m_fileMapping.Add(doc.URL, id);

                writer.WriteStartElement("file");
                {
                    writer.WriteAttributeString("id", CultureInvariantToString(id));
                    writer.WriteAttributeString("name", doc.URL);
                    writer.WriteAttributeString("language", doc.Language.ToString());
                    writer.WriteAttributeString("languageVendor", doc.LanguageVendor.ToString());
                    writer.WriteAttributeString("documentType", doc.DocumentType.ToString());

                    var checkSum = string.Concat(doc.GetCheckSum().Select(b => string.Format("{0,2:X}", b) + ", ")) ;

                    if (!string.IsNullOrEmpty(checkSum))
                    {
                        writer.WriteAttributeString("checkSumAlgorithmId", doc.CheckSumAlgorithmId.ToString());
                        writer.WriteAttributeString("checkSum", checkSum);
                    }
                }

                writer.WriteEndElement(); // file
            }
            writer.WriteEndElement(); // files
        }

        private void WriteAllMethodSpans()
        {
            writer.WriteStartElement("method-spans");

            var symReader2 = (ISymUnmanagedReader2)pdbReader.RawSymbolReader;
            foreach (ISymUnmanagedDocument doc in GetDocuments(pdbReader.RawSymbolReader))
            {
                foreach (ISymUnmanagedMethod m in GetMethodsInDocument(symReader2, doc))
                {
                    var menc = (ISymENCUnmanagedMethod)m;
                    writer.WriteStartElement("method");

                    int token;
                    int hr = m.GetToken(out token);
                    SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
                    WriteMethodAttributes(token, isReference: true);

                    foreach (var mdoc in GetDocumentsForMethod(menc))
                    {
                        writer.WriteStartElement("document");
                        
                        int startLine, endLine;
                        menc.GetSourceExtentInDocument(mdoc, out startLine, out endLine);

                        writer.WriteAttributeString("startLine", startLine.ToString());
                        writer.WriteAttributeString("endLine", endLine.ToString());

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        private static ISymUnmanagedDocument[] GetDocumentsForMethod(ISymENCUnmanagedMethod symMethod)
        {
            int count = symMethod.GetDocumentsForMethodCount();

            var result = new ISymUnmanagedDocument[count];
            symMethod.GetDocumentsForMethod(count, out count, result);

            return result;
        }

        private static ISymUnmanagedDocument[] GetDocuments(ISymUnmanagedReader symReader)
        {
            int count;
            int hr = symReader.GetDocuments(0, out count, null);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);

            var result = new ISymUnmanagedDocument[count];
            hr = symReader.GetDocuments(count, out count, result);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);

            return result;
        }

        private static ISymUnmanagedMethod[] GetMethodsInDocument(ISymUnmanagedReader2 symReader, ISymUnmanagedDocument symDocument)
        {
            int count;
            int hr = symReader.GetMethodsInDocument(symDocument, 0, out count, null);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);

            var result = new ISymUnmanagedMethod[count];
            hr = symReader.GetMethodsInDocument(symDocument, count, out count, result);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);

            return result;
        }

        // Write out a reference to the entry point method (if one exists)
        private void WriteEntryPoint()
        {
            // If there is no entry point token (such as in a dll), this will throw.
            SymbolToken token = pdbReader.SymbolReader.UserEntryPoint;
            int rawToken = token.GetToken();
            if (rawToken == 0)
            {
                // If the Symbol APIs fail when looking for an entry point token, there is no entry point.
                // m_writer.WriteComment(
                //     "There is no entry point token such as a 'Main' method. This module is probably a '.dll'");
                return;
            }

            // Should not throw past this point
            //m_writer.WriteComment(
            //    "This is the token for the 'entry point' method, which is the method that will be called when the assembly is loaded." +
            //    " This usually corresponds to 'Main'");

            writer.WriteStartElement("entryPoint");
            WriteMethodAttributes(rawToken, isReference: true);
            writer.WriteEndElement();

        }

        // Write out XML snippet to refer to the given method.
        private void WriteMethodAttributes(int token, bool isReference)
        {
            if ((options & PdbToXmlOptions.ResolveTokens) != 0)
            {
                var handle = MetadataTokens.Handle(token);

                try
                {
                    switch (handle.Kind)
                    {
                        case HandleKind.MethodDefinition:
                            WriteResolvedToken((MethodDefinitionHandle)handle, isReference);
                            break;

                        case HandleKind.MemberReference:
                            WriteResolvedToken((MemberReferenceHandle)handle);
                            break;

                        default:
                            WriteToken(token);
                            writer.WriteAttributeString("error", string.Format("Unexpected token type: {0}", handle.Kind));
                            break;
                    }
                }
                catch (BadImageFormatException e) // TODO: filter
                {
                    if ((options & PdbToXmlOptions.ThrowOnError) != 0)
                    {
                        throw;
                    }

                    WriteToken(token);
                    writer.WriteAttributeString("metadata-error", e.Message);
                }
            }

            if ((options & PdbToXmlOptions.IncludeTokens) != 0)
            {
                WriteToken(token);
            }
        }

        private static string GetQualifiedMethodName(MetadataReader metadataReader, MethodDefinitionHandle methodHandle)
        {
            var method = metadataReader.GetMethodDefinition(methodHandle);
            var containingTypeHandle = method.GetDeclaringType();

            string fullTypeName = GetFullTypeName(metadataReader, containingTypeHandle);
            string methodName = metadataReader.GetString(method.Name);

            return fullTypeName != null ? fullTypeName + "." + methodName : methodName;
        }

        private void WriteResolvedToken(MethodDefinitionHandle methodHandle, bool isReference)
        {
            var method = metadataReader.GetMethodDefinition(methodHandle);

            // type name
            var containingTypeHandle = method.GetDeclaringType();
            var fullName = GetFullTypeName(metadataReader, containingTypeHandle);
            if (fullName != null)
            {
                writer.WriteAttributeString(isReference ? "declaringType" :  "containingType", fullName);
            }

            // method name
            writer.WriteAttributeString(isReference ? "methodName" : "name", metadataReader.GetString(method.Name));

            // parameters:
            var parameterNames = from paramHandle in method.GetParameters()
                                 let parameter = metadataReader.GetParameter(paramHandle)
                                 where parameter.SequenceNumber > 0 // exclude return parameter
                                 select parameter.Name.IsNil ? "?" : metadataReader.GetString(parameter.Name);

            writer.WriteAttributeString("parameterNames", string.Join(", ", parameterNames));           
        }

        private void WriteResolvedToken(MemberReferenceHandle memberRefHandle)
        {
            var memberRef = metadataReader.GetMemberReference(memberRefHandle);

            // type name
            string fullName = GetFullTypeName(metadataReader, memberRef.Parent);
            if (fullName != null)
            {
                writer.WriteAttributeString("declaringType", fullName);
            }

            // method name
            writer.WriteAttributeString("methodName", metadataReader.GetString(memberRef.Name));
        }

        private static bool IsNested(TypeAttributes flags)
        {
            return (flags & ((TypeAttributes)0x00000006)) != 0;
        }

        private static string GetFullTypeName(MetadataReader metadataReader, Handle handle)
        {
            if (handle.IsNil)
            {
                return null;
            }

            if (handle.Kind == HandleKind.TypeDefinition)
            {
                var type = metadataReader.GetTypeDefinition((TypeDefinitionHandle)handle);
                string name = metadataReader.GetString(type.Name);

                while (IsNested(type.Attributes))
                {
                    var enclosingType = metadataReader.GetTypeDefinition(type.GetDeclaringType());
                    name = metadataReader.GetString(enclosingType.Name) + "+" + name;
                    type = enclosingType;
                }

                if (type.Namespace.IsNil)
                {
                    return name;
                }

                return metadataReader.GetString(type.Namespace) + "." + name;
            }

            if (handle.Kind == HandleKind.TypeReference)
            {
                var typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)handle);
                string name = metadataReader.GetString(typeRef.Name);
                if (typeRef.Namespace.IsNil)
                {
                    return name;
                }

                return metadataReader.GetString(typeRef.Namespace) + "." + name;
            }

            return string.Format("<unexpected token kind: {0}>", AsToken(metadataReader.GetToken(handle)));
        }

        #region Utils

        private void WriteToken(int token)
        {
            writer.WriteAttributeString("token", AsToken(token));
        }

        internal static string AsToken(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x}", i);
        }

        internal static string AsILOffset(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x}", i);
        }

        internal static SymbolToken AsSymToken(string token)
        {
            return new SymbolToken(ToInt32(token, 16));
        }

        internal static int ToInt32(string input)
        {
            return ToInt32(input, 10);
        }

        internal static int ToInt32(string input, int numberBase)
        {
            return Convert.ToInt32(input, numberBase);
        }

        internal static string ToHexString(byte[] input)
        {
            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooled.Builder;
            foreach (byte b in input)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return pooled.ToStringAndFree();
        }

        internal static byte[] ToByteArray(string input)
        {
            byte[] retval = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                retval[i] = Convert.ToByte(input[i]);
            }
            return retval;
        }

        internal static string CultureInvariantToString(int input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }

        internal static void Error(string message)
        {
            Console.WriteLine("Error: {0}", message);
            Debug.Assert(false, message);
        }

        #endregion
    }
}

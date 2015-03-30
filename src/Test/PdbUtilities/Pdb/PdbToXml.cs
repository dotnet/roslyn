﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.DiaSymReader;
using CDI = Microsoft.CodeAnalysis.CustomDebugInfoReader;
using CDIC = Microsoft.Cci.CustomDebugInfoConstants;
using PooledStringBuilder = Microsoft.CodeAnalysis.Collections.PooledStringBuilder;

namespace Roslyn.Test.PdbUtilities
{
    /// <summary>
    /// Class to write out XML for a PDB.
    /// </summary>
    public sealed class PdbToXmlConverter
    {
        // For printing integers in a standard hex format.
        private const string IntHexFormat = "0x{0:X}";

        private readonly MetadataReader _metadataReader;
        private readonly ISymUnmanagedReader _symReader;
        private readonly PdbToXmlOptions _options;
        private readonly XmlWriter _writer;

        // Maps files to ids. 
        private readonly Dictionary<string, int> _fileMapping = new Dictionary<string, int>();

        private PdbToXmlConverter(XmlWriter writer, ISymUnmanagedReader symReader, MetadataReader metadataReader, PdbToXmlOptions options)
        {
            _symReader = symReader;
            _metadataReader = metadataReader;
            _writer = writer;
            _options = options;
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

            using (SymReader symReader = new SymReader(pdbStream))
            {
                var converter = new PdbToXmlConverter(writer, symReader, metadataReaderOpt, options);

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
            _writer.WriteStartDocument();

            _writer.WriteStartElement("symbols");

            WriteDocList();
            WriteEntryPoint();
            WriteAllMethods(methodHandles);

            if ((_options & PdbToXmlOptions.IncludeMethodSpans) != 0)
            {
                WriteAllMethodSpans();
            }

            _writer.WriteEndElement();
        }

        // Dump all of the methods in the given ISymbolReader to the XmlWriter provided in the ctor.
        private void WriteAllMethods(IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            _writer.WriteStartElement("methods");

            foreach (var methodHandle in methodHandles)
            {
                WriteMethod(methodHandle);
            }

            _writer.WriteEndElement();
        }

        private void WriteMethod(MethodDefinitionHandle methodHandle)
        {
            int token = _metadataReader.GetToken(methodHandle);

            byte[] cdi = _symReader.GetCustomDebugInfoBytes(token, methodVersion: 1);
            ISymUnmanagedMethod method = _symReader.GetMethod(token);
            if (cdi == null && method == null)
            {
                // no debug info for the method
                return;
            }

            _writer.WriteStartElement("method");
            WriteMethodAttributes(token, isReference: false);

            if (cdi != null)
            {
                WriteCustomDebugInfo(cdi);
            }

            if (method != null)
            {
                WriteSequencePoints(method);

                // TODO (tomat): Ideally this would be done in a separate test helper, not in PdbToXml.
                // verify ISymUnmanagedMethod APIs:
                var expectedSlotNames = new Dictionary<int, ImmutableArray<string>>();
                WriteLocals(method, expectedSlotNames);

                var actualSlotNames = method.GetLocalVariableSlots();

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

                ImmutableArray<ISymUnmanagedScope> children = method.GetRootScope().GetScopes();
                if (children.Length != 0)
                {
                    WriteScopes(children[0]);
                }

                WriteAsyncInfo(method);
            }

            _writer.WriteEndElement(); // method
        }

        /// <summary>
        /// Given a byte array of custom debug info, parse the array and write out XML describing
        /// its structure and contents.
        /// </summary>
        private void WriteCustomDebugInfo(byte[] bytes)
        {
            var records = CustomDebugInfoReader.GetCustomDebugInfoRecords(bytes).ToArray();

            _writer.WriteStartElement("customDebugInfo");

            foreach (var record in records)
            {
                if (record.Version != CDIC.CdiVersion)
                {
                    WriteUnknownCustomDebugInfo(record);
                }
                else
                {
                    switch (record.Kind)
                    {
                        case CustomDebugInfoKind.UsingInfo:
                            WriteUsingCustomDebugInfo(record);
                            break;
                        case CustomDebugInfoKind.ForwardInfo:
                            WriteForwardCustomDebugInfo(record);
                            break;
                        case CustomDebugInfoKind.ForwardToModuleInfo:
                            WriteForwardToModuleCustomDebugInfo(record);
                            break;
                        case CustomDebugInfoKind.StateMachineHoistedLocalScopes:
                            WriteStatemachineHoistedLocalScopesCustomDebugInfo(record);
                            break;
                        case CustomDebugInfoKind.ForwardIterator:
                            WriteForwardIteratorCustomDebugInfo(record);
                            break;
                        case CustomDebugInfoKind.DynamicLocals:
                            WriteDynamicLocalsCustomDebugInfo(record);
                            break;
                        case CustomDebugInfoKind.EditAndContinueLocalSlotMap:
                            WriteEditAndContinueLocalSlotMap(record);
                            break;
                        case CustomDebugInfoKind.EditAndContinueLambdaMap:
                            WriteEditAndContinueLambdaMap(record);
                            break;
                        default:
                            WriteUnknownCustomDebugInfo(record);
                            break;
                    }
                }
            }

            _writer.WriteEndElement(); //customDebugInfo
        }

        /// <summary>
        /// If the custom debug info is in a format that we don't understand, then we will
        /// just print a standard record header followed by the rest of the record as a
        /// single hex string.
        /// </summary>
        private void WriteUnknownCustomDebugInfo(CustomDebugInfoRecord record)
        {
            _writer.WriteStartElement("unknown");
            _writer.WriteAttributeString("kind", record.Kind.ToString());
            _writer.WriteAttributeString("version", record.Version.ToString());

            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;
            foreach (byte b in record.Data)
            {
                builder.AppendFormat("{0:X2}", b);
            }

            _writer.WriteAttributeString("payload", pooled.ToStringAndFree());

            _writer.WriteEndElement(); //unknown
        }

        /// <summary>
        /// For each namespace declaration enclosing a method (innermost-to-outermost), there is a count
        /// of the number of imports in that declaration.
        /// </summary>
        /// <remarks>
        /// There's always at least one entry (for the global namespace).
        /// </remarks>
        private void WriteUsingCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.UsingInfo);

            _writer.WriteStartElement("using");

            ImmutableArray<short> counts = CDI.DecodeUsingRecord(record.Data);

            foreach (short importCount in counts)
            {
                _writer.WriteStartElement("namespace");
                _writer.WriteAttributeString("usingCount", importCount.ToString());
                _writer.WriteEndElement(); //namespace
            }

            _writer.WriteEndElement(); //using
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Emitting tokens makes tests more fragile.
        /// </remarks>
        private void WriteForwardCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.ForwardInfo);

            _writer.WriteStartElement("forward");

            int token = CDI.DecodeForwardRecord(record.Data);
            WriteMethodAttributes(token, isReference: true);

            _writer.WriteEndElement(); //forward
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when there are extern aliases and edit-and-continue is disabled.
        /// Emitting tokens makes tests more fragile.
        /// </remarks>
        private void WriteForwardToModuleCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.ForwardToModuleInfo);

            _writer.WriteStartElement("forwardToModule");

            int token = CDI.DecodeForwardRecord(record.Data);
            WriteMethodAttributes(token, isReference: true);

            _writer.WriteEndElement(); //forwardToModule
        }

        /// <summary>
        /// Appears when iterator locals have to lifted into fields.  Contains a list of buckets with
        /// start and end offsets (presumably, into IL).
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when there are locals in iterator methods.
        /// </remarks>
        private void WriteStatemachineHoistedLocalScopesCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.StateMachineHoistedLocalScopes);

            _writer.WriteStartElement("hoistedLocalScopes");

            var scopes = CDI.DecodeStateMachineHoistedLocalScopesRecord(record.Data);

            foreach (StateMachineHoistedLocalScope scope in scopes)
            {
                _writer.WriteStartElement("slot");
                _writer.WriteAttributeString("startOffset", AsILOffset(scope.StartOffset));
                _writer.WriteAttributeString("endOffset", AsILOffset(scope.EndOffset));
                _writer.WriteEndElement(); //bucket
            }

            _writer.WriteEndElement();
        }

        /// <summary>
        /// Contains a name string.
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when are iterator methods.
        /// </remarks>
        private void WriteForwardIteratorCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.ForwardIterator);

            _writer.WriteStartElement("forwardIterator");

            string name = CDI.DecodeForwardIteratorRecord(record.Data);

            _writer.WriteAttributeString("name", name);

            _writer.WriteEndElement(); //forwardIterator
        }

        /// <summary>
        /// Contains a list of buckets, each of which contains a number of flags, a slot ID, and a name.
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when there are dynamic locals.
        /// </remarks>
        private void WriteDynamicLocalsCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.DynamicLocals);

            _writer.WriteStartElement("dynamicLocals");

            var buckets = CDI.DecodeDynamicLocalsRecord(record.Data);

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

                _writer.WriteStartElement("bucket");
                _writer.WriteAttributeString("flagCount", flagCount.ToString());
                _writer.WriteAttributeString("flags", pooled.ToStringAndFree());
                _writer.WriteAttributeString("slotId", bucket.SlotId.ToString());
                _writer.WriteAttributeString("localName", bucket.Name);
                _writer.WriteEndElement(); //bucket
            }

            _writer.WriteEndElement(); //dynamicLocals
        }

        private unsafe void WriteEditAndContinueLocalSlotMap(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.EditAndContinueLocalSlotMap);

            _writer.WriteStartElement("encLocalSlotMap");
            try
            {
                int syntaxOffsetBaseline = -1;

                fixed (byte* compressedSlotMapPtr = &record.Data.ToArray()[0])
                {
                    var blobReader = new BlobReader(compressedSlotMapPtr, record.Data.Length);

                    while (blobReader.RemainingBytes > 0)
                    {
                        byte b = blobReader.ReadByte();

                        if (b == 0xff)
                        {
                            if (!blobReader.TryReadCompressedInteger(out syntaxOffsetBaseline))
                            {
                                _writer.WriteElementString("baseline", "?");
                                return;
                            }

                            syntaxOffsetBaseline = -syntaxOffsetBaseline;
                            continue;
                        }

                        _writer.WriteStartElement("slot");

                        if (b == 0)
                        {
                            // short-lived temp, no info
                            _writer.WriteAttributeString("kind", "temp");
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

                            _writer.WriteAttributeString("kind", synthesizedKind.ToString());
                            _writer.WriteAttributeString("offset", badSyntaxOffset ? "?" : syntaxOffset.ToString());

                            if (badOrdinal || hasOrdinal)
                            {
                                _writer.WriteAttributeString("ordinal", badOrdinal ? "?" : ordinal.ToString());
                            }
                        }

                        _writer.WriteEndElement();
                    }
                }
            }
            finally
            {
                _writer.WriteEndElement(); //encLocalSlotMap
            }
        }

        private unsafe void WriteEditAndContinueLambdaMap(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.EditAndContinueLambdaMap);

            _writer.WriteStartElement("encLambdaMap");
            try
            {
                if (record.Data.Length == 0)
                {
                    return;
                }

                int methodOrdinal = -1;
                int syntaxOffsetBaseline = -1;
                int closureCount;

                fixed (byte* blobPtr = &record.Data.ToArray()[0])
                {
                    var blobReader = new BlobReader(blobPtr, record.Data.Length);

                    if (!blobReader.TryReadCompressedInteger(out methodOrdinal))
                    {
                        _writer.WriteElementString("methodOrdinal", "?");
                        _writer.WriteEndElement();
                        return;
                    }

                    // [-1, inf)
                    methodOrdinal--;
                    _writer.WriteElementString("methodOrdinal", methodOrdinal.ToString());

                    if (!blobReader.TryReadCompressedInteger(out syntaxOffsetBaseline))
                    {
                        _writer.WriteElementString("baseline", "?");
                        _writer.WriteEndElement();
                        return;
                    }

                    syntaxOffsetBaseline = -syntaxOffsetBaseline;
                    if (!blobReader.TryReadCompressedInteger(out closureCount))
                    {
                        _writer.WriteElementString("closureCount", "?");
                        _writer.WriteEndElement();
                        return;
                    }

                    for (int i = 0; i < closureCount; i++)
                    {
                        _writer.WriteStartElement("closure");
                        try
                        {
                            int syntaxOffset;
                            if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                            {
                                _writer.WriteElementString("offset", "?");
                                break;
                            }

                            _writer.WriteAttributeString("offset", (syntaxOffset + syntaxOffsetBaseline).ToString());
                        }
                        finally
                        {
                            _writer.WriteEndElement();
                        }
                    }

                    while (blobReader.RemainingBytes > 0)
                    {
                        _writer.WriteStartElement("lambda");
                        try
                        {
                            int syntaxOffset;
                            if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                            {
                                _writer.WriteElementString("offset", "?");
                                return;
                            }

                            _writer.WriteAttributeString("offset", (syntaxOffset + syntaxOffsetBaseline).ToString());

                            int closureOrdinal;
                            if (!blobReader.TryReadCompressedInteger(out closureOrdinal))
                            {
                                _writer.WriteElementString("closure", "?");
                                return;
                            }

                            closureOrdinal -= 2;

                            if (closureOrdinal == -2)
                            {
                                _writer.WriteAttributeString("closure", "this");
                            }
                            else if (closureOrdinal != -1)
                            {
                                _writer.WriteAttributeString("closure",
                                    closureOrdinal.ToString() + (closureOrdinal >= closureCount ? " (invalid)" : ""));
                            }
                        }
                        finally
                        {
                            _writer.WriteEndElement();
                        }
                    }
                }
            }
            finally
            {
                _writer.WriteEndElement(); //encLocalSlotMap
            }
        }

        private void WriteScopes(ISymUnmanagedScope scope)
        {
            _writer.WriteStartElement("scope");
            {
                _writer.WriteAttributeString("startOffset", AsILOffset(scope.GetStartOffset()));
                _writer.WriteAttributeString("endOffset", AsILOffset(scope.GetEndOffset()));
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
            _writer.WriteEndElement(); // </scope>
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
                if ((_options & PdbToXmlOptions.ThrowOnError) != 0)
                {
                    throw;
                }

                _writer.WriteStartElement("invalid-custom-data");
                _writer.WriteAttributeString("raw", rawName);
                _writer.WriteEndElement();
                return;
            }

            switch (kind)
            {
                case ImportTargetKind.CurrentNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    _writer.WriteStartElement("currentnamespace");
                    _writer.WriteAttributeString("name", target);
                    _writer.WriteEndElement(); // </currentnamespace>
                    break;
                case ImportTargetKind.DefaultNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    _writer.WriteStartElement("defaultnamespace");
                    _writer.WriteAttributeString("name", target);
                    _writer.WriteEndElement(); // </defaultnamespace>
                    break;
                case ImportTargetKind.MethodToken:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    int token = Convert.ToInt32(target);
                    _writer.WriteStartElement("importsforward");
                    WriteMethodAttributes(token, isReference: true);
                    _writer.WriteEndElement(); // </importsforward>
                    break;
                case ImportTargetKind.XmlNamespace:
                    Debug.Assert(externAlias == null);
                    _writer.WriteStartElement("xmlnamespace");
                    _writer.WriteAttributeString("prefix", alias);
                    _writer.WriteAttributeString("name", target);
                    WriteScopeAttribute(scope);
                    _writer.WriteEndElement(); // </xmlnamespace>
                    break;
                case ImportTargetKind.NamespaceOrType:
                    Debug.Assert(externAlias == null);
                    _writer.WriteStartElement("alias");
                    _writer.WriteAttributeString("name", alias);
                    _writer.WriteAttributeString("target", target);
                    _writer.WriteAttributeString("kind", "namespace"); // Strange, but retaining to avoid breaking tests.
                    WriteScopeAttribute(scope);
                    _writer.WriteEndElement(); // </alias>
                    break;
                case ImportTargetKind.Namespace:
                    if (alias != null)
                    {
                        _writer.WriteStartElement("alias");
                        _writer.WriteAttributeString("name", alias);
                        if (externAlias != null) _writer.WriteAttributeString("qualifier", externAlias);
                        _writer.WriteAttributeString("target", target);
                        _writer.WriteAttributeString("kind", "namespace");
                        Debug.Assert(scope == ImportScope.Unspecified); // Only C# hits this case.
                        _writer.WriteEndElement(); // </alias>
                    }
                    else
                    {
                        _writer.WriteStartElement("namespace");
                        if (externAlias != null) _writer.WriteAttributeString("qualifier", externAlias);
                        _writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        _writer.WriteEndElement(); // </namespace>
                    }
                    break;
                case ImportTargetKind.Type:
                    Debug.Assert(externAlias == null);
                    if (alias != null)
                    {
                        _writer.WriteStartElement("alias");
                        _writer.WriteAttributeString("name", alias);
                        _writer.WriteAttributeString("target", target);
                        _writer.WriteAttributeString("kind", "type");
                        Debug.Assert(scope == ImportScope.Unspecified); // Only C# hits this case.
                        _writer.WriteEndElement(); // </alias>
                    }
                    else
                    {
                        _writer.WriteStartElement("type");
                        _writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        _writer.WriteEndElement(); // </type>
                    }
                    break;
                case ImportTargetKind.Assembly:
                    Debug.Assert(alias != null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    if (target == null)
                    {
                        _writer.WriteStartElement("extern");
                        _writer.WriteAttributeString("alias", alias);
                        _writer.WriteEndElement(); // </extern>
                    }
                    else
                    {
                        _writer.WriteStartElement("externinfo");
                        _writer.WriteAttributeString("alias", alias);
                        _writer.WriteAttributeString("assembly", target);
                        _writer.WriteEndElement(); // </externinfo>
                    }
                    break;
                case ImportTargetKind.Defunct:
                    Debug.Assert(alias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    _writer.WriteStartElement("defunct");
                    _writer.WriteAttributeString("name", rawName);
                    _writer.WriteEndElement(); // </defunct>
                    break;
                default:
                    Debug.Assert(false, "Unexpected import kind '" + kind + "'");
                    _writer.WriteStartElement("unknown");
                    _writer.WriteAttributeString("name", rawName);
                    _writer.WriteEndElement(); // </unknown>
                    break;
            }
        }

        private void WriteScopeAttribute(ImportScope scope)
        {
            if (scope == ImportScope.File)
            {
                _writer.WriteAttributeString("importlevel", "file");
            }
            else if (scope == ImportScope.Project)
            {
                _writer.WriteAttributeString("importlevel", "project");
            }
            else
            {
                Debug.Assert(scope == ImportScope.Unspecified, "Unexpected scope '" + scope + "'");
            }
        }

        private void WriteAsyncInfo(ISymUnmanagedMethod method)
        {
            var asyncMethod = method.AsAsync();
            if (asyncMethod == null)
            {
                return;
            }

            _writer.WriteStartElement("asyncInfo");

            var catchOffset = asyncMethod.GetCatchHandlerILOffset();
            if (catchOffset >= 0)
            {
                _writer.WriteStartElement("catchHandler");
                _writer.WriteAttributeString("offset", AsILOffset(catchOffset));
                _writer.WriteEndElement();
            }

            _writer.WriteStartElement("kickoffMethod");
            WriteMethodAttributes(asyncMethod.GetKickoffMethod(), isReference: true);
            _writer.WriteEndElement();

            foreach (var info in asyncMethod.GetAsyncStepInfos())
            {
                _writer.WriteStartElement("await");
                _writer.WriteAttributeString("yield", AsILOffset(info.YieldOffset));
                _writer.WriteAttributeString("resume", AsILOffset(info.ResumeOffset));
                WriteMethodAttributes(info.ResumeMethod, isReference: true);
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        // Write all the locals in the given method out to an XML file.
        // Since the symbol store represents the locals in a recursive scope structure, we need to walk a tree.
        // Although the locals are technically a hierarchy (based off nested scopes), it's easiest for clients
        // if we present them as a linear list. We will provide the range for each local's scope so that somebody
        // could reconstruct an approximation of the scope tree. The reconstruction may not be exact.
        // (Note this would still break down if you had an empty scope nested in another scope.
        private void WriteLocals(ISymUnmanagedMethod method, Dictionary<int, ImmutableArray<string>> slotNames)
        {
            _writer.WriteStartElement("locals");
            // If there are no locals, then this element will just be empty.
            WriteLocalsHelper(method.GetRootScope(), slotNames, includeChildScopes: true);
            _writer.WriteEndElement();
        }

        private void WriteLocalsHelper(ISymUnmanagedScope scope, Dictionary<int, ImmutableArray<string>> slotNames, bool includeChildScopes)
        {
            foreach (ISymUnmanagedVariable l in scope.GetLocals())
            {
                _writer.WriteStartElement("local");
                {
                    _writer.WriteAttributeString("name", l.GetName());

                    // Each local maps to a "IL Index" or "slot" number. 
                    // The index is not necessarily unique. Several locals may refer to the same slot. 
                    // It just means that the same local is known under different names inside the same or different scopes.
                    // This index is what you pass to ICorDebugILFrame::GetLocalVariable() to get
                    // a specific local variable. 
                    // NOTE: VB emits "fake" locals for resumable locals which are actually backed by fields.
                    //       These locals always map to the slot #0 which is just a valid number that is 
                    //       not used. Only scoping information is used by EE in this case.
                    int slot = l.GetSlot();
                    _writer.WriteAttributeString("il_index", CultureInvariantToString(slot));

                    bool reusingSlot = false;

                    // collect slot names so that we can verify ISymUnmanagedReader APIs
                    if (slotNames != null)
                    {
                        ImmutableArray<string> existingNames;
                        if (slotNames.TryGetValue(slot, out existingNames))
                        {
                            slotNames[slot] = existingNames.Add(l.GetName());
                            reusingSlot = true;
                        }
                        else
                        {
                            slotNames.Add(slot, ImmutableArray.Create(l.GetName()));
                        }
                    }

                    // Provide scope range
                    _writer.WriteAttributeString("il_start", AsILOffset(scope.GetStartOffset()));
                    _writer.WriteAttributeString("il_end", AsILOffset(scope.GetEndOffset()));
                    _writer.WriteAttributeString("attributes", l.GetAttributes().ToString());

                    if (reusingSlot)
                    {
                        _writer.WriteAttributeString("reusingslot", reusingSlot.ToString(CultureInfo.InvariantCulture));
                    }
                }
                _writer.WriteEndElement(); // </local>
            }

            foreach (ISymUnmanagedConstant c in scope.GetConstants())
            {
                // Note: We can retrieve constant tokens by saving it into signature blob
                // in our implementation of IMetadataImport.GetSigFromToken.
                _writer.WriteStartElement("constant");
                {
                    _writer.WriteAttributeString("name", c.GetName());

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

                    _writer.WriteAttributeString("value", value.ToString());
                    _writer.WriteAttributeString("type", typeName);
                }
                _writer.WriteEndElement(); // </constant>
            }

            if (includeChildScopes)
            {
                foreach (ISymUnmanagedScope childScope in scope.GetScopes())
                {
                    WriteLocalsHelper(childScope, slotNames, includeChildScopes);
                }
            }
        }

        // Write the sequence points for the given method
        // Sequence points are the map between IL offsets and source lines.
        // A single method could span multiple files (use C#'s #line directive to see for yourself).        
        private void WriteSequencePoints(ISymUnmanagedMethod method)
        {
            _writer.WriteStartElement("sequencePoints");

            var sequencePoints = method.GetSequencePoints();

            // Write out sequence points
            foreach (var sequencePoint in sequencePoints)
            {
                _writer.WriteStartElement("entry");
                _writer.WriteAttributeString("offset", AsILOffset(sequencePoint.Offset));

                if (sequencePoint.IsHidden)
                {
                    if (sequencePoint.StartLine != sequencePoint.EndLine || sequencePoint.StartColumn != 0 || sequencePoint.EndColumn != 0)
                    {
                        _writer.WriteAttributeString("hidden", "invalid");
                    }
                    else
                    {
                        _writer.WriteAttributeString("hidden", XmlConvert.ToString(true));
                    }
                }
                else
                {
                    _writer.WriteAttributeString("startLine", CultureInvariantToString(sequencePoint.StartLine));
                    _writer.WriteAttributeString("startColumn", CultureInvariantToString(sequencePoint.StartColumn));
                    _writer.WriteAttributeString("endLine", CultureInvariantToString(sequencePoint.EndLine));
                    _writer.WriteAttributeString("endColumn", CultureInvariantToString(sequencePoint.EndColumn));
                }

                int documentId;
                _fileMapping.TryGetValue(sequencePoint.Document.GetName(), out documentId);
                _writer.WriteAttributeString("document", CultureInvariantToString(documentId));

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement(); // sequencepoints
        }

        // Write all docs, and add to the m_fileMapping list.
        // Other references to docs will then just refer to this list.
        private void WriteDocList()
        {
            var documents = _symReader.GetDocuments();
            if (documents.Length == 0)
            {
                return;
            }

            int id = 0;
            _writer.WriteStartElement("files");
            foreach (ISymUnmanagedDocument doc in documents)
            {
                string name = doc.GetName();

                // Symbol store may give out duplicate documents. We'll fold them here
                if (_fileMapping.ContainsKey(name))
                {
                    _writer.WriteComment("There is a duplicate entry for: " + name);
                    continue;
                }

                id++;
                _fileMapping.Add(name, id);

                _writer.WriteStartElement("file");

                _writer.WriteAttributeString("id", CultureInvariantToString(id));
                _writer.WriteAttributeString("name", name);
                _writer.WriteAttributeString("language", doc.GetLanguage().ToString());
                _writer.WriteAttributeString("languageVendor", doc.GetLanguageVendor().ToString());
                _writer.WriteAttributeString("documentType", doc.GetDocumentType().ToString());

                var checkSum = string.Concat(doc.GetChecksum().Select(b => string.Format("{0,2:X}", b) + ", "));

                if (!string.IsNullOrEmpty(checkSum))
                {
                    _writer.WriteAttributeString("checkSumAlgorithmId", doc.GetHashAlgorithm().ToString());
                    _writer.WriteAttributeString("checkSum", checkSum);
                }

                _writer.WriteEndElement(); // file
            }
            _writer.WriteEndElement(); // files
        }

        private void WriteAllMethodSpans()
        {
            _writer.WriteStartElement("method-spans");

            foreach (ISymUnmanagedDocument doc in _symReader.GetDocuments())
            {
                foreach (ISymUnmanagedMethod method in _symReader.GetMethodsInDocument(doc))
                {
                    _writer.WriteStartElement("method");

                    WriteMethodAttributes(method.GetToken(), isReference: true);

                    foreach (var methodDocument in method.GetDocumentsForMethod())
                    {
                        _writer.WriteStartElement("document");

                        int startLine, endLine;
                        method.GetSourceExtentInDocument(methodDocument, out startLine, out endLine);

                        _writer.WriteAttributeString("startLine", startLine.ToString());
                        _writer.WriteAttributeString("endLine", endLine.ToString());

                        _writer.WriteEndElement();
                    }

                    _writer.WriteEndElement();
                }
            }

            _writer.WriteEndElement();
        }

        // Write out a reference to the entry point method (if one exists)
        private void WriteEntryPoint()
        {
            int token = _symReader.GetUserEntryPoint();
            if (token != 0)
            {
                _writer.WriteStartElement("entryPoint");
                WriteMethodAttributes(token, isReference: true);
                _writer.WriteEndElement();
            }
        }

        // Write out XML snippet to refer to the given method.
        private void WriteMethodAttributes(int token, bool isReference)
        {
            if ((_options & PdbToXmlOptions.ResolveTokens) != 0)
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
                            _writer.WriteAttributeString("error", string.Format("Unexpected token type: {0}", handle.Kind));
                            break;
                    }
                }
                catch (BadImageFormatException e) // TODO: filter
                {
                    if ((_options & PdbToXmlOptions.ThrowOnError) != 0)
                    {
                        throw;
                    }

                    WriteToken(token);
                    _writer.WriteAttributeString("metadata-error", e.Message);
                }
            }

            if ((_options & PdbToXmlOptions.IncludeTokens) != 0)
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
            var method = _metadataReader.GetMethodDefinition(methodHandle);

            // type name
            var containingTypeHandle = method.GetDeclaringType();
            var fullName = GetFullTypeName(_metadataReader, containingTypeHandle);
            if (fullName != null)
            {
                _writer.WriteAttributeString(isReference ? "declaringType" : "containingType", fullName);
            }

            // method name
            _writer.WriteAttributeString(isReference ? "methodName" : "name", _metadataReader.GetString(method.Name));

            // parameters:
            var parameterNames = (from paramHandle in method.GetParameters()
                                  let parameter = _metadataReader.GetParameter(paramHandle)
                                  where parameter.SequenceNumber > 0 // exclude return parameter
                                  select parameter.Name.IsNil ? "?" : _metadataReader.GetString(parameter.Name)).ToArray();

            if (parameterNames.Length > 0)
            {
                _writer.WriteAttributeString("parameterNames", string.Join(", ", parameterNames));
            }
        }

        private void WriteResolvedToken(MemberReferenceHandle memberRefHandle)
        {
            var memberRef = _metadataReader.GetMemberReference(memberRefHandle);

            // type name
            string fullName = GetFullTypeName(_metadataReader, memberRef.Parent);
            if (fullName != null)
            {
                _writer.WriteAttributeString("declaringType", fullName);
            }

            // method name
            _writer.WriteAttributeString("methodName", _metadataReader.GetString(memberRef.Name));
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
            _writer.WriteAttributeString("token", AsToken(token));
        }

        internal static string AsToken(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x}", i);
        }

        internal static string AsILOffset(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x}", i);
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

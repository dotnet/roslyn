// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Roslyn.Utilities;
using CDI = Microsoft.CodeAnalysis.CustomDebugInfoReader;
using CDIC = Microsoft.Cci.CustomDebugInfoConstants;
using ImportScope = Microsoft.CodeAnalysis.ImportScope;
using PooledStringBuilder = Microsoft.CodeAnalysis.Collections.PooledStringBuilder;

namespace Roslyn.Test.PdbUtilities
{
    using Roslyn.Reflection.Metadata.Decoding;

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

        private static readonly XmlWriterSettings s_xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\r\n",
        };

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
                        xmlWriter.WriteLine(string.Format("<message><![CDATA[No method '{0}' found in metadata.]]></message>", methodName));
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

            using (var writer = XmlWriter.Create(xmlWriter, s_xmlWriterSettings))
            {
                // metadata reader is on stack -> no owner needed
                var symReader = SymReaderFactory.CreateReader(pdbStream, metadataReaderOpt, metadataMemoryOwnerOpt: null);

                try
                {
                    var converter = new PdbToXmlConverter(writer, symReader, metadataReaderOpt, options);
                    converter.WriteRoot(methodHandles ?? metadataReaderOpt.MethodDefinitions);
                }
                finally
                {
                    ((ISymUnmanagedDispose)symReader).Destroy();
                }
            }
        }

        private void WriteRoot(IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            _writer.WriteStartDocument();
            _writer.WriteStartElement("symbols");

            var documents = _symReader.GetDocuments();
            var documentIndex = BuildDocumentIndex(documents);

            if ((_options & PdbToXmlOptions.ExcludeDocuments) == 0)
            {
                WriteDocuments(documents, documentIndex);
            }

            if ((_options & PdbToXmlOptions.ExcludeMethods) == 0)
            {
                WriteEntryPoint();
                WriteAllMethods(methodHandles, documentIndex);
                WriteAllMethodSpans();
            }

            _writer.WriteEndElement();
        }

        private void WriteAllMethods(IEnumerable<MethodDefinitionHandle> methodHandles, IReadOnlyDictionary<string, int> documentIndex)
        {
            _writer.WriteStartElement("methods");

            foreach (var methodHandle in methodHandles)
            {
                WriteMethod(methodHandle, documentIndex);
            }

            _writer.WriteEndElement();
        }

        private void WriteMethod(MethodDefinitionHandle methodHandle, IReadOnlyDictionary<string, int> documentIndex)
        {
            int token = _metadataReader.GetToken(methodHandle);
            ISymUnmanagedMethod method = _symReader.GetMethod(token);

            byte[] cdi = null;
            var sequencePoints = default(ImmutableArray<SymUnmanagedSequencePoint>);
            ISymUnmanagedAsyncMethod asyncMethod = null;
            ISymUnmanagedScope rootScope = null;

            if ((_options & PdbToXmlOptions.ExcludeCustomDebugInformation) == 0)
            {
                cdi = _symReader.GetCustomDebugInfoBytes(token, methodVersion: 1);
            }

            if (method != null)
            {
                if ((_options & PdbToXmlOptions.ExcludeAsyncInfo) == 0)
                {
                    asyncMethod = method.AsAsync();
                }

                if ((_options & PdbToXmlOptions.ExcludeSequencePoints) == 0)
                {
                    sequencePoints = method.GetSequencePoints();
                }

                if ((_options & PdbToXmlOptions.ExcludeScopes) == 0)
                {
                    rootScope = method.GetRootScope();
                }
            }

            if (cdi == null && sequencePoints.IsDefaultOrEmpty && rootScope == null && asyncMethod == null)
            {
                // no debug info to write
                return;
            }

            _writer.WriteStartElement("method");
            WriteMethodAttributes(token, isReference: false);

            if (cdi != null)
            {
                WriteCustomDebugInfo(cdi);
            }

            if (!sequencePoints.IsDefaultOrEmpty)
            {
                WriteSequencePoints(sequencePoints, documentIndex);
            }

            if (rootScope != null)
            {
                WriteScopes(rootScope);
            }

            if (asyncMethod != null)
            {
                WriteAsyncInfo(asyncMethod);
            }

            _writer.WriteEndElement();
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
                            WriteStateMachineHoistedLocalScopesCustomDebugInfo(record);
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
            _writer.WriteAttributeString("version", CultureInvariantToString(record.Version));

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
                _writer.WriteAttributeString("usingCount", CultureInvariantToString(importCount));
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
        private void WriteStateMachineHoistedLocalScopesCustomDebugInfo(CustomDebugInfoRecord record)
        {
            Debug.Assert(record.Kind == CustomDebugInfoKind.StateMachineHoistedLocalScopes);

            _writer.WriteStartElement("hoistedLocalScopes");

            var scopes = CDI.DecodeStateMachineHoistedLocalScopesRecord(record.Data);

            foreach (StateMachineHoistedLocalScope scope in scopes)
            {
                _writer.WriteStartElement("slot");
                _writer.WriteAttributeString("startOffset", AsILOffset(scope.StartOffset));
                _writer.WriteAttributeString("endOffset", AsILOffset(scope.EndOffset));
                _writer.WriteEndElement(); //slot
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
                _writer.WriteAttributeString("flagCount", CultureInvariantToString(flagCount));
                _writer.WriteAttributeString("flags", pooled.ToStringAndFree());
                _writer.WriteAttributeString("slotId", CultureInvariantToString(bucket.SlotId));
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

                            _writer.WriteAttributeString("kind", CultureInvariantToString(synthesizedKind));
                            _writer.WriteAttributeString("offset", badSyntaxOffset ? "?" : CultureInvariantToString(syntaxOffset));

                            if (badOrdinal || hasOrdinal)
                            {
                                _writer.WriteAttributeString("ordinal", badOrdinal ? "?" : CultureInvariantToString(ordinal));
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
                    _writer.WriteElementString("methodOrdinal", CultureInvariantToString(methodOrdinal));

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

                            _writer.WriteAttributeString("offset", CultureInvariantToString(syntaxOffset + syntaxOffsetBaseline));
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

                            _writer.WriteAttributeString("offset", CultureInvariantToString(syntaxOffset + syntaxOffsetBaseline));

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
                                    CultureInvariantToString(closureOrdinal) + (closureOrdinal >= closureCount ? " (invalid)" : ""));
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

        private void WriteScopes(ISymUnmanagedScope rootScope)
        {
            // The root scope is always empty. The first scope opened by SymWriter is the child of the root scope.
            if (rootScope.GetNamespaces().IsEmpty && rootScope.GetLocals().IsEmpty && rootScope.GetConstants().IsEmpty)
            {
                foreach (ISymUnmanagedScope child in rootScope.GetScopes())
                {
                    WriteScope(child, isRoot: false);
                }
            }
            else
            {
                // This shouldn't be executed for PDBs generated via SymWriter.
                WriteScope(rootScope, isRoot: true);
            }
        }

        private void WriteScope(ISymUnmanagedScope scope, bool isRoot)
        {
            _writer.WriteStartElement(isRoot ? "rootScope" : "scope");
            _writer.WriteAttributeString("startOffset", AsILOffset(scope.GetStartOffset()));
            _writer.WriteAttributeString("endOffset", AsILOffset(scope.GetEndOffset()));

            if ((_options & PdbToXmlOptions.ExcludeNamespaces) == 0)
            {
                foreach (ISymUnmanagedNamespace @namespace in scope.GetNamespaces())
                {
                    WriteNamespace(@namespace);
                }
            }

            WriteLocals(scope);

            foreach (ISymUnmanagedScope child in scope.GetScopes())
            {
                WriteScope(child, isRoot: false);
            }

            _writer.WriteEndElement();
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
                                throw new InvalidOperationException($"Invalid import '{rawName}'");
                            }
                            break;

                        default:
                            externAlias = null;
                            if (!CDI.TryParseVisualBasicImportString(rawName, out alias, out target, out kind, out scope))
                            {
                                throw new InvalidOperationException($"Invalid import '{rawName}'");
                            }
                            break;
                    }
                }
            }
            catch (ArgumentException) when ((_options & PdbToXmlOptions.ThrowOnError) == 0)
            {
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
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.DefaultNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);

                    _writer.WriteStartElement("defaultnamespace");
                    _writer.WriteAttributeString("name", target);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.MethodToken:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);

                    int token = Convert.ToInt32(target);
                    _writer.WriteStartElement("importsforward");
                    WriteMethodAttributes(token, isReference: true);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.XmlNamespace:
                    Debug.Assert(externAlias == null);

                    _writer.WriteStartElement("xmlnamespace");
                    _writer.WriteAttributeString("prefix", alias);
                    _writer.WriteAttributeString("name", target);
                    WriteScopeAttribute(scope);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.NamespaceOrType:
                    Debug.Assert(externAlias == null);

                    _writer.WriteStartElement("alias");
                    _writer.WriteAttributeString("name", alias);
                    _writer.WriteAttributeString("target", target);
                    _writer.WriteAttributeString("kind", "namespace"); // Strange, but retaining to avoid breaking tests.
                    WriteScopeAttribute(scope);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.Namespace:
                    if (alias != null)
                    {
                        _writer.WriteStartElement("alias");
                        _writer.WriteAttributeString("name", alias);
                        if (externAlias != null)
                        {
                            _writer.WriteAttributeString("qualifier", externAlias);
                        }

                        _writer.WriteAttributeString("target", target);
                        _writer.WriteAttributeString("kind", "namespace");
                        Debug.Assert(scope == ImportScope.Unspecified); // Only C# hits this case.
                        _writer.WriteEndElement();
                    }
                    else
                    {
                        _writer.WriteStartElement("namespace");
                        if (externAlias != null) _writer.WriteAttributeString("qualifier", externAlias);
                        _writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        _writer.WriteEndElement();
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
                        _writer.WriteEndElement();
                    }
                    else
                    {
                        _writer.WriteStartElement("type");
                        _writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        _writer.WriteEndElement();
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
                        _writer.WriteEndElement();
                    }
                    else
                    {
                        _writer.WriteStartElement("externinfo");
                        _writer.WriteAttributeString("alias", alias);
                        _writer.WriteAttributeString("assembly", target);
                        _writer.WriteEndElement();
                    }

                    break;

                case ImportTargetKind.Defunct:
                    Debug.Assert(alias == null);
                    Debug.Assert(scope == ImportScope.Unspecified);
                    _writer.WriteStartElement("defunct");
                    _writer.WriteAttributeString("name", rawName);
                    _writer.WriteEndElement();
                    break;

                default:
                    Debug.Assert(false, "Unexpected import kind '" + kind + "'");
                    _writer.WriteStartElement("unknown");
                    _writer.WriteAttributeString("name", rawName);
                    _writer.WriteEndElement();
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

        private void WriteAsyncInfo(ISymUnmanagedAsyncMethod asyncMethod)
        {
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

        private void WriteLocals(ISymUnmanagedScope scope)
        {
            foreach (ISymUnmanagedVariable l in scope.GetLocals())
            {
                _writer.WriteStartElement("local");
                _writer.WriteAttributeString("name", l.GetName());

                // NOTE: VB emits "fake" locals for resumable locals which are actually backed by fields.
                //       These locals always map to the slot #0 which is just a valid number that is 
                //       not used. Only scoping information is used by EE in this case.
                _writer.WriteAttributeString("il_index", CultureInvariantToString(l.GetSlot()));

                _writer.WriteAttributeString("il_start", AsILOffset(scope.GetStartOffset()));
                _writer.WriteAttributeString("il_end", AsILOffset(scope.GetEndOffset()));
                _writer.WriteAttributeString("attributes", CultureInvariantToString(l.GetAttributes()));
                _writer.WriteEndElement();
            }

            foreach (ISymUnmanagedConstant constant in scope.GetConstants())
            {
                string name = constant.GetName();
                var signature = constant.GetSignature();
                object value = constant.GetValue();

                _writer.WriteStartElement("constant");
                _writer.WriteAttributeString("name", name);

                if (value is int &&
                    (int)value == 0 &&
                    (signature[0] == (byte)ConstantTypeCode.NullReference ||
                     signature[0] == (int)SignatureTypeCode.Object ||
                     signature[0] == (int)SignatureTypeCode.String ||
                     (signature.Length > 2 && signature[0] == (int)SignatureTypeCode.GenericTypeInstance && signature[1] == (byte)ConstantTypeCode.NullReference)))
                {
                    _writer.WriteAttributeString("value", "null");

                    if (signature[0] == (int)SignatureTypeCode.String)
                    {
                        _writer.WriteAttributeString("type", "String");
                    }
                    else if (signature[0] == (int)SignatureTypeCode.Object)
                    {
                        _writer.WriteAttributeString("type", "Object");
                    }
                    else
                    {
                        _writer.WriteAttributeString("signature", FormatLocalConstantSignature(signature));
                    }
                }
                else if (value == null)
                {
                    // empty string
                    if (signature[0] == (byte)SignatureTypeCode.String)
                    {
                        _writer.WriteAttributeString("value", "");
                        _writer.WriteAttributeString("type", "String");
                    }
                    else
                    {
                        _writer.WriteAttributeString("value", "null");
                        _writer.WriteAttributeString("unknown-signature", BitConverter.ToString(signature.ToArray()));
                    }
                }
                else if (value is decimal)
                {
                    // TODO: check that the signature is a TypeRef
                    _writer.WriteAttributeString("value", ((decimal)value).ToString(CultureInfo.InvariantCulture));
                    _writer.WriteAttributeString("type", value.GetType().Name);
                }
                else if (value is double && signature[0] != (byte)SignatureTypeCode.Double)
                {
                    // TODO: check that the signature is a TypeRef
                    _writer.WriteAttributeString("value", DateTimeUtilities.ToDateTime((double)value).ToString(CultureInfo.InvariantCulture));
                    _writer.WriteAttributeString("type", "DateTime");
                }
                else
                {
                    string str = value as string;
                    if (str != null)
                    {
                        _writer.WriteAttributeString("value", StringUtilities.EscapeNonPrintableCharacters(str));
                    }
                    else
                    {
                        _writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0}", value));
                    }

                    var runtimeType = GetConstantRuntimeType(signature);
                    if (runtimeType == null &&
                        (value is sbyte || value is byte || value is short || value is ushort ||
                         value is int || value is uint || value is long || value is ulong))
                    {
                        _writer.WriteAttributeString("signature", FormatLocalConstantSignature(signature));
                    }
                    else if (runtimeType == value.GetType())
                    {
                        _writer.WriteAttributeString("type", ((SignatureTypeCode)signature[0]).ToString());
                    }
                    else
                    {
                        _writer.WriteAttributeString("runtime-type", value.GetType().Name);
                        _writer.WriteAttributeString("unknown-signature", BitConverter.ToString(signature.ToArray()));
                    }
                }

                _writer.WriteEndElement();
            }
        }

        private unsafe string FormatLocalConstantSignature(ImmutableArray<byte> signature)
        {
            fixed (byte* sigPtr = signature.ToArray())
            {
                var sigReader = new BlobReader(sigPtr, signature.Length);
                var decoder = new SignatureDecoder<string>(ConstantSignatureVisualizer.Instance, _metadataReader);
                return decoder.DecodeType(ref sigReader, allowTypeSpecifications: true);
            }
        }

        private sealed class ConstantSignatureVisualizer : ISignatureTypeProvider<string>
        {
            public static readonly ConstantSignatureVisualizer Instance = new ConstantSignatureVisualizer();

            public string GetArrayType(string elementType, ArrayShape shape)
            {
                return elementType + "[" + new string(',', shape.Rank) + "]";
            }

            public string GetByReferenceType(string elementType)
            {
                return elementType + "&";
            }

            public string GetFunctionPointerType(MethodSignature<string> signature)
            {
                // TODO:
                return "method-ptr";
            }

            public string GetGenericInstance(string genericType, ImmutableArray<string> typeArguments)
            {
                // using {} since the result is embedded in XML
                return genericType + "{" + string.Join(", ", typeArguments) + "}";
            }

            public string GetGenericMethodParameter(int index)
            {
                return "!!" + index;
            }

            public string GetGenericTypeParameter(int index)
            {
                return "!" + index;
            }

            public string GetModifiedType(MetadataReader reader, bool isRequired, string modifier, string unmodifiedType)
            {
                return (isRequired ? "modreq" : "modopt") + "(" + modifier + ") " + unmodifiedType;
            }

            public string GetPinnedType(string elementType)
            {
                return "pinned " + elementType;
            }

            public string GetPointerType(string elementType)
            {
                return elementType + "*";
            }

            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return typeCode.ToString();
            }

            public string GetSZArrayType(string elementType)
            {
                return elementType + "[]";
            }

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, SignatureTypeHandleCode code)
            {
                var typeDef = reader.GetTypeDefinition(handle);
                var name = reader.GetString(typeDef.Name);
                return typeDef.Namespace.IsNil ? name : reader.GetString(typeDef.Namespace) + "." + name;
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, SignatureTypeHandleCode code)
            {
                var typeRef = reader.GetTypeReference(handle);
                var name = reader.GetString(typeRef.Name);
                return typeRef.Namespace.IsNil ? name : reader.GetString(typeRef.Namespace) + "." + name;
            }

            public string GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, SignatureTypeHandleCode code)
            {
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<string>(Instance, reader).DecodeType(ref sigReader);
            }
        }

        private static Type GetConstantRuntimeType(ImmutableArray<byte> signature)
        {
            switch ((SignatureTypeCode)signature[0])
            {
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Int16:
                    return typeof(short);

                case SignatureTypeCode.Char:
                case SignatureTypeCode.UInt16:
                    return typeof(ushort);

                case SignatureTypeCode.Int32:
                    return typeof(int);

                case SignatureTypeCode.UInt32:
                    return typeof(uint);

                case SignatureTypeCode.Int64:
                    return typeof(long);

                case SignatureTypeCode.UInt64:
                    return typeof(ulong);

                case SignatureTypeCode.Single:
                    return typeof(float);

                case SignatureTypeCode.Double:
                    return typeof(double);

                case SignatureTypeCode.String:
                    return typeof(string);
            }

            return null;
        }

        private void WriteSequencePoints(ImmutableArray<SymUnmanagedSequencePoint> sequencePoints, IReadOnlyDictionary<string, int> documentIndex)
        {
            Debug.Assert(!sequencePoints.IsDefaultOrEmpty);

            _writer.WriteStartElement("sequencePoints");

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
                string documentName = sequencePoint.Document.GetName();
                if (documentName.Length > 0)
                {
                    if (documentIndex.TryGetValue(documentName, out documentId))
                    {
                        _writer.WriteAttributeString("document", CultureInvariantToString(documentId));
                    }
                    else
                    {
                        _writer.WriteAttributeString("document", "?");
                    }
                }

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement(); // sequencepoints
        }

        private IReadOnlyDictionary<string, int> BuildDocumentIndex(ImmutableArray<ISymUnmanagedDocument> documents)
        {
            var index = new Dictionary<string, int>(documents.Length);

            int id = 1;
            foreach (var document in documents)
            {
                string name = document.GetName();

                // Native PDB doesn't allow no-name documents. SymWriter silently ignores them.
                // In Portable PDB all methods must be contained in a document, whose name may be empty. 
                // Skip such documents - like they were never in the document table.
                if (name.Length == 0)
                {
                    continue;
                }

                // Skip adding dups into the index, but increment id so that we 
                // can tell what methods are referring to the duplicate.
                if (!index.ContainsKey(name))
                {
                    index.Add(name, id);
                }

                id++;
            }

            return index;
        }

        private void WriteDocuments(ImmutableArray<ISymUnmanagedDocument> documents, IReadOnlyDictionary<string, int> documentIndex)
        {
            bool hasDocument = false;

            foreach (var doc in documents)
            {
                string name = doc.GetName();

                int id;
                if (!documentIndex.TryGetValue(name, out id))
                {
                    continue;
                }

                if (!hasDocument)
                {
                    _writer.WriteStartElement("files");
                }

                hasDocument = true;

                _writer.WriteStartElement("file");

                _writer.WriteAttributeString("id", CultureInvariantToString(id));
                _writer.WriteAttributeString("name", name);
                _writer.WriteAttributeString("language", doc.GetLanguage().ToString());
                _writer.WriteAttributeString("languageVendor", doc.GetLanguageVendor().ToString());
                _writer.WriteAttributeString("documentType", doc.GetDocumentType().ToString());

                var checkSum = string.Concat(doc.GetChecksum().Select(b => $"{b,2:X}, "));

                if (!string.IsNullOrEmpty(checkSum))
                {
                    _writer.WriteAttributeString("checkSumAlgorithmId", doc.GetHashAlgorithm().ToString());
                    _writer.WriteAttributeString("checkSum", checkSum);
                }

                _writer.WriteEndElement();
            }

            if (hasDocument)
            {
                _writer.WriteEndElement();
            }
        }

        private void WriteAllMethodSpans()
        {
            if ((_options & PdbToXmlOptions.IncludeMethodSpans) == 0)
            {
                return;
            }

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
                            _writer.WriteAttributeString("error", $"Unexpected token type: {handle.Kind}");
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

        private static string GetFullTypeName(MetadataReader metadataReader, EntityHandle handle)
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

        #endregion
    }
}

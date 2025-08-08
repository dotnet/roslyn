// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DiaSymReader.Tools;
using Microsoft.Metadata.Tools;

namespace Roslyn.Test.Utilities
{
    public static class ILValidation
    {
        private const int ChecksumOffset = 0x40;

        /// <summary>
        /// Validates that the given stream is marked as signed, the signature matches
        /// the public key, and the header checksum is correct.
        /// </summary>
        public static bool IsStreamFullSigned(Stream moduleContents)
        {
            var savedPosition = moduleContents.Position;

            try
            {
                moduleContents.Position = 0;

                var peHeaders = new PEHeaders(moduleContents);

                moduleContents.Position = 0;

                using (var metadata = ModuleMetadata.CreateFromStream(moduleContents, leaveOpen: true))
                {
                    var metadataReader = metadata.MetadataReader;
                    var peReader = metadata.Module.PEReaderOpt;
                    var flags = peHeaders.CorHeader.Flags;

                    if (CorFlags.StrongNameSigned != (flags & CorFlags.StrongNameSigned))
                    {
                        return false;
                    }

                    var snDirectory = peReader.PEHeaders.CorHeader.StrongNameSignatureDirectory;
                    if (!peHeaders.TryGetDirectoryOffset(snDirectory, out int snOffset))
                    {
                        return false;
                    }

                    moduleContents.Position = 0;
                    int peSize;
                    try
                    {
                        peSize = checked((int)moduleContents.Length);
                    }
                    catch
                    {
                        return false;
                    }

                    var peImage = new BlobBuilder(peSize);
                    if (peSize != peImage.TryWriteBytes(moduleContents, peSize))
                    {
                        return false;
                    }

                    byte[] buffer = GetBlobBuffer(peImage.GetBlobs().Single());

                    uint expectedChecksum = peHeaders.PEHeader.CheckSum;
                    Blob checksumBlob = MakeBlob(buffer, peHeaders.PEHeaderStartOffset + ChecksumOffset, sizeof(uint));

                    if (expectedChecksum != PeWriter.CalculateChecksum(peImage, checksumBlob))
                    {
                        return false;
                    }

                    int snSize = snDirectory.Size;
                    byte[] hash = ComputeSigningHash(peImage, peHeaders, checksumBlob, snOffset, snSize);

                    ImmutableArray<byte> publicKeyBlob = metadataReader.GetBlobContent(metadataReader.GetAssemblyDefinition().PublicKey);
                    // RSA parameters start after the public key offset
                    byte[] publicKeyParams = new byte[publicKeyBlob.Length - CryptoBlobParser.s_publicKeyHeaderSize];
                    publicKeyBlob.CopyTo(CryptoBlobParser.s_publicKeyHeaderSize, publicKeyParams, 0, publicKeyParams.Length);
                    var snKey = CryptoBlobParser.ToRSAParameters(publicKeyParams.AsSpan(), includePrivateParameters: false);

                    using (var rsa = RSA.Create())
                    {
                        rsa.ImportParameters(snKey);
                        var reversedSignature = peReader.GetSectionData(snDirectory.RelativeVirtualAddress).GetContent(0, snSize).ToArray();

                        // Unknown why the signature is reversed, but this matches the behavior of the CLR
                        // signing implementation.
                        Array.Reverse(reversedSignature);

                        // CodeQL [SM02196] ECMA-335 requires us to support SHA-1 and this is testing that support
                        if (!rsa.VerifyHash(hash, reversedSignature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            finally
            {
                moduleContents.Position = savedPosition;
            }
        }

        private static byte[] ComputeSigningHash(
            BlobBuilder peImage,
            PEHeaders peHeaders,
            Blob checksumBlob,
            int strongNameOffset,
            int strongNameSize)
        {
            const int SectionHeaderSize = 40;

            bool is32bit = peHeaders.PEHeader.Magic == PEMagic.PE32;
            int peHeadersSize = peHeaders.PEHeaderStartOffset
                + PEHeaderSize(is32bit)
                + SectionHeaderSize * peHeaders.SectionHeaders.Length;

            // Signature is calculated with the checksum and authenticode signature zeroed
            new BlobWriter(checksumBlob).WriteUInt32(0);
            var buffer = peImage.GetBlobs().Single().GetBytes().Array;
            int authenticodeOffset = GetAuthenticodeOffset(peHeaders, is32bit);
            var authenticodeDir = peHeaders.PEHeader.CertificateTableDirectory;
            for (int i = 0; i < 2 * sizeof(int); i++)
            {
                buffer[authenticodeOffset + i] = 0;
            }

            // CodeQL [SM02196] ECMA-335 requires us to support SHA-1 and this is testing that support
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            {
                // First hash the DOS header and PE headers
                hash.AppendData(buffer, 0, peHeadersSize);

                // Now each section, skipping the strong name signature if present
                foreach (var sectionHeader in peHeaders.SectionHeaders)
                {
                    int sectionOffset = sectionHeader.PointerToRawData;
                    int sectionSize = sectionHeader.SizeOfRawData;

                    if ((strongNameOffset + strongNameSize) < sectionOffset ||
                        strongNameOffset >= (sectionOffset + sectionSize))
                    {
                        // No signature overlap, hash the whole section
                        hash.AppendData(buffer, sectionOffset, sectionSize);
                    }
                    else
                    {
                        // There is overlap. Hash both sides of signature
                        hash.AppendData(buffer, sectionOffset, strongNameOffset - sectionOffset);
                        var strongNameEndOffset = strongNameOffset + strongNameSize;
                        hash.AppendData(buffer, strongNameEndOffset, sectionSize - (strongNameEndOffset - sectionOffset));
                    }
                }

                return hash.GetHashAndReset();
            }
        }

        private static int GetAuthenticodeOffset(PEHeaders peHeaders, bool is32bit)
        {
            return peHeaders.PEHeaderStartOffset
                + ChecksumOffset
                + sizeof(int)                                  // Checksum
                + sizeof(short)                                // Subsystem
                + sizeof(short)                                // DllCharacteristics
                + 4 * (is32bit ? sizeof(int) : sizeof(long))   // SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit
                + sizeof(int)                                  // LoaderFlags
                + sizeof(int)                                  // NumberOfRvaAndSizes
                + 4 * sizeof(long);                            // directory entries before Authenticode
        }

        private static MethodInfo s_peheaderSizeMethod;
        private static int PEHeaderSize(bool is32Bit)
        {
            if (s_peheaderSizeMethod == null)
            {
                Interlocked.CompareExchange(
                    ref s_peheaderSizeMethod,
                    typeof(PEHeader).GetMethod(
                        "Size",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    null);
            }

            return (int)s_peheaderSizeMethod.Invoke(null, new object[] { is32Bit });
        }

        private static ConstructorInfo s_blobCtor;
        private static Blob MakeBlob(byte[] buffer, int offset, int size)
        {
            if (s_blobCtor == null)
            {
                Interlocked.CompareExchange(
                    ref s_blobCtor,
                    typeof(Blob).GetConstructors(
                        BindingFlags.NonPublic | BindingFlags.Instance).Single(),
                    null);
            }

            return (Blob)s_blobCtor.Invoke(new object[] { buffer, offset, size });
        }

        private static FieldInfo s_bufferField;
        private static byte[] GetBlobBuffer(Blob blob)
        {
            if (s_bufferField == null)
            {
                Interlocked.CompareExchange(
                    ref s_bufferField,
                    typeof(Blob).GetField(
                        "Buffer",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    null);
            }

            return (byte[])s_bufferField.GetValue(blob);
        }

        private static MethodInfo s_getContentToSignMethod;
        private static IEnumerable<Blob> GetContentToSign(
            BlobBuilder peImage,
            int peHeadersSize,
            int peHeaderAlignment,
            Blob strongNameSignatureFixup)
        {
            if (s_getContentToSignMethod == null)
            {
                Interlocked.CompareExchange(
                    ref s_getContentToSignMethod,
                    typeof(PEBuilder).GetMethod(
                        "GetContentToSign",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    null);
            }

            return (IEnumerable<Blob>)s_getContentToSignMethod.Invoke(null, new object[]
            {
                peImage,
                peHeadersSize,
                peHeaderAlignment,
                strongNameSignatureFixup
            });
        }

        public static unsafe string DumpEncDeltaMethodBodies(ImmutableArray<byte> il, ImmutableArray<MetadataReader> readers)
        {
            var reader = readers[^1];

            var rvasAndNames = from handle in reader.MethodDefinitions
                               let method = reader.GetMethodDefinition(handle)
                               // filter out runtime-implemented methods that do not have IL:
                               where (method.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL
                               orderby method.RelativeVirtualAddress
                               group method.Name by method.RelativeVirtualAddress into g
                               // Legacy test support: name can only be resolved when readers for all generations are given.
                               select (g.Key, readers.Length > 1 ? string.Join(", ", g.Select(readers.GetString)) : null);

            var result = new StringBuilder();

            fixed (byte* ilPtr = il.ToArray())
            {
                var bodyReader = new BlobReader(ilPtr, il.Length);

                foreach (var (rva, name) in rvasAndNames)
                {
                    if (name != null)
                    {
                        result.AppendLine(name);
                    }

                    bodyReader.Offset = rva;

                    MethodBodyBlock body;
                    try
                    {
                        body = MethodBodyBlock.Create(bodyReader);
                    }
                    catch (BadImageFormatException)
                    {
                        result.AppendFormat("<invalid byte 0x{0:X2} at offset {1}>", il[rva], rva);
                        continue;
                    }

                    ILVisualizer.Default.DumpMethod(
                        result,
                        body.MaxStack,
                        body.GetILContent(),
                        locals: [],
                        exceptionHandlers: []);
                }
            }

            return result.ToString();
        }

        public static unsafe string DumpEncDeltaFieldData(ImmutableArray<byte> il, ImmutableArray<MetadataReader> readers)
        {
            var reader = readers[^1];
            var aggregator = new MetadataAggregator(readers[0], readers[1..]);

            var fieldRvaTablePtr = reader.MetadataPointer + reader.GetTableMetadataOffset(TableIndex.FieldRva);
            var rowCount = reader.GetTableRowCount(TableIndex.FieldRva);
            var rowSize = reader.GetTableRowSize(TableIndex.FieldRva);
            var tableReader = new BlobReader(fieldRvaTablePtr, rowCount * rowSize);

            var rvasAndNames = new List<(int rva, string name)>();
            for (var i = 0; i < rowCount; i++)
            {
                var rva = tableReader.ReadInt32();

                // RowIds are 4 bytes in EnC deltas 
                var fieldRowId = tableReader.ReadInt32();

                if (rva > 0)
                {
                    var fieldHandle = MetadataTokens.FieldDefinitionHandle(fieldRowId);
                    var genFieldHandle = (FieldDefinitionHandle)aggregator.GetGenerationHandle(fieldHandle, out var fieldGen);
                    var fieldDef = readers[fieldGen].GetFieldDefinition(genFieldHandle);
                    var genNameHandle = (StringHandle)aggregator.GetGenerationHandle(fieldDef.Name, out var nameGen);
                    var fieldName = readers[nameGen].GetString(genNameHandle);

                    rvasAndNames.Add((rva, fieldName));
                }
            }

            var result = new StringBuilder();

            for (var i = 0; i < rvasAndNames.Count; i++)
            {
                var (startRva, name) = rvasAndNames[i];
                var endRva = i + 1 < rvasAndNames.Count ? rvasAndNames[i + 1].rva : il.Length;

                result.AppendLine($"{name}: {BitConverter.ToString(il[startRva..endRva].ToArray())}");
            }

            return result.ToString();
        }

        public static unsafe MethodBodyBlock GetMethodBodyBlock(this ImmutableArray<byte> ilArray)
        {
            fixed (byte* ilPtr = ilArray.AsSpan())
            {
                int offset = 0;
                // skip padding:
                while (offset < ilArray.Length && ilArray[offset] == 0)
                {
                    offset++;
                }

                var reader = new BlobReader(ilPtr + offset, ilArray.Length - offset);
                return MethodBodyBlock.Create(reader);
            }
        }

        /// <summary>
        /// Returns "method" element with a specified token.
        /// <see cref="PdbToXmlOptions.IncludeTokens"/> must be set to include "token" attributes in "method" elements.
        /// </summary>
        public static XElement GetMethodElement(XElement document, int methodToken)
            => (from e in document.DescendantsAndSelf()
                where e.Name == "method"
                let xmlTokenValue = e.Attribute("token")?.Value
                where xmlTokenValue != null &&
                      xmlTokenValue.StartsWith("0x") &&
                      Convert.ToInt32(xmlTokenValue[2..], 16) == methodToken
                select e).SingleOrDefault();

        public static ImmutableDictionary<string, string> GetDocumentIdToPathMap(XElement document)
            => document
                .Descendants().Where(e => e.Name == "files").Single()
                .Descendants().ToImmutableDictionary(e => e.Attribute("id").Value, e => e.Attribute("name").Value);

        public static Dictionary<int, string> GetSequencePointMarkers(XElement methodXml)
        {
            var result = new Dictionary<int, string>();

            void add(int key, string value)
                => result[key] = result.TryGetValue(key, out var existing) ? existing + value : value;

            foreach (var e in methodXml.Descendants())
            {
                if (e.Name == "entry" && e.Parent.Name == "sequencePoints")
                {
                    add(Convert.ToInt32(e.Attribute("offset").Value, 16), (e.Attribute("hidden")?.Value == "true") ? "~" : "-");
                }
            }

            foreach (var e in methodXml.Descendants())
            {
                if (e.Name == "await" && e.Parent.Name == "asyncInfo")
                {
                    add(Convert.ToInt32(e.Attribute("yield").Value, 16), "<");
                    add(Convert.ToInt32(e.Attribute("resume").Value, 16), ">");
                }
                else if (e.Name == "catchHandler" && e.Parent.Name == "asyncInfo")
                {
                    add(Convert.ToInt32(e.Attribute("offset").Value, 16), "$");
                }
            }

            return result;
        }

        public static Dictionary<int, string> GetSequencePointMarkers(XElement methodXml, Func<string, SourceText> getSource)
        {
            var result = new Dictionary<int, string>();

            void add(int key, string value)
                => result[key] = result.TryGetValue(key, out var existing) ? existing + ", " + value : "// " + value;

            foreach (var e in methodXml.Descendants())
            {
                if (e.Name == "await" && e.Parent.Name == "asyncInfo")
                {
                    add(Convert.ToInt32(e.Attribute("yield").Value, 16), "async: yield");
                    add(Convert.ToInt32(e.Attribute("resume").Value, 16), "async: resume");
                }
                else if (e.Name == "catchHandler" && e.Parent.Name == "asyncInfo")
                {
                    add(Convert.ToInt32(e.Attribute("offset").Value, 16), "async: catch handler");
                }
            }

            foreach (var e in methodXml.Descendants())
            {
                if (e.Name == "entry" && e.Parent.Name == "sequencePoints")
                {
                    var documentId = e.Attribute("document").Value;
                    var source = getSource(documentId);

                    add(Convert.ToInt32(e.Attribute("offset").Value, 16), "sequence point: " + SnippetFromSpan(source, e));
                }
            }

            return result;
        }

        private static string SnippetFromSpan(SourceText text, XElement sequencePointXml)
        {
            if (sequencePointXml.Attribute("hidden")?.Value == "true")
            {
                return "<hidden>";
            }

            var startLine = Convert.ToInt32(sequencePointXml.Attribute("startLine").Value) - 1;
            var startColumn = Convert.ToInt32(sequencePointXml.Attribute("startColumn").Value) - 1;
            var endLine = Convert.ToInt32(sequencePointXml.Attribute("endLine").Value) - 1;
            var endColumn = Convert.ToInt32(sequencePointXml.Attribute("endColumn").Value) - 1;

            var lineSpan = new LinePositionSpan(new LinePosition(startLine, startColumn), new LinePosition(endLine, endColumn));
            var span = text.Lines.GetTextSpan(lineSpan);
            var subtext = text.GetSubText(span);

            if (startLine == endLine)
            {
                return subtext.ToString();
            }

            static string TruncateStart(string text, int maxLength)
                => (text.Length < maxLength) ? text : text[..maxLength];

            static string TruncateEnd(string text, int maxLength)
                => (text.Length < maxLength) ? text : text.Substring(text.Length - maxLength - 1, maxLength);

            var start = subtext.Lines[0].ToString();
            var end = subtext.Lines[^1].ToString();
            return TruncateStart(start, 12) + " ... " + TruncateEnd(end, 12);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.Metadata.Tools;

namespace Roslyn.Test.Utilities
{
    public static class ILValidation
    {
        /// <summary>
        /// Validates that the given stream is marked as signed, the signature matches
        /// the public key, and the header checksum is correct.
        /// </summary>
        public static bool IsStreamFullSigned(Stream moduleContents)
        {
            moduleContents.Position = 0;

            using (var metadata = ModuleMetadata.CreateFromStream(moduleContents))
            {
                var metadataReader = metadata.MetadataReader;
                var peReader = metadata.Module.PEReaderOpt;
                var peHeaders = peReader.PEHeaders;
                var flags = peHeaders.CorHeader.Flags;

                bool is32bit = peHeaders.PEHeader.Magic == PEMagic.PE32;
                const int SectionHeaderSize = 40;

                if (CorFlags.StrongNameSigned != (flags & CorFlags.StrongNameSigned))
                {
                    return false;
                }

                var snDirectory = peReader.PEHeaders.CorHeader.StrongNameSignatureDirectory;
                int rva = snDirectory.RelativeVirtualAddress;
                int size = snDirectory.Size;
                ImmutableArray<byte> signature = peReader.GetSectionData(rva).GetContent(0, size);
                if (!peHeaders.TryGetDirectoryOffset(snDirectory, out int snOffset))
                {
                    return false;
                }

                moduleContents.Position = 0;
                int peSize = checked((int)moduleContents.Length);
                var peImage = new BlobBuilder(peSize);
                if (peSize != peImage.TryWriteBytes(moduleContents, peSize))
                {
                    return false;
                }

                byte[] buffer = GetBlobBuffer(peImage.GetBlobs().Single());

                const int ChecksumOffset = 0x40;
                uint expectedChecksum = peHeaders.PEHeader.CheckSum;
                Blob checksumBlob = MakeBlob(buffer, peHeaders.PEHeaderStartOffset + ChecksumOffset, sizeof(uint));
                Blob signatureBlob = MakeBlob(buffer, snOffset, size);

                if (expectedChecksum != PeWriter.CalculateChecksum(peImage, checksumBlob))
                {
                    return false;
                }

                var snKey = CryptoBlobParser.ToRSAParameters(
                    metadataReader.GetBlobBytes(metadataReader.GetAssemblyDefinition().PublicKey),
                    includePrivateParameters: false);

                // Signature is calculated with checksum zeroed
                new BlobWriter(checksumBlob).WriteUInt32(0);

                int peHeadersSize = peHeaders.PEHeaderStartOffset
                    + PEHeaderSize(is32bit)
                    + SectionHeaderSize * peHeaders.SectionHeaders.Length;
                IEnumerable<Blob> content = GetContentToSign(peImage, peHeadersSize, peHeaders.PEHeader.FileAlignment, signatureBlob);
                byte[] hash = SigningUtilities.CalculateSha1(content);

                using (var rsa = RSA.Create())
                {
                    rsa.ImportParameters(snKey);
                    var reversedSignature = signature.ToArray();

                    // Unknown why the signature is reversed, but this matches the behavior of the CLR
                    // signing implementation.
                    Array.Reverse(reversedSignature);

                    if (!rsa.VerifyHash(hash, reversedSignature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
                    {
                        return false;
                    }
                }

                return true;
            }
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

        public static unsafe string GetMethodIL(this ImmutableArray<byte> ilArray)
        {
            var result = new StringBuilder();
            fixed (byte* ilPtr = ilArray.ToArray())
            {
                int offset = 0;
                while (true)
                {
                    // skip padding:
                    while (offset < ilArray.Length && ilArray[offset] == 0)
                    {
                        offset++;
                    }

                    if (offset == ilArray.Length)
                    {
                        break;
                    }

                    var reader = new BlobReader(ilPtr + offset, ilArray.Length - offset);
                    var methodIL = MethodBodyBlock.Create(reader);

                    if (methodIL == null)
                    {
                        result.AppendFormat("<invalid byte 0x{0:X2} at offset {1}>", ilArray[offset], offset);
                        offset++;
                    }
                    else
                    {
                        ILVisualizer.Default.DumpMethod(
                            result,
                            methodIL.MaxStack,
                            methodIL.GetILContent(),
                            ImmutableArray.Create<ILVisualizer.LocalInfo>(),
                            ImmutableArray.Create<ILVisualizer.HandlerSpan>());

                        offset += methodIL.Size;
                    }
                }
            }

            return result.ToString();
        }

        public static Dictionary<int, string> GetSequencePointMarkers(string pdbXml, string source = null)
        {
            string[] lines = source?.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var doc = new XmlDocument();
            doc.LoadXml(pdbXml);
            var result = new Dictionary<int, string>();

            if (source == null)
            {
                foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        Add(result,
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
                            Add(result, Convert.ToInt32(item.GetAttribute("yield"), 16), "<");
                            Add(result, Convert.ToInt32(item.GetAttribute("resume"), 16), ">");
                        }
                        else if (item.Name == "catchHandler")
                        {
                            Add(result, Convert.ToInt32(item.GetAttribute("offset"), 16), "$");
                        }
                    }
                }
            }
            else
            {
                foreach (XmlNode entry in doc.GetElementsByTagName("asyncInfo"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        if (item.Name == "await")
                        {
                            AddTextual(result, Convert.ToInt32(item.GetAttribute("yield"), 16), "async: yield");
                            AddTextual(result, Convert.ToInt32(item.GetAttribute("resume"), 16), "async: resume");
                        }
                        else if (item.Name == "catchHandler")
                        {
                            AddTextual(result, Convert.ToInt32(item.GetAttribute("offset"), 16), "async: catch handler");
                        }
                    }
                }

                foreach (XmlNode entry in doc.GetElementsByTagName("sequencePoints"))
                {
                    foreach (XmlElement item in entry.ChildNodes)
                    {
                        AddTextual(result, Convert.ToInt32(item.GetAttribute("offset"), 16), "sequence point: " + SnippetFromSpan(lines, item));
                    }
                }
            }

            return result;

            void Add(Dictionary<int, string> dict, int key, string value)
            {
                if (dict.TryGetValue(key, out string found))
                {
                    dict[key] = found + value;
                }
                else
                {
                    dict[key] = value;
                }
            }

            void AddTextual(Dictionary<int, string> dict, int key, string value)
            {
                if (dict.TryGetValue(key, out string found))
                {
                    dict[key] = found + ", " + value;
                }
                else
                {
                    dict[key] = "// " + value;
                }
            }
        }

        private static string SnippetFromSpan(string[] lines, XmlElement span)
        {
            if (span.GetAttribute("hidden") != "true")
            {
                var startLine = Convert.ToInt32(span.GetAttribute("startLine"));
                var startColumn = Convert.ToInt32(span.GetAttribute("startColumn"));
                var endLine = Convert.ToInt32(span.GetAttribute("endLine"));
                var endColumn = Convert.ToInt32(span.GetAttribute("endColumn"));
                if (startLine == endLine)
                {
                    return lines[startLine - 1].Substring(startColumn - 1, endColumn - startColumn);
                }
                else
                {
                    var start = lines[startLine - 1].Substring(startColumn - 1);
                    var end = lines[endLine - 1].Substring(0, endColumn - 1);
                    return TruncateStart(start, 12) + " ... " + TruncateEnd(end, 12);
                }
            }
            else
            {
                return "<hidden>";
            }

            string TruncateStart(string text, int maxLength)
            {
                if (text.Length < maxLength) { return text; }
                return text.Substring(0, maxLength);
            }

            string TruncateEnd(string text, int maxLength)
            {
                if (text.Length < maxLength) { return text; }
                return text.Substring(text.Length - maxLength - 1, maxLength);
            }
        }
    }
}

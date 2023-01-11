// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using BYTE = System.Byte;
using DWORD = System.UInt32;
using WCHAR = System.Char;
using WORD = System.UInt16;
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class RESOURCE
    {
        internal RESOURCE_STRING? pstringType;
        internal RESOURCE_STRING? pstringName;

        internal DWORD DataSize;               // size of data without header
        internal DWORD HeaderSize;     // Length of the header
        // [Ordinal or Name TYPE]
        // [Ordinal or Name NAME]
        internal DWORD DataVersion;    // version of data struct
        internal WORD MemoryFlags;    // state of the resource
        internal WORD LanguageId;     // Unicode support for NLS
        internal DWORD Version;        // Version of the resource data
        internal DWORD Characteristics;        // Characteristics of the data
        internal byte[]? data;       //data
    };

    internal class RESOURCE_STRING
    {
        internal WORD Ordinal;
        internal string? theString;
    };

    /// <summary>
    /// Parses .RES a file into its constituent resource elements.
    /// Mostly translated from cvtres.cpp.
    /// </summary>
    internal class CvtResFile
    {
        private const WORD RT_DLGINCLUDE = 17;

        internal static List<RESOURCE> ReadResFile(Stream stream)
        {
            var reader = new BinaryReader(stream, Encoding.Unicode);
            var resourceNames = new List<RESOURCE>();

            var startPos = stream.Position;

            var initial32Bits = reader.ReadUInt32();

            //RC.EXE output starts with a resource that contains no data.
            if (initial32Bits != 0)
                throw new ResourceException("Stream does not begin with a null resource and is not in .RES format.");

            stream.Position = startPos;

            // Build up Type and Name directories

            while (stream.Position < stream.Length)
            {
                // Get the sizes from the file

                var cbData = reader.ReadUInt32();
                var cbHdr = reader.ReadUInt32();

                if (cbHdr < 2 * sizeof(DWORD))
                {
                    throw new ResourceException(String.Format("Resource header beginning at offset 0x{0:x} is malformed.", stream.Position - 8));
                    //ErrorPrint(ERR_FILECORRUPT, szFilename);
                }

                // Discard null resource

                if (cbData == 0)
                {
                    stream.Position += cbHdr - 2 * sizeof(DWORD);
                    continue;
                }

                var pAdditional = new RESOURCE()
                {
                    HeaderSize = cbHdr,
                    DataSize = cbData
                };

                // Read the TYPE and NAME

                pAdditional.pstringType = ReadStringOrID(reader);
                pAdditional.pstringName = ReadStringOrID(reader);

                //round up to dword boundary.
                stream.Position = (stream.Position + 3) & ~3;

                // Read the rest of the header
                pAdditional.DataVersion = reader.ReadUInt32();
                pAdditional.MemoryFlags = reader.ReadUInt16();
                pAdditional.LanguageId = reader.ReadUInt16();
                pAdditional.Version = reader.ReadUInt32();
                pAdditional.Characteristics = reader.ReadUInt32();

                pAdditional.data = new byte[pAdditional.DataSize];
                reader.Read(pAdditional.data, 0, pAdditional.data.Length);

                stream.Position = (stream.Position + 3) & ~3;

                if (pAdditional.pstringType.theString == null && (pAdditional.pstringType.Ordinal == (WORD)RT_DLGINCLUDE))
                {
                    // Ignore DLGINCLUDE resources
                    continue;
                }

                resourceNames.Add(pAdditional);
            }

            return resourceNames;
        }

        private static RESOURCE_STRING ReadStringOrID(BinaryReader fhIn)
        {
            // Reads a String structure from fhIn
            // If the first word is 0xFFFF then this is an ID
            // return the ID instead

            RESOURCE_STRING pstring = new RESOURCE_STRING();

            WCHAR firstWord = fhIn.ReadChar();

            if (firstWord == 0xFFFF)
            {
                // An ID

                pstring.Ordinal = fhIn.ReadUInt16();
            }
            else
            {
                // A string
                pstring.Ordinal = 0xFFFF;

                //keep reading until null reached.

                StringBuilder sb = new StringBuilder();

                WCHAR curChar = firstWord;

                do
                {
                    sb.Append(curChar);
                    curChar = fhIn.ReadChar();
                }
                while (curChar != 0);

                pstring.theString = sb.ToString();
            }

            return (pstring);
        }
    }

    internal static class COFFResourceReader
    {
        private static void ConfirmSectionValues(SectionHeader hdr, long fileSize)
        {
            if ((long)hdr.PointerToRawData + hdr.SizeOfRawData > fileSize)
                throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidSectionSize);
        }

        internal static Microsoft.Cci.ResourceSection ReadWin32ResourcesFromCOFF(Stream stream)
        {
            var peHeaders = new PEHeaders(stream);
            var rsrc1 = new SectionHeader();
            var rsrc2 = new SectionHeader();

            int foundCount = 0;
            foreach (var sectionHeader in peHeaders.SectionHeaders)
            {
                if (sectionHeader.Name == ".rsrc$01")
                {
                    rsrc1 = sectionHeader;
                    foundCount++;
                }
                else if (sectionHeader.Name == ".rsrc$02")
                {
                    rsrc2 = sectionHeader;
                    foundCount++;
                }
            }

            if (foundCount != 2)
                throw new ResourceException(CodeAnalysisResources.CoffResourceMissingSection);

            ConfirmSectionValues(rsrc1, stream.Length);
            ConfirmSectionValues(rsrc2, stream.Length);

            //This will be the final resource section bytes without a header. It contains the concatenation
            //of .rsrc$02 on to the end of .rsrc$01.
            var imageResourceSectionBytes = new byte[checked(rsrc1.SizeOfRawData + rsrc2.SizeOfRawData)];

            stream.Seek(rsrc1.PointerToRawData, SeekOrigin.Begin);
            stream.TryReadAll(imageResourceSectionBytes, 0, rsrc1.SizeOfRawData); // ConfirmSectionValues ensured that data are available
            stream.Seek(rsrc2.PointerToRawData, SeekOrigin.Begin);
            stream.TryReadAll(imageResourceSectionBytes, rsrc1.SizeOfRawData, rsrc2.SizeOfRawData); // ConfirmSectionValues ensured that data are available

            const int SizeOfRelocationEntry = 10;

            try
            {
                var relocLastAddress = checked(rsrc1.PointerToRelocations + (rsrc1.NumberOfRelocations * SizeOfRelocationEntry));

                if (relocLastAddress > stream.Length)
                    throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidRelocation);
            }
            catch (OverflowException)
            {
                throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidRelocation);
            }

            //.rsrc$01 contains the directory tree. .rsrc$02 contains the raw resource data.
            //.rsrc$01 has references to spots in .rsrc$02. Those spots are expressed as relocations.
            //These will need to be fixed up when the RVA of the .rsrc section in the final image is known.
            var relocationOffsets = new uint[rsrc1.NumberOfRelocations];    //offsets into .rsrc$01

            var relocationSymbolIndices = new uint[rsrc1.NumberOfRelocations];

            var reader = new BinaryReader(stream, Encoding.Unicode);
            stream.Position = rsrc1.PointerToRelocations;

            for (int i = 0; i < rsrc1.NumberOfRelocations; i++)
            {
                relocationOffsets[i] = reader.ReadUInt32();
                //What is being read and stored is the reloc's "Value"
                //This is the symbol's index.
                relocationSymbolIndices[i] = reader.ReadUInt32();
                reader.ReadUInt16(); //we do nothing with the "Type"
            }

            //now that symbol indices are gathered, begin indexing the symbols
            stream.Position = peHeaders.CoffHeader.PointerToSymbolTable;
            const uint ImageSizeOfSymbol = 18;

            try
            {
                var lastSymAddress = checked(peHeaders.CoffHeader.PointerToSymbolTable + peHeaders.CoffHeader.NumberOfSymbols * ImageSizeOfSymbol);

                if (lastSymAddress > stream.Length)
                    throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidSymbol);
            }
            catch (OverflowException)
            {
                throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidSymbol);
            }

            var outputStream = new MemoryStream(imageResourceSectionBytes);
            var writer = new BinaryWriter(outputStream);  //encoding shouldn't matter. There are no strings being written.

            for (int i = 0; i < relocationSymbolIndices.Length; i++)
            {
                if (relocationSymbolIndices[i] > peHeaders.CoffHeader.NumberOfSymbols)
                    throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidRelocation);

                var offsetOfSymbol = peHeaders.CoffHeader.PointerToSymbolTable + relocationSymbolIndices[i] * ImageSizeOfSymbol;

                stream.Position = offsetOfSymbol;
                stream.Position += 8; //skip over symbol name
                var symValue = reader.ReadUInt32();
                var symSection = reader.ReadInt16();
                var symType = reader.ReadUInt16();
                //ignore the rest of the fields.

                const ushort IMAGE_SYM_TYPE_NULL = 0x0000;

                if (symType != IMAGE_SYM_TYPE_NULL ||
                    symSection != 3)  //3rd section is .rsrc$02
                    throw new ResourceException(CodeAnalysisResources.CoffResourceInvalidSymbol);

                //perform relocation. We are concatenating the contents of .rsrc$02 (the raw resource data)
                //on to the end of .rsrc$01 (the directory tree) to yield the final resource section for the image.
                //The directory tree has references into the raw resource data. These references are expressed
                //in the final image as file positions, not positions relative to the beginning of the section.
                //First make the resources be relative to the beginning of the section by adding the size
                //of .rsrc$01 to them. They will ultimately need the RVA of the final image resource section added 
                //to them. We don't know that yet. That is why the array of offsets is preserved. 

                outputStream.Position = relocationOffsets[i];
                writer.Write((uint)(symValue + rsrc1.SizeOfRawData));
            }

            return new Cci.ResourceSection(imageResourceSectionBytes, relocationOffsets);
        }
    }

    internal static class Win32ResourceConversions
    {
        private struct ICONDIRENTRY
        {
            internal BYTE bWidth;
            internal BYTE bHeight;
            internal BYTE bColorCount;
            internal BYTE bReserved;
            internal WORD wPlanes;
            internal WORD wBitCount;
            internal DWORD dwBytesInRes;
            internal DWORD dwImageOffset;
        };

        internal static void AppendIconToResourceStream(Stream resStream, Stream iconStream)
        {
            var iconReader = new BinaryReader(iconStream);

            //read magic reserved WORD
            var reserved = iconReader.ReadUInt16();
            if (reserved != 0)
                throw new ResourceException(CodeAnalysisResources.IconStreamUnexpectedFormat);

            var type = iconReader.ReadUInt16();
            if (type != 1)
                throw new ResourceException(CodeAnalysisResources.IconStreamUnexpectedFormat);

            var count = iconReader.ReadUInt16();
            if (count == 0)
                throw new ResourceException(CodeAnalysisResources.IconStreamUnexpectedFormat);

            var iconDirEntries = new ICONDIRENTRY[count];
            for (ushort i = 0; i < count; i++)
            {
                // Read the Icon header
                iconDirEntries[i].bWidth = iconReader.ReadByte();
                iconDirEntries[i].bHeight = iconReader.ReadByte();
                iconDirEntries[i].bColorCount = iconReader.ReadByte();
                iconDirEntries[i].bReserved = iconReader.ReadByte();
                iconDirEntries[i].wPlanes = iconReader.ReadUInt16();
                iconDirEntries[i].wBitCount = iconReader.ReadUInt16();
                iconDirEntries[i].dwBytesInRes = iconReader.ReadUInt32();
                iconDirEntries[i].dwImageOffset = iconReader.ReadUInt32();
            }

            // Because Icon files don't seem to record the actual w and BitCount in
            // the ICONDIRENTRY, get the info from the BITMAPINFOHEADER at the beginning
            // of the data here:
            //EDMAURER: PNG compressed icons must be treated differently. Do what has always
            //been done for uncompressed icons. Assume modern, compressed icons set the 
            //ICONDIRENTRY fields correctly.
            //if (*(DWORD*)icoBuffer == sizeof(BITMAPINFOHEADER))
            //{
            //    grp[i].Planes = ((BITMAPINFOHEADER*)icoBuffer)->biPlanes;
            //    grp[i].BitCount = ((BITMAPINFOHEADER*)icoBuffer)->biBitCount;
            //}

            for (ushort i = 0; i < count; i++)
            {
                iconStream.Position = iconDirEntries[i].dwImageOffset;
                if (iconReader.ReadUInt32() == 40)
                {
                    iconStream.Position += 8;
                    iconDirEntries[i].wPlanes = iconReader.ReadUInt16();
                    iconDirEntries[i].wBitCount = iconReader.ReadUInt16();
                }
            }

            //read everything and no exceptions. time to write.
            var resWriter = new BinaryWriter(resStream);

            //write all of the icon images as individual resources, then follow up with
            //a resource that groups them.
            const WORD RT_ICON = 3;

            for (ushort i = 0; i < count; i++)
            {
                /* write resource header.
                struct RESOURCEHEADER
                {
                    DWORD DataSize;
                    DWORD HeaderSize;
                    WORD Magic1;
                    WORD Type;
                    WORD Magic2;
                    WORD Name;
                    DWORD DataVersion;
                    WORD MemoryFlags;
                    WORD LanguageId;
                    DWORD Version;
                    DWORD Characteristics;
                };
                */

                resStream.Position = (resStream.Position + 3) & ~3; //headers begin on 4-byte boundaries.
                resWriter.Write((DWORD)iconDirEntries[i].dwBytesInRes);
                resWriter.Write((DWORD)0x00000020);
                resWriter.Write((WORD)0xFFFF);
                resWriter.Write((WORD)RT_ICON);
                resWriter.Write((WORD)0xFFFF);
                resWriter.Write((WORD)(i + 1));       //EDMAURER this is not general. Implies you can only append one icon to the resources.
                                                      //This icon ID would seem to be global among all of the icons not just this group.
                                                      //Zero appears to not be an acceptable ID. Note that this ID is referred to below.
                resWriter.Write((DWORD)0x00000000);
                resWriter.Write((WORD)0x1010);
                resWriter.Write((WORD)0x0000);
                resWriter.Write((DWORD)0x00000000);
                resWriter.Write((DWORD)0x00000000);

                //write the data.
                iconStream.Position = iconDirEntries[i].dwImageOffset;
                resWriter.Write(iconReader.ReadBytes(checked((int)iconDirEntries[i].dwBytesInRes)));
            }

            /*
            
            struct ICONDIR
            {
                WORD           idReserved;   // Reserved (must be 0)
                WORD           idType;       // Resource Type (1 for icons)
                WORD           idCount;      // How many images?
                ICONDIRENTRY   idEntries[1]; // An entry for each image (idCount of 'em)
            }/
             
            struct ICONRESDIR
            {
                BYTE Width;        // = ICONDIRENTRY.bWidth;
                BYTE Height;       // = ICONDIRENTRY.bHeight;
                BYTE ColorCount;   // = ICONDIRENTRY.bColorCount;
                BYTE reserved;     // = ICONDIRENTRY.bReserved;
                WORD Planes;       // = ICONDIRENTRY.wPlanes;
                WORD BitCount;     // = ICONDIRENTRY.wBitCount;
                DWORD BytesInRes;   // = ICONDIRENTRY.dwBytesInRes;
                WORD IconId;       // = RESOURCEHEADER.Name
            };
            */

            const WORD RT_GROUP_ICON = RT_ICON + 11;

            resStream.Position = (resStream.Position + 3) & ~3; //align 4-byte boundary
            //write the icon group. first a RESOURCEHEADER. the data is the ICONDIR
            resWriter.Write((DWORD)(3 * sizeof(WORD) + count * /*sizeof(ICONRESDIR)*/ 14));
            resWriter.Write((DWORD)0x00000020);
            resWriter.Write((WORD)0xFFFF);
            resWriter.Write((WORD)RT_GROUP_ICON);
            resWriter.Write((WORD)0xFFFF);
            resWriter.Write((WORD)0x7F00);  //IDI_APPLICATION
            resWriter.Write((DWORD)0x00000000);
            resWriter.Write((WORD)0x1030);
            resWriter.Write((WORD)0x0000);
            resWriter.Write((DWORD)0x00000000);
            resWriter.Write((DWORD)0x00000000);

            //the ICONDIR
            resWriter.Write((WORD)0x0000);
            resWriter.Write((WORD)0x0001);
            resWriter.Write((WORD)count);

            for (ushort i = 0; i < count; i++)
            {
                resWriter.Write((BYTE)iconDirEntries[i].bWidth);
                resWriter.Write((BYTE)iconDirEntries[i].bHeight);
                resWriter.Write((BYTE)iconDirEntries[i].bColorCount);
                resWriter.Write((BYTE)iconDirEntries[i].bReserved);
                resWriter.Write((WORD)iconDirEntries[i].wPlanes);
                resWriter.Write((WORD)iconDirEntries[i].wBitCount);
                resWriter.Write((DWORD)iconDirEntries[i].dwBytesInRes);
                resWriter.Write((WORD)(i + 1));   //ID
            }
        }

        /*
         * Dev10 alink had the following fallback behavior.
                private uint[] FileVersion
                {
                    get
                    {
                        if (fileVersionContents != null)
                            return fileVersionContents;
                        else
                        {
                            System.Diagnostics.Debug.Assert(assemblyVersionContents != null);
                            return assemblyVersionContents;
                        }
                    }
                }

                private uint[] ProductVersion
                {
                    get
                    {
                        if (productVersionContents != null)
                            return productVersionContents;
                        else
                            return this.FileVersion;
                    }
                }
                */

        internal static void AppendVersionToResourceStream(Stream resStream, bool isDll,
            string fileVersion, //should be [major.minor.build.rev] but doesn't have to be
            string originalFileName,
            string internalName,
            string productVersion,  //4 ints
            Version assemblyVersion, //individual values must be smaller than 65535
            string fileDescription = " ",   //the old compiler put blank here if nothing was user-supplied
            string legalCopyright = " ",    //the old compiler put blank here if nothing was user-supplied
            string? legalTrademarks = null,
            string? productName = null,
            string? comments = null,
            string? companyName = null)
        {
            var resWriter = new BinaryWriter(resStream, Encoding.Unicode);
            resStream.Position = (resStream.Position + 3) & ~3;

            const DWORD RT_VERSION = 16;

            var ver = new VersionResourceSerializer(isDll,
                comments,
                companyName,
                fileDescription,
                fileVersion,
                internalName,
                legalCopyright,
                legalTrademarks,
                originalFileName,
                productName,
                productVersion,
                assemblyVersion);

            var startPos = resStream.Position;
            var dataSize = ver.GetDataSize();
            const int headerSize = 0x20;

            resWriter.Write((DWORD)dataSize);    //data size
            resWriter.Write((DWORD)headerSize);                 //header size
            resWriter.Write((WORD)0xFFFF);                      //identifies type as ordinal.
            resWriter.Write((WORD)RT_VERSION);                 //type
            resWriter.Write((WORD)0xFFFF);                      //identifies name as ordinal.
            resWriter.Write((WORD)0x0001);                      //only ever 1 ver resource (what Dev10 does)
            resWriter.Write((DWORD)0x00000000);                 //data version
            resWriter.Write((WORD)0x0030);                      //memory flags (this is what the Dev10 compiler uses)
            resWriter.Write((WORD)0x0000);                      //languageId
            resWriter.Write((DWORD)0x00000000);                 //version
            resWriter.Write((DWORD)0x00000000);                 //characteristics

            ver.WriteVerResource(resWriter);

            System.Diagnostics.Debug.Assert(resStream.Position - startPos == dataSize + headerSize);
        }

        internal static void AppendManifestToResourceStream(Stream resStream, Stream manifestStream, bool isDll)
        {
            resStream.Position = (resStream.Position + 3) & ~3;
            const WORD RT_MANIFEST = 24;

            var resWriter = new BinaryWriter(resStream);
            resWriter.Write((DWORD)(manifestStream.Length));    //data size
            resWriter.Write((DWORD)0x00000020);                 //header size
            resWriter.Write((WORD)0xFFFF);                      //identifies type as ordinal.
            resWriter.Write((WORD)RT_MANIFEST);                 //type
            resWriter.Write((WORD)0xFFFF);                      //identifies name as ordinal.
            resWriter.Write((WORD)((isDll) ? 0x0002 : 0x0001));  //EDMAURER executables are named "1", DLLs "2"
            resWriter.Write((DWORD)0x00000000);                 //data version
            resWriter.Write((WORD)0x1030);                      //memory flags
            resWriter.Write((WORD)0x0000);                      //languageId
            resWriter.Write((DWORD)0x00000000);                 //version
            resWriter.Write((DWORD)0x00000000);                 //characteristics

            manifestStream.CopyTo(resStream);
        }

        private class VersionResourceSerializer
        {
            private readonly string? _commentsContents;
            private readonly string? _companyNameContents;
            private readonly string _fileDescriptionContents;
            private readonly string _fileVersionContents;
            private readonly string _internalNameContents;
            private readonly string _legalCopyrightContents;
            private readonly string? _legalTrademarksContents;
            private readonly string _originalFileNameContents;
            private readonly string? _productNameContents;
            private readonly string _productVersionContents;
            private readonly Version _assemblyVersionContents;

            private const string vsVersionInfoKey = "VS_VERSION_INFO";
            private const string varFileInfoKey = "VarFileInfo";
            private const string translationKey = "Translation";
            private const string stringFileInfoKey = "StringFileInfo";
            private readonly string _langIdAndCodePageKey; //should be 8 characters
            private const DWORD CP_WINUNICODE = 1200;

            private const ushort sizeVS_FIXEDFILEINFO = sizeof(DWORD) * 13;
            private readonly bool _isDll;

            internal VersionResourceSerializer(bool isDll, string? comments, string? companyName, string fileDescription, string fileVersion,
                string internalName, string legalCopyright, string? legalTrademark, string originalFileName, string? productName, string productVersion,
                Version assemblyVersion)
            {
                _isDll = isDll;
                _commentsContents = comments;
                _companyNameContents = companyName;
                _fileDescriptionContents = fileDescription;
                _fileVersionContents = fileVersion;
                _internalNameContents = internalName;
                _legalCopyrightContents = legalCopyright;
                _legalTrademarksContents = legalTrademark;
                _originalFileNameContents = originalFileName;
                _productNameContents = productName;
                _productVersionContents = productVersion;
                _assemblyVersionContents = assemblyVersion;
                _langIdAndCodePageKey = System.String.Format("{0:x4}{1:x4}", 0 /*langId*/, CP_WINUNICODE /*codepage*/);
            }

            private const uint VFT_APP = 0x00000001;
            private const uint VFT_DLL = 0x00000002;

            private IEnumerable<KeyValuePair<string, string>> GetVerStrings()
            {
                if (_commentsContents != null) yield return new KeyValuePair<string, string>("Comments", _commentsContents);
                if (_companyNameContents != null) yield return new KeyValuePair<string, string>("CompanyName", _companyNameContents);
                if (_fileDescriptionContents != null) yield return new KeyValuePair<string, string>("FileDescription", _fileDescriptionContents);

                yield return new KeyValuePair<string, string>("FileVersion", _fileVersionContents);

                if (_internalNameContents != null) yield return new KeyValuePair<string, string>("InternalName", _internalNameContents);
                if (_legalCopyrightContents != null) yield return new KeyValuePair<string, string>("LegalCopyright", _legalCopyrightContents);
                if (_legalTrademarksContents != null) yield return new KeyValuePair<string, string>("LegalTrademarks", _legalTrademarksContents);
                if (_originalFileNameContents != null) yield return new KeyValuePair<string, string>("OriginalFilename", _originalFileNameContents);
                if (_productNameContents != null) yield return new KeyValuePair<string, string>("ProductName", _productNameContents);

                yield return new KeyValuePair<string, string>("ProductVersion", _productVersionContents);

                if (_assemblyVersionContents != null) yield return new KeyValuePair<string, string>("Assembly Version", _assemblyVersionContents.ToString());
            }

            private uint FileType { get { return (_isDll) ? VFT_DLL : VFT_APP; } }

            private void WriteVSFixedFileInfo(BinaryWriter writer)
            {
                //There's nothing guaranteeing that these are n.n.n.n format.
                //The documentation says that if they're not that format the behavior is undefined.
                Version fileVersion;
                VersionHelper.TryParse(_fileVersionContents, version: out fileVersion);

                Version productVersion;
                VersionHelper.TryParse(_productVersionContents, version: out productVersion);

                writer.Write((DWORD)0xFEEF04BD);
                writer.Write((DWORD)0x00010000);
                writer.Write((DWORD)((uint)fileVersion.Major << 16) | (uint)fileVersion.Minor);
                writer.Write((DWORD)((uint)fileVersion.Build << 16) | (uint)fileVersion.Revision);
                writer.Write((DWORD)((uint)productVersion.Major << 16) | (uint)productVersion.Minor);
                writer.Write((DWORD)((uint)productVersion.Build << 16) | (uint)productVersion.Revision);
                writer.Write((DWORD)0x0000003F);   //VS_FFI_FILEFLAGSMASK  (EDMAURER) really? all these bits are valid?
                writer.Write((DWORD)0);    //file flags
                writer.Write((DWORD)0x00000004);   //VOS__WINDOWS32
                writer.Write((DWORD)this.FileType);
                writer.Write((DWORD)0);    //file subtype
                writer.Write((DWORD)0);    //date most sig
                writer.Write((DWORD)0);    //date least sig
            }

            /// <summary>
            /// Assume that 3 WORDs preceded this string and that they began 32-bit aligned.
            /// Given the string length compute the number of bytes that should be written to end
            /// the buffer on a 32-bit boundary</summary>
            /// <param name="cb"></param>
            /// <returns></returns>
            private static int PadKeyLen(int cb)
            {
                //add previously written 3 WORDS, round up, then subtract the 3 WORDS.
                return PadToDword(cb + 3 * sizeof(WORD)) - 3 * sizeof(WORD);
            }
            /// <summary>
            /// assuming the length of bytes submitted began on a 32-bit boundary,
            /// round up this length as necessary so that it ends at a 32-bit boundary.
            /// </summary>
            /// <param name="cb"></param>
            /// <returns></returns>
            private static int PadToDword(int cb)
            {
                return (cb + 3) & ~3;
            }

            private const int HDRSIZE = 3 * sizeof(ushort);

            private static ushort SizeofVerString(string lpszKey, string lpszValue)
            {
                int cbKey, cbValue;

                cbKey = (lpszKey.Length + 1) * 2;  // Make room for the NULL
                cbValue = (lpszValue.Length + 1) * 2;

                return checked((ushort)(PadKeyLen(cbKey) +    // key, 0 padded to DWORD boundary
                                cbValue +               // value
                                HDRSIZE));             // block header.
            }

            private static void WriteVersionString(KeyValuePair<string, string> keyValuePair, BinaryWriter writer)
            {
                RoslynDebug.Assert(keyValuePair.Value != null);

                ushort cbBlock = SizeofVerString(keyValuePair.Key, keyValuePair.Value);
                int cbKey = (keyValuePair.Key.Length + 1) * 2;     // includes terminating NUL
                int cbVal = (keyValuePair.Value.Length + 1) * 2;     // includes terminating NUL

                var startPos = writer.BaseStream.Position;
                Debug.Assert((startPos & 3) == 0);

                writer.Write((WORD)cbBlock);
                writer.Write((WORD)(keyValuePair.Value.Length + 1)); //add 1 for nul
                writer.Write((WORD)1);
                writer.Write(keyValuePair.Key.ToCharArray());
                writer.Write((WORD)'\0');
                writer.Write(new byte[PadKeyLen(cbKey) - cbKey]);
                Debug.Assert((writer.BaseStream.Position & 3) == 0);
                writer.Write(keyValuePair.Value.ToCharArray());
                writer.Write((WORD)'\0');
                //writer.Write(new byte[PadToDword(cbVal) - cbVal]);

                System.Diagnostics.Debug.Assert(cbBlock == writer.BaseStream.Position - startPos);
            }

            /// <summary>
            /// compute number of chars needed to end up on a 32-bit boundary assuming that three
            /// WORDS preceded this string.
            /// </summary>
            /// <param name="sz"></param>
            /// <returns></returns>
            private static int KEYSIZE(string sz)
            {
                return PadKeyLen((sz.Length + 1) * sizeof(WCHAR)) / sizeof(WCHAR);
            }
            private static int KEYBYTES(string sz)
            {
                return KEYSIZE(sz) * sizeof(WCHAR);
            }

            private int GetStringsSize()
            {
                int sum = 0;

                foreach (var verString in GetVerStrings())
                {
                    sum = (sum + 3) & ~3;   //ensure that each String data structure starts on a 32bit boundary.
                    sum += SizeofVerString(verString.Key, verString.Value);
                }

                return sum;
            }

            internal int GetDataSize()
            {
                int sizeEXEVERRESOURCE = sizeof(WORD) * 3 * 5 + 2 * sizeof(WORD) + //five headers + two words for CP and lang
                    KEYBYTES(vsVersionInfoKey) +
                    KEYBYTES(varFileInfoKey) +
                    KEYBYTES(translationKey) +
                    KEYBYTES(stringFileInfoKey) +
                    KEYBYTES(_langIdAndCodePageKey) +
                    sizeVS_FIXEDFILEINFO;

                return GetStringsSize() + sizeEXEVERRESOURCE;
            }

            internal void WriteVerResource(BinaryWriter writer)
            {
                /*
                    must be assumed to start on a 32-bit boundary.
                 * 
                 * the sub-elements of the VS_VERSIONINFO consist of a header (3 WORDS) a string
                 * and then beginning on the next 32-bit boundary, the elements children
                 
                    struct VS_VERSIONINFO
                    {
                        WORD cbRootBlock;                                     // size of whole resource
                        WORD cbRootValue;                                     // size of VS_FIXEDFILEINFO structure
                        WORD fRootText;                                       // root is text?
                        WCHAR szRootKey[KEYSIZE("VS_VERSION_INFO")];          // Holds "VS_VERSION_INFO"
                        VS_FIXEDFILEINFO vsFixed;                             // fixed information.
                          WORD cbVarBlock;                                      //   size of VarFileInfo block
                          WORD cbVarValue;                                      //   always 0
                          WORD fVarText;                                        //   VarFileInfo is text?
                          WCHAR szVarKey[KEYSIZE("VarFileInfo")];               //   Holds "VarFileInfo"
                            WORD cbTransBlock;                                    //     size of Translation block
                            WORD cbTransValue;                                    //     size of Translation value
                            WORD fTransText;                                      //     Translation is text?
                            WCHAR szTransKey[KEYSIZE("Translation")];             //     Holds "Translation"
                              WORD langid;                                          //     language id
                              WORD codepage;                                        //     codepage id
                          WORD cbStringBlock;                                   //   size of StringFileInfo block
                          WORD cbStringValue;                                   //   always 0
                          WORD fStringText;                                     //   StringFileInfo is text?
                          WCHAR szStringKey[KEYSIZE("StringFileInfo")];         //   Holds "StringFileInfo"
                            WORD cbLangCpBlock;                                   //     size of language/codepage block
                            WORD cbLangCpValue;                                   //     always 0
                            WORD fLangCpText;                                     //     LangCp is text?
                            WCHAR szLangCpKey[KEYSIZE("12345678")];               //     Holds hex version of language/codepage
                        // followed by strings
                    };
                */

                var debugPos = writer.BaseStream.Position;
                var dataSize = GetDataSize();

                writer.Write((WORD)dataSize);
                writer.Write((WORD)sizeVS_FIXEDFILEINFO);
                writer.Write((WORD)0);
                writer.Write(vsVersionInfoKey.ToCharArray());
                writer.Write(new byte[KEYBYTES(vsVersionInfoKey) - vsVersionInfoKey.Length * 2]);
                System.Diagnostics.Debug.Assert((writer.BaseStream.Position & 3) == 0);
                WriteVSFixedFileInfo(writer);
                writer.Write((WORD)(sizeof(WORD) * 2 + 2 * HDRSIZE + KEYBYTES(varFileInfoKey) + KEYBYTES(translationKey)));
                writer.Write((WORD)0);
                writer.Write((WORD)1);
                writer.Write(varFileInfoKey.ToCharArray());
                writer.Write(new byte[KEYBYTES(varFileInfoKey) - varFileInfoKey.Length * 2]);   //padding
                System.Diagnostics.Debug.Assert((writer.BaseStream.Position & 3) == 0);
                writer.Write((WORD)(sizeof(WORD) * 2 + HDRSIZE + KEYBYTES(translationKey)));
                writer.Write((WORD)(sizeof(WORD) * 2));
                writer.Write((WORD)0);
                writer.Write(translationKey.ToCharArray());
                writer.Write(new byte[KEYBYTES(translationKey) - translationKey.Length * 2]);   //padding
                System.Diagnostics.Debug.Assert((writer.BaseStream.Position & 3) == 0);
                writer.Write((WORD)0);      //langId; MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL)) = 0
                writer.Write((WORD)CP_WINUNICODE);   //codepage; 1200 = CP_WINUNICODE
                System.Diagnostics.Debug.Assert((writer.BaseStream.Position & 3) == 0);
                writer.Write((WORD)(2 * HDRSIZE + KEYBYTES(stringFileInfoKey) + KEYBYTES(_langIdAndCodePageKey) + GetStringsSize()));
                writer.Write((WORD)0);
                writer.Write((WORD)1);
                writer.Write(stringFileInfoKey.ToCharArray());      //actually preceded by 5 WORDS so not consistent with the
                                                                    //assumptions of KEYBYTES, but equivalent.
                writer.Write(new byte[KEYBYTES(stringFileInfoKey) - stringFileInfoKey.Length * 2]); //padding. 
                System.Diagnostics.Debug.Assert((writer.BaseStream.Position & 3) == 0);
                writer.Write((WORD)(HDRSIZE + KEYBYTES(_langIdAndCodePageKey) + GetStringsSize()));
                writer.Write((WORD)0);
                writer.Write((WORD)1);
                writer.Write(_langIdAndCodePageKey.ToCharArray());
                writer.Write(new byte[KEYBYTES(_langIdAndCodePageKey) - _langIdAndCodePageKey.Length * 2]); //padding
                System.Diagnostics.Debug.Assert((writer.BaseStream.Position & 3) == 0);

                System.Diagnostics.Debug.Assert(writer.BaseStream.Position - debugPos == dataSize - GetStringsSize());
                debugPos = writer.BaseStream.Position;

                foreach (var entry in GetVerStrings())
                {
                    var writerPos = writer.BaseStream.Position;

                    //write any padding necessary to align the String struct on a 32 bit boundary.
                    writer.Write(new byte[((writerPos + 3) & ~3) - writerPos]);

                    System.Diagnostics.Debug.Assert(entry.Value != null);
                    WriteVersionString(entry, writer);
                }

                System.Diagnostics.Debug.Assert(writer.BaseStream.Position - debugPos == GetStringsSize());
            }
        }
    }
}

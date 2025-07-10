// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal static class NativeResourceWriter
    {
        ////
        //// Resource Format.
        ////

        ////
        //// Resource directory consists of two counts, following by a variable length
        //// array of directory entries.  The first count is the number of entries at
        //// beginning of the array that have actual names associated with each entry.
        //// The entries are in ascending order, case insensitive strings.  The second
        //// count is the number of entries that immediately follow the named entries.
        //// This second count identifies the number of entries that have 16-bit integer
        //// Ids as their name.  These entries are also sorted in ascending order.
        ////
        //// This structure allows fast lookup by either name or number, but for any
        //// given resource entry only one form of lookup is supported, not both.
        //// This is consistent with the syntax of the .RC file and the .RES file.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY {
        //    DWORD   Characteristics;
        //    DWORD   TimeDateStamp;
        //    WORD    MajorVersion;
        //    WORD    MinorVersion;
        //    WORD    NumberOfNamedEntries;
        //    WORD    NumberOfIdEntries;
        ////  IMAGE_RESOURCE_DIRECTORY_ENTRY DirectoryEntries[];
        //} IMAGE_RESOURCE_DIRECTORY, *PIMAGE_RESOURCE_DIRECTORY;

        //#define IMAGE_RESOURCE_NAME_IS_STRING        0x80000000
        //#define IMAGE_RESOURCE_DATA_IS_DIRECTORY     0x80000000
        ////
        //// Each directory contains the 32-bit Name of the entry and an offset,
        //// relative to the beginning of the resource directory of the data associated
        //// with this directory entry.  If the name of the entry is an actual text
        //// string instead of an integer Id, then the high order bit of the name field
        //// is set to one and the low order 31-bits are an offset, relative to the
        //// beginning of the resource directory of the string, which is of type
        //// IMAGE_RESOURCE_DIRECTORY_STRING.  Otherwise the high bit is clear and the
        //// low-order 16-bits are the integer Id that identify this resource directory
        //// entry. If the directory entry is yet another resource directory (i.e. a
        //// subdirectory), then the high order bit of the offset field will be
        //// set to indicate this.  Otherwise the high bit is clear and the offset
        //// field points to a resource data entry.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY_ENTRY {
        //    union {
        //        struct {
        //            DWORD NameOffset:31;
        //            DWORD NameIsString:1;
        //        } DUMMYSTRUCTNAME;
        //        DWORD   Name;
        //        WORD    Id;
        //    } DUMMYUNIONNAME;
        //    union {
        //        DWORD   OffsetToData;
        //        struct {
        //            DWORD   OffsetToDirectory:31;
        //            DWORD   DataIsDirectory:1;
        //        } DUMMYSTRUCTNAME2;
        //    } DUMMYUNIONNAME2;
        //} IMAGE_RESOURCE_DIRECTORY_ENTRY, *PIMAGE_RESOURCE_DIRECTORY_ENTRY;

        ////
        //// For resource directory entries that have actual string names, the Name
        //// field of the directory entry points to an object of the following type.
        //// All of these string objects are stored together after the last resource
        //// directory entry and before the first resource data object.  This minimizes
        //// the impact of these variable length objects on the alignment of the fixed
        //// size directory entry objects.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY_STRING {
        //    WORD    Length;
        //    CHAR    NameString[ 1 ];
        //} IMAGE_RESOURCE_DIRECTORY_STRING, *PIMAGE_RESOURCE_DIRECTORY_STRING;

        //typedef struct _IMAGE_RESOURCE_DIR_STRING_U {
        //    WORD    Length;
        //    WCHAR   NameString[ 1 ];
        //} IMAGE_RESOURCE_DIR_STRING_U, *PIMAGE_RESOURCE_DIR_STRING_U;

        ////
        //// Each resource data entry describes a leaf node in the resource directory
        //// tree.  It contains an offset, relative to the beginning of the resource
        //// directory of the data for the resource, a size field that gives the number
        //// of bytes of data at that offset, a CodePage that should be used when
        //// decoding code point values within the resource data.  Typically for new
        //// applications the code page would be the unicode code page.
        ////

        //typedef struct _IMAGE_RESOURCE_DATA_ENTRY {
        //    DWORD   OffsetToData;
        //    DWORD   Size;
        //    DWORD   CodePage;
        //    DWORD   Reserved;
        //} IMAGE_RESOURCE_DATA_ENTRY, *PIMAGE_RESOURCE_DATA_ENTRY;

        private class Directory
        {
            internal readonly string Name;
            internal readonly int ID;
            internal ushort NumberOfNamedEntries;
            internal ushort NumberOfIdEntries;
            internal readonly List<object> Entries;

            internal Directory(string name, int id)
            {
                this.Name = name;
                this.ID = id;
                this.Entries = new List<object>();
            }
        }

        private static int CompareResources(IWin32Resource left, IWin32Resource right)
        {
            int result = CompareResourceIdentifiers(left.TypeId, left.TypeName, right.TypeId, right.TypeName);

            return (result == 0) ? CompareResourceIdentifiers(left.Id, left.Name, right.Id, right.Name) : result;
        }

        //when comparing a string vs ordinal, the string should always be less than the ordinal. Per the spec,
        //entries identified by string must precede those identified by ordinal.
        private static int CompareResourceIdentifiers(int xOrdinal, string xString, int yOrdinal, string yString)
        {
            if (xString == null)
            {
                if (yString == null)
                {
                    return xOrdinal - yOrdinal;
                }
                else
                {
                    return 1;
                }
            }
            else if (yString == null)
            {
                return -1;
            }
            else
            {
                return String.Compare(xString, yString, StringComparison.OrdinalIgnoreCase);
            }
        }

        //sort the resources by ID least to greatest then by NAME.
        //Where strings and ordinals are compared, strings are less than ordinals.
        internal static IEnumerable<IWin32Resource> SortResources(IEnumerable<IWin32Resource> resources)
        {
            return resources.OrderBy(CompareResources);
        }

        public static void SerializeWin32Resources(BlobBuilder builder, IEnumerable<IWin32Resource> theResources, int resourcesRva)
        {
            theResources = SortResources(theResources);

            Directory typeDirectory = new Directory(string.Empty, 0);
            Directory nameDirectory = null;
            Directory languageDirectory = null;
            int lastTypeID = int.MinValue;
            string lastTypeName = null;
            int lastID = int.MinValue;
            string lastName = null;
            uint sizeOfDirectoryTree = 16;

            //EDMAURER note that this list is assumed to be sorted lowest to highest 
            //first by typeId, then by Id.
            foreach (IWin32Resource r in theResources)
            {
                bool typeDifferent = (r.TypeId < 0 && r.TypeName != lastTypeName) || r.TypeId > lastTypeID;
                if (typeDifferent)
                {
                    lastTypeID = r.TypeId;
                    lastTypeName = r.TypeName;
                    if (lastTypeID < 0)
                    {
                        Debug.Assert(typeDirectory.NumberOfIdEntries == 0, "Not all Win32 resources with types encoded as strings precede those encoded as ints");
                        typeDirectory.NumberOfNamedEntries++;
                    }
                    else
                    {
                        typeDirectory.NumberOfIdEntries++;
                    }

                    sizeOfDirectoryTree += 24;
                    typeDirectory.Entries.Add(nameDirectory = new Directory(lastTypeName, lastTypeID));
                }

                if (typeDifferent || (r.Id < 0 && r.Name != lastName) || r.Id > lastID)
                {
                    lastID = r.Id;
                    lastName = r.Name;
                    if (lastID < 0)
                    {
                        Debug.Assert(nameDirectory.NumberOfIdEntries == 0, "Not all Win32 resources with names encoded as strings precede those encoded as ints");
                        nameDirectory.NumberOfNamedEntries++;
                    }
                    else
                    {
                        nameDirectory.NumberOfIdEntries++;
                    }

                    sizeOfDirectoryTree += 24;
                    nameDirectory.Entries.Add(languageDirectory = new Directory(lastName, lastID));
                }

                languageDirectory.NumberOfIdEntries++;
                sizeOfDirectoryTree += 8;
                languageDirectory.Entries.Add(r);
            }

            var dataWriter = new BlobBuilder();

            //'dataWriter' is where opaque resource data goes as well as strings that are used as type or name identifiers
            WriteDirectory(typeDirectory, builder, 0, 0, sizeOfDirectoryTree, resourcesRva, dataWriter);
            builder.LinkSuffix(dataWriter);
            builder.WriteByte(0);
            builder.Align(4);
        }

        private static void WriteDirectory(Directory directory, BlobBuilder writer, uint offset, uint level, uint sizeOfDirectoryTree, int virtualAddressBase, BlobBuilder dataWriter)
        {
            writer.WriteUInt32(0); // Characteristics
            writer.WriteUInt32(0); // Timestamp
            writer.WriteUInt32(0); // Version
            writer.WriteUInt16(directory.NumberOfNamedEntries);
            writer.WriteUInt16(directory.NumberOfIdEntries);
            uint n = (uint)directory.Entries.Count;
            uint k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                int id;
                string name;
                uint nameOffset = (uint)dataWriter.Count + sizeOfDirectoryTree;
                uint directoryOffset = k;
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    id = subDir.ID;
                    name = subDir.Name;
                    if (level == 0)
                    {
                        k += SizeOfDirectory(subDir);
                    }
                    else
                    {
                        k += 16 + 8 * (uint)subDir.Entries.Count;
                    }
                }
                else
                {
                    //EDMAURER write out an IMAGE_RESOURCE_DATA_ENTRY followed
                    //immediately by the data that it refers to. This results
                    //in a layout different than that produced by pulling the resources
                    //from an OBJ. In that case all of the data bits of a resource are
                    //contiguous in .rsrc$02. After processing these will end up at
                    //the end of .rsrc following all of the directory
                    //info and IMAGE_RESOURCE_DATA_ENTRYs
                    IWin32Resource r = (IWin32Resource)directory.Entries[i];
                    id = level == 0 ? r.TypeId : level == 1 ? r.Id : (int)r.LanguageId;
                    name = level == 0 ? r.TypeName : level == 1 ? r.Name : null;
                    dataWriter.WriteUInt32((uint)(virtualAddressBase + sizeOfDirectoryTree + 16 + dataWriter.Count));
                    byte[] data = new List<byte>(r.Data).ToArray();
                    dataWriter.WriteUInt32((uint)data.Length);
                    dataWriter.WriteUInt32(r.CodePage);
                    dataWriter.WriteUInt32(0);
                    dataWriter.WriteBytes(data);
                    while ((dataWriter.Count % 4) != 0)
                    {
                        dataWriter.WriteByte(0);
                    }
                }

                if (id >= 0)
                {
                    writer.WriteInt32(id);
                }
                else
                {
                    if (name == null)
                    {
                        name = string.Empty;
                    }

                    writer.WriteUInt32(nameOffset | 0x80000000);
                    dataWriter.WriteUInt16((ushort)name.Length);
                    dataWriter.WriteUTF16(name);
                }

                if (subDir != null)
                {
                    writer.WriteUInt32(directoryOffset | 0x80000000);
                }
                else
                {
                    writer.WriteUInt32(nameOffset);
                }
            }

            k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    WriteDirectory(subDir, writer, k, level + 1, sizeOfDirectoryTree, virtualAddressBase, dataWriter);
                    if (level == 0)
                    {
                        k += SizeOfDirectory(subDir);
                    }
                    else
                    {
                        k += 16 + 8 * (uint)subDir.Entries.Count;
                    }
                }
            }
        }

        private static uint SizeOfDirectory(Directory/*!*/ directory)
        {
            uint n = (uint)directory.Entries.Count;
            uint size = 16 + 8 * n;
            for (int i = 0; i < n; i++)
            {
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    size += 16 + 8 * (uint)subDir.Entries.Count;
                }
            }

            return size;
        }

        public static void SerializeWin32Resources(BlobBuilder builder, ResourceSection resourceSections, int resourcesRva)
        {
            var sectionWriter = new BlobWriter(builder.ReserveBytes(resourceSections.SectionBytes.Length));
            sectionWriter.WriteBytes(resourceSections.SectionBytes);

            var readStream = new MemoryStream(resourceSections.SectionBytes);
            var reader = new BinaryReader(readStream);

            foreach (int addressToFixup in resourceSections.Relocations)
            {
                sectionWriter.Offset = addressToFixup;
                reader.BaseStream.Position = addressToFixup;
                sectionWriter.WriteUInt32(reader.ReadUInt32() + (uint)resourcesRva);
            }
        }
    }
}

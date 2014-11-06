// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ConsoleApplication3
{
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_DOS_HEADER
    {
        [FieldOffset(60)]   public uint e_lfanew;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_NT_HEADERS32
    {
        [FieldOffset(0)]    public uint Signature;
        [FieldOffset(4)]    public IMAGE_FILE_HEADER FileHeader;
        [FieldOffset(24)]   public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_NT_HEADERS64
    {
        [FieldOffset(0)]   public uint Signature;
        [FieldOffset(4)]   public IMAGE_FILE_HEADER FileHeader;
        [FieldOffset(24)]  public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
    }
    public struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint   TimeDateStamp;
        public uint   PointerToSymbolTable;
        public uint   NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        [FieldOffset(0)]   public ushort Magic;
        [FieldOffset(2)]   public byte MajorLinkerVersion;
        [FieldOffset(3)]   public byte MinorLinkerVersion;
        [FieldOffset(4)]   public uint SizeOfCode;
        [FieldOffset(8)]   public uint SizeOfInitializedData;
        [FieldOffset(12)]  public uint SizeOfUninitializedData;
        [FieldOffset(16)]  public uint AddressOfEntryPoint;
        [FieldOffset(20)]  public uint BaseOfCode;
        [FieldOffset(24)]  public uint BaseOfData;                  // PE32 contains this additional field
        [FieldOffset(28)]  public uint ImageBase;
        [FieldOffset(32)]  public uint SectionAlignment;
        [FieldOffset(36)]  public uint FileAlignment;
        [FieldOffset(40)]  public ushort MajorOperatingSystemVersion;
        [FieldOffset(42)]  public ushort MinorOperatingSystemVersion;
        [FieldOffset(44)]  public ushort MajorImageVersion;
        [FieldOffset(46)]  public ushort MinorImageVersion;
        [FieldOffset(48)]  public ushort MajorSubsystemVersion;
        [FieldOffset(50)]  public ushort MinorSubsystemVersion;
        [FieldOffset(52)]  public uint Win32VersionValue;
        [FieldOffset(56)]  public uint SizeOfImage;
        [FieldOffset(60)]  public uint SizeOfHeaders;
        [FieldOffset(64)]  public uint CheckSum;
        [FieldOffset(68)]  public uint Subsystem;
        [FieldOffset(70)]  public uint DllCharacteristics;
        [FieldOffset(72)]  public uint SizeOfStackReserve;
        [FieldOffset(76)]  public uint SizeOfStackCommit;
        [FieldOffset(80)]  public uint SizeOfHeapReserve;
        [FieldOffset(84)]  public uint SizeOfHeapCommit;
        [FieldOffset(88)]  public uint LoaderFlags;
        [FieldOffset(92)]  public uint NumberOfRvaAndSizes;
        [FieldOffset(96)]  public IMAGE_DATA_DIRECTORY ExportTable;
        [FieldOffset(104)] public IMAGE_DATA_DIRECTORY ImportTable;
        [FieldOffset(112)] public IMAGE_DATA_DIRECTORY ResourceTable;
        [FieldOffset(120)] public IMAGE_DATA_DIRECTORY ExceptionTable;
        [FieldOffset(128)] public IMAGE_DATA_DIRECTORY CertificateTable;
        [FieldOffset(136)] public IMAGE_DATA_DIRECTORY BaseRelocationTable;
        [FieldOffset(144)] public IMAGE_DATA_DIRECTORY Debug;
        [FieldOffset(152)] public IMAGE_DATA_DIRECTORY Architecture;
        [FieldOffset(160)] public IMAGE_DATA_DIRECTORY GlobalPtr;
        [FieldOffset(168)] public IMAGE_DATA_DIRECTORY TLSTable;
        [FieldOffset(176)] public IMAGE_DATA_DIRECTORY LoadConfigTable;
        [FieldOffset(184)] public IMAGE_DATA_DIRECTORY BoundImport;
        [FieldOffset(192)] public IMAGE_DATA_DIRECTORY IAT;
        [FieldOffset(200)] public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
        [FieldOffset(208)] public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
        [FieldOffset(216)] public IMAGE_DATA_DIRECTORY Reserved;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        [FieldOffset(0)]   public ushort Magic;
        [FieldOffset(2)]   public byte MajorLinkerVersion;
        [FieldOffset(3)]   public byte MinorLinkerVersion;
        [FieldOffset(4)]   public uint SizeOfCode;
        [FieldOffset(8)]   public uint SizeOfInitializedData;
        [FieldOffset(12)]  public uint SizeOfUninitializedData;
        [FieldOffset(16)]  public uint AddressOfEntryPoint;
        [FieldOffset(20)]  public uint BaseOfCode;
        [FieldOffset(24)]  public ulong ImageBase;
        [FieldOffset(32)]  public uint SectionAlignment;
        [FieldOffset(36)]  public uint FileAlignment;
        [FieldOffset(40)]  public ushort MajorOperatingSystemVersion;
        [FieldOffset(42)]  public ushort MinorOperatingSystemVersion;
        [FieldOffset(44)]  public ushort MajorImageVersion;
        [FieldOffset(46)]  public ushort MinorImageVersion;
        [FieldOffset(48)]  public ushort MajorSubsystemVersion;
        [FieldOffset(50)]  public ushort MinorSubsystemVersion;
        [FieldOffset(52)]  public uint Win32VersionValue;
        [FieldOffset(56)]  public uint SizeOfImage;
        [FieldOffset(60)]  public uint SizeOfHeaders;
        [FieldOffset(64)]  public uint CheckSum;
        [FieldOffset(68)]  public ushort Subsystem;
        [FieldOffset(70)]  public ushort DllCharacteristics;
        [FieldOffset(72)]  public ulong SizeOfStackReserve;
        [FieldOffset(80)]  public ulong SizeOfStackCommit;
        [FieldOffset(88)]  public ulong SizeOfHeapReserve;
        [FieldOffset(96)]  public ulong SizeOfHeapCommit;
        [FieldOffset(104)] public uint LoaderFlags;
        [FieldOffset(108)] public uint NumberOfRvaAndSizes;
        [FieldOffset(112)] public IMAGE_DATA_DIRECTORY ExportTable;
        [FieldOffset(120)] public IMAGE_DATA_DIRECTORY ImportTable;
        [FieldOffset(128)] public IMAGE_DATA_DIRECTORY ResourceTable;
        [FieldOffset(136)] public IMAGE_DATA_DIRECTORY ExceptionTable;
        [FieldOffset(144)] public IMAGE_DATA_DIRECTORY CertificateTable;
        [FieldOffset(152)] public IMAGE_DATA_DIRECTORY BaseRelocationTable;
        [FieldOffset(160)] public IMAGE_DATA_DIRECTORY Debug;
        [FieldOffset(168)] public IMAGE_DATA_DIRECTORY Architecture;
        [FieldOffset(176)] public IMAGE_DATA_DIRECTORY GlobalPtr;
        [FieldOffset(184)] public IMAGE_DATA_DIRECTORY TLSTable;
        [FieldOffset(192)] public IMAGE_DATA_DIRECTORY LoadConfigTable;
        [FieldOffset(200)] public IMAGE_DATA_DIRECTORY BoundImport;
        [FieldOffset(208)] public IMAGE_DATA_DIRECTORY IAT;
        [FieldOffset(216)] public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
        [FieldOffset(224)] public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
        [FieldOffset(232)] public IMAGE_DATA_DIRECTORY Reserved;
    }
    public struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_SECTION_HEADER
    {
        [FieldOffset(8)]  public uint   VirtualSize;
        [FieldOffset(12)] public uint   VirtualAddress;
        [FieldOffset(16)] public uint   SizeOfRawData;
        [FieldOffset(20)] public uint   PointerToRawData;
        [FieldOffset(24)] public uint   PointerToRelocations;
        [FieldOffset(28)] public uint   PointerToLinenumbers;
        [FieldOffset(32)] public ushort NumberOfRelocations;
        [FieldOffset(34)] public ushort NumberOfLinenumbers;
        [FieldOffset(36)] public ushort Characteristics;
    }

    public struct IMAGE_COR20_HEADER
    {
        public uint   cb;
        public ushort MajorRuntimeVersion;
        public ushort MinorRuntimeVersion;

        // Symbol table and startup information
        public IMAGE_DATA_DIRECTORY MetaData;
        public uint Flags;
        public uint EntryPoint;     // was union { DWORD EntryPointToken; DWORD EntryPointRVA; };

        // Binding information
        public IMAGE_DATA_DIRECTORY Resources;
        public IMAGE_DATA_DIRECTORY StrongNameSignature;

        // Regular fixup and binding information
        public IMAGE_DATA_DIRECTORY CodeManagerTable;
        public IMAGE_DATA_DIRECTORY VTableFixups;
        public IMAGE_DATA_DIRECTORY ExportAddressTableJumps;
        public IMAGE_DATA_DIRECTORY ManagedNativeHeader;                   // Precompiled image info (internal use only - set to zero)
    }

    static class BufferHelpers
    {
        static byte[] EmptyByteArray = new byte[0];
        static public byte[] readBlock(this FileStream stream, uint offset, int length)
        {
            byte[] data = new byte[length];
            var x = stream.Seek(offset, SeekOrigin.Begin);
            long bytes = stream.Read(data, 0, (int)length);
            if (bytes == 0) data = EmptyByteArray;
            return data;
        }

        static public void writeBlock(this FileStream stream, byte[] data, uint offset)
        {
            var x = stream.Seek(offset, SeekOrigin.Begin);
            stream.Write(data, 0, data.Length);
            return;
        }
        public static unsafe IMAGE_DOS_HEADER GetIMAGE_DOS_HEADER(this FileStream stream, uint location)
        {
            byte[] data = stream.readBlock(location, sizeof(IMAGE_DOS_HEADER));
            if (data.Length == 0) throw new System.InvalidProgramException();
            fixed (byte* pData = data) { return *(IMAGE_DOS_HEADER*)pData; }
        }

        public static unsafe ushort GetMagic(this FileStream stream, uint location)
        {
            byte[] data = stream.readBlock(location, sizeof(ushort));
            if (data.Length == 0) throw new System.InvalidProgramException();
            fixed (byte* pData = data) { return *(ushort*)pData; }
        }

        public static unsafe IMAGE_NT_HEADERS32 GetIMAGE_NT_HEADERS32(this FileStream stream, uint location)
        {
            byte[] data = stream.readBlock(location, sizeof(IMAGE_NT_HEADERS32));
            if (data.Length == 0) throw new System.InvalidProgramException();
            fixed (byte* pData = data) { return *(IMAGE_NT_HEADERS32*)pData; }
        }

        public static unsafe IMAGE_NT_HEADERS64 GetIMAGE_NT_HEADERS64(this FileStream stream, uint location)
        {
            byte[] data = stream.readBlock(location, sizeof(IMAGE_NT_HEADERS64));
            if (data.Length == 0) throw new System.InvalidProgramException();
            fixed (byte* pData = data) { return *(IMAGE_NT_HEADERS64*)pData; }
        }

        public static unsafe IMAGE_SECTION_HEADER GetIMAGE_SECTION_HEADER(this FileStream stream, uint location)
        {
            byte[] data = stream.readBlock(location, sizeof(IMAGE_SECTION_HEADER));
            if (data.Length == 0) throw new System.InvalidProgramException();
            fixed (byte* pData = data) { return *(IMAGE_SECTION_HEADER*)pData; }
        }

        public static unsafe IMAGE_COR20_HEADER GetIMAGE_COR20_HEADER(this FileStream stream, uint location)
        {
            byte[] data = stream.readBlock(location, sizeof(IMAGE_COR20_HEADER));
            if (data.Length == 0) throw new System.InvalidProgramException();
            fixed (byte* pData = data) { return *(IMAGE_COR20_HEADER*)pData; }
        }

        public static unsafe void SetIMAGE_COR20_HEADER(this FileStream stream, uint location, IMAGE_COR20_HEADER cor20Header)
        {
            byte[] data = cor20Header.GetBytes();
            stream.writeBlock(data, location);
        }

        public static uint MapRvaToFileOffset(this List<IMAGE_SECTION_HEADER> sections, uint offset)
        {
            // Map RvaTo FileLocation
            foreach (var section in sections)
            {
                if (offset >= section.VirtualAddress && offset < section.VirtualAddress + section.VirtualSize)
                {
                    // Rva is in this section
                    return (offset - section.VirtualAddress) + section.PointerToRawData;
                }
            }
            throw new System.InvalidProgramException();
        }

        public static byte[] GetBytes<T>(this T cor20Header)
        {
            int size = Marshal.SizeOf(cor20Header);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(cor20Header, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }

    class Program
    {
        static unsafe void  FlipDelaySignedBit(string path)
        {
            // Open the file
            FileInfo fileInfo = new FileInfo(path);
            using (FileStream pe = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite))
            {
                var dosHeader = pe.GetIMAGE_DOS_HEADER(0);
                var magic = pe.GetMagic(dosHeader.e_lfanew);
                var sections = new List<IMAGE_SECTION_HEADER>();
                uint clrRuntimeHeaderSize = 0;
                uint clrRuntimeHeaderRva = 0;
                if (magic == 0x20b)
                {
                    var ntHeaders = pe.GetIMAGE_NT_HEADERS64(dosHeader.e_lfanew);
                    clrRuntimeHeaderSize = ntHeaders.OptionalHeader.CLRRuntimeHeader.Size;
                    clrRuntimeHeaderRva = ntHeaders.OptionalHeader.CLRRuntimeHeader.VirtualAddress;
                    for (int i = 0; i < ntHeaders.FileHeader.NumberOfSections; i++)
                    {
                        uint offset = (uint)(dosHeader.e_lfanew + sizeof(IMAGE_NT_HEADERS64) + (i * sizeof(IMAGE_SECTION_HEADER)));
                        var section = pe.GetIMAGE_SECTION_HEADER(offset);
                        sections.Add(section);
                    }
                }
                else
                {
                    var ntHeaders = pe.GetIMAGE_NT_HEADERS32(dosHeader.e_lfanew);
                    clrRuntimeHeaderSize = ntHeaders.OptionalHeader.CLRRuntimeHeader.Size;
                    clrRuntimeHeaderRva = ntHeaders.OptionalHeader.CLRRuntimeHeader.VirtualAddress;
                    for (int i = 0; i < ntHeaders.FileHeader.NumberOfSections; i++)
                    {
                        uint offset = (uint)(dosHeader.e_lfanew + sizeof(IMAGE_NT_HEADERS32) + (i * sizeof(IMAGE_SECTION_HEADER)));
                        var section = pe.GetIMAGE_SECTION_HEADER(offset);
                        sections.Add(section);
                    }
                }

                var clrHeaderOffset = sections.MapRvaToFileOffset(clrRuntimeHeaderRva);
                var Cor20Header= pe.GetIMAGE_COR20_HEADER(clrHeaderOffset);

                var flagsOffSet = Marshal.OffsetOf(typeof(IMAGE_COR20_HEADER), "Flags");
                Cor20Header.Flags |= 0x8;                               // Make it signed
                pe.SetIMAGE_COR20_HEADER(clrHeaderOffset, Cor20Header);
                pe.Flush();
            }
            return;
        }


        static int Main(string[] args)
        {
            // Create a byte array to hold the information.
            if (args.Length == 0)
            {
                Console.WriteLine("No file passed");
                return 1;
            }
            FlipDelaySignedBit(args[0]);
            return 0;
        }
    }
}


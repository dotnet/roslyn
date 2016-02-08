using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging.Patching
{
    internal static unsafe class Delta
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DeltaInput
        {
            public byte* pBuf;
            public IntPtr cbBuf; // SIZE_T, so different size on x86/x64
            [MarshalAs(UnmanagedType.Bool)]
            public bool editable;

            public DeltaInput(byte* pBuf_, int cbBuf_, bool editable_) : this()
            {
                pBuf = pBuf_;
                cbBuf = new IntPtr(cbBuf_);
                editable = editable_;
            }

            public static DeltaInput Empty = new DeltaInput();
        } // end struct DeltaInput


        [StructLayout(LayoutKind.Sequential)]
        struct DeltaOutput
        {
            public IntPtr pBuf;
            public IntPtr cbBuf; // SIZE_T, so different size on x86/x64
        } // end struct DeltaOutput

        [Flags]
        enum DeltaFileType : long
        {
            Raw = 0x01,
            i386 = 0x02,
            ia64 = 0x04,
            amd64 = 0x08,
            Executable = Raw | i386 | ia64 | amd64
        }

        [Flags]
        enum DeltaApplyFlag : long
        {
            None = 0,
            AllowPa19 = 0x00000001
        }

        [Flags]
        enum DeltaFlag : long
        {
            None = 0,
            E8 = 0x00000001, // Transform E8 pieces (relative calls in x86) of target file.
            Mark = 0x00000002, // Mark non-executable parts of source PE.
            Imports = 0x00000004, // Transform imports of source PE.
            Exports = 0x00000008, // Transform exports of source PE.
            Resources = 0x00000010, // Transform resources of source PE.
            Relocs = 0x00000020, // Transform relocations of source PE.
            i386SmashLock = 0x00000040, // Smash lock prefixes of source PE.
            i386Jmps = 0x00000080, // Transform relative jumps of source I386 (x86) PE.
            i386Calls = 0x00000100, // Transform relative calls of source I386 (x86) PE.
            amd64Disasm = 0x00000200, // Transform instructions of source AMD64 (x86-64) PE.
            amd64PData = 0x00000400, // Transform pdata of source AMD64 (x86-64) PE.
            ia64Disasm = 0x00000800, // Transform intstructions of source IA64 (Itanium) PE.
            ia64PData = 0x00001000, // Transform pdata of source IA64 (Itanium) PE.
            Unbind = 0x00002000, // Unbind source PE.
            CliDisasm = 0x00004000, // Transform CLI instructions of source PE.
            CliMetadata = 0x00008000, // Transform CLI Metadata of source PE.
            Headers = 0x00010000, // Transform headers of source PE.
            IgnoreFileSizeLimit = 0x00020000, // Allow source or target file or buffer to exceed its default size limit.
            IgnoreOptionsSizeLimit = 0x00040000, // Allow options buffer or file to exceeed its default size limit.

            DefaultRaw = None,

            DefaultI386 = (Mark |
                                          Imports |
                                          Exports |
                                          Resources |
                                          Relocs |
                                          i386SmashLock |
                                          i386Jmps |
                                          i386Calls |
                                          Unbind |
                                          CliDisasm |
                                          CliMetadata),

            DefaultIa64 = (Mark |
                                          Imports |
                                          Exports |
                                          Resources |
                                          Relocs |
                                          ia64Disasm |
                                          ia64PData |
                                          Unbind |
                                          CliDisasm |
                                          CliMetadata),

            DefaultAmd64 = (Mark |
                                          Imports |
                                          Exports |
                                          Resources |
                                          Relocs |
                                          amd64Disasm |
                                          amd64PData |
                                          Unbind |
                                          CliDisasm |
                                          CliMetadata)
        } // end DeltaFlag enum


        [StructLayout(LayoutKind.Sequential)]
        unsafe struct DeltaHash
        {
            public int HashSize; // do not exceed 32 (DELTA_MAX_HASH_SIZE)
            public fixed byte HashValue[32];
        }


        enum AlgId : int
        {
            None = 0,
            Crc32FromMsdelta = 32
        }


        [StructLayout(LayoutKind.Sequential)]
        class DeltaHeaderInfo
        {
            public DeltaFileType FileTypeSet;   // Used file type set.
            public DeltaFileType FileType;      // Source file type.
            public DeltaFlag Flags;
            public Int64 TargetSize;            // Size of target file in bytes.
            public System.Runtime.InteropServices.ComTypes.FILETIME TargetFileTime; // Time of target file.
            public AlgId TarrgetHashAlgId;        // Algorithm used for hashing.
            public DeltaHash TargetHash;        // target hash.
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("msdelta.dll", SetLastError = true)]
        private static extern bool CreateDeltaB(
                DeltaFileType fileTypeSet,   // File type set.
                DeltaFlag setFlags,          // Set these flags.
                DeltaFlag resetFlags,        // Reset (suppress) these flags.
                DeltaInput source,           // Source memory block.
                DeltaInput target,           // Target memory block.
                DeltaInput sourceOptions,    // Memory block with source-specific options.
                DeltaInput targetOptions,    // Memory block with target-specific options.
                DeltaInput globalOptions,    // Memory block with global options.
                ref System.Runtime.InteropServices.ComTypes.FILETIME targetFileTime, // Target file time to use, null to use current time.
                AlgId hashAlgId,
                out DeltaOutput delta);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("msdelta.dll", SetLastError = true)]
        private static extern bool CreateDeltaB(
                DeltaFileType fileTypeSet,   // File type set.
                DeltaFlag setFlags,          // Set these flags.
                DeltaFlag resetFlags,        // Reset (suppress) these flags.
                DeltaInput source,           // Source memory block.
                DeltaInput target,           // Target memory block.
                DeltaInput sourceOptions,    // Memory block with source-specific options.
                DeltaInput targetOptions,    // Memory block with target-specific options.
                DeltaInput globalOptions,    // Memory block with global options.
                IntPtr targetFileTime,       // Target file time to use, null to use current time. (need this overload because the compiler can't convert from "null" to "ref FILETIME")
                AlgId hashAlgId,
                out DeltaOutput delta);


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("msdelta.dll", SetLastError = true)]
        private static extern bool ApplyDeltaB(
                DeltaApplyFlag applyFlags,
                DeltaInput source,
                DeltaInput delta,
                out DeltaOutput target);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("msdelta.dll", SetLastError = true)]
        private static extern bool DeltaFree(IntPtr memory);


        // Warning: the sourceBytes byte[] will get zeroed out.
        public static unsafe byte[] CreatePatch(byte[] sourceBytes, byte[] targetBytes)
        {
            fixed (byte* pSourceBuf = sourceBytes)
            fixed (byte* pTargetBuf = targetBytes)
            {
                DeltaInput ds = new DeltaInput(pSourceBuf, sourceBytes.Length, true);
                DeltaInput dt = new DeltaInput(pTargetBuf, targetBytes.Length, true);
                DeltaOutput output;

                if (!CreateDeltaB(DeltaFileType.Executable,
                                   DeltaFlag.None,
                                   DeltaFlag.None,  // "reset flags"
                                   ds,
                                   dt,
                                   DeltaInput.Empty,
                                   DeltaInput.Empty,
                                   DeltaInput.Empty,
                                   IntPtr.Zero,
                                   AlgId.Crc32FromMsdelta,
                                   out output))
                {
                    throw new Win32Exception();
                }

                byte[] patchBytes = new byte[output.cbBuf.ToInt32()];
                Marshal.Copy(output.pBuf, patchBytes, 0, patchBytes.Length);
                DeltaFree(output.pBuf);
                return patchBytes;
            }
        } // end CreatePatch()

        public static unsafe byte[] ApplyPatch(byte[] sourceBytes, byte[] patchBytes)
        {
            fixed (byte* pSourceBuf = sourceBytes)
            fixed (byte* pPatchBuf = patchBytes)
            {
                DeltaInput ds = new DeltaInput(pSourceBuf, sourceBytes.Length, true);
                DeltaInput dp = new DeltaInput(pPatchBuf, patchBytes.Length, true);
                DeltaOutput output;

                if (!ApplyDeltaB(DeltaApplyFlag.None,
                                  ds,
                                  dp,
                                  out output))
                {
                    throw new Win32Exception();
                }

                byte[] targetBytes = new byte[output.cbBuf.ToInt32()];
                Marshal.Copy(output.pBuf, targetBytes, 0, targetBytes.Length);
                DeltaFree(output.pBuf);
                return targetBytes;
            }
        } // end ApplyPatch()
    } // end class Delta
}
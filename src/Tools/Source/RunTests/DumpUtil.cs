using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace RunTests
{
    internal static class DumpUtil
    {
        [Flags]
        private enum DumpType : uint
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000,
            MiniDumpWithoutAuxiliaryState = 0x00004000,
            MiniDumpWithFullAuxiliaryState = 0x00008000,
            MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
            MiniDumpIgnoreInaccessibleMemory = 0x00020000,
            MiniDumpValidTypeFlags = 0x0003ffff,
        };

        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(
          IntPtr hProcess,
          uint processId,
          SafeFileHandle hFile,
          uint dumpType,
          IntPtr exceptionParam,
          IntPtr userStreamParam,
          IntPtr callbackParam);

        internal static void WriteDump(Process process, string dumpFilePath)
        {
            using (var stream = File.Open(dumpFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var success = MiniDumpWriteDump(
                    process.Handle,
                    (uint)process.Id,
                    stream.SafeFileHandle,
                    (uint)(DumpType.MiniDumpNormal | DumpType.MiniDumpWithFullMemory | DumpType.MiniDumpWithFullMemoryInfo),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
                if (!success)
                {
                    throw new Exception($"Creating the minidump failed with error code {Marshal.GetLastWin32Error()}");
                }
            }
        }
    }
}

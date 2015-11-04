// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal partial class ServerDispatcher
    {
        /// <summary>
        /// Uses p/invoke to gain access to information about how much memory this process is using
        /// and how much is still available.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private class MemoryHelper
        {
            private MemoryHelper()
            {
                this.Length = (int)Marshal.SizeOf(this);
            }

            // The length field must be set to the size of this data structure.
            public int Length;
            public int PercentPhysicalUsed;
            public ulong MaxPhysical;
            public ulong AvailablePhysical;
            public ulong MaxPageFile;
            public ulong AvailablePageFile;
            public ulong MaxVirtual;
            public ulong AvailableVirtual;
            public ulong Reserved; //always 0

            public static bool IsMemoryAvailable()
            {
                MemoryHelper status = new MemoryHelper();
                GlobalMemoryStatusEx(status);
                ulong max = status.MaxVirtual;
                ulong free = status.AvailableVirtual;

                int shift = 20;
                string unit = "MB";
                if (free >> shift == 0)
                {
                    shift = 10;
                    unit = "KB";
                }

                CompilerServerLogger.Log("Free memory: {1}{0} of {2}{0}.", unit, free >> shift, max >> shift);

                return free >= 800 << 20; // Value (500MB) is arbitrary; feel free to improve.
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GlobalMemoryStatusEx([In, Out] MemoryHelper buffer);
        }
    }
}

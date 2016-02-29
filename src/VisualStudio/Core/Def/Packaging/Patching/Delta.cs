// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging.Patching
{
    /// <summary>
    /// Wrapper around the msdelta api so we can consume patches produced by the Elfie service.
    /// Pinvokes and code provided by Dan Thompson
    /// </summary>
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
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DeltaOutput
        {
            public IntPtr pBuf;
            public IntPtr cbBuf; // SIZE_T, so different size on x86/x64
        }

        [Flags]
        private enum DeltaApplyFlag : long
        {
            None = 0,
            AllowPa19 = 0x00000001
        }

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
        }
    }
}
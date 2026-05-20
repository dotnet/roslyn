// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Security;

#pragma warning disable CS0436 // Type conflicts with imported type: SuppressUnmanagedCodeSecurity

namespace Microsoft.CodeAnalysis.Interop
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D"), SuppressUnmanagedCodeSecurity]
    [GeneratedWhenPossibleComInterface]
    internal partial interface IClrStrongName
    {
        void GetHashFromAssemblyFile(
            [MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            int cchHash,
            out int pchHash);

        void GetHashFromAssemblyFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            int cchHash,
            out int pchHash);

        void GetHashFromBlob(
            IntPtr pbBlob,
            int cchBlob,
            ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] pbHash,
            int cchHash,
            out int pchHash);

        void GetHashFromFile(
            [MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            int cchHash,
            out int pchHash);

        void GetHashFromFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            int cchHash,
            out int pchHash);

        void GetHashFromHandle(
            IntPtr hFile,
            ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            int cchHash,
            out int pchHash);

        int StrongNameCompareAssemblies(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly1,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly2);

        void StrongNameFreeBuffer(
            IntPtr pbMemory);

        void StrongNameGetBlob(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbBlob,
            ref int pcbBlob);

        void StrongNameGetBlobFromImage(
            IntPtr pbBase,
            int dwLength,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbBlob,
            ref int pcbBlob);

        void StrongNameGetPublicKey(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            IntPtr pbKeyBlob,
            int cbKeyBlob,
            out IntPtr ppbPublicKeyBlob,
            out int pcbPublicKeyBlob);

        int StrongNameHashSize(
            int ulHashAlg);

        void StrongNameKeyDelete(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer);

        void StrongNameKeyGen(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            int dwFlags,
            out IntPtr ppbKeyBlob,
            out int pcbKeyBlob);

        void StrongNameKeyGenEx(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            int dwFlags,
            int dwKeySize,
            out IntPtr ppbKeyBlob,
            out int pcbKeyBlob);

        void StrongNameKeyInstall(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            IntPtr pbKeyBlob,
            int cbKeyBlob);

        void StrongNameSignatureGeneration(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            IntPtr pbKeyBlob,
            int cbKeyBlob,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] ppbSignatureBlob,
            out int pcbSignatureBlob);

        void StrongNameSignatureGenerationEx(
            [MarshalAs(UnmanagedType.LPWStr)] string wszFilePath,
            [MarshalAs(UnmanagedType.LPWStr)] string wszKeyContainer,
            IntPtr pbKeyBlob,
            int cbKeyBlob,
            out IntPtr ppbSignatureBlob,
            out int pcbSignatureBlob,
            int dwFlags);

        void StrongNameSignatureSize(
            IntPtr pbPublicKeyBlob,
            int cbPublicKeyBlob,
            out int pcbSize);

        int StrongNameSignatureVerification(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            int dwInFlags);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool StrongNameSignatureVerificationEx(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [MarshalAs(UnmanagedType.Bool)] bool fForceVerification,
            out IntPtr ptr);

        int StrongNameSignatureVerificationFromImage(
            IntPtr pbBase,
            int dwLength,
            int dwInFlags);

        void StrongNameTokenFromAssembly(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            out int pcbStrongNameToken);

        void StrongNameTokenFromAssemblyEx(
            [MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            out int pcbStrongNameToken,
            out IntPtr ppbPublicKeyBlob,
            out int pcbPublicKeyBlob);

        void StrongNameTokenFromPublicKey(
            IntPtr pbPublicKeyBlob,
            int cbPublicKeyBlob,
            out IntPtr ppbStrongNameToken,
            out int pcbStrongNameToken);
    }
}

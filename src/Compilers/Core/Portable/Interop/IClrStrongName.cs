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
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D"), SuppressUnmanagedCodeSecurity]
    internal interface IClrStrongName
    {
        void GetHashFromAssemblyFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromAssemblyFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromBlob(
            [In] IntPtr pbBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cchBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromHandle(
            [In] IntPtr hFile,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameCompareAssemblies(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly1,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly2);

        void StrongNameFreeBuffer(
            [In] IntPtr pbMemory);

        void StrongNameGetBlob(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        void StrongNameGetBlobFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        void StrongNameGetPublicKey(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameHashSize(
            [In, MarshalAs(UnmanagedType.U4)] int ulHashAlg);

        void StrongNameKeyDelete(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer);

        void StrongNameKeyGen(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        void StrongNameKeyGenEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            [In, MarshalAs(UnmanagedType.U4)] int dwKeySize,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        void StrongNameKeyInstall(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob);

        void StrongNameSignatureGeneration(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob);

        void StrongNameSignatureGenerationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            out IntPtr ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags);

        void StrongNameSignatureSize(
            [In] IntPtr pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSize);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameSignatureVerification(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool StrongNameSignatureVerificationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.Bool)] bool fForceVerification,
            out IntPtr ptr);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameSignatureVerificationFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags);

        void StrongNameTokenFromAssembly(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);

        void StrongNameTokenFromAssemblyEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        void StrongNameTokenFromPublicKey(
            [In] IntPtr pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);
    }
}


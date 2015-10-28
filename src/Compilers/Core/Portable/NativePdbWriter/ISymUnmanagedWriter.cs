// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Cci
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedDocumentWriter
    {
        void SetSource(uint sourceSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] source);
        void SetCheckSum(Guid algorithmId, uint checkSumSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] checkSum);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0B97726E-9E6D-4f05-9A26-424022093CAA"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter2
    {
        ISymUnmanagedDocumentWriter DefineDocument(string url, ref Guid language, ref Guid languageVendor, ref Guid documentType);
        void SetUserEntryPoint(uint entryMethod);
        void OpenMethod(uint method);
        void CloseMethod();
        uint OpenScope(uint startOffset);
        void CloseScope(uint endOffset);
        void SetScopeRange(uint scopeID, uint startOffset, uint endOffset);
        void DefineLocalVariable(string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint startOffset, uint endOffset);
        void DefineParameter(string name, uint attributes, uint sequence, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineField(uint parent, string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineGlobalVariable(string name, uint attributes, uint sig, IntPtr signature, uint addrKind, uint addr1, uint addr2, uint addr3);
        void Close();
        void SetSymAttribute(uint parent, string name, uint data, IntPtr signature);
        void OpenNamespace(string name);
        void CloseNamespace();
        void UsingNamespace(string fullName);
        void SetMethodSourceRange(ISymUnmanagedDocumentWriter startDoc, uint startLine, uint startColumn, object endDoc, uint endLine, uint endColumn);
        void Initialize([MarshalAs(UnmanagedType.IUnknown)] object emitter, string filename, [MarshalAs(UnmanagedType.IUnknown)] object ptrIStream, bool fullBuild);
        void GetDebugInfo(ref ImageDebugDirectory ptrIDD, uint dataCount, out uint dataCountPtr, IntPtr data);
        void DefineSequencePoints(ISymUnmanagedDocumentWriter document, uint count,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] offsets,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] lines,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] columns,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] endLines,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] endColumns);
        void RemapToken(uint oldToken, uint newToken);
        void Initialize2([MarshalAs(UnmanagedType.IUnknown)] object emitter, string tempfilename, [MarshalAs(UnmanagedType.IUnknown)] object ptrIStream, bool fullBuild, string finalfilename);
        void DefineConstant(string name, object value, uint sig, IntPtr signature);
        void Abort();
        void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset);
        void DefineGlobalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3);

        /// <remarks>
        /// <paramref name="value"/> has type <see cref="VariantStructure"/>, rather than <see cref="object"/>,
        /// so that we can do custom marshalling of <see cref="System.DateTime"/>.  Unfortunately, .NET marshals
        /// <see cref="System.DateTime"/>s as the number of days since 1899/12/30, whereas the native VB compiler
        ///  marshalled them as the number of ticks since the Unix epoch (i.e. a much, much larger number).
        /// </remarks>
        void DefineConstant2([MarshalAs(UnmanagedType.LPWStr)] string name, VariantStructure value, uint sigToken);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("98ECEE1E-752D-11d3-8D56-00C04F680B2B"), SuppressUnmanagedCodeSecurity]
    internal interface IPdbWriter
    {
        int __SetPath(/*[in] const WCHAR* szFullPathName, [in] IStream* pIStream, [in] BOOL fFullBuild*/);
        int __OpenMod(/*[in] const WCHAR* szModuleName, [in] const WCHAR* szFileName*/);
        int __CloseMod();
        int __GetPath(/*[in] DWORD ccData,[out] DWORD* pccData,[out, size_is(ccData),length_is(*pccData)] WCHAR szPath[]*/);

        void GetSignatureAge(out uint sig, out uint age);
    }

    internal static class ISymUnmanagedWriter2Helper
    {
        public static unsafe void DefineConstant2(this ISymUnmanagedWriter2 writer, string name, object value, uint sigToken)
        {
            VariantStructure variant = new VariantStructure();
            Marshal.GetNativeVariantForObject(value, new IntPtr(&variant));
            writer.DefineConstant2(name, variant, sigToken);
        }
    }

    /// <summary>
    /// A struct with the same size and layout as the native VARIANT type:
    ///   2 bytes for a discriminator (i.e. which type of variant it is).
    ///   6 bytes of padding
    ///   8 or 16 bytes of data
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct VariantStructure
    {
        public VariantStructure(DateTime date) : this() // Need this to avoid errors about the uninteresting union fields.
        {
            _longValue = date.Ticks;
            _type = (short)VarEnum.VT_DATE;
        }

        [FieldOffset(0)]
        private readonly short _type;

        [FieldOffset(8)]
        private readonly long _longValue;

        /// <summary>
        /// This field determines the size of the struct 
        /// (16 bytes on 32-bit platforms, 24 bytes on 64-bit platforms).
        /// </summary>
        [FieldOffset(8)]
        private readonly VariantPadding _padding;

        // Fields below this point are only used to make inspecting this struct in the debugger easier.

        [FieldOffset(0)] // NB: 0, not 8
        private readonly decimal _decimalValue;

        [FieldOffset(8)]
        private readonly bool _boolValue;

        [FieldOffset(8)]
        private readonly long _intValue;

        [FieldOffset(8)]
        private readonly double _doubleValue;
    }

    /// <summary>
    /// This type is 8 bytes on a 32-bit platforms and 16 bytes on 64-bit platforms.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct VariantPadding
    {
        public readonly IntPtr Data2;
        public readonly IntPtr Data3;
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("DCF7780D-BDE9-45DF-ACFE-21731A32000C"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter5 : ISymUnmanagedWriter2
    {
        //  ISymUnmanagedWriter, ISymUnmanagedWriter2, ISymUnmanagedWriter3, ISymUnmanagedWriter4
        void _VtblGap1_30();

        //  ISymUnmanagedWriter5
        void OpenMapTokensToSourceSpans();
        void CloseMapTokensToSourceSpans();
        void MapTokenToSourceSpan(uint token, ISymUnmanagedDocumentWriter document, uint startLine, uint startColumn, uint endLine, uint endColumn);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CA6C2ED9-103D-46A9-B03B-05446485848B"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter6 : ISymUnmanagedWriter5
    {
        //  ISymUnmanagedWriter, ISymUnmanagedWriter2, ISymUnmanagedWriter3, ISymUnmanagedWriter4, ISymUnmanagedWriter5
        void _VtblGap1_33();

        // ISymUnmanagedWriter6
        void InitializeDeterministic([MarshalAs(UnmanagedType.IUnknown)] object emitter, [MarshalAs(UnmanagedType.IUnknown)] object stream);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("22DAEAF2-70F6-4EF1-B0C3-984F0BF27BFD"), SuppressUnmanagedCodeSecurity]
    interface ISymUnmanagedWriter7 : ISymUnmanagedWriter6
    {
        //  ISymUnmanagedWriter, ISymUnmanagedWriter2, ISymUnmanagedWriter3, ISymUnmanagedWriter4, ISymUnmanagedWriter5, ISymUnmanagedWriter6
        void _VtblGap1_34();

        // ISymUnmanagedWriter7
        unsafe void UpdateSignatureByHashingContent([In]byte* buffer, int size);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("B473C610-C958-4C3D-99A0-F2BA0A38807C"), SuppressUnmanagedCodeSecurity]
    interface ISymUnmanagedWriter100 : ISymUnmanagedWriter6
    {
        //  ISymUnmanagedWriter, ISymUnmanagedWriter2, ISymUnmanagedWriter3, ISymUnmanagedWriter4, ISymUnmanagedWriter5, ISymUnmanagedWriter6
        void _VtblGap1_34();

        // ISymUnmanagedWriter100
        void SetSignature(uint sig, Guid sig70);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ImageDebugDirectory
    {
        internal int Characteristics;
        internal int TimeDateStamp;
        internal short MajorVersion;
        internal short MinorVersion;
        internal int Type;
        internal int SizeOfData;
        internal int AddressOfRawData;
        internal int PointerToRawData;
    }
}

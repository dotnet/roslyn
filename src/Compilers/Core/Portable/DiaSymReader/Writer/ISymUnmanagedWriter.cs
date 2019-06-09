// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.DiaSymReader
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("98ECEE1E-752D-11d3-8D56-00C04F680B2B"), SuppressUnmanagedCodeSecurity]
    internal interface IPdbWriter
    {
        int __SetPath(/*[in] const WCHAR* szFullPathName, [in] IStream* pIStream, [in] BOOL fFullBuild*/);
        int __OpenMod(/*[in] const WCHAR* szModuleName, [in] const WCHAR* szFileName*/);
        int __CloseMod();
        int __GetPath(/*[in] DWORD ccData,[out] DWORD* pccData,[out, size_is(ccData),length_is(*pccData)] WCHAR szPath[]*/);

        void GetSignatureAge(out uint sig, out int age);
    }

    /// <summary>
    /// The highest version of the interface available on Desktop FX 4.0+.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("DCF7780D-BDE9-45DF-ACFE-21731A32000C"), SuppressUnmanagedCodeSecurity]
    internal unsafe interface ISymUnmanagedWriter5
    {
        #region ISymUnmanagedWriter

        ISymUnmanagedDocumentWriter DefineDocument(string url, ref Guid language, ref Guid languageVendor, ref Guid documentType);
        void SetUserEntryPoint(int entryMethodToken);
        void OpenMethod(uint methodToken);
        void CloseMethod();
        uint OpenScope(int startOffset);
        void CloseScope(int endOffset);
        void SetScopeRange(uint scopeID, uint startOffset, uint endOffset);
        void DefineLocalVariable(string name, uint attributes, uint sig, byte* signature, uint addrKind, uint addr1, uint addr2, uint startOffset, uint endOffset);
        void DefineParameter(string name, uint attributes, uint sequence, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineField(uint parent, string name, uint attributes, uint sig, byte* signature, uint addrKind, uint addr1, uint addr2, uint addr3);
        void DefineGlobalVariable(string name, uint attributes, uint sig, byte* signature, uint addrKind, uint addr1, uint addr2, uint addr3);
        void Close();
        void SetSymAttribute(uint parent, string name, int length, byte* data);
        void OpenNamespace(string name);
        void CloseNamespace();
        void UsingNamespace(string fullName);
        void SetMethodSourceRange(ISymUnmanagedDocumentWriter startDoc, uint startLine, uint startColumn, object endDoc, uint endLine, uint endColumn);
        void Initialize([MarshalAs(UnmanagedType.IUnknown)] object emitter, string filename, [MarshalAs(UnmanagedType.IUnknown)] object ptrIStream, bool fullBuild);
        void GetDebugInfo(ref ImageDebugDirectory debugDirectory, uint dataCount, out uint dataCountPtr, byte* data);
        void DefineSequencePoints(ISymUnmanagedDocumentWriter document, int count,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] offsets,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] lines,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] columns,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] endLines,
          [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] endColumns);
        void RemapToken(uint oldToken, uint newToken);
        void Initialize2([MarshalAs(UnmanagedType.IUnknown)] object emitter, string tempfilename, [MarshalAs(UnmanagedType.IUnknown)] object ptrIStream, bool fullBuild, string finalfilename);
        void DefineConstant(string name, object value, uint sig, byte* signature);
        void Abort();

        #endregion

        #region ISymUnmanagedWriter2

        void DefineLocalVariable2(string name, int attributes, int localSignatureToken, uint addrKind, int index, uint addr2, uint addr3, uint startOffset, uint endOffset);
        void DefineGlobalVariable2(string name, int attributes, int sigToken, uint addrKind, uint addr1, uint addr2, uint addr3);

        /// <remarks>
        /// <paramref name="value"/> has type <see cref="VariantStructure"/>, rather than <see cref="object"/>,
        /// so that we can do custom marshalling of <see cref="System.DateTime"/>.  Unfortunately, .NET marshals
        /// <see cref="System.DateTime"/>s as the number of days since 1899/12/30, whereas the native VB compiler
        ///  marshalled them as the number of ticks since the Unix epoch (i.e. a much, much larger number).
        /// </remarks>
        void DefineConstant2([MarshalAs(UnmanagedType.LPWStr)] string name, VariantStructure value, int constantSignatureToken);

        #endregion

        #region ISymUnmanagedWriter3

        void OpenMethod2(uint methodToken, int sectionIndex, int offsetRelativeOffset);
        void Commit();

        #endregion

        #region ISymUnmanagedWriter4

        void GetDebugInfoWithPadding(ref ImageDebugDirectory debugDirectory, uint dataCount, out uint dataCountPtr, byte* data);

        #endregion

        #region ISymUnmanagedWriter5

        /// <summary>
        /// Open a special custom data section to emit token to source span mapping information into. 
        /// Opening this section while a method is already open or vice versa is an error.
        /// </summary>
        void OpenMapTokensToSourceSpans();

        /// <summary>
        /// Close the special custom data section for token to source span mapping
        /// information. Once it is closed no more mapping information can be added.
        /// </summary>
        void CloseMapTokensToSourceSpans();

        /// <summary>
        /// Maps the given metadata token to the given source line span in the specified source file. 
        /// Must be called between calls to <see cref="OpenMapTokensToSourceSpans"/> and <see cref="CloseMapTokensToSourceSpans"/>.
        /// </summary>
        void MapTokenToSourceSpan(int token, ISymUnmanagedDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn);

        #endregion
    }

    /// <summary>
    /// The highest version of the interface available in Microsoft.DiaSymReader.Native.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("5ba52f3b-6bf8-40fc-b476-d39c529b331e"), SuppressUnmanagedCodeSecurity]
    internal interface ISymUnmanagedWriter8 : ISymUnmanagedWriter5
    {
        //  ISymUnmanagedWriter, ISymUnmanagedWriter2, ISymUnmanagedWriter3, ISymUnmanagedWriter4, ISymUnmanagedWriter5
        void _VtblGap1_33();

        // ISymUnmanagedWriter6
        void InitializeDeterministic([MarshalAs(UnmanagedType.IUnknown)] object emitter, [MarshalAs(UnmanagedType.IUnknown)] object stream);

        // ISymUnmanagedWriter7
        unsafe void UpdateSignatureByHashingContent([In]byte* buffer, int size);

        // ISymUnmanagedWriter8
        void UpdateSignature(Guid pdbId, uint stamp, int age);
        unsafe void SetSourceServerData([In]byte* data, int size);
        unsafe void SetSourceLinkData([In]byte* data, int size);
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
#pragma warning disable CS0618 // Type or member is obsolete
            _type = (short)VarEnum.VT_DATE;
#pragma warning restore CS0618
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
    internal unsafe struct VariantPadding
    {
        public readonly byte* Data2;
        public readonly byte* Data3;
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

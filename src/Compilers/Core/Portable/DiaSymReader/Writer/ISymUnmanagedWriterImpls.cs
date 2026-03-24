// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader;

[DynamicInterfaceCastableImplementation]
internal interface IPdbWriterImpl : IPdbWriter
{
    // vtable slot 7 (IUnknown=3, __SetPath=3, __OpenMod=4, __CloseMod=5, __GetPath=6, GetSignatureAge=7)
    int IPdbWriter.__SetPath() => throw new NotImplementedException();
    int IPdbWriter.__OpenMod() => throw new NotImplementedException();
    int IPdbWriter.__CloseMod() => throw new NotImplementedException();
    int IPdbWriter.__GetPath() => throw new NotImplementedException();

    unsafe void IPdbWriter.GetSignatureAge(out uint sig, out int age)
    {
        var inst = ((WriterWrapper)this).PdbWriterInst;
        var func = (delegate* unmanaged<IntPtr, uint*, int*, int>)(*(*(void***)inst + 7));
        fixed (uint* sigPtr = &sig)
        fixed (int* agePtr = &age)
        {
            int hr = func(inst, sigPtr, agePtr);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }
}

[DynamicInterfaceCastableImplementation]
internal unsafe interface ISymWriter5Impl : ISymUnmanagedWriter5
{
    protected static IntPtr GetInst(ISymWriter5Impl self) => ((WriterWrapper)self).WriterInst;

    // Slot 3: DefineDocument
    ISymUnmanagedDocumentWriter ISymUnmanagedWriter5.DefineDocument(string url, ref Guid language, ref Guid languageVendor, ref Guid documentType)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, char*, Guid*, Guid*, Guid*, IntPtr*, int>)(*(*(void***)inst + 3));
        IntPtr docWriterPtr;
        fixed (char* urlPtr = url)
        fixed (Guid* languagePtr = &language)
        fixed (Guid* languageVendorPtr = &languageVendor)
        fixed (Guid* documentTypePtr = &documentType)
        {
            int hr = func(inst, urlPtr, languagePtr, languageVendorPtr, documentTypePtr, &docWriterPtr);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
        return DocumentWriterRcw.Create(docWriterPtr);
    }

    // Slot 4: SetUserEntryPoint
    void ISymUnmanagedWriter5.SetUserEntryPoint(int entryMethodToken)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int, int>)(*(*(void***)inst + 4));
        int hr = func(inst, entryMethodToken);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 5: OpenMethod
    void ISymUnmanagedWriter5.OpenMethod(uint methodToken)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, uint, int>)(*(*(void***)inst + 5));
        int hr = func(inst, methodToken);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 6: CloseMethod
    void ISymUnmanagedWriter5.CloseMethod()
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int>)(*(*(void***)inst + 6));
        int hr = func(inst);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 7: OpenScope
    uint ISymUnmanagedWriter5.OpenScope(int startOffset)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int, uint*, int>)(*(*(void***)inst + 7));
        uint result;
        int hr = func(inst, startOffset, &result);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
        return result;
    }

    // Slot 8: CloseScope
    void ISymUnmanagedWriter5.CloseScope(int endOffset)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int, int>)(*(*(void***)inst + 8));
        int hr = func(inst, endOffset);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 9: SetScopeRange
    void ISymUnmanagedWriter5.SetScopeRange(uint scopeID, uint startOffset, uint endOffset)
        => throw new NotImplementedException();

    // Slot 10: DefineLocalVariable
    void ISymUnmanagedWriter5.DefineLocalVariable(string name, uint attributes, uint sig, byte* signature, uint addrKind, uint addr1, uint addr2, uint startOffset, uint endOffset)
        => throw new NotImplementedException();

    // Slot 11: DefineParameter
    void ISymUnmanagedWriter5.DefineParameter(string name, uint attributes, uint sequence, uint addrKind, uint addr1, uint addr2, uint addr3)
        => throw new NotImplementedException();

    // Slot 12: DefineField
    void ISymUnmanagedWriter5.DefineField(uint parent, string name, uint attributes, uint sig, byte* signature, uint addrKind, uint addr1, uint addr2, uint addr3)
        => throw new NotImplementedException();

    // Slot 13: DefineGlobalVariable
    void ISymUnmanagedWriter5.DefineGlobalVariable(string name, uint attributes, uint sig, byte* signature, uint addrKind, uint addr1, uint addr2, uint addr3)
        => throw new NotImplementedException();

    // Slot 14: Close
    void ISymUnmanagedWriter5.Close()
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int>)(*(*(void***)inst + 14));
        int hr = func(inst);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 15: SetSymAttribute
    void ISymUnmanagedWriter5.SetSymAttribute(uint parent, string name, int length, byte* data)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, uint, char*, int, byte*, int>)(*(*(void***)inst + 15));
        fixed (char* namePtr = name)
        {
            int hr = func(inst, parent, namePtr, length, data);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    // Slot 16: OpenNamespace
    void ISymUnmanagedWriter5.OpenNamespace(string name) => throw new NotImplementedException();

    // Slot 17: CloseNamespace
    void ISymUnmanagedWriter5.CloseNamespace() => throw new NotImplementedException();

    // Slot 18: UsingNamespace
    void ISymUnmanagedWriter5.UsingNamespace(string fullName)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, char*, int>)(*(*(void***)inst + 18));
        fixed (char* namePtr = fullName)
        {
            int hr = func(inst, namePtr);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    // Slot 19: SetMethodSourceRange
    void ISymUnmanagedWriter5.SetMethodSourceRange(ISymUnmanagedDocumentWriter startDoc, uint startLine, uint startColumn, object endDoc, uint endLine, uint endColumn)
        => throw new NotImplementedException();

    // Slot 20: Initialize
    void ISymUnmanagedWriter5.Initialize(object emitter, string filename, object ptrIStream, bool fullBuild)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, IntPtr, char*, IntPtr, int, int>)(*(*(void***)inst + 20));
        var emitterPtr = Marshal.GetIUnknownForObject(emitter);
        var streamPtr = ComMemoryStreamWrapperCache.Instance.GetOrCreateComInterfaceForObject(ptrIStream, CreateComInterfaceFlags.None);
        try
        {
            fixed (char* filenamePtr = filename)
            {
                int hr = func(inst, emitterPtr, filenamePtr, streamPtr, fullBuild ? 1 : 0);
                if (hr != HResult.S_OK)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            Marshal.Release(emitterPtr);
        }
    }

    // Slot 21: GetDebugInfo
    void ISymUnmanagedWriter5.GetDebugInfo(ref ImageDebugDirectory debugDirectory, uint dataCount, out uint dataCountPtr, byte* data)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, ImageDebugDirectory*, uint, uint*, byte*, int>)(*(*(void***)inst + 21));
        fixed (ImageDebugDirectory* debugDir = &debugDirectory)
        fixed (uint* dataCountPtrPtr = &dataCountPtr)
        {
            int hr = func(inst, debugDir, dataCount, dataCountPtrPtr, data);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    // Slot 22: DefineSequencePoints
    void ISymUnmanagedWriter5.DefineSequencePoints(ISymUnmanagedDocumentWriter document, int count, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, IntPtr, int, int*, int*, int*, int*, int*, int>)(*(*(void***)inst + 22));

        IntPtr docPtr;
        if (document is DocumentWriterRcw rcw)
        {
            docPtr = rcw.Inst;
            Marshal.AddRef(docPtr);
        }
        else
        {
            docPtr = Marshal.GetIUnknownForObject(document);
        }

        try
        {
            fixed (int* offsetsPtr = offsets)
            fixed (int* linesPtr = lines)
            fixed (int* columnsPtr = columns)
            fixed (int* endLinesPtr = endLines)
            fixed (int* endColumnsPtr = endColumns)
            {
                int hr = func(inst, docPtr, count, offsetsPtr, linesPtr, columnsPtr, endLinesPtr, endColumnsPtr);
                if (hr != HResult.S_OK)
                    Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            Marshal.Release(docPtr);
        }
    }

    // Slot 23: RemapToken
    void ISymUnmanagedWriter5.RemapToken(uint oldToken, uint newToken) => throw new NotImplementedException();

    // Slot 24: Initialize2
    void ISymUnmanagedWriter5.Initialize2(object emitter, string tempfilename, object ptrIStream, bool fullBuild, string finalfilename)
        => throw new NotImplementedException();

    // Slot 25: DefineConstant
    void ISymUnmanagedWriter5.DefineConstant(string name, object value, uint sig, byte* signature)
        => throw new NotImplementedException();

    // Slot 26: Abort
    void ISymUnmanagedWriter5.Abort() => throw new NotImplementedException();

    // Slot 27: DefineLocalVariable2
    void ISymUnmanagedWriter5.DefineLocalVariable2(string name, int attributes, int localSignatureToken, uint addrKind, int index, uint addr2, uint addr3, uint startOffset, uint endOffset)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, char*, int, int, uint, int, uint, uint, uint, uint, int>)(*(*(void***)inst + 27));
        fixed (char* namePtr = name)
        {
            int hr = func(inst, namePtr, attributes, localSignatureToken, addrKind, index, addr2, addr3, startOffset, endOffset);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    // Slot 28: DefineGlobalVariable2
    void ISymUnmanagedWriter5.DefineGlobalVariable2(string name, int attributes, int sigToken, uint addrKind, uint addr1, uint addr2, uint addr3)
        => throw new NotImplementedException();

    // Slot 29: DefineConstant2
    void ISymUnmanagedWriter5.DefineConstant2(string name, VariantStructure value, int constantSignatureToken)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, char*, VariantStructure, int, int>)(*(*(void***)inst + 29));
        fixed (char* namePtr = name)
        {
            int hr = func(inst, namePtr, value, constantSignatureToken);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    // Slot 30: OpenMethod2
    void ISymUnmanagedWriter5.OpenMethod2(uint methodToken, int sectionIndex, int offsetRelativeOffset)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, uint, int, int, int>)(*(*(void***)inst + 30));
        int hr = func(inst, methodToken, sectionIndex, offsetRelativeOffset);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 31: Commit
    void ISymUnmanagedWriter5.Commit()
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int>)(*(*(void***)inst + 31));
        int hr = func(inst);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 32: GetDebugInfoWithPadding
    void ISymUnmanagedWriter5.GetDebugInfoWithPadding(ref ImageDebugDirectory debugDirectory, uint dataCount, out uint dataCountPtr, byte* data)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, ImageDebugDirectory*, uint, uint*, byte*, int>)(*(*(void***)inst + 32));
        fixed (ImageDebugDirectory* debugDir = &debugDirectory)
        fixed (uint* dataCountPtrPtr = &dataCountPtr)
        {
            int hr = func(inst, debugDir, dataCount, dataCountPtrPtr, data);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
    }

    // Slot 33: OpenMapTokensToSourceSpans
    void ISymUnmanagedWriter5.OpenMapTokensToSourceSpans()
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int>)(*(*(void***)inst + 33));
        int hr = func(inst);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 34: CloseMapTokensToSourceSpans
    void ISymUnmanagedWriter5.CloseMapTokensToSourceSpans()
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int>)(*(*(void***)inst + 34));
        int hr = func(inst);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 35: MapTokenToSourceSpan
    void ISymUnmanagedWriter5.MapTokenToSourceSpan(int token, ISymUnmanagedDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
    {
        var inst = GetInst(this);
        var func = (delegate* unmanaged<IntPtr, int, IntPtr, int, int, int, int, int>)(*(*(void***)inst + 35));

        IntPtr docPtr;
        if (document is DocumentWriterRcw rcw)
        {
            docPtr = rcw.Inst;
            Marshal.AddRef(docPtr);
        }
        else
        {
            docPtr = Marshal.GetIUnknownForObject(document);
        }

        try
        {
            int hr = func(inst, token, docPtr, startLine, startColumn, endLine, endColumn);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
        finally
        {
            Marshal.Release(docPtr);
        }
    }
}

[DynamicInterfaceCastableImplementation]
#pragma warning disable CA2256 // Interface members declared in base interfaces are not re-implemented
internal unsafe interface ISymWriter8Impl : ISymWriter5Impl, ISymUnmanagedWriter8
#pragma warning restore CA2256
{
    // Slot 36: InitializeDeterministic (ISymUnmanagedWriter6)
    void ISymUnmanagedWriter8.InitializeDeterministic(object emitter, object stream)
    {
        var inst = ISymWriter5Impl.GetInst(this);
        var func = (delegate* unmanaged<IntPtr, IntPtr, IntPtr, int>)(*(*(void***)inst + 36));
        var emitterPtr = Marshal.GetIUnknownForObject(emitter);
        var streamPtr = ComMemoryStreamWrapperCache.Instance.GetOrCreateComInterfaceForObject(stream, CreateComInterfaceFlags.None);
        var iid = IUnsafeComStream.IID;
        if (Marshal.QueryInterface(streamPtr, ref iid, out IntPtr istreamPtr) != HResult.S_OK)
        {
            Marshal.Release(emitterPtr);
            throw new ArgumentException("Stream parameter must implement IStream", nameof(stream));
        }

        try
        {
            int hr = func(inst, emitterPtr, istreamPtr);
            if (hr != HResult.S_OK)
                Marshal.ThrowExceptionForHR(hr);
        }
        finally
        {
            Marshal.Release(emitterPtr);
            Marshal.Release(istreamPtr);
        }
    }

    // Slot 37: UpdateSignatureByHashingContent (ISymUnmanagedWriter7)
    void ISymUnmanagedWriter8.UpdateSignatureByHashingContent(byte* buffer, int size)
    {
        var inst = ISymWriter5Impl.GetInst(this);
        var func = (delegate* unmanaged<IntPtr, byte*, int, int>)(*(*(void***)inst + 37));
        int hr = func(inst, buffer, size);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 38: UpdateSignature (ISymUnmanagedWriter8)
    void ISymUnmanagedWriter8.UpdateSignature(Guid pdbId, uint stamp, int age)
    {
        var inst = ISymWriter5Impl.GetInst(this);
        var func = (delegate* unmanaged<IntPtr, Guid, uint, int, int>)(*(*(void***)inst + 38));
        int hr = func(inst, pdbId, stamp, age);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 39: SetSourceServerData
    void ISymUnmanagedWriter8.SetSourceServerData(byte* data, int size)
    {
        var inst = ISymWriter5Impl.GetInst(this);
        var func = (delegate* unmanaged<IntPtr, byte*, int, int>)(*(*(void***)inst + 39));
        int hr = func(inst, data, size);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    // Slot 40: SetSourceLinkData
    void ISymUnmanagedWriter8.SetSourceLinkData(byte* data, int size)
    {
        var inst = ISymWriter5Impl.GetInst(this);
        var func = (delegate* unmanaged<IntPtr, byte*, int, int>)(*(*(void***)inst + 40));
        int hr = func(inst, data, size);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }
}

[DynamicInterfaceCastableImplementation]
internal interface ICompilerInfoWriterImpl : ISymUnmanagedCompilerInfoWriter
{
    unsafe int ISymUnmanagedCompilerInfoWriter.AddCompilerInfo(ushort major, ushort minor, ushort build, ushort revision, string name)
    {
        var inst = ((WriterWrapper)this).CompilerInfoWriterInst;
        var func = (delegate* unmanaged<IntPtr, ushort, ushort, ushort, ushort, char*, int>)(*(*(void***)inst + 3));
        fixed (char* namePtr = name)
        {
            return func(inst, major, minor, build, revision, namePtr);
        }
    }
}

#endif

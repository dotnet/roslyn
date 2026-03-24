// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader;

/// <summary>
/// Runtime callable wrapper for ISymUnmanagedDocumentWriter COM objects.
/// </summary>
internal sealed class DocumentWriterRcw : ISymUnmanagedDocumentWriter, IDisposable
{
    private bool _isDisposed;
    internal readonly IntPtr Inst;

    private DocumentWriterRcw(IntPtr inst)
    {
        Inst = inst;
    }

    public static DocumentWriterRcw Create(IntPtr ptr)
    {
        var iid = ISymUnmanagedDocumentWriter.IID;
        int hr = Marshal.QueryInterface(ptr, ref iid, out IntPtr docPtr);
        if (hr != HResult.S_OK)
            throw new NotSupportedException();
        return new DocumentWriterRcw(docPtr);
    }

    unsafe void ISymUnmanagedDocumentWriter.SetSource(uint sourceSize, byte* source)
    {
        var func = (delegate* unmanaged<IntPtr, uint, byte*, int>)(*(*(void***)Inst + 3));
        int hr = func(Inst, sourceSize, source);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    unsafe void ISymUnmanagedDocumentWriter.SetCheckSum(Guid algorithmId, uint checkSumSize, byte* checkSum)
    {
        var func = (delegate* unmanaged<IntPtr, Guid, uint, byte*, int>)(*(*(void***)Inst + 4));
        int hr = func(Inst, algorithmId, checkSumSize, checkSum);
        if (hr != HResult.S_OK)
            Marshal.ThrowExceptionForHR(hr);
    }

    ~DocumentWriterRcw()
    {
        DisposeInternal();
    }

    public void Dispose()
    {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    private void DisposeInternal()
    {
        if (_isDisposed)
            return;
        Marshal.Release(Inst);
        _isDisposed = true;
    }
}

#endif

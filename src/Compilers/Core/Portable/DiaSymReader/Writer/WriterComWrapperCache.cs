// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader;

internal sealed class WriterComWrapperCache : ComWrappers
{
    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        throw new NotImplementedException();
    }

    protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
        return WriterWrapper.CreateIfSupported(externalComObject) ?? throw new NotSupportedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}

internal class WriterWrapper : IDynamicInterfaceCastable, IDisposable
{
    private bool _isDisposed;

    public readonly IntPtr WriterInst;
    public readonly IntPtr CompilerInfoWriterInst;
    public readonly IntPtr PdbWriterInst;

    private WriterWrapper(IntPtr writerInst, IntPtr compilerInfoWriterInst, IntPtr pdbWriterInst)
    {
        WriterInst = writerInst;
        CompilerInfoWriterInst = compilerInfoWriterInst;
        PdbWriterInst = pdbWriterInst;
    }

    public static WriterWrapper? CreateIfSupported(IntPtr ptr)
    {
        var iid = ISymUnmanagedCompilerInfoWriter.IID;
        int hr = Marshal.QueryInterface(ptr, ref iid, out IntPtr compilerInfoWriterPtr);
        if (hr != HResult.S_OK)
        {
            compilerInfoWriterPtr = IntPtr.Zero;
        }

        iid = IPdbWriter.IID;
        hr = Marshal.QueryInterface(ptr, ref iid, out IntPtr pdbWriterPtr);
        if (hr != HResult.S_OK)
        {
            if (compilerInfoWriterPtr != IntPtr.Zero)
                Marshal.Release(compilerInfoWriterPtr);
            return null;
        }

        // Try ISymUnmanagedWriter8 first, fall back to ISymUnmanagedWriter5
        iid = ISymUnmanagedWriter8.IID;
        hr = Marshal.QueryInterface(ptr, ref iid, out IntPtr unmanagedWriterPtr);
        if (hr != HResult.S_OK)
        {
            iid = ISymUnmanagedWriter5.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out unmanagedWriterPtr);
        }

        if (hr == HResult.S_OK)
        {
            return new WriterWrapper(unmanagedWriterPtr, compilerInfoWriterPtr, pdbWriterPtr);
        }

        if (compilerInfoWriterPtr != IntPtr.Zero)
            Marshal.Release(compilerInfoWriterPtr);
        Marshal.Release(pdbWriterPtr);
        return null;
    }

    public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        if (interfaceType.Equals(typeof(ISymUnmanagedWriter5).TypeHandle))
            return typeof(ISymWriter5Impl).TypeHandle;
        if (interfaceType.Equals(typeof(ISymUnmanagedWriter8).TypeHandle))
            return typeof(ISymWriter8Impl).TypeHandle;
        if (interfaceType.Equals(typeof(ISymUnmanagedCompilerInfoWriter).TypeHandle))
            return typeof(ICompilerInfoWriterImpl).TypeHandle;
        if (interfaceType.Equals(typeof(IPdbWriter).TypeHandle))
            return typeof(IPdbWriterImpl).TypeHandle;
        return default;
    }

    public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        if (interfaceType.Equals(typeof(ISymUnmanagedWriter8).TypeHandle) ||
            interfaceType.Equals(typeof(ISymUnmanagedWriter5).TypeHandle) ||
            interfaceType.Equals(typeof(ISymUnmanagedCompilerInfoWriter).TypeHandle) ||
            interfaceType.Equals(typeof(IPdbWriter).TypeHandle))
        {
            return true;
        }

        if (throwIfNotImplemented)
            throw new InvalidCastException($"{nameof(WriterWrapper)} does not implement {interfaceType}");

        return false;
    }

    public void Dispose()
    {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    ~WriterWrapper()
    {
        DisposeInternal();
    }

    private void DisposeInternal()
    {
        if (_isDisposed)
            return;

        Marshal.Release(WriterInst);
        if (CompilerInfoWriterInst != IntPtr.Zero)
            Marshal.Release(CompilerInfoWriterInst);
        Marshal.Release(PdbWriterInst);
        _isDisposed = true;
    }
}

#endif

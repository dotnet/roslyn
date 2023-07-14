// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

internal sealed class StubLocalRegistry : ILocalRegistry5, ILocalRegistry4, ILocalRegistry3
{
    int ILocalRegistry.CreateInstance(Guid clsid, object punkOuter, ref Guid riid, uint dwFlags, out IntPtr ppvObj)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry.GetTypeLibOfClsid(Guid clsid, out ITypeLib pptLib)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry.GetClassObjectOfClsid(ref Guid clsid, uint dwFlags, IntPtr lpReserved, ref Guid riid, out IntPtr ppvClassObject)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry2.CreateInstance(Guid clsid, object punkOuter, ref Guid riid, uint dwFlags, out IntPtr ppvObj)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry2.GetTypeLibOfClsid(Guid clsid, out ITypeLib pptLib)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry2.GetClassObjectOfClsid(ref Guid clsid, uint dwFlags, IntPtr lpReserved, ref Guid riid, IntPtr ppvClassObject)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry2.GetLocalRegistryRoot(out string pbstrRoot)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry3.CreateInstance(Guid clsid, object punkOuter, ref Guid riid, uint dwFlags, out IntPtr ppvObj)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry3.GetTypeLibOfClsid(Guid clsid, out ITypeLib pptLib)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry3.GetClassObjectOfClsid(ref Guid clsid, uint dwFlags, IntPtr lpReserved, ref Guid riid, IntPtr ppvClassObject)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry3.GetLocalRegistryRoot(out string pbstrRoot)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry3.CreateManagedInstance(string codeBase, string assemblyName, string typeName, ref Guid riid, out IntPtr ppvObj)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry3.GetClassObjectOfManagedClass(string codeBase, string assemblyName, string typeName, ref Guid riid, out IntPtr ppvClassObject)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry4.RegisterClassObject(ref Guid rclsid, out uint pdwCookie)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry4.RevokeClassObject(uint dwCookie)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry4.RegisterInterface(ref Guid riid)
    {
        throw new NotImplementedException();
    }

    int ILocalRegistry4.GetLocalRegistryRootEx(uint dwRegType, out uint pdwRegRootHandle, out string pbstrRoot)
    {
        pdwRegRootHandle = unchecked((uint)__VsLocalRegistryRootHandle.RegHandle_CurrentUser);
        pbstrRoot = "Software\\Microsoft\\VisualStudio\\17.0_Test";
        return VSConstants.S_OK;
    }

    int ILocalRegistry5.CreateAggregatedManagedInstance(string codeBase, string AssemblyName, string TypeName, IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObj)
    {
        throw new NotImplementedException();
    }
}

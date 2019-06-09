// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    internal static class SymUnmanagedFactory
    {
        private const string AlternateLoadPathEnvironmentVariableName = "MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH";

        private const string LegacyDiaSymReaderModuleName = "diasymreader.dll";
        private const string DiaSymReaderModuleName32 = "Microsoft.DiaSymReader.Native.x86.dll";
        private const string DiaSymReaderModuleName64 = "Microsoft.DiaSymReader.Native.amd64.dll";

        private const string CreateSymReaderFactoryName = "CreateSymReader";
        private const string CreateSymWriterFactoryName = "CreateSymWriter";

        // CorSymWriter_SxS from corsym.idl
        private const string SymWriterClsid = "0AE2DEB0-F901-478b-BB9F-881EE8066788";

        // CorSymReader_SxS from corsym.idl
        private const string SymReaderClsid = "0A3976C5-4529-4ef8-B0B0-42EED37082CD";

        private static Type s_lazySymReaderComType, s_lazySymWriterComType;

        internal static string DiaSymReaderModuleName
            => (IntPtr.Size == 4) ? DiaSymReaderModuleName32 : DiaSymReaderModuleName64;

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName32, EntryPoint = CreateSymReaderFactoryName)]
        private extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName64, EntryPoint = CreateSymReaderFactoryName)]
        private extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName32, EntryPoint = CreateSymWriterFactoryName)]
        private extern static void CreateSymWriter32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName64, EntryPoint = CreateSymWriterFactoryName)]
        private extern static void CreateSymWriter64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        private delegate void NativeFactory(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object instance);

#if !NET20
        private static Lazy<Func<string, string>> s_lazyGetEnvironmentVariable = new Lazy<Func<string, string>>(() =>
        {
            try
            {
                foreach (var method in typeof(Environment).GetTypeInfo().GetDeclaredMethods("GetEnvironmentVariable"))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        return (Func<string, string>)method.CreateDelegate(typeof(Func<string, string>));
                    }
                }
            }
            catch
            {
            }

            return null;
        });
#endif

        // internal for testing
        internal static string GetEnvironmentVariable(string name)
        {
            try
            {
#if NET20
                return Environment.GetEnvironmentVariable(name);
#else
                return s_lazyGetEnvironmentVariable.Value?.Invoke(name);
#endif
            }
            catch
            {
                return null;
            }
        }

        private static object TryLoadFromAlternativePath(Guid clsid, string factoryName)
        {
            var dir = GetEnvironmentVariable(AlternateLoadPathEnvironmentVariableName);
            if (string.IsNullOrEmpty(dir))
            {
                return null;
            }

            var moduleHandle = LoadLibrary(Path.Combine(dir, DiaSymReaderModuleName));
            if (moduleHandle == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            object instance = null;
            try
            {
                var createAddress = GetProcAddress(moduleHandle, factoryName);
                if (createAddress == IntPtr.Zero)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

#if NET20 || NETSTANDARD1_1
                var creator = (NativeFactory)Marshal.GetDelegateForFunctionPointer(createAddress, typeof(NativeFactory));
#else
                var creator = Marshal.GetDelegateForFunctionPointer<NativeFactory>(createAddress);
#endif
                creator(ref clsid, out instance);
            }
            finally
            {
                if (instance == null && !FreeLibrary(moduleHandle))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            return instance;
        }

        private static Type GetComTypeType(ref Type lazyType, Guid clsid)
        {
            if (lazyType == null)
            {
#if NET20
                lazyType = Type.GetTypeFromCLSID(clsid);
#else
                lazyType = Marshal.GetTypeFromCLSID(clsid);
#endif
            }

            return lazyType;
        }

        internal static object CreateObject(bool createReader, bool useAlternativeLoadPath, bool useComRegistry, out string moduleName, out Exception loadException)
        {
            object instance = null;
            loadException = null;
            moduleName = null;

            var clsid = new Guid(createReader ? SymReaderClsid : SymWriterClsid);

            try
            {
                try
                {
                    if (IntPtr.Size == 4)
                    {
                        if (createReader)
                        {
                            CreateSymReader32(ref clsid, out instance);
                        }
                        else
                        {
                            CreateSymWriter32(ref clsid, out instance);
                        }
                    }
                    else
                    {
                        if (createReader)
                        {
                            CreateSymReader64(ref clsid, out instance);
                        }
                        else
                        {
                            CreateSymWriter64(ref clsid, out instance);
                        }
                    }
                }
                catch (DllNotFoundException e) when (useAlternativeLoadPath)
                {
                    instance = TryLoadFromAlternativePath(clsid, createReader ? CreateSymReaderFactoryName : CreateSymWriterFactoryName);
                    if (instance == null)
                    {
                        loadException = e;
                    }
                }
            }
            catch (Exception e)
            {
                loadException = e;
                instance = null;
            }

            if (instance != null)
            {
                moduleName = DiaSymReaderModuleName;
            }
            else if (useComRegistry)
            {
                // Try to find a registered CLR implementation
                try
                {
                    var comType = createReader ?
                        GetComTypeType(ref s_lazySymReaderComType, clsid) :
                        GetComTypeType(ref s_lazySymWriterComType, clsid);

                    instance = Activator.CreateInstance(comType);
                    moduleName = LegacyDiaSymReaderModuleName;
                }
                catch (Exception e)
                {
                    loadException = e;
                    instance = null;
                }
            }

            return instance;
        }

    }
}

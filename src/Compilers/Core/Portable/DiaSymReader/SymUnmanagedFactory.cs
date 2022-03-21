// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.DiaSymReader
{
    internal static class SymUnmanagedFactory
    {
        private const string AlternateLoadPathEnvironmentVariableName = "MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH";

        private const string LegacyDiaSymReaderModuleName = "diasymreader.dll";
        private const string DiaSymReaderModuleName32 = "Microsoft.DiaSymReader.Native.x86.dll";
        private const string DiaSymReaderModuleNameAmd64 = "Microsoft.DiaSymReader.Native.amd64.dll";
        private const string DiaSymReaderModuleNameArm64 = "Microsoft.DiaSymReader.Native.arm64.dll";

        private const string CreateSymReaderFactoryName = "CreateSymReader";
        private const string CreateSymWriterFactoryName = "CreateSymWriter";

        // CorSymWriter_SxS from corsym.idl
        private const string SymWriterClsid = "0AE2DEB0-F901-478b-BB9F-881EE8066788";

        // CorSymReader_SxS from corsym.idl
        private const string SymReaderClsid = "0A3976C5-4529-4ef8-B0B0-42EED37082CD";

        private static Type s_lazySymReaderComType, s_lazySymWriterComType;

        internal static string DiaSymReaderModuleName
            => RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => DiaSymReaderModuleName32,
                Architecture.X64 => DiaSymReaderModuleNameAmd64,
                Architecture.Arm64 => DiaSymReaderModuleNameArm64,
                _ => throw new NotSupportedException()
            };

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName32, EntryPoint = CreateSymReaderFactoryName)]
        private static extern void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameAmd64, EntryPoint = CreateSymReaderFactoryName)]
        private static extern void CreateSymReaderAmd64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameArm64, EntryPoint = CreateSymReaderFactoryName)]
        private static extern void CreateSymReaderArm64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName32, EntryPoint = CreateSymWriterFactoryName)]
        private static extern void CreateSymWriter32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameAmd64, EntryPoint = CreateSymWriterFactoryName)]
        private static extern void CreateSymWriterAmd64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameArm64, EntryPoint = CreateSymWriterFactoryName)]
        private static extern void CreateSymWriterArm64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symWriter);

        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        private delegate void NativeFactory(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

        private static readonly Lazy<Func<string, string>> s_lazyGetEnvironmentVariable = new Lazy<Func<string, string>>(() =>
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

        // internal for testing
        internal static string GetEnvironmentVariable(string name)
        {
            try
            {
                return s_lazyGetEnvironmentVariable.Value?.Invoke(name);
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

#if NETSTANDARD1_1
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
                lazyType = Marshal.GetTypeFromCLSID(clsid);
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
                    switch (RuntimeInformation.ProcessArchitecture, createReader)
                    {
                        case (Architecture.X86, true):
                            CreateSymReader32(ref clsid, out instance);
                            break;
                        case (Architecture.X86, false):
                            CreateSymWriter32(ref clsid, out instance);
                            break;
                        case (Architecture.X64, true):
                            CreateSymReaderAmd64(ref clsid, out instance);
                            break;
                        case (Architecture.X64, false):
                            CreateSymWriterAmd64(ref clsid, out instance);
                            break;
                        case (Architecture.Arm64, true):
                            CreateSymReaderArm64(ref clsid, out instance);
                            break;
                        case (Architecture.Arm64, false):
                            CreateSymWriterArm64(ref clsid, out instance);
                            break;
                        default:
                            throw new NotSupportedException();
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

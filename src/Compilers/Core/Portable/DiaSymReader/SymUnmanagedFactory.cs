// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// COM interop is traditionally handled as a built-in part of the .NET Framework. It requires generating interop
// stubs at runtime that bridge the gap between the native signature and the managed signature. This is not compatible
// with trimming, as some of the dependencies of the generated stubs are not statically discoverable. .NET 8 includes
// a source generator and compile-time generated stubs called "ComWrappers" that can be used instead of the built-in COM interop support.
// To achieve trim-compatibility, we use generated ComWrappers whenever possible and only fall back to built-in COM interop when necessary.
#if NET
global using GeneratedWhenPossibleComInterfaceAttribute = System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute;
#else
global using GeneratedWhenPossibleComInterfaceAttribute = System.Runtime.InteropServices.ComImportAttribute;
#endif

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

#if NET
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
#endif

namespace Microsoft.DiaSymReader
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class SymUnmanagedFactory
    {
        private const string AlternativeLoadPathEnvironmentVariableName = "MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH";
        private const string AlternativeLoadPathOnlyEnvironmentVariableName = "MICROSOFT_DIASYMREADER_NATIVE_USE_ALT_LOAD_PATH_ONLY";

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

#if NET
        private const string IUnknownIid = "00000000-0000-0000-C000-000000000046";
#else
        private static Type s_lazySymReaderComType, s_lazySymWriterComType;
#endif

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
        private static extern unsafe void CreateSymReader32(Guid* id, IntPtr* symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameAmd64, EntryPoint = CreateSymReaderFactoryName)]
        private static extern unsafe void CreateSymReaderAmd64(Guid* id, IntPtr* symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameArm64, EntryPoint = CreateSymReaderFactoryName)]
        private static extern unsafe void CreateSymReaderArm64(Guid* id, IntPtr* symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleName32, EntryPoint = CreateSymWriterFactoryName)]
        private static extern unsafe void CreateSymWriter32(Guid* id, IntPtr* symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameAmd64, EntryPoint = CreateSymWriterFactoryName)]
        private static extern unsafe void CreateSymWriterAmd64(Guid* id, IntPtr* symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport(DiaSymReaderModuleNameArm64, EntryPoint = CreateSymWriterFactoryName)]
        private static extern unsafe void CreateSymWriterArm64(Guid* id, IntPtr* symWriter);

        [DllImport("kernel32", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern unsafe IntPtr LoadLibraryW(char* path);

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern int FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern unsafe IntPtr GetProcAddress(IntPtr hModule, byte* procedureName);

#if NETSTANDARD2_0
        private delegate void NativeFactory(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
#endif

#if NET
        [LibraryImport("Ole32")]
        private static unsafe partial int CoCreateInstance(in Guid rclsid, void* pUnkOuter, int dwClsContext, in Guid riid, [MarshalUsing(typeof(ComInterfaceMarshaller<object>))] out object ppObj);
#endif

        // internal for testing
        internal static string GetEnvironmentVariable(string name)
        {
            try
            {
                return Environment.GetEnvironmentVariable(name);
            }
            catch
            {
                return null;
            }
        }

        private static unsafe object TryLoadFromAlternativePath(Guid clsid, bool createReader)
        {
            var dir = GetEnvironmentVariable(AlternativeLoadPathEnvironmentVariableName);
            if (string.IsNullOrEmpty(dir))
            {
                return null;
            }

            var modulePath = Path.Combine(dir, DiaSymReaderModuleName);
            IntPtr moduleHandle;
            fixed (char* modulePathPtr = modulePath)
            {
                moduleHandle = LoadLibraryW(modulePathPtr);
            }

            if (moduleHandle == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            object instance = null;
            try
            {
                byte[] factoryNameBytes = System.Text.Encoding.ASCII.GetBytes(
                    createReader ? CreateSymReaderFactoryName : CreateSymWriterFactoryName);
                IntPtr createAddress;
                fixed (byte* factoryNamePtr = factoryNameBytes)
                {
                    createAddress = GetProcAddress(moduleHandle, factoryNamePtr);
                }

                if (createAddress == IntPtr.Zero)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

#if NETSTANDARD2_0
                var creator = Marshal.GetDelegateForFunctionPointer<NativeFactory>(createAddress);
                creator(ref clsid, out instance);
#else
                var creator = (delegate* unmanaged<Guid*, IntPtr*, void>)createAddress;
                IntPtr rawInstance = default;
                creator(&clsid, &rawInstance);
                instance = ComInterfaceMarshaller<ISymUnmanagedWriter5>.ConvertToManaged(rawInstance.ToPointer());
#endif
            }
            finally
            {
                if (instance == null && FreeLibrary(moduleHandle) == 0)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            return instance;
        }

#if NET
        private static unsafe object ActivateClass(Guid clsid)
        {
            int hr = CoCreateInstance(in clsid, null, 1, new Guid(IUnknownIid), out object instance);
            Marshal.ThrowExceptionForHR(hr);
            return instance;
        }
#else
        private static object ActivateClass(ref Type lazyType, Guid clsid)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("COM lookup is not supported");
            }
            lazyType ??= Marshal.GetTypeFromCLSID(clsid);
            return Activator.CreateInstance(lazyType);
        }
#endif

        internal static unsafe object CreateObject(bool createReader, bool useAlternativeLoadPath, bool useComRegistry, out string moduleName, out Exception loadException)
        {
            object instance = null;
            loadException = null;
            moduleName = null;

            var clsid = new Guid(createReader ? SymReaderClsid : SymWriterClsid);

            try
            {
                DllNotFoundException loadExceptionCandidate = null;

                try
                {
                    if (!(useAlternativeLoadPath && GetEnvironmentVariable(AlternativeLoadPathOnlyEnvironmentVariableName) == "1"))
                    {
                        IntPtr rawInstance = default;
                        switch (RuntimeInformation.ProcessArchitecture, createReader)
                        {
                            case (Architecture.X86, true):
                                CreateSymReader32(&clsid, &rawInstance);
                                break;
                            case (Architecture.X86, false):
                                CreateSymWriter32(&clsid, &rawInstance);
                                break;
                            case (Architecture.X64, true):
                                CreateSymReaderAmd64(&clsid, &rawInstance);
                                break;
                            case (Architecture.X64, false):
                                CreateSymWriterAmd64(&clsid, &rawInstance);
                                break;
                            case (Architecture.Arm64, true):
                                CreateSymReaderArm64(&clsid, &rawInstance);
                                break;
                            case (Architecture.Arm64, false):
                                CreateSymWriterArm64(&clsid, &rawInstance);
                                break;
                            default:
                                throw new NotSupportedException();
                        }

                        if (rawInstance != default)
                        {
#if NET
                            instance = ComInterfaceMarshaller<ISymUnmanagedWriter5>.ConvertToManaged(rawInstance.ToPointer());
#else
                            instance = (ISymUnmanagedWriter5)Marshal.GetObjectForIUnknown(rawInstance);
#endif
                        }
                    }
                }
                catch (DllNotFoundException e) when (useAlternativeLoadPath)
                {
                    instance = null;
                    loadExceptionCandidate = e;
                }

                instance ??= TryLoadFromAlternativePath(clsid, createReader);
                if (instance == null)
                {
                    loadException = loadExceptionCandidate;
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
#if NET
                    instance = ActivateClass(clsid);
#else
                    instance = ActivateClass(ref createReader ? ref s_lazySymReaderComType : ref s_lazySymWriterComType, clsid);
#endif
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

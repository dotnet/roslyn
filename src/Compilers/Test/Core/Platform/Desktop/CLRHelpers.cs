// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET472

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities.Desktop.ComTypes;

namespace Roslyn.Test.Utilities.Desktop
{
    public static class CLRHelpers
    {
        private static readonly Guid s_clsIdClrRuntimeHost = new Guid("90F1A06E-7712-4762-86B5-7A5EBA6BDB02");
        private static readonly Guid s_clsIdCorMetaDataDispenser = new Guid("E5CB7A31-7512-11d2-89CE-0080C792E5D8");

        public static event ResolveEventHandler ReflectionOnlyAssemblyResolve;

        static CLRHelpers()
        {
            // Work around CLR bug: 
            // PE Verifier adds a handler to ReflectionOnlyAssemblyResolve event in AppDomain.EnableResolveAssembliesForIntrospection
            // (called from ValidateWorker in Validator.cpp) in which it directly calls Assembly.ReflectionOnlyLoad.
            // If that happens before we get a chance to resolve the assembly the resolution fails.
            // 
            // The handlers are invoked in the order they were added until one of them returns non-null assembly.
            // Therefore once we call Validate we can't add any more handlers -- they would all follow the CLR one, which fails.
            // 
            // As A workaround we add a single forwarding handler before any calls to Validate and then subscribe all of our true handlers 
            // to this event. 
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolveHandler;
        }

        private static Assembly ReflectionOnlyAssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            var handler = ReflectionOnlyAssemblyResolve;
            if (handler != null)
            {
                return handler(sender, args);
            }

            return null;
        }

        public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid)
        {
            // This API isn't available on Mono hence we must use reflection to access it.  
            Debug.Assert(!MonoHelpers.IsRunningOnMono());

            var getRuntimeInterfaceAsObject = typeof(RuntimeEnvironment).GetMethod("GetRuntimeInterfaceAsObject", BindingFlags.Public | BindingFlags.Static);
            return getRuntimeInterfaceAsObject.Invoke(null, new object[] { clsid, riid });
        }

        /// <summary>
        /// Verifies the specified image. Subscribe to <see cref="ReflectionOnlyAssemblyResolve"/> to provide a loader for dependent assemblies.
        /// </summary>
        public static string[] PeVerify(ImmutableArray<byte> peImage)
        {
            // fileName must be null, otherwise AssemblyResolve events won't fire
            return PeVerify(peImage.ToArray(), AppDomain.CurrentDomain.Id, assemblyPath: null);
        }

        /// <summary>
        /// Verifies the specified file. All dependencies must be on disk next to the file.
        /// </summary>
        public static string[] PeVerify(string filePath)
        {
            return PeVerify(File.ReadAllBytes(filePath), AppDomain.CurrentDomain.Id, filePath);
        }

        private static readonly object s_guard = new object();

        private static string[] PeVerify(byte[] peImage, int domainId, string assemblyPath)
        {
            if (MonoHelpers.IsRunningOnMono())
            {
                // PEVerify is currently unsupported on Mono hence return an empty 
                // set of messages
                return new string[0];
            }

            lock (s_guard)
            {
                GCHandle pinned = GCHandle.Alloc(peImage, GCHandleType.Pinned);
                try
                {
                    IntPtr buffer = pinned.AddrOfPinnedObject();

                    ICLRValidator validator = (ICLRValidator)GetRuntimeInterfaceAsObject(s_clsIdClrRuntimeHost, typeof(ICLRRuntimeHost).GUID);
                    ValidationErrorHandler errorHandler = new ValidationErrorHandler(validator);

                    IMetaDataDispenser dispenser = (IMetaDataDispenser)GetRuntimeInterfaceAsObject(s_clsIdCorMetaDataDispenser, typeof(IMetaDataDispenser).GUID);

                    // the buffer needs to be pinned during validation
                    Guid riid = typeof(IMetaDataImport).GUID;
                    object metaDataImport = null;
                    if (assemblyPath != null)
                    {
                        dispenser.OpenScope(assemblyPath, CorOpenFlags.ofRead, ref riid, out metaDataImport);
                    }
                    else
                    {
                        dispenser.OpenScopeOnMemory(buffer, (uint)peImage.Length, CorOpenFlags.ofRead, ref riid, out metaDataImport);
                    }

                    IMetaDataValidate metaDataValidate = (IMetaDataValidate)metaDataImport;
                    metaDataValidate.ValidatorInit(CorValidatorModuleType.ValidatorModuleTypePE, errorHandler);
                    metaDataValidate.ValidateMetaData();

                    validator.Validate(errorHandler, (uint)domainId, ValidatorFlags.VALIDATOR_EXTRA_VERBOSE,
                        ulMaxError: 10, token: 0, fileName: assemblyPath, pe: buffer, ulSize: (uint)peImage.Length);

                    return errorHandler.GetOutput();
                }
                finally
                {
                    pinned.Free();
                }
            }
        }

        private class ValidationErrorHandler : IVEHandler
        {
            private readonly ICLRValidator _validator;
            private readonly List<string> _output;
            private const int MessageLength = 256;

            public ValidationErrorHandler(ICLRValidator validator)
            {
                _validator = validator;
                _output = new List<string>();
            }

            public void SetReporterFtn(long lFnPtr)
            {
                throw new NotImplementedException();
            }

            public void VEHandler(int VECode, tag_VerError Context, Array psa)
            {
                StringBuilder sb = new StringBuilder(MessageLength);
                string message = null;

                if (Context.Flags == (uint)ValidatorFlags.VALIDATOR_CHECK_PEFORMAT_ONLY)
                {
                    GetErrorResourceString(VECode, sb);
                    string formatString = ReplaceFormatItems(sb.ToString(), "%08x", ":x8");
                    formatString = ReplaceFormatItems(formatString, "%d", "");
                    if (psa == null)
                    {
                        psa = new object[0];
                    }

                    message = string.Format(formatString, (object[])psa);
                }
                else
                {
                    _validator.FormatEventInfo(VECode, Context, sb, (uint)MessageLength - 1, psa);
                    message = sb.ToString();
                }

                // retail version of peverify.exe filters out CLS warnings...
                if (!message.Contains("[CLS]"))
                {
                    _output.Add(message);
                }
            }

            public string[] GetOutput()
            {
                return _output.ToArray();
            }

            private static readonly string s_resourceFilePath = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "mscorrc.dll");
            private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
            private static readonly IntPtr s_hMod = LoadLibraryEx(s_resourceFilePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

            private static void GetErrorResourceString(int code, StringBuilder message)
            {
                LoadString(s_hMod, (uint)(code & 0x0000FFFF), message, MessageLength - 1);
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

            private static string ReplaceFormatItems(string input, string oldFormat, string newFormat)
            {
                // not foolproof/efficient, but easy to write/understand...
                var parts = input.Replace(oldFormat, "|").Split('|');

                var formatString = new StringBuilder();
                for (int i = 0; i < parts.Length; i++)
                {
                    formatString.Append(parts[i]);
                    if (i < (parts.Length - 1))
                    {
                        formatString.Append('{');
                        formatString.Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        formatString.Append(newFormat);
                        formatString.Append('}');
                    }
                }

                return formatString.ToString();
            }
        }
    }

    namespace ComTypes
    {
        [ComImport, CoClass(typeof(object)), Guid("90F1A06C-7712-4762-86B5-7A5EBA6BDB02"), TypeIdentifier]
        public interface CLRRuntimeHost : ICLRRuntimeHost
        {
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("90F1A06C-7712-4762-86B5-7A5EBA6BDB02"), TypeIdentifier]
        public interface ICLRRuntimeHost
        {
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("63DF8730-DC81-4062-84A2-1FF943F59FDD"), TypeIdentifier]
        public interface ICLRValidator
        {
            void Validate(
                [In, MarshalAs(UnmanagedType.Interface)] IVEHandler veh,
                [In] uint ulAppDomainId,
                [In] ValidatorFlags ulFlags,
                [In] uint ulMaxError,
                [In] uint token,
                [In, MarshalAs(UnmanagedType.LPWStr)] string fileName,
                [In] IntPtr pe,
                [In] uint ulSize);

            void FormatEventInfo(
                [In, MarshalAs(UnmanagedType.Error)] int hVECode,
                [In] tag_VerError Context,
                [In, Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder msg,
                [In] uint ulMaxLength,
                [In, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array psa);
        }

        [ComImport, Guid("856CA1B2-7DAB-11D3-ACEC-00C04F86C309"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
        public interface IVEHandler
        {
            void VEHandler([In, MarshalAs(UnmanagedType.Error)] int VECode, [In] tag_VerError Context, [In, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array psa);
            void SetReporterFtn([In] long lFnPtr);
        }

        [ComImport, Guid("809C652E-7396-11D2-9771-00A0C9B4D50C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
        public interface IMetaDataDispenser
        {
            void DefineScope(
                [In] ref Guid rclsid,
                [In] uint dwCreateFlags,
                [In] ref Guid riid,
                [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);

            void OpenScope(
                [In, MarshalAs(UnmanagedType.LPWStr)] string szScope,
                [In] CorOpenFlags dwOpenFlags,
                [In] ref Guid riid,
                [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);

            void OpenScopeOnMemory(
                [In] IntPtr pData,
                [In] uint cbData,
                [In] CorOpenFlags dwOpenFlags,
                [In] ref Guid riid,
                [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);
        }

        [ComImport, Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
        public interface IMetaDataImport
        {
        }

        [ComImport, Guid("4709C9C6-81FF-11D3-9FC7-00C04F79A0A3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
        public interface IMetaDataValidate
        {
            void ValidatorInit([In] CorValidatorModuleType dwModuleType, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnk);
            void ValidateMetaData();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4), TypeIdentifier("5477469e-83b1-11d2-8b49-00a0c9b7c9c4", "mscoree.tag_VerError")]
        public struct tag_VerError
        {
            public uint Flags;
            public uint opcode;
            public uint uOffset;
            public uint Token;
            public uint item1_flags;
            public IntPtr item1_data;
            public uint item2_flags;
            public IntPtr item2_data;
        }

        public enum ValidatorFlags : uint
        {
            VALIDATOR_EXTRA_VERBOSE = 0x00000001,
            VALIDATOR_SHOW_SOURCE_LINES = 0x00000002,
            VALIDATOR_CHECK_ILONLY = 0x00000004,
            VALIDATOR_CHECK_PEFORMAT_ONLY = 0x00000008,
            VALIDATOR_NOCHECK_PEFORMAT = 0x00000010
        };

        public enum CorValidatorModuleType : uint
        {
            ValidatorModuleTypeInvalid = 0x00000000,
            ValidatorModuleTypeMin = 0x00000001,
            ValidatorModuleTypePE = 0x00000001,
            ValidatorModuleTypeObj = 0x00000002,
            ValidatorModuleTypeEnc = 0x00000003,
            ValidatorModuleTypeIncr = 0x00000004,
            ValidatorModuleTypeMax = 0x00000004
        };

        public enum CorOpenFlags : uint
        {
            ofRead = 0x00000000,
            ofWrite = 0x00000001,
            ofReadWriteMask = 0x00000001,
            ofCopyMemory = 0x00000002,
            ofCacheImage = 0x00000004,
            ofManifestMetadata = 0x00000008,
            ofReadOnly = 0x00000010,
            ofTakeOwnership = 0x00000020,
            ofNoTypeLib = 0x00000080,
            ofReserved1 = 0x00000100,
            ofReserved2 = 0x00000200,
            ofReserved = 0xffffff40,
        };
    }
}

#endif

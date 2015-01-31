// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//////////////////////////////////////////////////////////////////////////////////////////////////////
// Note: This implementation is copied from vsproject\cps\components\implementations\NativeMethods.cs
//////////////////////////////////////////////////////////////////////////////////////////////////////

//-----------------------------------------------------------------------
// </copyright>
// <summary>Native method calls.</summary>
//-----------------------------------------------------------------------

#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using HANDLE = System.IntPtr;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Watson
{
    internal static class Watson
    {
        private const string ClrRuntimeHostClassIdAsString = "90F1A06E-7712-4762-86B5-7A5EBA6BDB02";
        private const string ClrRuntimeHostInterfaceIdAsString = "90F1A06C-7712-4762-86B5-7A5EBA6BDB02";
        private const string ClrErrorReportingManagerInterfaceIdAsString = "980D2F1A-BF79-4c08-812A-BB9778928F78";

        internal static Guid ClrRuntimeHostClassId = new Guid(ClrRuntimeHostClassIdAsString);
        internal static Guid ClrRuntimeHostInterfaceId = new Guid(ClrRuntimeHostInterfaceIdAsString);
        internal static Guid ClrErrorReportingManagerInterfaceId = new Guid(ClrErrorReportingManagerInterfaceIdAsString);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct BucketParameters
        {
            private int _inited;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _pszEventTypeName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _appName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _appVer;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _appStamp;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            internal string AsmAndModName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _asmVer;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _modStamp;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _methodDef;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _offset;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            private string _exceptionType;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            internal string Component;

            internal string EventType { get { return _pszEventTypeName; } }

            internal IEnumerable<KeyValuePair<string, string>> Parameters
            {
                get
                {
                    yield return new KeyValuePair<string, string>("AppName", _appName);
                    yield return new KeyValuePair<string, string>("AppVer", _appVer);
                    yield return new KeyValuePair<string, string>("AppStamp", _appStamp);
                    yield return new KeyValuePair<string, string>("AsmAndModName", this.AsmAndModName);
                    yield return new KeyValuePair<string, string>("AsmVer", _asmVer);
                    yield return new KeyValuePair<string, string>("ModStamp", _modStamp);
                    yield return new KeyValuePair<string, string>("MethodDef", _methodDef);
                    yield return new KeyValuePair<string, string>("Offset", _offset);
                    yield return new KeyValuePair<string, string>("ExceptionType", _exceptionType);
                    yield return new KeyValuePair<string, string>("Component", this.Component);
                }
            }
        }

        [Guid(ClrRuntimeHostInterfaceIdAsString), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IClrRuntimeHost
        {
            void Start();
            void Stop();
            void SetHostControl(IntPtr hostControl);
            IClrControl GetCLRControl();
            void UnloadAppDomain(int appDomainId, bool waitUntilDone);
            void ExecuteInAppDomain(int appDomainId, IntPtr callback, IntPtr cookie);
            int GetCurrentAppDomainId();
            int ExecuteApplication(string appFullName, int manifestPathCount, string[] manifestPaths, int activationDataCount, string[] activationData);
            int ExecuteInDefaultAppDomain(string assemblyPath, string typeName, string methodName, string argument);
        }

        [Guid("9065597E-D1A1-4fb2-B6BA-7E1FCE230F61"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IClrControl
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object GetCLRManager([In] ref Guid riid);
            void SetAppDomainManagerType(string appDomainManagerAssembly, string appDomainManagerType);
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid(ClrErrorReportingManagerInterfaceIdAsString)]
        internal interface IClrErrorReportingManager
        {
            [PreserveSig]
            int GetBucketParametersForCurrentException([Out]out BucketParameters parameters);
        }
    }
}

#endif

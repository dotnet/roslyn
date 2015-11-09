// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class SqmServiceProvider
    {
        public enum CompilerType
        {
            Compiler = 0,
            CompilerServer = 1,
            Interactive = 2
        }

        // SQM Constants -----  these are NOT FINAL --- real ones will be assigned when approved by VS telemetry team
        public const uint CSHARP_APPID = 50u;
        public const uint BASIC_APPID = 51u;

        public const uint DATAID_SQM_ROSLYN_COMPILERTYPE = 1354u;           // 0=Compiler, 1= CompilerServer, 2=Interactive
        public const uint DATAID_SQM_BUILDVERSION = 1523u;                  // Roslyn Build Version
        public const uint DATAID_SQM_ROSLYN_SOURCES = 1360u;                // No of source code files this compile
        public const uint DATAID_SQM_ROSLYN_REFERENCES = 1359u;             // No of referenced assemblies this compile
        public const uint DATAID_SQM_ROSLYN_ERRORNUMBERS = 1356u;           // List of errors [and warnings as errors]
        public const uint DATAID_SQM_ROSLYN_WARNINGNUMBERS = 1366u;         // List of warnings [excluding warnings as errors]
        public const uint DATAID_SQM_ROSLYN_WARNINGLEVEL = 1365u;           // -warn:n 0 - 4
        public const uint DATAID_SQM_ROSLYN_WARNINGASERRORS = 1364u;        // -warnaserror[+/-]
        public const uint DATAID_SQM_ROSLYN_SUPPRESSWARNINGNUMBERS = 1361u; // -nowarn:blah;blah;blah
        public const uint DATAID_SQM_ROSLYN_WARNASERRORS_NUMBERS = 1362u;   // -warnaserror+:blah;blah;blah
        public const uint DATAID_SQM_ROSLYN_WARNASWARNINGS_NUMBERS = 1363u; // -warnaserror-:blah;blah;blah
        public const uint DATAID_SQM_ROSLYN_OUTPUTKIND = 1358u;             // /target:exe|winexe ... [ConsoleApplication = 0, WindowsApplication = 1, DynamicallyLinkedLibrary = 2, NetModule = 3, WindowsRuntimeMetadata = 4]
        public const uint DATAID_SQM_ROSLYN_LANGUAGEVERSION = 1357u;
        public const uint DATAID_SQM_ROSLYN_EMBEDVBCORE = 1355u;

        private delegate bool QueryServiceDelegate([In]ref Guid rsid, [In]ref Guid riid, [Out]out IVsSqmMulti vssqm);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr moduleHandle, String procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(String libPath);

        private static Optional<QueryServiceDelegate> s_queryService;
        private static readonly object s_guard = new object();

        private static QueryServiceDelegate TryGetSqmServiceDelegateCore(string baseDirectory)
        {
            try
            {
                // These DLLs are only distributed with VS, they are installed to "Program Files (x86)\MSBuild\14.0\Bin".
                IntPtr vssqmdll = IntPtr.Zero;
                string vssqmpath;
                if (IntPtr.Size == 8)
                {
                    vssqmpath = Path.Combine(baseDirectory, @"sqmamd64\vssqmmulti.dll");
                }
                else
                {
                    vssqmpath = Path.Combine(baseDirectory, @"sqmx86\vssqmmulti.dll");
                }
                vssqmdll = SqmServiceProvider.LoadLibrary(vssqmpath);
                if (vssqmdll != IntPtr.Zero)
                {
                    IntPtr queryServicePtr = SqmServiceProvider.GetProcAddress(vssqmdll, "QueryService");
                    return (QueryServiceDelegate)Marshal.GetDelegateForFunctionPointer(queryServicePtr, typeof(QueryServiceDelegate));
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, string.Format("Could not get dll entry point: {0}", e.ToString()));
            }
            return null;
        }

        private static QueryServiceDelegate TryGetSqmServiceDelegate(string baseDirectory)
        {
            lock (s_guard)
            {
                if (!s_queryService.HasValue)
                {
                    s_queryService = TryGetSqmServiceDelegateCore(baseDirectory);
                }

                return s_queryService.Value;
            }
        }

        public static IVsSqmMulti TryGetSqmService(string baseDirectory)
        {
            IVsSqmMulti result = null;
            Guid rsid = new Guid("2508FDF0-EF80-4366-878E-C9F024B8D981");
            Guid riid = new Guid("B17A7D4A-C1A3-45A2-B916-826C3ABA067E");
            QueryServiceDelegate queryService = TryGetSqmServiceDelegate(baseDirectory);
            if (queryService != null)
            {
                try
                {
                    queryService(ref rsid, ref riid, out result);
                }
                catch (Exception e)
                {
                    Debug.Assert(false, string.Format("Could not get SQM service or have SQM related errors: {0}", e.ToString()));
                    return null;
                }
            }
            return result;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
        public const uint DATAID_SQM_BUILDVERSION = 1353u;                  // Roslyn Build Version
        public const uint DATAID_SQM_ROSLYN_SOURCES = 1360u;                // No of source code files this compile
        public const uint DATAID_SQM_ROSLYN_REFERENCES = 1359u;             // No of referenced assemblies this compile
        public const uint DATAID_SQM_ROSLYN_ERRORNUMBERS = 1356u;           // List of errors [and warnings as errors]
        public const uint DATAID_SQM_ROSLYN_WARNINGNUMBERS = 1366u;         // List of warnings [excluding wanings as errors]
        public const uint DATAID_SQM_ROSLYN_WARNINGLEVEL = 1365u;           // -warn:n 0 - 4
        public const uint DATAID_SQM_ROSLYN_WARNINGASERRORS = 1364u;        // -warnaserror[+/-]
        public const uint DATAID_SQM_ROSLYN_SUPPRESSWARNINGNUMBERS = 1361u; // -nowarn:blah;blah;blah
        public const uint DATAID_SQM_ROSLYN_WARNASERRORS_NUMBERS = 1362u;   // -warnaserror+:blah;blah;blah
        public const uint DATAID_SQM_ROSLYN_WARNASWARNINGS_NUMBERS = 1363u; // -warnaserror-:blah;blah;blah
        public const uint DATAID_SQM_ROSLYN_OUTPUTKIND = 1358u;             // /target:exe|winexe ... [ConsoleApplication = 0, WindowsApplication = 1, DynamicallyLinkedLibrary = 2, NetModule = 3, WindowsRuntimeMetadata = 4]
        public const uint DATAID_SQM_ROSLYN_LANGUAGEVERSION = 1357u; 
        public const uint DATAID_SQM_ROSLYN_EMBEDVBCORE = 1355u; 

        [DllImport("vssqmmulti.dll", SetLastError = true)]
        private static extern void QueryService(ref Guid rsid, ref Guid riid, out IVsSqmMulti vssqm);

        public static IVsSqmMulti TryGetSqmService()
        {
            IVsSqmMulti result = null;
            Guid rsid = new Guid("2508FDF0-EF80-4366-878E-C9F024B8D981");
            Guid riid = new Guid("B17A7D4A-C1A3-45A2-B916-826C3ABA067E");
            try
            {
                QueryService(ref rsid, ref riid, out result);
            }
            catch (Exception e)
            {
                Debug.Assert(false, string.Format("Could not get SQM service or have SQM related errors: {0}", e.ToString()));
                return null;
            }
            return result;
        }
    }
}

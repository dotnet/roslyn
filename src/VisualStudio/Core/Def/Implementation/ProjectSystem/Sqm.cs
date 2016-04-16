// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// We want to track Roslyn adoption through SQM.  This datapoint
    /// is used in a cube to track how many sessions are using the 
    /// Roslyn language services.
    /// </summary>
    internal static class Sqm
    {
        // From the V_VSAppID variable
        // 38 CS VB Language Services 
        private const uint CS_VB_Language_Services = 38;
        private const bool AlwaysSendTelemetryEvenIfUserIsOptedOut = false;

        private const int DATAID_SQM_STARTUPAPPBUILDNUMBERSTRING = 780;
        private const int DATAID_SQM_STARTUPAPPBUILDNUMBER = 11;
        private const int DATAID_DP_PRIVATEDOGFOODBUILDNUMBER = 1437;
        private const int DATAID_DP_PRIVATEBUILDNUMBER = 1438;
        private const int DATAID_SQM_USERTYPE = 37;

        private static int s_sqmGuidSet = 0;

        public static void LogSession(IVsSqmMulti session, bool isMicrosoftInternal)
        {
            SetVSSessionGuid(session);
            ReportVSSessionSQM(session, isMicrosoftInternal);
        }

        private static void SetVSSessionGuid(IVsSqmMulti session)
        {
            // Set the VSSessionGuid so our out-of-proc compiler builds can record sqm data
            if (!(System.Threading.Interlocked.CompareExchange(ref s_sqmGuidSet, 1, 0) == 1))
            {
                try
                {
                    var globalSessionGuid = session.GetGlobalSessionGuid();

                    // make sure we disable marking global project collection dirty before setting this global property
                    // otherwise, it will cause full property re-evaluation after this point since project system thinks something related
                    // to projects has changed.
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.DisableMarkDirty = true;
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.SetGlobalProperty("VSSessionGuid", globalSessionGuid.ToString());
                }
                finally
                {
                    Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.DisableMarkDirty = false;
                }
            }
        }

        private static void ReportVSSessionSQM(IVsSqmMulti session, bool isMicrosoftInternal)
        {
            var multiSession = session;
            var vsBuild = Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductVersion;
            var roslynBuild = FileVersionInfo.GetVersionInfo(typeof(Sqm).Assembly.Location).FileVersion;

            uint sessionHandle;
            multiSession.BeginSession(CS_VB_Language_Services, AlwaysSendTelemetryEvenIfUserIsOptedOut, out sessionHandle);

            // Log some common datapoints useful in all cubes
            multiSession.SetStringDatapoint(sessionHandle, DATAID_SQM_STARTUPAPPBUILDNUMBERSTRING, vsBuild);
            LogNumericBuildNumber(multiSession, sessionHandle, DATAID_SQM_STARTUPAPPBUILDNUMBER, vsBuild);

            multiSession.SetStringDatapoint(sessionHandle, DATAID_DP_PRIVATEDOGFOODBUILDNUMBER, roslynBuild);
            LogNumericBuildNumber(multiSession, sessionHandle, DATAID_DP_PRIVATEBUILDNUMBER, roslynBuild);

            multiSession.SetDatapoint(sessionHandle, DATAID_SQM_USERTYPE, isMicrosoftInternal ? 1u : 0);

            multiSession.EndSession(sessionHandle);
        }

        private static void LogNumericBuildNumber(IVsSqmMulti session, uint sessionHandle, uint datapoint, string build)
        {
            uint numericBuild = 0;
            uint.TryParse(build.Split('.').Join(""), out numericBuild);
            session.SetDatapoint(sessionHandle, datapoint, numericBuild);
        }
    }
}

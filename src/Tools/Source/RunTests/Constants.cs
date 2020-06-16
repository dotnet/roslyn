// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal static class Constants
    {
        internal static string JenkinsUrl => Environment.GetEnvironmentVariable("JENKINS_URL");

        internal static bool IsJenkinsRun => !string.IsNullOrEmpty(JenkinsUrl);

        internal static string EnlistmentRoot = IsJenkinsRun
            ? Environment.GetEnvironmentVariable("WORKSPACE")
            : AppDomain.CurrentDomain.BaseDirectory;

        internal static string DashboardUriString => "http://jdash.azurewebsites.net";
    }
}

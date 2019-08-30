// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

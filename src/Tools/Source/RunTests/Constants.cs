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
        internal static string ResultsDirectoryName => "xUnitResults";

        internal static bool IsJenkinsRun => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));

        internal static string DashboardUriString => "http://jdash.azurewebsites.net";
    }
}

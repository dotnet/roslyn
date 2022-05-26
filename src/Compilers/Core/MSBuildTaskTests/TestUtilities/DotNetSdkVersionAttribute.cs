// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    /// <summary>
    /// Attribute added to the test assembly during build. 
    /// Captures the version of dotnet SDK the build is targeting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class DotNetSdkVersionAttribute : Attribute
    {
        public string Version { get; }

        public DotNetSdkVersionAttribute(string version)
        {
            Version = version;
        }
    }
}

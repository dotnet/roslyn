// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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

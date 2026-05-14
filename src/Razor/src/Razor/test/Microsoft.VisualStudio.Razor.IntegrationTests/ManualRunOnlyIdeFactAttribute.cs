// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ManualRunOnlyIdeFactAttribute : IdeFactAttribute
{
    public ManualRunOnlyIdeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("_IntegrationTestsRunningInCI") is not null)
        {
            Skip = "This test can only run manually";
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

extern alias InteractiveHost;

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using System.IO;
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    public abstract class AbstractInteractiveHostTests : CSharpTestBase
    {
        internal static string GetInteractiveHostDirectory()
            => Path.GetDirectoryName(typeof(StressTests).Assembly.Location);

        // Forces xUnit to load dependent assemblies before we launch InteractiveHost.exe process.
        private static readonly Type[] s_testDependencies = new[]
        {
            typeof(InteractiveHost),
            typeof(CSharpCompilation)
        };
    }
}

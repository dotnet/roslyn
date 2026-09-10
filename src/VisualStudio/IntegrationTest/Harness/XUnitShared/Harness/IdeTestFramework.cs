// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System.Reflection;
    using Xunit.Sdk;
    using Xunit.v3;

    public class IdeTestFramework : XunitTestFramework
    {
        protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
        {
            return new IdeTestFrameworkExecutor(new XunitTestAssembly(assembly, configFilePath: null));
        }
    }
}

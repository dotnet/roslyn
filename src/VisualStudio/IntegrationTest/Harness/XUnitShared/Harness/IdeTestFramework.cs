// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Runner.Common;
    using Xunit.Sdk;
    using Xunit.v3;

    public class IdeTestFramework : XunitTestFramework
    {
        private ITestFrameworkDiscoveryOptions? _discoveryOptions;

        protected override ITestFrameworkDiscoverer CreateDiscoverer(Assembly assembly)
        {
            return new IdeTestFrameworkDiscoverer(assembly, this);
        }

        protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
        {
            var discoveryOptions = _discoveryOptions ?? TestFrameworkOptions.ForDiscovery(new TestAssemblyConfiguration());
            return new IdeTestFrameworkExecutor(new XunitTestAssembly(assembly), discoveryOptions);
        }

        private sealed class IdeTestFrameworkDiscoverer(Assembly assembly, IdeTestFramework testFramework) : XunitTestFrameworkDiscoverer(new XunitTestAssembly(assembly))
        {
            public override ValueTask Find(Func<ITestCase, ValueTask<bool>> callback, ITestFrameworkDiscoveryOptions discoveryOptions, Type[]? types = null, CancellationToken? cancellationToken = null)
            {
                // Capture the discovery options so we can pass them to our executor, allowing us to rediscover the tests the same way in the Visual Studio process
                testFramework._discoveryOptions = discoveryOptions;
                return base.Find(callback, discoveryOptions, types, cancellationToken);
            }
        }
    }
}

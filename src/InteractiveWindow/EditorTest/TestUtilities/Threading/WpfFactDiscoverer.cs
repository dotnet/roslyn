// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    public class WpfFactDiscoverer : FactDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        /// <summary>
        /// A <see cref="SemaphoreSlim"/> used to ensure that only a single <see cref="WpfFactAttribute"/>-attributed test runs at once.
        /// This requirement must be made because, currently, <see cref="WpfTestCase"/>'s logic sets various static state before a method
        /// runs. If two tests run interleaved on the same scheduler (i.e. if one yields with an await) then all bets are off.
        /// </summary>
        private readonly SemaphoreSlim _wpfTestSerializationGate = new SemaphoreSlim(initialCount: 1);

        public WpfFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
            => new WpfTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, _wpfTestSerializationGate);
    }
}

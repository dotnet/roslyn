// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System.Reflection;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class IdeTestFramework : XunitTestFramework
    {
        public IdeTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new IdeTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }
    }
}

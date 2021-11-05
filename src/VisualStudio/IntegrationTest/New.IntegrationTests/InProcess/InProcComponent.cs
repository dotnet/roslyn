// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal abstract class InProcComponent
    {
        protected InProcComponent(TestServices testServices)
        {
            TestServices = testServices ?? throw new ArgumentNullException(nameof(testServices));
        }

        public TestServices TestServices { get; }

        protected JoinableTaskFactory JoinableTaskFactory => TestServices.JoinableTaskFactory;
    }
}

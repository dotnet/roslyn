// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [Export(typeof(IDiagnosticUpdateSourceRegistrationService))]
    [Shared]
    [PartNotDiscoverable]
    internal class MockDiagnosticUpdateSourceRegistrationService : IDiagnosticUpdateSourceRegistrationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockDiagnosticUpdateSourceRegistrationService()
        {
        }

        public void Register(IDiagnosticUpdateSource source)
        {
            // do nothing
        }
    }
}

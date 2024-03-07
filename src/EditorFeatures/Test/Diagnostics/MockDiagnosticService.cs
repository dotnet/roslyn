// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared, PartNotDiscoverable]
    internal class MockDiagnosticService : IDiagnosticService
    {
        public const string DiagnosticId = "MockId";

        public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>> DiagnosticsUpdated { add { } remove { } }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockDiagnosticService()
        {
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class TestDiagnosticAnalyzerService : DiagnosticAnalyzerService
    {
        internal TestDiagnosticAnalyzerService(
            IDiagnosticUpdateSourceRegistrationService registrationService = null,
            IAsynchronousOperationListener listener = null)
            : base(registrationService ?? new MockDiagnosticUpdateSourceRegistrationService(),
                   listener)
        {
        }
    }
}

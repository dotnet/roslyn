﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class MockDiagnosticUpdateSourceRegistrationService : IDiagnosticUpdateSourceRegistrationService
    {
        public void Register(IDiagnosticUpdateSource source)
        {
            // do nothing
        }
    }
}

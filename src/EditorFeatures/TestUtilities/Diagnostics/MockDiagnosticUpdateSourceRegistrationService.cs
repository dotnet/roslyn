// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
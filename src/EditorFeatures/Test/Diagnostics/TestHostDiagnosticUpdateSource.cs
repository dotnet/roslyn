// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class TestHostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource
    {
        private readonly Workspace _workspace;

        public TestHostDiagnosticUpdateSource(Workspace workspace)
        {
            _workspace = workspace;
        }

        public override Workspace Workspace
        {
            get
            {
                return _workspace;
            }
        }

        public override int GetHashCode()
        {
            return _workspace.GetHashCode();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class TestHostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource
    {
        private readonly Workspace _workspace;

        public TestHostDiagnosticUpdateSource(Workspace workspace)
            => _workspace = workspace;

        public override Workspace Workspace
        {
            get
            {
                return _workspace;
            }
        }

        public override int GetHashCode()
            => _workspace.GetHashCode();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.Composition;

namespace Roslyn.Test.Utilities
{
    public abstract partial class AbstractLanguageServerProtocolTests
    {
        [Export(typeof(LspWorkspaceRegistrationService)), Shared, PartNotDiscoverable]
        internal class TestWorkspaceRegistrationService : LspWorkspaceRegistrationService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestWorkspaceRegistrationService()
            {
            }

            public override string GetHostWorkspaceKind()
            {
                return WorkspaceKind.Host;
            }
        }
    }
}

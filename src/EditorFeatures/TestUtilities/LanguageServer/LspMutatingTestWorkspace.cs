// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities
{
    internal sealed class LspMutatingTestWorkspace : TestWorkspace, IMutatingLspWorkspace
    {
        public LspMutatingTestWorkspace(
            TestComposition? composition = null,
            string? workspaceKind = WorkspaceKind.Host,
            Guid solutionTelemetryId = default,
            bool disablePartialSolutions = true,
            bool ignoreUnchangeableDocumentsWhenApplyingChanges = true,
            WorkspaceConfigurationOptions? configurationOptions = null)
            : base(composition,
                  workspaceKind,
                  solutionTelemetryId,
                  disablePartialSolutions,
                  ignoreUnchangeableDocumentsWhenApplyingChanges,
                  configurationOptions)
        {
        }
    }
}

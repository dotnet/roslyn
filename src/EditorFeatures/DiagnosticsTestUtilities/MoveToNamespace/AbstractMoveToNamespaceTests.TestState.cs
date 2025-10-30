// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace;

public abstract partial class AbstractMoveToNamespaceTests
{
    internal sealed class TestState : IDisposable
    {
        public TestState(EditorTestWorkspace workspace)
            => Workspace = workspace;

        public void Dispose()
            => Workspace?.Dispose();

        public EditorTestWorkspace Workspace { get; }
        public EditorTestHostDocument TestInvocationDocument => Workspace.Documents.Single();
        public Document InvocationDocument => Workspace.CurrentSolution.GetDocument(TestInvocationDocument.Id);

        public TestMoveToNamespaceOptionsService TestMoveToNamespaceOptionsService
            => (TestMoveToNamespaceOptionsService)MoveToNamespaceService.OptionsService;

        public IMoveToNamespaceService MoveToNamespaceService
            => InvocationDocument.GetRequiredLanguageService<IMoveToNamespaceService>();
    }
}

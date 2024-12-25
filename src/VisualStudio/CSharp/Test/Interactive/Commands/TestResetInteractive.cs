// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using System.Collections.Generic;
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    internal class TestResetInteractive : ResetInteractive
    {
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        private readonly bool _buildSucceeds;

        internal int BuildProjectCount { get; private set; }

        internal int CancelBuildProjectCount { get; private set; }

        internal ImmutableArray<string> References { get; set; }

        internal ImmutableArray<string> ReferenceSearchPaths { get; set; }

        internal ImmutableArray<string> SourceSearchPaths { get; set; }

        internal ImmutableArray<string> ProjectNamespaces { get; set; }

        internal ImmutableArray<string> NamespacesToImport { get; set; }

        internal InteractiveHostPlatform? Platform { get; set; }

        internal string ProjectDirectory { get; set; }

        public TestResetInteractive(
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            EditorOptionsService editorOptionsService,
            Func<string, string> createReference,
            Func<string, string> createImport,
            bool buildSucceeds)
            : base(editorOptionsService, createReference, createImport)
        {
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _buildSucceeds = buildSucceeds;
        }

        protected override void CancelBuildProject()
        {
            CancelBuildProjectCount++;
        }

        protected override Task<bool> BuildProjectAsync()
        {
            BuildProjectCount++;
            return Task.FromResult(_buildSucceeds);
        }

        protected override bool GetProjectProperties(
            out ImmutableArray<string> references,
            out ImmutableArray<string> referenceSearchPaths,
            out ImmutableArray<string> sourceSearchPaths,
            out ImmutableArray<string> projectNamespaces,
            out string projectDirectory,
            out InteractiveHostPlatform? platform)
        {
            references = References;
            referenceSearchPaths = ReferenceSearchPaths;
            sourceSearchPaths = SourceSearchPaths;
            projectNamespaces = ProjectNamespaces;
            projectDirectory = ProjectDirectory;
            platform = Platform;
            return true;
        }

        protected override IUIThreadOperationExecutor GetUIThreadOperationExecutor()
        {
            return _uiThreadOperationExecutor;
        }

        protected override Task<IEnumerable<string>> GetNamespacesToImportAsync(IEnumerable<string> namespacesToImport, IInteractiveWindow interactiveWindow)
        {
            return Task.FromResult((IEnumerable<string>)NamespacesToImport);
        }
    }
}

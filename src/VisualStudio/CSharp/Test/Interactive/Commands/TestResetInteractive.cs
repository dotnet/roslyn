﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias InteractiveHost;

using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using System.Collections.Generic;
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    internal class TestResetInteractive : ResetInteractive
    {
        private readonly IWaitIndicator _waitIndicator;

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
            IWaitIndicator waitIndicator,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            Func<string, string> createReference,
            Func<string, string> createImport,
            bool buildSucceeds)
            : base(editorOptionsFactoryService, createReference, createImport)
        {
            _waitIndicator = waitIndicator;
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

        protected override IWaitIndicator GetWaitIndicator()
        {
            return _waitIndicator;
        }

        protected override Task<IEnumerable<string>> GetNamespacesToImportAsync(IEnumerable<string> namespacesToImport, IInteractiveWindow interactiveWindow)
        {
            return Task.FromResult((IEnumerable<string>)NamespacesToImport);
        }
    }
}

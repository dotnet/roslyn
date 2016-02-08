// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    internal class TestResetInteractive : ResetInteractive
    {
        private IWaitIndicator _waitIndicator;

        private bool _buildSucceeds;

        internal int BuildProjectCount { get; private set; }

        internal int CancelBuildProjectCount { get; private set; }

        internal ImmutableArray<string> References { get; set; }

        internal ImmutableArray<string> ReferenceSearchPaths { get; set; }

        internal ImmutableArray<string> SourceSearchPaths { get; set; }

        internal ImmutableArray<string> NamespacesToImport { get; set; }

        internal string ProjectDirectory { get; set; }

        public TestResetInteractive(IWaitIndicator waitIndicator, Func<string, string> createReference, Func<string, string> createImport, bool buildSucceeds)
            : base(createReference, createImport)
        {
            _waitIndicator = waitIndicator;
            _buildSucceeds = buildSucceeds;
        }

        protected override void CancelBuildProject()
        {
            CancelBuildProjectCount++;
        }

        protected override Task<bool> BuildProject()
        {
            BuildProjectCount++;
            return Task.FromResult(_buildSucceeds);
        }

        protected override bool GetProjectProperties(
            out ImmutableArray<string> references,
            out ImmutableArray<string> referenceSearchPaths,
            out ImmutableArray<string> sourceSearchPaths,
            out ImmutableArray<string> namespacesToImport,
            out string projectDirectory)
        {
            references = References;
            referenceSearchPaths = ReferenceSearchPaths;
            sourceSearchPaths = SourceSearchPaths;
            namespacesToImport = NamespacesToImport;
            projectDirectory = ProjectDirectory;
            return true;
        }

        protected override IWaitIndicator GetWaitIndicator()
        {
            return _waitIndicator;
        }
    }
}

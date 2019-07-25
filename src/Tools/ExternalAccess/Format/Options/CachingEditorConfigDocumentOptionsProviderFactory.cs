// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Format.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    [Export(typeof(IDocumentOptionsProviderFactory)), Shared]
    class CachingEditorConfigDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        private readonly ICodingConventionsManager _codingConventionsManager;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CachingEditorConfigDocumentOptionsProviderFactory(
            ICodingConventionsManager codingConventionsManager)
        {
            _codingConventionsManager = codingConventionsManager;
        }

        public IDocumentOptionsProvider TryCreate(Workspace workspace)
        {
            return new CachingEditorConfigDocumentOptionsProvider(workspace, _codingConventionsManager);
        }
    }
}

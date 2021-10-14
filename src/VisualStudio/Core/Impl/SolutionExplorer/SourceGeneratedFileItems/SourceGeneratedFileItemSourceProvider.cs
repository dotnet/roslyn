// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(SourceGeneratedFileItemSourceProvider))]
    [Order]
    [AppliesToProject("CSharp | VB")]
    internal sealed class SourceGeneratedFileItemSourceProvider : AttachedCollectionSourceProvider<SourceGeneratorItem>
    {
        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SourceGeneratedFileItemSourceProvider(VisualStudioWorkspace workspace, IAsynchronousOperationListenerProvider asyncListenerProvider, IThreadingContext threadingContext)
        {
            _workspace = workspace;
            _asyncListener = asyncListenerProvider.GetListener(FeatureAttribute.SourceGenerators);
            _threadingContext = threadingContext;
        }

        protected override IAttachedCollectionSource? CreateCollectionSource(SourceGeneratorItem item, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                return new SourceGeneratedFileItemSource(item, _workspace, _asyncListener, _threadingContext);
            }

            return null;
        }
    }
}

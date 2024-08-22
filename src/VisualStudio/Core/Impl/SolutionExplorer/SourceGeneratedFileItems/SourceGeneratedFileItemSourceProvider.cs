// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(SourceGeneratedFileItemSourceProvider)), Order]
[AppliesToProject("CSharp | VB")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SourceGeneratedFileItemSourceProvider(
    VisualStudioWorkspace workspace,
    IAsynchronousOperationListenerProvider asyncListenerProvider,
    IThreadingContext threadingContext)
    : AttachedCollectionSourceProvider<SourceGeneratorItem>
{
    private readonly IAsynchronousOperationListener _asyncListener = asyncListenerProvider.GetListener(FeatureAttribute.SourceGenerators);

    protected override IAttachedCollectionSource? CreateCollectionSource(SourceGeneratorItem item, string relationshipName)
        => relationshipName == KnownRelationships.Contains
            ? new SourceGeneratedFileItemSource(item, threadingContext, workspace, _asyncListener)
            : null;
}

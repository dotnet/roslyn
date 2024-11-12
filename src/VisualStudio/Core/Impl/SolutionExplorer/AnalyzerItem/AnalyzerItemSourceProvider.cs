// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(AnalyzerItemSourceProvider)), Order]
[AppliesToProject("(CSharp | VB) & !CPS")]  // in the CPS case, the Analyzers items are created by the project system
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AnalyzerItemSourceProvider(
    [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler,
    IAsynchronousOperationListenerProvider listenerProvider)
    : AttachedCollectionSourceProvider<AnalyzersFolderItem>
{
    protected override IAttachedCollectionSource? CreateCollectionSource(AnalyzersFolderItem analyzersFolder, string relationshipName)
        => relationshipName == KnownRelationships.Contains
            ? new AnalyzerItemSource(analyzersFolder, commandHandler, listenerProvider)
            : null;
}

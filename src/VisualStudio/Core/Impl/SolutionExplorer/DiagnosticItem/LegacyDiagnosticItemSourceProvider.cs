// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(LegacyDiagnosticItemSourceProvider))]
[Order]
[AppliesToProject("(CSharp | VB) & !CPS")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LegacyDiagnosticItemSourceProvider(
    [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler,
    IDiagnosticAnalyzerService diagnosticAnalyzerService) : AttachedCollectionSourceProvider<AnalyzerItem>
{
    protected override IAttachedCollectionSource? CreateCollectionSource(AnalyzerItem item, string relationshipName)
    {
        if (relationshipName == KnownRelationships.Contains)
        {
            return new LegacyDiagnosticItemSource(
                item, commandHandler, diagnosticAnalyzerService);
        }

        return null;
    }
}

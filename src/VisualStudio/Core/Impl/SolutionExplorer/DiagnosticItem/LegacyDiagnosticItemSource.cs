// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class LegacyDiagnosticItemSource : BaseDiagnosticAndGeneratorItemSource
{
    private readonly AnalyzerItem _item;

    public LegacyDiagnosticItemSource(
        AnalyzerItem item,
        IAnalyzersCommandHandler commandHandler,
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(
            item.AnalyzersFolder.Workspace,
            item.AnalyzersFolder.ProjectId,
            commandHandler,
            diagnosticAnalyzerService,
            listenerProvider)
    {
        _item = item;
        this.AnalyzerReference = item.AnalyzerReference;
    }

    public override object SourceItem => _item;
}

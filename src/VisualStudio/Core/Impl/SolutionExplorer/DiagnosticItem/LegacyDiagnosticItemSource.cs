// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class LegacyDiagnosticItemSource : BaseDiagnosticAndGeneratorItemSource
{
    private readonly AnalyzerItem _item;

    public LegacyDiagnosticItemSource(
        IThreadingContext threadingContext,
        AnalyzerItem item,
        IAnalyzersCommandHandler commandHandler,
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(
            threadingContext,
            item.AnalyzersFolder.Workspace,
            item.AnalyzersFolder.ProjectId,
            commandHandler,
            listenerProvider)
    {
        _item = item;
        this.AnalyzerReference = item.AnalyzerReference;
    }

    public override object SourceItem => _item;
}

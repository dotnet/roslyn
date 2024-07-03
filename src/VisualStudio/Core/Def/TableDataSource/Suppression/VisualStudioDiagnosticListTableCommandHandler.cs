// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Configuration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;

[Export(typeof(VisualStudioDiagnosticListTableCommandHandler))]
internal partial class VisualStudioDiagnosticListTableCommandHandler
{
    private readonly IThreadingContext _threadingContext;
    private readonly VisualStudioWorkspace _workspace;
    private readonly VisualStudioSuppressionFixService _suppressionFixService;
    private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
    private readonly IDiagnosticAnalyzerService _diagnosticService;
    private readonly ICodeActionEditHandlerService _editHandlerService;
    private readonly IAsynchronousOperationListener _listener;

    private IWpfTableControl? _tableControl;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioDiagnosticListTableCommandHandler(
        IThreadingContext threadingContext,
        SVsServiceProvider serviceProvider,
        VisualStudioWorkspace workspace,
        IVisualStudioSuppressionFixService suppressionFixService,
        VisualStudioDiagnosticListSuppressionStateService suppressionStateService,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        IDiagnosticAnalyzerService diagnosticService,
        ICodeActionEditHandlerService editHandlerService,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _workspace = workspace;
        _suppressionFixService = (VisualStudioSuppressionFixService)suppressionFixService;
        _suppressionStateService = suppressionStateService;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _diagnosticService = diagnosticService;
        _editHandlerService = editHandlerService;
        _listener = listenerProvider.GetListener(FeatureAttribute.ErrorList);
    }

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
    {
        var errorList = await serviceProvider.GetServiceAsync<SVsErrorList, IErrorList>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(false);
        _tableControl = errorList?.TableControl;
    }
}

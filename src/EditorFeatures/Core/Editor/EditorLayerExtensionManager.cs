// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Internal.Log.FunctionId;
using static Microsoft.CodeAnalysis.Internal.Log.Logger;
using static Microsoft.CodeAnalysis.RoslynAssemblyHelper;

namespace Microsoft.CodeAnalysis.Editor;

[ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Editor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorLayerExtensionManager(
    [ImportMany] IEnumerable<IExtensionErrorHandler> errorHandlers) : IWorkspaceServiceFactory
{
    private readonly ImmutableArray<IExtensionErrorHandler> _errorHandlers = errorHandlers.ToImmutableArray();

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        var errorReportingService = workspaceServices.GetRequiredService<IErrorReportingService>();
        var errorLoggerService = workspaceServices.GetRequiredService<IErrorLoggerService>();
        return new ExtensionManager(errorReportingService, errorLoggerService, _errorHandlers);
    }

    internal sealed class ExtensionManager(
        IErrorReportingService errorReportingService,
        IErrorLoggerService errorLoggerService,
        ImmutableArray<IExtensionErrorHandler> errorHandlers) : AbstractExtensionManager
    {
        protected override void HandleNonCancellationException(object provider, Exception exception)
        {
            Debug.Assert(exception is not OperationCanceledException);

            if (provider is CodeFixProvider
                or CodeRefactoringProvider
                or CodeRefactorings.FixAllProvider
                or CodeFixes.FixAllProvider
                or CompletionProvider)
            {
                if (!IsIgnored(provider))
                {
                    this.DisableProvider(provider);

                    var providerType = provider.GetType();

                    errorReportingService?.ShowGlobalErrorInfo(
                        message: string.Format(WorkspacesResources._0_encountered_an_error_and_has_been_disabled, providerType.Name),
                        TelemetryFeatureName.GetExtensionName(providerType),
                        exception,
                        new InfoBarUI(WorkspacesResources.Show_Stack_Trace, InfoBarUI.UIKind.HyperLink, () => ShowDetailedErrorInfo(exception), closeAfterAction: false),
                        new InfoBarUI(WorkspacesResources.Enable, InfoBarUI.UIKind.Button, () =>
                        {
                            EnableProvider(provider);
                            LogEnableProvider(provider);
                        }),
                        new InfoBarUI(WorkspacesResources.Enable_and_ignore_future_errors, InfoBarUI.UIKind.Button, () =>
                        {
                            EnableProvider(provider);
                            IgnoreProvider(provider);
                            LogEnableAndIgnoreProvider(provider);
                        }),
                        new InfoBarUI(string.Empty, InfoBarUI.UIKind.Close, () => LogLeaveDisabled(provider)));
                }
                else
                {
                    LogAction(CodefixInfobar_ErrorIgnored, provider);
                }
            }
            else
            {

                this.DisableProvider(provider);
                errorHandlers.Do(h => h.HandleError(provider, exception));
            }

            errorLoggerService?.LogException(provider, exception);
        }

        private void ShowDetailedErrorInfo(Exception exception)
            => errorReportingService.ShowDetailedErrorInfo(exception);

        private static void LogLeaveDisabled(object provider)
            => LogAction(CodefixInfobar_LeaveDisabled, provider);

        private static void LogEnableAndIgnoreProvider(object provider)
            => LogAction(CodefixInfobar_EnableAndIgnoreFutureErrors, provider);

        private static void LogEnableProvider(object provider)
            => LogAction(CodefixInfobar_Enable, provider);

        private static void LogAction(FunctionId functionId, object provider)
        {
            if (IsRoslynCodefix(provider))
            {
                Log(functionId, $"Name: {provider.GetType().FullName} Assembly Version: {provider.GetType().Assembly.GetName().Version}");
            }
            else
            {
                Log(functionId);
            }
        }

        private static bool IsRoslynCodefix(object source) => HasRoslynPublicKey(source);
    }
}

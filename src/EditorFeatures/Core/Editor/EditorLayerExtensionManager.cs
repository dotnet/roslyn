// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Internal.Log.FunctionId;
using static Microsoft.CodeAnalysis.Internal.Log.Logger;
using static Microsoft.CodeAnalysis.RoslynAssemblyHelper;

namespace Microsoft.CodeAnalysis.Editor
{
    [ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Editor), Shared]
    internal class EditorLayerExtensionManager : IWorkspaceServiceFactory
    {
        private readonly List<IExtensionErrorHandler> _errorHandlers;
        private readonly IGlobalOptionService _optionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorLayerExtensionManager(
            IGlobalOptionService optionService,
            [ImportMany] IEnumerable<IExtensionErrorHandler> errorHandlers)
        {
            _optionService = optionService;
            _errorHandlers = errorHandlers.ToList();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var errorReportingService = workspaceServices.GetRequiredService<IErrorReportingService>();
            var errorLoggerService = workspaceServices.GetRequiredService<IErrorLoggerService>();
            return new ExtensionManager(_optionService, errorReportingService, errorLoggerService, _errorHandlers);
        }

        internal class ExtensionManager : AbstractExtensionManager
        {
            private readonly List<IExtensionErrorHandler> _errorHandlers;
            private readonly IGlobalOptionService _globalOptions;
            private readonly IErrorReportingService _errorReportingService;
            private readonly IErrorLoggerService _errorLoggerService;

            public ExtensionManager(
                IGlobalOptionService globalOptions,
                IErrorReportingService errorReportingService,
                IErrorLoggerService errorLoggerService,
                List<IExtensionErrorHandler> errorHandlers)
            {
                _globalOptions = globalOptions;
                _errorHandlers = errorHandlers;
                _errorReportingService = errorReportingService;
                _errorLoggerService = errorLoggerService;
            }

            public override void HandleException(object provider, Exception exception)
            {
                if (provider is CodeFixProvider or FixAllProvider or CodeRefactoringProvider)
                {
                    if (!IsIgnored(provider) &&
                        _globalOptions.GetOption(ExtensionManagerOptions.DisableCrashingExtensions))
                    {
                        base.HandleException(provider, exception);

                        var providerType = provider.GetType();

                        _errorReportingService?.ShowGlobalErrorInfo(
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
                    if (_globalOptions.GetOption(ExtensionManagerOptions.DisableCrashingExtensions))
                    {
                        base.HandleException(provider, exception);
                    }

                    _errorHandlers.Do(h => h.HandleError(provider, exception));
                }

                _errorLoggerService?.LogException(provider, exception);
            }

            private void ShowDetailedErrorInfo(Exception exception)
                => _errorReportingService.ShowDetailedErrorInfo(exception);

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
}

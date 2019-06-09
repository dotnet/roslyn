// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
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

        [ImportingConstructor]
        public EditorLayerExtensionManager(
            [ImportMany]IEnumerable<IExtensionErrorHandler> errorHandlers)
        {
            _errorHandlers = errorHandlers.ToList();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var optionService = workspaceServices.GetService<IOptionService>();
            var errorReportingService = workspaceServices.GetService<IErrorReportingService>();
            var errorLoggerService = workspaceServices.GetService<IErrorLoggerService>();
            return new ExtensionManager(optionService, errorReportingService, errorLoggerService, _errorHandlers);
        }

        internal class ExtensionManager : AbstractExtensionManager
        {
            private readonly List<IExtensionErrorHandler> _errorHandlers;
            private readonly IOptionService _optionsService;
            private readonly IErrorReportingService _errorReportingService;
            private readonly IErrorLoggerService _errorLoggerService;

            public ExtensionManager(
                IOptionService optionsService,
                IErrorReportingService errorReportingService,
                IErrorLoggerService errorLoggerService,
                List<IExtensionErrorHandler> errorHandlers)
            {
                _optionsService = optionsService;
                _errorHandlers = errorHandlers;
                _errorReportingService = errorReportingService;
                _errorLoggerService = errorLoggerService;
            }

            public override void HandleException(object provider, Exception exception)
            {
                if (provider is CodeFixProvider || provider is FixAllProvider || provider is CodeRefactoringProvider)
                {
                    if (!IsIgnored(provider) &&
                        _optionsService.GetOption(ExtensionManagerOptions.DisableCrashingExtensions))
                    {
                        base.HandleException(provider, exception);

                        _errorReportingService?.ShowErrorInfoInActiveView(String.Format(WorkspacesResources._0_encountered_an_error_and_has_been_disabled, provider.GetType().Name),
                            new InfoBarUI(WorkspacesResources.Show_Stack_Trace, InfoBarUI.UIKind.HyperLink, () => ShowDetailedErrorInfo(exception), closeAfterAction: false),
                            new InfoBarUI(WorkspacesResources.Enable, InfoBarUI.UIKind.Button, () => { EnableProvider(provider); LogEnableProvider(provider); }),
                            new InfoBarUI(WorkspacesResources.Enable_and_ignore_future_errors, InfoBarUI.UIKind.Button, () => { EnableProvider(provider); IgnoreProvider(provider); LogEnableAndIgnoreProvider(provider); }),
                            new InfoBarUI(String.Empty, InfoBarUI.UIKind.Close, () => LogLeaveDisabled(provider)));
                    }
                    else
                    {
                        LogAction(CodefixInfobar_ErrorIgnored, provider);
                    }
                }
                else
                {
                    if (_optionsService.GetOption(ExtensionManagerOptions.DisableCrashingExtensions))
                    {
                        base.HandleException(provider, exception);
                    }

                    _errorHandlers.Do(h => h.HandleError(provider, exception));
                }

                _errorLoggerService?.LogException(provider, exception);
            }

            private void ShowDetailedErrorInfo(Exception exception)
            {
                _errorReportingService.ShowDetailedErrorInfo(exception);
            }

            private static void LogLeaveDisabled(object provider)
            {
                LogAction(CodefixInfobar_LeaveDisabled, provider);
            }

            private static void LogEnableAndIgnoreProvider(object provider)
            {
                LogAction(CodefixInfobar_EnableAndIgnoreFutureErrors, provider);
            }

            private static void LogEnableProvider(object provider)
            {
                LogAction(CodefixInfobar_Enable, provider);
            }

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

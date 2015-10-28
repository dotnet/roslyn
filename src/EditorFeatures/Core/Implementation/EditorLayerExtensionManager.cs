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

                        _errorReportingService?.ShowErrorInfoForCodeFix(
                            provider.GetType().Name,
                            () => { EnableProvider(provider); LogEnableProvider(provider); },
                            () => { EnableProvider(provider); IgnoreProvider(provider); LogEnableAndIgnoreProvider(provider); },
                            () => LogLeaveDisabled(provider));
                    }
                    else
                    {
                        if (IsRoslynCodefix(provider))
                        {
                            Log(CodefixInfobar_ErrorIgnored, $"Name: {provider.GetType().Name} Assembly Version: {provider.GetType().Assembly.GetName().Version}");
                        }
                        else
                        {
                            Log(CodefixInfobar_ErrorIgnored);
                        }
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

            private void LogLeaveDisabled(object provider)
            {
                if (IsRoslynCodefix(provider))
                {
                    Log(CodefixInfobar_LeaveDisabled, $"Name: {provider.GetType().Name} Assembly Version: {provider.GetType().Assembly.GetName().Version}");
                }
                else
                {
                    Log(CodefixInfobar_LeaveDisabled);
                }
            }

            private void LogEnableAndIgnoreProvider(object provider)
            {
                if (IsRoslynCodefix(provider))
                {
                    Log(CodefixInfobar_EnableAndIgnoreFutureErrors, $"Name: {provider.GetType().Name} Assembly Version: {provider.GetType().Assembly.GetName().Version}");
                }
                else
                {
                    Log(CodefixInfobar_EnableAndIgnoreFutureErrors);
                }
            }

            private void LogEnableProvider(object provider)
            {
                if (IsRoslynCodefix(provider))
                {
                    Log(CodefixInfobar_Enable, $"Name: {provider.GetType().Name} Assembly Version: {provider.GetType().Assembly.GetName().Version}");
                }
                else
                {
                    Log(CodefixInfobar_Enable);
                }
            }

            private bool IsRoslynCodefix(object source) => HasRoslynPublicKey(source);
        }
    }
}

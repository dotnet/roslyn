// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Editor)]
    [Shared]
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
            var documentTrackingService = workspaceServices.GetService<IDocumentTrackingService>();
            var errorLoggerService = workspaceServices.GetService<IErrorLogger>();
            return new ExtensionManager(optionService, errorReportingService, documentTrackingService, errorLoggerService,  _errorHandlers);
        }

        internal class ExtensionManager : AbstractExtensionManager
        {
            private readonly List<IExtensionErrorHandler> _errorHandlers;
            private readonly IOptionService _optionsService;
            private readonly IDocumentTrackingService _documentTrackingService;
            private readonly IErrorReportingService _errorReportingService;
            private readonly IErrorLogger _errorLoggerService;

            public ExtensionManager(
                IOptionService optionsService,
                IErrorReportingService errorReportingService,
                IDocumentTrackingService documentTrackingService,
                IErrorLogger errorLoggerService,
                List<IExtensionErrorHandler> errorHandlers)
            {
                _optionsService = optionsService;
                _errorHandlers = errorHandlers;
                _errorReportingService = errorReportingService;
                _documentTrackingService = documentTrackingService;
                _errorLoggerService = errorLoggerService;
            }

            public override void HandleException(object provider, Exception exception)
            {
                if (provider is CodeFixProvider || provider is FixAllProvider)
                {
                    if (!IsIgnored(provider) && _optionsService.GetOption(ExtensionManagerOptions.DisableCrashingExtensions))
                    {
                        base.HandleException(provider, exception);

                        _errorReportingService?.ShowErrorInfoForCodeFix(
                            _documentTrackingService.GetActiveDocument(),
                            provider.GetType().Name,
                            () => EnableProvider(provider),
                            () => { EnableProvider(provider); IgnoreProvider(provider); });
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

                _errorLoggerService?.LogError(provider.GetType().Name, exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }
    }
}

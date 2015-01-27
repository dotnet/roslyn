// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
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
            return new ExtensionManager(optionService, _errorHandlers);
        }

        private class ExtensionManager : AbstractExtensionManager
        {
            private readonly List<IExtensionErrorHandler> _errorHandlers;
            private readonly IOptionService _optionsService;

            public ExtensionManager(IOptionService optionsService, List<IExtensionErrorHandler> errorHandlers)
            {
                _optionsService = optionsService;
                _errorHandlers = errorHandlers;
            }

            public override void HandleException(object provider, Exception exception)
            {
                if (_optionsService.GetOption(ExtensionManagerOptions.DisableCrashingExtensions))
                {
                    base.HandleException(provider, exception);
                }

                _errorHandlers.Do(h => h.HandleError(provider, exception));
            }
        }
    }
}

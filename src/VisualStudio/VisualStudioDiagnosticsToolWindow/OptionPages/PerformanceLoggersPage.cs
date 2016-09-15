// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    [Guid(Guids.RoslynOptionPagePerformanceLoggersIdString)]
    internal class PerformanceLoggersPage : AbstractOptionPage
    {
        private IOptionService _optionService;

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            if (_optionService == null)
            {
                var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                _optionService = workspace.Services.GetService<IOptionService>();
            }

            return new InternalOptionsControl(nameof(LoggerOptions), serviceProvider);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (_optionService.GetOption(LoggerOptions.EtwLoggerKey))
            {
                Logger.SetLogger(AggregateLogger.AddOrReplace(new EtwLogger(_optionService), Logger.GetLogger(), l => l is EtwLogger));
            }

            if (_optionService.GetOption(LoggerOptions.TraceLoggerKey))
            {
                Logger.SetLogger(AggregateLogger.AddOrReplace(new TraceLogger(_optionService), Logger.GetLogger(), l => l is TraceLogger));
            }
        }
    }
}

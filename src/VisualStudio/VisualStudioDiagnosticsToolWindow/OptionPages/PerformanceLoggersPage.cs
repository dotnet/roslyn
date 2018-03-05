// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.DiagnosticsWindow.OptionsPages
{
    [Guid(Guids.RoslynOptionPagePerformanceLoggersIdString)]
    internal class PerformanceLoggersPage : AbstractOptionPage
    {
        private IGlobalOptionService _optionService;
        private IRemoteHostClientService _remoteHostClientService;

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            if (_optionService == null)
            {
                var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
                _optionService = componentModel.GetService<IGlobalOptionService>();

                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                _remoteHostClientService = workspace.Services.GetService<IRemoteHostClientService>();
            }

            return new InternalOptionsControl(nameof(LoggerOptions), serviceProvider);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            var loggerTypes = GetLoggerTypes().ToList();

            // first set VS options
            var options = Logger.GetLoggingChecker(_optionService);
            SetRoslynLogger(loggerTypes, () => new EtwLogger(options));
            SetRoslynLogger(loggerTypes, () => new TraceLogger(options));

            // second set RemoteHost options
            var client = _remoteHostClientService.TryGetRemoteHostClientAsync(CancellationToken.None).Result;
            if (client == null)
            {
                // Remote host is disabled
                return;
            }

            var functionIds = GetFunctionIds(options).ToList();
            var unused = client.TryRunRemoteAsync(
                WellKnownRemoteHostServices.RemoteHostService,
                nameof(IRemoteHostService.SetLoggingFunctionIds),
                new object[] { loggerTypes, functionIds },
                CancellationToken.None).Result;
        }

        private static IEnumerable<string> GetFunctionIds(Func<FunctionId, bool> options)
        {
            foreach (var functionId in Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>())
            {
                if (options(functionId))
                {
                    yield return functionId.ToString();
                }
            }
        }

        private IEnumerable<string> GetLoggerTypes()
        {
            if (_optionService.GetOption(LoggerOptions.EtwLoggerKey))
            {
                yield return nameof(EtwLogger);
            }

            if (_optionService.GetOption(LoggerOptions.TraceLoggerKey))
            {
                yield return nameof(TraceLogger);
            }
        }

        private static void SetRoslynLogger<T>(List<string> loggerTypes, Func<T> creator) where T : ILogger
        {
            if (loggerTypes.Contains(typeof(T).Name))
            {
                Logger.SetLogger(AggregateLogger.AddOrReplace(creator(), Logger.GetLogger(), l => l is T));
            }
            else
            {
                Logger.SetLogger(AggregateLogger.Remove(Logger.GetLogger(), l => l is T));
            }
        }
    }
}

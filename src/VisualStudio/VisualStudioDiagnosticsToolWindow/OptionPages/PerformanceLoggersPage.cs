// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private IThreadingContext _threadingContext;
        private IRemoteHostClientService _remoteService;

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            if (_optionService == null)
            {
                var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

                _optionService = componentModel.GetService<IGlobalOptionService>();
                _threadingContext = componentModel.GetService<IThreadingContext>();

                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                _remoteService = workspace.Services.GetService<IRemoteHostClientService>();
            }

            return new InternalOptionsControl(nameof(LoggerOptions), optionStore);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            SetLoggers(_optionService, _threadingContext, _remoteService);
        }

        public static void SetLoggers(IGlobalOptionService optionService, IThreadingContext threadingContext, IRemoteHostClientService remoteService)
        {
            var loggerTypes = GetLoggerTypes(optionService).ToList();

            // first set VS options
            var options = Logger.GetLoggingChecker(optionService);

            SetRoslynLogger(loggerTypes, () => new EtwLogger(options));
            SetRoslynLogger(loggerTypes, () => new TraceLogger(options));
            SetRoslynLogger(loggerTypes, () => new OutputWindowLogger(options));

            // second set RemoteHost options
            var client = threadingContext.JoinableTaskFactory.Run(() => remoteService.TryGetRemoteHostClientAsync(CancellationToken.None));
            if (client == null)
            {
                // Remote host is disabled
                return;
            }

            var functionIds = GetFunctionIds(options).ToList();

            _ = threadingContext.JoinableTaskFactory.Run(() => client.TryRunRemoteAsync(
                WellKnownRemoteHostServices.RemoteHostService,
                nameof(IRemoteHostService.SetLoggingFunctionIds),
                new object[] { loggerTypes, functionIds },
                CancellationToken.None));
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

        private static IEnumerable<string> GetLoggerTypes(IGlobalOptionService optionService)
        {
            if (optionService.GetOption(LoggerOptions.EtwLoggerKey))
            {
                yield return nameof(EtwLogger);
            }

            if (optionService.GetOption(LoggerOptions.TraceLoggerKey))
            {
                yield return nameof(TraceLogger);
            }

            if (optionService.GetOption(LoggerOptions.OutputWindowLoggerKey))
            {
                yield return nameof(OutputWindowLogger);
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

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
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
        private HostWorkspaceServices _workspaceServices;

        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
        {
            if (_optionService == null)
            {
                var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

                _optionService = componentModel.GetService<IGlobalOptionService>();
                _threadingContext = componentModel.GetService<IThreadingContext>();

                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                _workspaceServices = workspace.Services;
            }

            return new InternalOptionsControl(nameof(LoggerOptions), optionStore);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            SetLoggers(_optionService, _threadingContext, _workspaceServices);
        }

        public static void SetLoggers(IGlobalOptionService optionService, IThreadingContext threadingContext, HostWorkspaceServices workspaceServices)
        {
            var loggerTypeNames = GetLoggerTypes(optionService).ToImmutableArray();

            // update loggers in VS
            var isEnabled = Logger.GetLoggingChecker(optionService);

            SetRoslynLogger(loggerTypeNames, () => new EtwLogger(isEnabled));
            SetRoslynLogger(loggerTypeNames, () => new TraceLogger(isEnabled));
            SetRoslynLogger(loggerTypeNames, () => new OutputWindowLogger(isEnabled));

            // update loggers in remote process
            var client = threadingContext.JoinableTaskFactory.Run(() => RemoteHostClient.TryGetClientAsync(workspaceServices, CancellationToken.None));
            if (client != null)
            {
                var functionIds = Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>().Where(isEnabled).ToImmutableArray();

                threadingContext.JoinableTaskFactory.Run(async () => _ = await client.TryInvokeAsync<IRemoteProcessTelemetryService>(
                    (service, cancellationToken) => service.EnableLoggingAsync(loggerTypeNames, functionIds, cancellationToken),
                    CancellationToken.None).ConfigureAwait(false));
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

        private static void SetRoslynLogger<T>(ImmutableArray<string> loggerTypeNames, Func<T> creator) where T : ILogger
        {
            if (loggerTypeNames.Contains(typeof(T).Name))
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

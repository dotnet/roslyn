// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal class OptionsLogger : IDisposable
    {
        private readonly SolutionEventMonitor _solutionEventMonitor;
        private readonly Workspace _workspace;

        public OptionsLogger(SolutionEventMonitor solutionEventMonitor, Workspace workspace)
        {
            _solutionEventMonitor = solutionEventMonitor;
            _workspace = workspace;
            _solutionEventMonitor.SolutionClosed += OnSolutionClosed;
        }

        private void OnSolutionClosed(object sender, EventArgs e)
        {
            ReportTelemetry(_workspace, CancellationToken.None);
        }

        public void Dispose()
        {
            _solutionEventMonitor.SolutionClosed -= OnSolutionClosed;
        }

        internal static void ReportTelemetry(Workspace workspace, CancellationToken cancellationToken)
        {
            var optionsService = workspace.Services.GetRequiredService<IOptionService>();
            var options = optionsService.GetRegisteredOptions();

            var message = KeyValueLogMessage.Create(
                LogType.Trace,
                kvp =>
                {
                    var perLanguageOptions = options.Where(o => o.IsPerLanguage).Cast<IPerLanguageOption>();
                    var languageIndependentOptions = options.Where(o => !o.IsPerLanguage);

                    foreach (var option in languageIndependentOptions)
                    {
                        kvp[GetKey(option)] = GetValue(optionsService.GetOption(new OptionKey(option)));
                    }

                    // Should we cover more languages here?
                    foreach (var language in new[] { LanguageNames.CSharp, LanguageNames.VisualBasic })
                    {
                        foreach (var option in perLanguageOptions)
                        {
                            kvp[GetKey(language, option)] = GetValue(optionsService.GetOption(new OptionKey(option, language)));
                        }
                    }
                },
                LogLevel.Information);

            Logger.Log(FunctionId.OptionsLogger_LogOptions, message);
        }
        private static string GetKey(IOption option)
        {
            Contract.ThrowIfTrue(option.IsPerLanguage);
            return $"{option.Name}_{option.Feature}";
        }

        private static string GetKey(string language, IOption option)
        {
            Contract.ThrowIfFalse(option.IsPerLanguage);
            return $"{language}_{option.Name}_{option.Feature}";
        }

        private static string? GetValue(object? option)
            => option switch
            {
                ICodeStyleOption codeStyleOption => codeStyleOption.ToXElement().ToString(),
                _ => option?.ToString()
            };
    }
}

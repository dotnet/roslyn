// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal abstract class AbstractWorkspaceTelemetryService : IWorkspaceTelemetryService
    {
        public TelemetrySession? CurrentSession { get; private set; }

        protected abstract ILogger CreateLogger(TelemetrySession telemetrySession);

        public void InitializeTelemetrySession(TelemetrySession telemetrySession)
        {
            Contract.ThrowIfFalse(CurrentSession is null);

            Logger.SetLogger(CreateLogger(telemetrySession));
            WatsonReporter.RegisterTelemetrySesssion(telemetrySession);

            CurrentSession = telemetrySession;

            TelemetrySessionInitialized();
        }

        protected virtual void TelemetrySessionInitialized()
        {
        }

        public bool HasActiveSession
            => CurrentSession != null && CurrentSession.IsOptedIn;

        public string? SerializeCurrentSessionSettings()
            => CurrentSession?.SerializeSettings();

        public void RegisterUnexpectedExceptionLogger(TraceSource logger)
            => WatsonReporter.RegisterLogger(logger);

        public void UnregisterUnexpectedExceptionLogger(TraceSource logger)
            => WatsonReporter.UnregisterLogger(logger);

        public void ReportApiUsage(HashSet<ISymbol> symbols, Guid solutionSessionId, Guid projectGuid)
        {
            const string EventName = "vs/compilers/api";
            const string ApiPropertyName = "vs.compilers.api.pii";
            const string ProjectIdPropertyName = "vs.solution.project.projectid";
            const string SessionIdPropertyName = "vs.solution.solutionsessionid";

            var groupByAssembly = symbols.GroupBy(symbol => symbol.ContainingAssembly);

            var apiPerAssembly = groupByAssembly.Select(assemblyGroup => new
            {
                // mark all string as PII (customer data)
                AssemblyName = new TelemetryPiiProperty(assemblyGroup.Key.Identity.Name),
                AssemblyVersion = assemblyGroup.Key.Identity.Version.ToString(),
                Namespaces = assemblyGroup.GroupBy(symbol => symbol.ContainingNamespace)
                    .Select(namespaceGroup =>
                    {
                        var namespaceName = namespaceGroup.Key?.ToString() ?? string.Empty;

                        return new
                        {
                            Namespace = new TelemetryPiiProperty(namespaceName),
                            Symbols = namespaceGroup.Select(symbol => symbol.GetDocumentationCommentId())
                                .Where(id => id != null)
                                .Select(id => new TelemetryPiiProperty(id))
                        };
                    })
            });

            // use telemetry API directly rather than Logger abstraction for PII data
            var telemetryEvent = new TelemetryEvent(EventName);
            telemetryEvent.Properties[ApiPropertyName] = new TelemetryComplexProperty(apiPerAssembly);
            telemetryEvent.Properties[SessionIdPropertyName] = new TelemetryPiiProperty(solutionSessionId.ToString("B"));
            telemetryEvent.Properties[ProjectIdPropertyName] = new TelemetryPiiProperty(projectGuid.ToString("B"));

            try
            {
                CurrentSession?.PostEvent(telemetryEvent);
            }
            catch
            {
                // no-op
            }
        }
    }
}

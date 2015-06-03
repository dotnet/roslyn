// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Telemetry
{
    [ExportWorkspaceServiceFactory(typeof(ITelemetryAggregationCoordinator), ServiceLayer.Host), Shared]
    internal class TelemetryAggregationCoordinatorFactory : IWorkspaceServiceFactory
    {
        public TelemetryAggregationCoordinatorFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new TelemetryAggregationCoordinator(workspaceServices);
        }

        internal class TelemetryAggregationCoordinator : ITelemetryAggregationCoordinator
        {
            private readonly IList<IAggregatedTelemetryLogger> loggers = new List<IAggregatedTelemetryLogger>();

            public TelemetryAggregationCoordinator(HostWorkspaceServices workspaceServices)
            {
                workspaceServices.Workspace.WorkspaceChanged += OnWorkspaceChanged;
            }

            public void RegisterSolutionClosedLogger(IAggregatedTelemetryLogger logger)
            {
                loggers.Add(logger);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                if (args.Kind == WorkspaceChangeKind.SolutionRemoved)
                {
                    foreach (var logger in loggers)
                    {
                        logger.Log();
                    }
                }
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Shared]
    [Export(typeof(TestExperimentationService))]
    [ExportWorkspaceService(typeof(IExperimentationServiceFactory), WorkspaceKind.Test)]
    [PartNotDiscoverable]
    internal sealed class TestExperimentationService : IExperimentationServiceFactory, IExperimentationService
    {
        private Dictionary<string, bool> _experimentsOptionValues = new Dictionary<string, bool>();

        [ImportingConstructor]
        public TestExperimentationService()
        {
        }

        public void SetExperimentOption(string experimentName, bool enabled)
        {
            _experimentsOptionValues[experimentName] = enabled;
        }

        public ValueTask<IExperimentationService> GetExperimentationServiceAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<IExperimentationService>(this);
        }

        public bool IsExperimentEnabled(string experimentName)
        {
            return _experimentsOptionValues.TryGetValue(experimentName, out var enabled) && enabled;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Shared]
    [Export(typeof(TestExperimentationService))]
    [ExportWorkspaceService(typeof(IExperimentationService), WorkspaceKind.Test), PartNotDiscoverable]
    internal sealed class TestExperimentationService : IExperimentationService
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

        public bool IsExperimentEnabled(string experimentName)
        {
            return _experimentsOptionValues.TryGetValue(experimentName, out var enabled) && enabled;
        }
    }
}

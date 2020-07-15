// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private readonly Dictionary<string, bool> _experimentsOptionValues = new Dictionary<string, bool>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestExperimentationService()
        {
        }

        public void SetExperimentOption(string experimentName, bool enabled)
            => _experimentsOptionValues[experimentName] = enabled;

        public bool IsExperimentEnabled(string experimentName)
            => _experimentsOptionValues.TryGetValue(experimentName, out var enabled) && enabled;
    }
}

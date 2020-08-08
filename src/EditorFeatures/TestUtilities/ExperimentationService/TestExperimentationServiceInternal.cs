// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.ExperimentationService
{
    [Export(typeof(IExperimentationServiceInternal)), Shared]
    [PartNotDiscoverable]
    internal class TestExperimentationServiceInternal : IExperimentationServiceInternal
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestExperimentationServiceInternal()
        {
        }

        public bool IsCachedFlightEnabled(string flightName)
            => true;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return true;
        }
    }
}

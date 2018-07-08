// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.ExperimentationService
{
    [Export(typeof(IExperimentationServiceInternal)), Shared]
    internal class TestExperimentationServiceInternal : IExperimentationServiceInternal
    {
        public bool IsCachedFlightEnabled(string flightName)
        {
            return true;
        }
    }
}

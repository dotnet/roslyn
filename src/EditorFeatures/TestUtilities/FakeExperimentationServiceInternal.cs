// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(IExperimentationServiceInternal))]
    [Shared]
    [PartNotDiscoverable]
    internal sealed class FakeExperimentationServiceInternal : IExperimentationServiceInternal
    {
        public bool IsCachedFlightEnabled(string flightName)
        {
            throw new NotImplementedException();
        }
    }
}

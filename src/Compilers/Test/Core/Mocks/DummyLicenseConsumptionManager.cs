// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Caravela />

using System;
using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;

namespace Microsoft.CodeAnalysis.Test.Utilities.Mocks
{
    public class DummyLicenseConsumptionManager : ILicenseConsumptionManager
    {
        public bool CanConsumeFeatures(ILicenseConsumer consumer, LicensedFeatures requiredFeatures)
        {
            return true;
        }

        public void ConsumeFeatures(ILicenseConsumer consumer, LicensedFeatures requiredFeatures)
        {
            if (!this.CanConsumeFeatures(consumer, requiredFeatures))
            {
                throw new InvalidOperationException("This class is not expected to refuse license consumption.");
            }
        }
    }
}

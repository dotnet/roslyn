// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;

namespace Caravela.Compiler.Licensing
{
    internal class TestLicenseConsumptionManager : ILicenseConsumptionManager
    {
        public bool CanConsumeFeatures(ILicenseConsumer consumer, LicensedFeatures requiredFeatures)
        {
            return true;
        }

        public void ConsumeFeatures(ILicenseConsumer consumer, LicensedFeatures requiredFeatures)
        {
        }
    }
}

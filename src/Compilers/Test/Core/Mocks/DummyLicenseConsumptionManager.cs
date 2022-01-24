// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Metalama />

using System;
using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;

namespace Microsoft.CodeAnalysis.Test.Utilities.Mocks
{
    public class DummyLicenseConsumptionManager : ILicenseConsumptionManager
    {
        public bool CanConsumeFeatures(LicensedFeatures requiredFeatures, string? consumingNamespace, Action<LicensingMessage> reportMessage )
        {
            return true;
        }

    }
}

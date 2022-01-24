// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using PostSharp.Backstage.Diagnostics;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Licensing.Consumption;
using PostSharp.Backstage.Licensing.Registration;

namespace Metalama.Compiler.Licensing
{
    internal class TimeBombLicenseActivator : IFirstRunLicenseActivator
    {
        internal const int PreviewLicensePeriod = 90;
        internal const int WarningPeriod = 15;
        
        private readonly IDateTimeProvider _time;
        private readonly IApplicationInfo _applicationInfo;
        
        private bool _warningReported;

        public TimeBombLicenseActivator(IServiceProvider services)
        {
            _time = services.GetRequiredService<IDateTimeProvider>();
            _applicationInfo = services.GetRequiredService<IApplicationInfo>();
        }

        public bool TryActivateLicense(Action<LicensingMessage> reportMessage)
        {
            var age = (int) (_time.Now - _applicationInfo.BuildDate).TotalDays;

            if (age > PreviewLicensePeriod)
            {
                return false;
            }

            if (!_warningReported && age > (PreviewLicensePeriod - WarningPeriod))
            {
                reportMessage( new(
                    $"The current preview build of Metalama is {age} days old and will stop working soon, because it is allowed to be used only for {PreviewLicensePeriod} days. Please update Metalama soon.") );
                _warningReported = true;
            }
            
            return true;
        }
    }
}

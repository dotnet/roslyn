// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Metalama.Compiler.Licensing;
using PostSharp.Backstage.Diagnostics;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Licensing.Consumption;
using Xunit;

namespace Metalama.Compiler.UnitTests
{
    public class TimeBombTests
    {
        private readonly TestDateTimeProvider _time = new();
        private readonly TestApplicationInfo _applicationInfo = new();
        private readonly TimeBombLicenseActivator _timeBomb;

        public TimeBombTests()
        {
            var services = new ServiceCollection();

            var serviceProviderBuilder = new ServiceProviderBuilder(
                (type, instance) => services.AddService(type, instance),
                () => services.GetServiceProvider());

            serviceProviderBuilder
                .AddSingleton<IDateTimeProvider>(_time)
                .AddSingleton<IApplicationInfo>(_applicationInfo);
            _timeBomb = new TimeBombLicenseActivator(serviceProviderBuilder.ServiceProvider);
        }

        private void SetAge(int daysAfterBuild)
        {
            _time.Set(_applicationInfo.BuildDate.AddDays(daysAfterBuild));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod - TimeBombLicenseActivator.WarningPeriod)]
        public void TimeBombIsSafeBeforeWarningPeriod(int daysAfterBuild)
        {
            SetAge(daysAfterBuild);
            var messages = new List<LicensingMessage>();
            Assert.True(_timeBomb.TryActivateLicense(messages.Add));
            Assert.Empty(messages);
        }

        [Theory]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod - TimeBombLicenseActivator.WarningPeriod + 1)]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod)]
        public void TimeBombWarnsDuringWarningPeriod(int daysAfterBuild)
        {
            var messages = new List<LicensingMessage>();
            
            SetAge(daysAfterBuild);
            Assert.True(_timeBomb.TryActivateLicense( messages.Add ));
            Assert.Single(messages);
        }

        [Theory]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod + 1)]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod + 100)]
        public void TimeBombExplodesAfterPreviewLicensePeriod(int daysAfterBuild)
        {
            var messages = new List<LicensingMessage>();
            SetAge(daysAfterBuild);
            Assert.False(_timeBomb.TryActivateLicense( messages.Add ));
            Assert.Empty(messages);
        }

        private class TestDateTimeProvider : IDateTimeProvider
        {
            public DateTime Now { get; private set; }

            public void Set(DateTime now)
            {
                Now = now;
            }
        }
        
        private class TestApplicationInfo : IApplicationInfo
        {
            public string Name => throw new NotImplementedException();
            public string Version => throw new NotImplementedException();
            public bool IsPrerelease => throw new NotImplementedException();
            public DateTime BuildDate => new DateTime(2000, 1, 1);
            public ProcessKind ProcessKind => ProcessKind.Other;
            public bool IsLongRunningProcess => false;
        }
    }
}

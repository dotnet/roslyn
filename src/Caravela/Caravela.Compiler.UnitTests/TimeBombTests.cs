// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Caravela.Compiler.Licensing;
using Caravela.Compiler.UnitTests.Utilities;
using PostSharp.Backstage.Extensibility;
using Xunit;

namespace Caravela.Compiler.UnitTests
{
    public class TimeBombTests
    {
        private readonly TestDateTimeProvider _time = new();
        private readonly TestApplicationInfo _applicationInfo = new();
        private readonly TestDiagnosticsSink _diagnostics = new();
        private readonly TimeBombLicenseActivator _timeBomb;

        public TimeBombTests()
        {
            var services = new ServiceCollection();

            var serviceProviderBuilder = new ServiceProviderBuilder(
                (type, instance) => services.AddService(type, instance),
                () => services.GetServiceProvider());

            serviceProviderBuilder
                .AddSingleton<IDateTimeProvider>(_time)
                .AddSingleton<IApplicationInfo>(_applicationInfo)
                .AddSingleton<IBackstageDiagnosticSink>(_diagnostics);
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
            Assert.True(_timeBomb.TryRegisterLicense());
            _diagnostics.AssertEmpty();
        }

        [Theory]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod - TimeBombLicenseActivator.WarningPeriod + 1)]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod)]
        public void TimeBombWarnsDuringWarningPeriod(int daysAfterBuild)
        {
            SetAge(daysAfterBuild);
            Assert.True(_timeBomb.TryRegisterLicense());
            _diagnostics.AssertEmptyErrors();
            Assert.Single(_diagnostics.GetWarnings(), $"The current preview build of Caravela is {daysAfterBuild} days old and will stop working soon, because it is allowed to be used only for {TimeBombLicenseActivator.PreviewLicensePeriod} days. Please update Caravela soon.");
        }

        [Theory]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod + 1)]
        [InlineData(TimeBombLicenseActivator.PreviewLicensePeriod + 100)]
        public void TimeBombExplodesAfterPreviewLicensePeriod(int daysAfterBuild)
        {
            SetAge(daysAfterBuild);
            Assert.False(_timeBomb.TryRegisterLicense());
            _diagnostics.AssertEmpty();
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
            public Version Version => throw new NotImplementedException();
            public bool IsPrerelease => throw new NotImplementedException();
            public DateTime BuildDate => new DateTime(2000, 1, 1);
        }
    }
}

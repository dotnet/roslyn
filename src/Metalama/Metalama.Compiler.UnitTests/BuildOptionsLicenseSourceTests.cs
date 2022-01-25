// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Metalama.Compiler.Licensing;
using Microsoft.CodeAnalysis.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Xunit;

namespace Metalama.Compiler.UnitTests
{
    public class BuildOptionsLicenseSourceTests
    {

        [Fact]
        public void LicensesAreRetrievedFromBuildOptions()
        {
            var services = new ServiceCollection();

            var serviceProviderBuilder = new ServiceProviderBuilder(
                (type, instance) => services.AddService(type, instance),
                () => services);
            
            serviceProviderBuilder
                .AddMinimalBackstageServices();

            var configuration = new Dictionary<string, string>
            {
                { "build_property.MetalamaLicense", "SomeLicense1,SomeLicense2" }
            };

            var globalOptions = new CompilerAnalyzerConfigOptions(configuration.ToImmutableDictionary());
            var analyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, globalOptions);

            BuildOptionsLicenseSource source = new(analyzerConfigOptionsProvider, serviceProviderBuilder.ServiceProvider);
            var messages = new List<LicensingMessage>();
            Assert.Collection(source.GetLicenses(messages.Add),
                l => Assert.Equal("License 'SOMELICENSE1'", l.ToString()),
                l => Assert.Equal("License 'SOMELICENSE2'", l.ToString()));
            Assert.Empty(messages);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Caravela.Compiler.Licensing;
using Caravela.Compiler.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Extensibility;
using Xunit;

namespace Caravela.Compiler.UnitTests
{
    public class BuildOptionsLicenseSourceTests
    {
        private readonly TestDiagnosticsSink _diagnostics = new();

        [Fact]
        public void LicensesAreRetrievedFromBuildOptions()
        {
            var services = new BackstageServiceCollection()
                .AddSingleton<IBackstageDiagnosticSink>(_diagnostics)
                .AddCurrentDateTimeProvider();

            var configuration = new Dictionary<string, string>
            {
                { "build_property.CaravelaLicense", "SomeLicense1,SomeLicense2" }
            };

            var globalOptions = new CompilerAnalyzerConfigOptions(configuration.ToImmutableDictionary());
            var analyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, globalOptions);

            BuildOptionsLicenseSource source = new(analyzerConfigOptionsProvider, services.ToServiceProvider());
            Assert.Collection(source.GetLicenses(),
                l => Assert.Equal("License 'SOMELICENSE1'", l.ToString()),
                l => Assert.Equal("License 'SOMELICENSE2'", l.ToString()));
            _diagnostics.AssertEmpty();
        }
    }
}

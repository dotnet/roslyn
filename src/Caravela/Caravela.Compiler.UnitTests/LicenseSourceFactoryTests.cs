// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Caravela.Compiler.Licensing;
using Caravela.Compiler.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Licensing.Consumption.Sources;
using PostSharp.Backstage.Licensing.Registration;
using Xunit;

namespace Caravela.Compiler.UnitTests
{
    public class LicenseSourceFactoryTests
    {
        private readonly IServiceProvider _services;
        private readonly TestDiagnosticsSink _diagnostics = new();

        public LicenseSourceFactoryTests()
        {
            var services = new ServiceCollection();

            var serviceProviderBuilder = new ServiceProviderBuilder(
                (type, instance) => services.AddService(type, instance),
                () => services.GetServiceProvider());

            serviceProviderBuilder
                .AddSingleton<IBackstageDiagnosticSink>(_diagnostics)
                .AddStandardDirectories()
                .AddStandardLicenseFilesLocations();
            _services = serviceProviderBuilder.ServiceProvider;
        }

        [Fact]
        public void UnknownSourceIsReported()
        {
            var factory = CreateFactory("SomeUnknown");
            var sources = factory.Create();

            Assert.Empty(sources);
            _diagnostics.AssertEmptyWarnings();
            Assert.Single(_diagnostics.GetErrors(),
                "Unknown license source 'someunknown' configured in the CaravelaLicenseSources property.");
        }

        [Fact]
        public void UserFileLicenseSourceCanBeCreated()
        {
            var factory = CreateFactory("User");
            var sources = factory.Create();

            Assert.Single(sources, s => s is FileLicenseSource);
            _diagnostics.AssertEmpty();
        }

        [Fact]
        public void BuildOptionsLicenseSourceCanBeCreated()
        {
            var factory = CreateFactory("Property");
            var sources = factory.Create();

            Assert.Single(sources, s => s is BuildOptionsLicenseSource);
            _diagnostics.AssertEmpty();
        }

        [Fact]
        public void MultipleDistinctSourcesCanBeCreated()
        {
            var factory = CreateFactory("User,Property");
            var sources = factory.Create();

            Assert.Collection(sources,
                source => Assert.True(source is FileLicenseSource),
                source => Assert.True(source is BuildOptionsLicenseSource));
            _diagnostics.AssertEmpty();
        }

        [Fact]
        public void MultipleEqualSourceNamesAreIgnored()
        {
            var factory = CreateFactory("User,user,USER");
            var sources = factory.Create();

            Assert.Single(sources, s => s is FileLicenseSource);
            _diagnostics.AssertEmpty();
        }

        private LicenseSourceFactory CreateFactory(string sourceNames)
        {
            var configuration = new Dictionary<string, string>
            {
                { "build_property.CaravelaLicenseSources", sourceNames }
            };

            var globalOptions = new CompilerAnalyzerConfigOptions(configuration.ToImmutableDictionary());
            var analyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(
                    ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, globalOptions);

            return new(analyzerConfigOptionsProvider, _services);
        }
    }
}

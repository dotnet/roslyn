// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Extensibility.Extensions;
using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;
using PostSharp.Backstage.Licensing.Consumption.Sources;
using PostSharp.Backstage.Licensing.Registration;

namespace Caravela.Compiler.Licensing
{
    // TODO: Make reusable for compiler server. (The diagnostics sink cannot be reused at the moment.)
    internal class CaravelaCompilerSimpleLicenseManager
    {
        private readonly ILicenseConsumptionManager _consumptionManager;
        private readonly LicensingDiagnosticsSink _diagnosticsSink;

        public CaravelaCompilerSimpleLicenseManager()
        {
            _diagnosticsSink = new();
            
            var services = new DefaultServiceProvider();
            services.AddService<IDiagnosticsSink>(_diagnosticsSink);
            services.AddService<IApplicationInfo>(new CaravelaCompilerApplicationInfo());
            var standardLicenseFileLocations = services.GetRequiredService<IStandardLicenseFileLocations>();
            var licenseSources = new ILicenseSource[]
            {
                new FileLicenseSource(standardLicenseFileLocations.UserLicenseFile, services),
                new BuildOptionsLicenseSource()
            };
            _consumptionManager = new LicenseConsumptionManager(services, licenseSources);
        }

        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            var consumer = new CaravelaCompilerLicenseConsumer(_diagnosticsSink);
            _consumptionManager.ConsumeFeatures(consumer, LicensedFeatures.Caravela);
            return _diagnosticsSink.GetDiagnostics();
        }
    }
}

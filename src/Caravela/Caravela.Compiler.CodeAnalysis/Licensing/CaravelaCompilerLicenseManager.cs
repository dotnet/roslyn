// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Extensibility.Extensions;
using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;

namespace Caravela.Compiler.Licensing
{
    internal class CaravelaCompilerLicenseConsumptionManager
    {
        private readonly ILicenseConsumptionManager _consumptionManager;
        private readonly CaravelaCompilerLicenseConsumer _consumer;

        public CaravelaCompilerLicenseConsumptionManager( IServiceProvider services )
        {
            _consumptionManager = services.GetRequiredService<ILicenseConsumptionManager>();

            var diagnosticsSink = services.GetRequiredService<IBackstageDiagnosticSink>();
            _consumer = new CaravelaCompilerLicenseConsumer(diagnosticsSink);
        }

        public void ConsumeFeatures(LicensedFeatures features)
        {
            _consumptionManager.ConsumeFeatures(_consumer, features);
        }
    }
}

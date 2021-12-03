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
    /// <summary>
    /// An adapter for the <see cref="ILicenseConsumptionManager"/>
    /// allowing consumption of <see cref="LicensedFeatures"/>
    /// by the <see cref="CaravelaCompilerLicenseConsumer"/>.
    /// </summary>
    internal class CaravelaCompilerLicenseConsumptionManager
    {
        private readonly ILicenseConsumptionManager _consumptionManager;
        private readonly CaravelaCompilerLicenseConsumer _consumer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaravelaCompilerLicenseConsumptionManager"/> class.
        /// </summary>
        /// <param name="services">Service provider.</param>
        public CaravelaCompilerLicenseConsumptionManager(IServiceProvider services)
        {
            _consumptionManager = services.GetRequiredService<ILicenseConsumptionManager>();

            var diagnosticsSink = services.GetRequiredService<IBackstageDiagnosticSink>();
            _consumer = new CaravelaCompilerLicenseConsumer(diagnosticsSink);
        }

        /// <summary>
        /// Consumes <see cref="LicensedFeatures"/>
        /// by the <see cref="CaravelaCompilerLicenseConsumer"/>.
        /// </summary>
        /// <param name="features">The licensed features to be consumed.</param>
        public void ConsumeFeatures(LicensedFeatures features)
        {
            _consumptionManager.ConsumeFeatures(_consumer, features);
        }
    }
}

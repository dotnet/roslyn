// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Extensibility.Extensions;
using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;

namespace Metalama.Compiler.Licensing
{
    /// <summary>
    /// An adapter for the <see cref="ILicenseConsumptionManager"/>
    /// allowing consumption of <see cref="LicensedFeatures"/>
    /// by the <see cref="MetalamaCompilerLicenseConsumer"/>.
    /// </summary>
    internal class MetalamaCompilerLicenseConsumptionManager
    {
        private readonly ILicenseConsumptionManager _consumptionManager;
        private readonly MetalamaCompilerLicenseConsumer _consumer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetalamaCompilerLicenseConsumptionManager"/> class.
        /// </summary>
        /// <param name="services">Service provider.</param>
        public MetalamaCompilerLicenseConsumptionManager(IServiceProvider services)
        {
            _consumptionManager = services.GetRequiredService<ILicenseConsumptionManager>();

            var diagnosticsSink = services.GetRequiredService<IBackstageDiagnosticSink>();
            _consumer = new MetalamaCompilerLicenseConsumer(diagnosticsSink);
        }

        /// <summary>
        /// Consumes <see cref="LicensedFeatures"/>
        /// by the <see cref="MetalamaCompilerLicenseConsumer"/>.
        /// </summary>
        /// <param name="features">The licensed features to be consumed.</param>
        public void ConsumeFeatures(LicensedFeatures features)
        {
            _consumptionManager.ConsumeFeatures(_consumer, features);
        }
    }
}

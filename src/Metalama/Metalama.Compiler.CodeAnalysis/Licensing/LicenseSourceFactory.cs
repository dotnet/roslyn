// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Extensibility.Extensions;
using PostSharp.Backstage.Licensing.Consumption.Sources;

namespace Metalama.Compiler.Licensing
{
    /// <summary>
    /// Factory class creating license sources enabled using
    /// the MetalamaLicenseSources MSBuild property / global analyzer option.
    /// </summary>
    internal class LicenseSourceFactory
    {
        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseSourceFactory"/> class.
        /// </summary>
        /// <param name="analyzerConfigOptionsProvider">Data source.</param>
        /// <param name="services">Service provider.</param>
        public LicenseSourceFactory(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, IServiceProvider services)
        {
            _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
            _services = services;
        }

        /// <summary>
        /// Creates the license sources enabled using
        /// the MetalamaLicenseSources MSBuild property / global analyzer option. 
        /// </summary>
        /// <returns><see cref="IEnumerable{T}"/> creating the license sources.</returns>
        /// <exception cref="InvalidOperationException">The MetalamaLicenseSources global analyzer option is missing.</exception>
        public IEnumerable<ILicenseSource> Create()
        {
            // TODO: trace

            // See src\Metalama\doc\Properties.md.
            if (!_analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                "build_property.MetalamaLicenseSources",
                out var sourcesConfig))
            {
                throw new InvalidOperationException(
                    "MetalamaLicenseSources property is required.");
            }

            var sourceNames = sourcesConfig.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sourceName in sourceNames.Select(n => n.Trim().ToLowerInvariant()).Distinct())
            {
                switch (sourceName.ToLowerInvariant())
                {
                    case "user":
                        yield return FileLicenseSource.CreateUserLicenseFileLicenseSource(_services);
                        break;

                    case "property":
                        yield return new BuildOptionsLicenseSource(_analyzerConfigOptionsProvider, _services);
                        break;

                    default:
                        var diagnostics = _services.GetRequiredService<IBackstageDiagnosticSink>();
                        diagnostics.ReportError(
                            $"Unknown license source '{sourceName}' configured in the MetalamaLicenseSources property.");
                        break;
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Extensibility.Extensions;
using PostSharp.Backstage.Licensing.Consumption.Sources;

namespace Caravela.Compiler.Licensing
{
    internal class LicenseSourceFactory
    {
        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;
        private readonly IServiceProvider _services;

        public LicenseSourceFactory(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, IServiceProvider services)
        {
            _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
            _services = services;
        }

        public IEnumerable<ILicenseSource> Create()
        {
            // TODO: trace

            if (!_analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                "build_property.CaravelaLicenseSources",
                out var sourcesConfig))
            {
                yield break;
            }

            var sourceNames = sourcesConfig.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var sourceName in sourceNames.Select(n => n.Trim().ToLowerInvariant()).Distinct())
            {
                switch (sourceName.ToLowerInvariant())
                {
                    case "user":
                        yield return FileLicenseSource.CreateUserLicenseFileLicenseSource(_services);
                        break;

                    case "file":
                        if (TryCreateFileLicenseSource(out var fileLicenseSource))
                        {
                            yield return fileLicenseSource;
                        }
                        else
                        {
                            yield break;
                        }

                        break;

                    case "property":
                        yield return new BuildOptionsLicenseSource(_analyzerConfigOptionsProvider, _services);
                        break;

                    default:
                        var diagnostics = _services.GetRequiredService<IDiagnosticsSink>();
                        diagnostics.ReportError(
                            $"Unknown license source '{sourceName}' configured in the CaravelaLicenseSources property.");
                        break;
                }
            }
        }

        private bool TryCreateFileLicenseSource(
            [NotNullWhen(true)] out FileLicenseSource? fileLicenseSource)
        {
            if (!_analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                "build_property.CaravelaLicenseFile",
                out var path))
            {
                fileLicenseSource = null;
                return false;
            }

            fileLicenseSource = new FileLicenseSource(path, _services);
            return true;
        }
    }
}

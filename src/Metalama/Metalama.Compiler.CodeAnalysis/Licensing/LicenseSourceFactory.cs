// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Licensing.Consumption.Sources;

namespace Metalama.Compiler.Licensing
{
    internal static class LicenseSourceFactory
    {

      
        public static IEnumerable<ILicenseSource> CreateSources( IServiceProvider services, AnalyzerConfigOptionsProvider configuration )
        {
            // The MetalamaIgnoreUserLicenses property is used in tests only. It is not imported from MSBuild.
            if (!(configuration.GlobalOptions.TryGetValue("build_property.MetalamaIgnoreUserLicenses", out var value) && bool.TryParse(value, out var parsedValue) && parsedValue))
            {
                yield return new FileLicenseSource(services);
            }
            
            yield return new BuildOptionsLicenseSource(configuration, services);
        }
    }
}

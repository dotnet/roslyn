// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Licensing.Consumption.Sources;

namespace Caravela.Compiler.Licensing
{
    internal class BuildOptionsLicenseSource : LicenseStringsLicenseSourceBase
    {
        private readonly AnalyzerConfigOptionsProvider _analyzerConfigProvider;

        public BuildOptionsLicenseSource(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
            IServiceProvider services)
            : base(services)
        {
            _analyzerConfigProvider = analyzerConfigOptionsProvider;
        }

        protected override IEnumerable<string> GetLicenseStrings()
        {
            if (!_analyzerConfigProvider.GlobalOptions.TryGetValue("build_property.CaravelaLicense", out var value))
            {
                yield break;
            }

            var licenseKeys = value.Trim().Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var licenseKey in licenseKeys)
            {
                yield return licenseKey;
            }
        }
    }
}

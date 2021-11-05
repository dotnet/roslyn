// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using PostSharp.Backstage.Licensing.Consumption.Sources;
using PostSharp.Backstage.Licensing.Licenses;

namespace Caravela.Compiler.Licensing
{
    internal class BuildOptionsLicenseSource : ILicenseSource
    {
        public IEnumerable<ILicense> GetLicenses()
        {
            // TODO
            yield break;
        }
    }
}

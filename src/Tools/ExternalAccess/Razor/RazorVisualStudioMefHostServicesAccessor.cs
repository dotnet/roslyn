// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorVisualStudioMefHostServicesAccessor
    {
        public static HostServices Create(ExportProvider exportProvider)
            => VisualStudioMefHostServices.Create(exportProvider);
    }
}

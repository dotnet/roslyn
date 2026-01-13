// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Setup;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// A placeholder service that exists to trigger a package load of <see cref="RoslynPackage"/>. The only way to trigger an explicit load
    /// is via <see cref="Microsoft.VisualStudio.Shell.Interop.IVsShell.LoadPackage"/>, but calling that API requires a transition of the UI thread first.
    /// Requesting a service that is free-threaded instead can trigger the package load from the background thread in the first place.
    /// </summary>
    [Guid("89e9dfce-d0f3-48c4-be76-140d5cce69ef")]
    internal sealed class RoslynPackageLoadService
    {
    }
}

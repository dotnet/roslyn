// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    internal static class SolutionExtensions
    {
#if CODE_STYLE
        /// <summary>
        /// Remove this extension once Solution.Services is shipped and can be used by CODE_STYLE layer.
        /// </summary>
        public static HostWorkspaceServices GetServices(this Solution solution)
        {
            return solution.Workspace.Services;
        }
#else
        public static HostWorkspaceServices GetServices(this Solution solution)
            => solution.Services
#endif
    }
}

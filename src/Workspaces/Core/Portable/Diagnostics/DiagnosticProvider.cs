// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provide a way for users to turn on and off analyzing workspace for compiler diagnostics
    /// </summary>
    internal static class DiagnosticProvider
    {
        public static void Enable(Workspace workspace)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            service.Register(workspace);
        }

        public static void Disable(Workspace workspace)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            service.Unregister(workspace);
        }
    }
}

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
#pragma warning disable IDE0060 // Remove unused parameter
        public static void Enable(Workspace workspace, Options options)
        {
            // 'options' intentionally ignored, but kept around for binary compat.
            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            service.Register(workspace);
        }
#pragma warning restore IDE0060 // Remove unused parameter

        public static void Disable(Workspace workspace)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            service.Unregister(workspace);
        }

        [Flags]
        public enum Options
        {
            /// <summary>
            /// Include syntax errors
            /// </summary>
            Syntax = 0x01,

            /// <summary>
            /// Include semantic errors
            /// </summary>
            Semantic = 0x02,

            /// <summary>
            /// Include script semantic errors
            /// </summary>
            ScriptSemantic = 0x04,
        }
    }
}

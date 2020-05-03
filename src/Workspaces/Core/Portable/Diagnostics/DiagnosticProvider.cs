// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void Enable(Workspace workspace, Options options)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();

            var newOptions = GetOptions(workspace, options);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(newOptions));
            service.Register(workspace);
        }

        public static void Disable(Workspace workspace)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            service.Unregister(workspace);
        }

        private static CodeAnalysis.Options.OptionSet GetOptions(Workspace workspace, Options options)
        {
            return workspace.Options
                            .WithChangedOption(InternalRuntimeDiagnosticOptions.Syntax, (options & Options.Syntax) == Options.Syntax)
                            .WithChangedOption(InternalRuntimeDiagnosticOptions.Semantic, (options & Options.Semantic) == Options.Semantic)
                            .WithChangedOption(InternalRuntimeDiagnosticOptions.ScriptSemantic, (options & Options.ScriptSemantic) == Options.ScriptSemantic);
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            workspace.Options = GetOptions(workspace, options);
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provide a way for users to turn on and off analyzing workspace for compiler diagnostics
    /// </summary>
    public static class DiagnosticServices
    {
        public static void Enable(Workspace workspace, DiagnosticServiceOptions options)
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

        private static OptionSet GetOptions(Workspace workspace, DiagnosticServiceOptions options)
        {
            return workspace.Options
                            .WithChangedOption(InternalRuntimeDiagnosticOptions.Syntax, (options & DiagnosticServiceOptions.Syntax) == DiagnosticServiceOptions.Syntax)
                            .WithChangedOption(InternalRuntimeDiagnosticOptions.Semantic, (options & DiagnosticServiceOptions.Semantic) == DiagnosticServiceOptions.Semantic);
        }
    }
}

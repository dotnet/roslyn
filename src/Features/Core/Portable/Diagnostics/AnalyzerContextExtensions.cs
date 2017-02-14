// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerContextExtensions
    {
        public static bool IsInContainedDocument(this SymbolAnalysisContext context)
        {
            var location = context.Symbol.Locations.First();
            if (location.IsInSource)
            {
                var workspace = (context.Options as WorkspaceAnalyzerOptions)?.Workspace;
                return location.SourceTree.IsInContainedDocument(workspace);
            }

            return false;
        }

        public static bool IsInContainedDocument(this SyntaxNodeAnalysisContext context)
        {
            var workspace = (context.Options as WorkspaceAnalyzerOptions)?.Workspace;
            return context.Node.SyntaxTree.IsInContainedDocument(workspace);
        }
    }
}
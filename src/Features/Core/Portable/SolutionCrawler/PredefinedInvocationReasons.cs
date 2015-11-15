// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class PredefinedInvocationReasons
    {
        public const string SolutionRemoved = "SolutionRemoved";

        public const string DocumentAdded = "DocumentAdded";
        public const string DocumentRemoved = "DocumentRemoved";
        public const string DocumentOpened = "DocumentOpened";
        public const string DocumentClosed = "DocumentClosed";
        public const string HighPriority = "HighPriority";

        public const string SyntaxChanged = "SyntaxChanged";
        public const string SemanticChanged = "SemanticChanged";

        public const string Reanalyze = "Reanalyze";
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class PredefinedInvocationReasons
    {
        public const string SolutionRemoved = nameof(SolutionRemoved);

        public const string DocumentAdded = nameof(DocumentAdded);
        public const string DocumentRemoved = nameof(DocumentRemoved);
        public const string DocumentOpened = nameof(DocumentOpened);
        public const string DocumentClosed = nameof(DocumentClosed);
        public const string HighPriority = nameof(HighPriority);

        public const string SyntaxChanged = nameof(SyntaxChanged);
        public const string SemanticChanged = nameof(SemanticChanged);

        public const string Reanalyze = nameof(Reanalyze);
    }
}

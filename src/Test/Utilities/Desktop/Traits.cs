// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class Traits
    {
        public const string Feature = nameof(Feature);
        public static class Features
        {
            public const string AddMissingTokens = nameof(AddMissingTokens);
            public const string AsyncLazy = nameof(AsyncLazy);
            public const string FindReferences = nameof(FindReferences);
            public const string FixIncorrectTokens = nameof(FixIncorrectTokens);
            public const string NormalizeModifiersOrOperators = nameof(NormalizeModifiersOrOperators);
            public const string ReduceTokens = nameof(ReduceTokens);
            public const string RemoveUnnecessaryLineContinuation = nameof(RemoveUnnecessaryLineContinuation);
            public const string Workspace = nameof(Workspace);
            public const string Diagnostics = nameof(Diagnostics);
            public const string Formatting = nameof(Formatting);
            public const string LinkedFileDiffMerging = nameof(LinkedFileDiffMerging);
        }

        public const string Environment = nameof(Environment);
        public static class Environments
        {
            public const string VSProductInstall = nameof(VSProductInstall);
        }
    }
}

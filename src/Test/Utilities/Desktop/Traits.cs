// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class Traits
    {
        public const string Feature = "Feature";
        public static class Features
        {
            public const string AddMissingTokens = "AddMissingTokens";
            public const string AsyncLazy = "AsyncLazy";
            public const string FindReferences = "FindReferences";
            public const string FixIncorrectTokens = "FixIncorrectTokens";
            public const string NormalizeModifiersOrOperators = "NormalizeModifiersOrOperators";
            public const string ReduceTokens = "ReduceTokens";
            public const string RemoveUnnecessaryLineContinuation = "RemoveUnnecessaryLineContinuation";
            public const string Workspace = "Workspace";
            public const string Diagnostics = "Diagnostics";
            public const string Formatting = "Formatting";
            public const string LinkedFileDiffMerging = "LinkedFileDiffMerging";
            public const string GoToNextAndPreviousMember = "GoToNextAndPreviousMember";
        }

        public const string Environment = "Environment";
        public static class Environments
        {
            public const string VSProductInstall = "VSProductInstall";
        }
    }
}

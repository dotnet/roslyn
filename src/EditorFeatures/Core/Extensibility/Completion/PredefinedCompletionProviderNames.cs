// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor
{
    internal static class PredefinedCompletionProviderNames
    {
        /// <summary>
        /// Completion provider for language keywords.
        /// </summary>
        public const string Keyword = "Keyword Completion Provider";

        /// <summary>
        /// Completion provider for language symbols.
        /// </summary>
        public const string Symbol = "Symbol Completion Provider";

        /// <summary>
        /// Completion provider for C# speculative "T" symbol.
        /// </summary>
        public const string SpeculativeT = "Speculative T Completion Provider";

        /// <summary>
        /// Completion provider for contextual keywords.
        /// </summary>
        public const string ContextualKeyword = "Contextual Keyword Completion Provider";

        /// <summary>
        /// Completion provider that preselects an appropriate type when creating a new object.
        /// </summary>
        public const string ObjectCreation = "Object Creation Completion Provider";

        /// <summary>
        /// Completion provider that comes up and provides Enum values in appropriate contexts.
        /// </summary>
        public const string Enum = "Enum Completion Provider";
    }
}

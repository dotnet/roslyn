// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockTypes
    {
        // Basic types.
        public const string Nonstructural = nameof(Nonstructural);

        // Trivia
        public const string Comment = nameof(Comment);
        public const string PreprocessorRegion = nameof(PreprocessorRegion);

        // Top level declarations.
        public const string Imports = nameof(Imports);
        public const string Namespace = nameof(Namespace);
        public const string Type = nameof(Type);
        public const string Member = nameof(Member);

        // Statements and expressions.
        public const string Statement = nameof(Statement);
        public const string Conditional = nameof(Conditional);
        public const string Loop = nameof(Loop);

        public const string Expression = nameof(Expression);

        internal static bool IsCommentOrPreprocessorRegion(string type)
        {
            switch (type)
            {
                case Comment:
                case PreprocessorRegion:
                    return true;
            }

            return false;
        }

        internal static bool IsExpressionLevelConstruct(string type)
        {
            return type == Expression;
        }

        internal static bool IsStatementLevelConstruct(string type)
        {
            switch (type)
            {
                case Statement:
                case Conditional:
                case Loop:
                    return true;
            }

            return false;
        }

        internal static bool IsCodeLevelConstruct(string type)
        {
            return IsExpressionLevelConstruct(type) || IsStatementLevelConstruct(type);
        }

        internal static bool IsDeclarationLevelConstruct(string type)
        {
            return !IsCodeLevelConstruct(type) && !IsCommentOrPreprocessorRegion(type);
        }
    }
}

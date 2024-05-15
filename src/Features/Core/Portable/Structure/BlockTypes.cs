// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Structure;

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
        => type == Expression;

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
        => IsExpressionLevelConstruct(type) || IsStatementLevelConstruct(type);

    internal static bool IsDeclarationLevelConstruct(string type)
        => !IsCodeLevelConstruct(type) && !IsCommentOrPreprocessorRegion(type);
}

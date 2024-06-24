// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

[DataContract]
internal sealed record class CSharpSyntaxFormattingOptions : SyntaxFormattingOptions, IEquatable<CSharpSyntaxFormattingOptions>
{
    private static readonly CodeStyleOption2<NamespaceDeclarationPreference> s_defaultNamespaceDeclarations =
        new(NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Silent);

    private static readonly CodeStyleOption2<bool> s_trueWithSilentEnforcement =
        new(value: true, notification: NotificationOption2.Silent);

    public const SpacePlacement SpacingDefault =
        SpacePlacement.AfterControlFlowStatementKeyword |
        SpacePlacement.AfterSemicolonsInForStatement |
        SpacePlacement.AfterColonInBaseTypeDeclaration |
        SpacePlacement.BeforeColonInBaseTypeDeclaration |
        SpacePlacement.AfterComma;

    public const NewLinePlacement NewLinesDefault =
        NewLinePlacement.BeforeMembersInObjectInitializers |
        NewLinePlacement.BeforeMembersInAnonymousTypes |
        NewLinePlacement.BeforeElse |
        NewLinePlacement.BeforeCatch |
        NewLinePlacement.BeforeFinally |
        NewLinePlacement.BeforeOpenBraceInTypes |
        NewLinePlacement.BeforeOpenBraceInAnonymousTypes |
        NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers |
        NewLinePlacement.BeforeOpenBraceInProperties |
        NewLinePlacement.BeforeOpenBraceInMethods |
        NewLinePlacement.BeforeOpenBraceInAccessors |
        NewLinePlacement.BeforeOpenBraceInAnonymousMethods |
        NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody |
        NewLinePlacement.BeforeOpenBraceInControlBlocks |
        NewLinePlacement.BetweenQueryExpressionClauses;

    public const IndentationPlacement IndentationDefault =
        IndentationPlacement.BlockContents |
        IndentationPlacement.SwitchCaseContents |
        IndentationPlacement.SwitchCaseContentsWhenBlock |
        IndentationPlacement.SwitchSection;

    public static readonly CSharpSyntaxFormattingOptions Default = new();

    [DataMember] public SpacePlacement Spacing { get; init; } = SpacingDefault;
    [DataMember] public BinaryOperatorSpacingOptions SpacingAroundBinaryOperator { get; init; } = BinaryOperatorSpacingOptions.Single;
    [DataMember] public NewLinePlacement NewLines { get; init; } = NewLinesDefault;
    [DataMember] public LabelPositionOptions LabelPositioning { get; init; } = LabelPositionOptions.OneLess;
    [DataMember] public IndentationPlacement Indentation { get; init; } = IndentationDefault;
    [DataMember] public bool WrappingKeepStatementsOnSingleLine { get; init; } = true;
    [DataMember] public bool WrappingPreserveSingleLine { get; init; } = true;
    [DataMember] public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations { get; init; } = s_defaultNamespaceDeclarations;
    [DataMember] public CodeStyleOption2<bool> PreferTopLevelStatements { get; init; } = s_trueWithSilentEnforcement;
    [DataMember] public int CollectionExpressionWrappingLength { get; init; } = 120;

    public CSharpSyntaxFormattingOptions()
    {
    }

    public CSharpSyntaxFormattingOptions(IOptionsReader options, CSharpSyntaxFormattingOptions? fallbackOptions)
        : base(options, fallbackOptions ??= Default, LanguageNames.CSharp)
    {
        Spacing =
            (options.GetOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, fallbackOptions.Spacing.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration)) ? SpacePlacement.IgnoreAroundVariableDeclaration : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName)) ? SpacePlacement.AfterMethodDeclarationName : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses)) ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis)) ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterMethodCallName, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterMethodCallName)) ? SpacePlacement.AfterMethodCallName : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses)) ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses)) ? SpacePlacement.WithinMethodCallParentheses : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword)) ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
            options.GetOption(CSharpFormattingOptions2.SpaceBetweenParentheses, fallbackOptions.Spacing.ToSpacingWithinParentheses()).ToSpacePlacement() |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement)) ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterSemicolonsInForStatement)) ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterCast, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterCast)) ? SpacePlacement.AfterCast : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeOpenSquareBracket)) ? SpacePlacement.BeforeOpenSquareBracket : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, fallbackOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptySquareBrackets)) ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinSquareBrackets)) ? SpacePlacement.WithinSquareBrackets : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration)) ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration)) ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterComma, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterComma)) ? SpacePlacement.AfterComma : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeComma, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeComma)) ? SpacePlacement.BeforeComma : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterDot, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterDot)) ? SpacePlacement.AfterDot : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeDot, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeDot)) ? SpacePlacement.BeforeDot : 0);
        SpacingAroundBinaryOperator = options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator, fallbackOptions.SpacingAroundBinaryOperator);
        NewLines =
            (options.GetOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers)) ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes)) ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForElse, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeElse)) ? NewLinePlacement.BeforeElse : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForCatch, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeCatch)) ? NewLinePlacement.BeforeCatch : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForFinally, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeFinally)) ? NewLinePlacement.BeforeFinally : 0) |
            options.GetOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, fallbackOptions.NewLines.ToNewLineBeforeOpenBracePlacement()).ToNewLinePlacement() |
            (options.GetOption(CSharpFormattingOptions2.NewLineForClausesInQuery, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses)) ? NewLinePlacement.BetweenQueryExpressionClauses : 0);
        LabelPositioning = options.GetOption(CSharpFormattingOptions2.LabelPositioning, fallbackOptions.LabelPositioning);
        Indentation =
            (options.GetOption(CSharpFormattingOptions2.IndentBraces, fallbackOptions.Indentation.HasFlag(IndentationPlacement.Braces)) ? IndentationPlacement.Braces : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentBlock, fallbackOptions.Indentation.HasFlag(IndentationPlacement.BlockContents)) ? IndentationPlacement.BlockContents : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSection, fallbackOptions.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents)) ? IndentationPlacement.SwitchCaseContents : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, fallbackOptions.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock)) ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentSwitchSection, fallbackOptions.Indentation.HasFlag(IndentationPlacement.SwitchSection)) ? IndentationPlacement.SwitchSection : 0);
        WrappingKeepStatementsOnSingleLine = options.GetOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, fallbackOptions.WrappingKeepStatementsOnSingleLine);
        WrappingPreserveSingleLine = options.GetOption(CSharpFormattingOptions2.WrappingPreserveSingleLine, fallbackOptions.WrappingPreserveSingleLine);
        NamespaceDeclarations = options.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations, fallbackOptions.NamespaceDeclarations);
        PreferTopLevelStatements = options.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements, fallbackOptions.PreferTopLevelStatements);
        CollectionExpressionWrappingLength = options.GetOption(CSharpFormattingOptions2.CollectionExpressionWrappingLength, fallbackOptions.CollectionExpressionWrappingLength);
    }
}

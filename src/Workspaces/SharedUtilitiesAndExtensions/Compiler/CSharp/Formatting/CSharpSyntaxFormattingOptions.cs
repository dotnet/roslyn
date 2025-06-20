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
    [DataMember] public BinaryOperatorSpacingOptionsInternal SpacingAroundBinaryOperator { get; init; } = BinaryOperatorSpacingOptionsInternal.Single;
    [DataMember] public NewLinePlacement NewLines { get; init; } = NewLinesDefault;
    [DataMember] public LabelPositionOptionsInternal LabelPositioning { get; init; } = LabelPositionOptionsInternal.OneLess;
    [DataMember] public IndentationPlacement Indentation { get; init; } = IndentationDefault;
    [DataMember] public bool WrappingKeepStatementsOnSingleLine { get; init; } = true;
    [DataMember] public bool WrappingPreserveSingleLine { get; init; } = true;
    [DataMember] public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations { get; init; } = s_defaultNamespaceDeclarations;
    [DataMember] public CodeStyleOption2<bool> PreferTopLevelStatements { get; init; } = s_trueWithSilentEnforcement;
    [DataMember] public int CollectionExpressionWrappingLength { get; init; } = 120;

    public CSharpSyntaxFormattingOptions()
    {
    }

    public CSharpSyntaxFormattingOptions(IOptionsReader options)
        : base(options, LanguageNames.CSharp)
    {
        Spacing =
            (options.GetOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration) ? SpacePlacement.IgnoreAroundVariableDeclaration : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName) ? SpacePlacement.AfterMethodDeclarationName : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses) ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis) ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterMethodCallName) ? SpacePlacement.AfterMethodCallName : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses) ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses) ? SpacePlacement.WithinMethodCallParentheses : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword) ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
            options.GetOption(CSharpFormattingOptions2.SpaceBetweenParentheses).ToSpacePlacement() |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement) ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement) ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterCast) ? SpacePlacement.AfterCast : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket) ? SpacePlacement.BeforeOpenSquareBracket : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets) ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets) ? SpacePlacement.WithinSquareBrackets : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration) ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration) ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterComma) ? SpacePlacement.AfterComma : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeComma) ? SpacePlacement.BeforeComma : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceAfterDot) ? SpacePlacement.AfterDot : 0) |
            (options.GetOption(CSharpFormattingOptions2.SpaceBeforeDot) ? SpacePlacement.BeforeDot : 0);
        SpacingAroundBinaryOperator = options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator);
        NewLines =
            (options.GetOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit) ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes) ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForElse) ? NewLinePlacement.BeforeElse : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForCatch) ? NewLinePlacement.BeforeCatch : 0) |
            (options.GetOption(CSharpFormattingOptions2.NewLineForFinally) ? NewLinePlacement.BeforeFinally : 0) |
            options.GetOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace).ToNewLinePlacement() |
            (options.GetOption(CSharpFormattingOptions2.NewLineForClausesInQuery) ? NewLinePlacement.BetweenQueryExpressionClauses : 0);
        LabelPositioning = options.GetOption(CSharpFormattingOptions2.LabelPositioning);
        Indentation =
            (options.GetOption(CSharpFormattingOptions2.IndentBraces) ? IndentationPlacement.Braces : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentBlock) ? IndentationPlacement.BlockContents : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSection) ? IndentationPlacement.SwitchCaseContents : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock) ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
            (options.GetOption(CSharpFormattingOptions2.IndentSwitchSection) ? IndentationPlacement.SwitchSection : 0);
        WrappingKeepStatementsOnSingleLine = options.GetOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine);
        WrappingPreserveSingleLine = options.GetOption(CSharpFormattingOptions2.WrappingPreserveSingleLine);
        NamespaceDeclarations = options.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations);
        PreferTopLevelStatements = options.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);
        CollectionExpressionWrappingLength = options.GetOption(CSharpFormattingOptions2.CollectionExpressionWrappingLength);
    }
}

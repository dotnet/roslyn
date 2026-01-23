// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

/// <summary>
/// Wrapper for CSharpSyntaxFormattingOptions for Razor external access.
/// </summary>
[DataContract]
internal sealed record class RazorCSharpSyntaxFormattingOptions(
    [property: DataMember] RazorSpacePlacement Spacing,
    [property: DataMember] RazorBinaryOperatorSpacingOptions SpacingAroundBinaryOperator,
    [property: DataMember] RazorNewLinePlacement NewLines,
    [property: DataMember] RazorLabelPositionOptions LabelPositioning,
    [property: DataMember] RazorIndentationPlacement Indentation,
    [property: DataMember] bool WrappingKeepStatementsOnSingleLine,
    [property: DataMember] bool WrappingPreserveSingleLine,
    [property: DataMember] RazorNamespaceDeclarationPreference NamespaceDeclarations,
    [property: DataMember] bool PreferTopLevelStatements,
    [property: DataMember] int CollectionExpressionWrappingLength)
{
    public static readonly RazorCSharpSyntaxFormattingOptions Default = new();

    public RazorCSharpSyntaxFormattingOptions()
        : this(CSharpSyntaxFormattingOptions.Default)
    {
    }

    public RazorCSharpSyntaxFormattingOptions(CSharpSyntaxFormattingOptions options)
        : this(
            (RazorSpacePlacement)options.Spacing,
            (RazorBinaryOperatorSpacingOptions)options.SpacingAroundBinaryOperator,
            (RazorNewLinePlacement)options.NewLines,
            (RazorLabelPositionOptions)options.LabelPositioning,
            (RazorIndentationPlacement)options.Indentation,
            options.WrappingKeepStatementsOnSingleLine,
            options.WrappingPreserveSingleLine,
            (RazorNamespaceDeclarationPreference)options.NamespaceDeclarations.Value,
            options.PreferTopLevelStatements.Value,
            options.CollectionExpressionWrappingLength)
    {
    }

    public CSharpSyntaxFormattingOptions ToCSharpSyntaxFormattingOptions()
        => new()
        {
            Spacing = (SpacePlacement)Spacing,
            SpacingAroundBinaryOperator = (BinaryOperatorSpacingOptionsInternal)SpacingAroundBinaryOperator,
            NewLines = (NewLinePlacement)NewLines,
            LabelPositioning = (LabelPositionOptionsInternal)LabelPositioning,
            Indentation = (IndentationPlacement)Indentation,
            WrappingKeepStatementsOnSingleLine = WrappingKeepStatementsOnSingleLine,
            WrappingPreserveSingleLine = WrappingPreserveSingleLine,
            NamespaceDeclarations = new CodeStyleOption2<NamespaceDeclarationPreference>(
                (NamespaceDeclarationPreference)NamespaceDeclarations,
                CSharpSyntaxFormattingOptions.Default.NamespaceDeclarations.Notification),
            PreferTopLevelStatements = new CodeStyleOption2<bool>(
                PreferTopLevelStatements,
                CSharpSyntaxFormattingOptions.Default.PreferTopLevelStatements.Notification),
            CollectionExpressionWrappingLength = CollectionExpressionWrappingLength
        };
}

[Flags]
public enum RazorSpacePlacement
{
    None = 0,
    IgnoreAroundVariableDeclaration = SpacePlacement.IgnoreAroundVariableDeclaration,
    AfterMethodDeclarationName = SpacePlacement.AfterMethodDeclarationName,
    BetweenEmptyMethodDeclarationParentheses = SpacePlacement.BetweenEmptyMethodDeclarationParentheses,
    WithinMethodDeclarationParenthesis = SpacePlacement.WithinMethodDeclarationParenthesis,
    AfterMethodCallName = SpacePlacement.AfterMethodCallName,
    BetweenEmptyMethodCallParentheses = SpacePlacement.BetweenEmptyMethodCallParentheses,
    WithinMethodCallParentheses = SpacePlacement.WithinMethodCallParentheses,
    AfterControlFlowStatementKeyword = SpacePlacement.AfterControlFlowStatementKeyword,
    WithinExpressionParentheses = SpacePlacement.WithinExpressionParentheses,
    WithinCastParentheses = SpacePlacement.WithinCastParentheses,
    BeforeSemicolonsInForStatement = SpacePlacement.BeforeSemicolonsInForStatement,
    AfterSemicolonsInForStatement = SpacePlacement.AfterSemicolonsInForStatement,
    WithinOtherParentheses = SpacePlacement.WithinOtherParentheses,
    AfterCast = SpacePlacement.AfterCast,
    BeforeOpenSquareBracket = SpacePlacement.BeforeOpenSquareBracket,
    BetweenEmptySquareBrackets = SpacePlacement.BetweenEmptySquareBrackets,
    WithinSquareBrackets = SpacePlacement.WithinSquareBrackets,
    AfterColonInBaseTypeDeclaration = SpacePlacement.AfterColonInBaseTypeDeclaration,
    BeforeColonInBaseTypeDeclaration = SpacePlacement.BeforeColonInBaseTypeDeclaration,
    AfterComma = SpacePlacement.AfterComma,
    BeforeComma = SpacePlacement.BeforeComma,
    AfterDot = SpacePlacement.AfterDot,
    BeforeDot = SpacePlacement.BeforeDot,
}

[Flags]
public enum RazorNewLinePlacement
{
    None = 0,
    BeforeMembersInObjectInitializers = NewLinePlacement.BeforeMembersInObjectInitializers,
    BeforeMembersInAnonymousTypes = NewLinePlacement.BeforeMembersInAnonymousTypes,
    BeforeElse = NewLinePlacement.BeforeElse,
    BeforeCatch = NewLinePlacement.BeforeCatch,
    BeforeFinally = NewLinePlacement.BeforeFinally,
    BeforeOpenBraceInTypes = NewLinePlacement.BeforeOpenBraceInTypes,
    BeforeOpenBraceInAnonymousTypes = NewLinePlacement.BeforeOpenBraceInAnonymousTypes,
    BeforeOpenBraceInObjectCollectionArrayInitializers = NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers,
    BeforeOpenBraceInProperties = NewLinePlacement.BeforeOpenBraceInProperties,
    BeforeOpenBraceInMethods = NewLinePlacement.BeforeOpenBraceInMethods,
    BeforeOpenBraceInAccessors = NewLinePlacement.BeforeOpenBraceInAccessors,
    BeforeOpenBraceInAnonymousMethods = NewLinePlacement.BeforeOpenBraceInAnonymousMethods,
    BeforeOpenBraceInLambdaExpressionBody = NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody,
    BeforeOpenBraceInControlBlocks = NewLinePlacement.BeforeOpenBraceInControlBlocks,
    BetweenQueryExpressionClauses = NewLinePlacement.BetweenQueryExpressionClauses,
}

[Flags]
public enum RazorIndentationPlacement
{
    None = 0,
    Braces = IndentationPlacement.Braces,
    BlockContents = IndentationPlacement.BlockContents,
    SwitchCaseContents = IndentationPlacement.SwitchCaseContents,
    SwitchCaseContentsWhenBlock = IndentationPlacement.SwitchCaseContentsWhenBlock,
    SwitchSection = IndentationPlacement.SwitchSection,
}

public enum RazorBinaryOperatorSpacingOptions
{
    Single = BinaryOperatorSpacingOptions.Single,
    Ignore = BinaryOperatorSpacingOptions.Ignore,
    Remove = BinaryOperatorSpacingOptions.Remove,
}

public enum RazorLabelPositionOptions
{
    LeftMost = LabelPositionOptions.LeftMost,
    OneLess = LabelPositionOptions.OneLess,
    NoIndent = LabelPositionOptions.NoIndent,
}

public enum RazorNamespaceDeclarationPreference
{
    BlockScoped = NamespaceDeclarationPreference.BlockScoped,
    FileScoped = NamespaceDeclarationPreference.FileScoped,
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [Flags]
    internal enum SpacePlacement
    {
        IgnoreAroundVariableDeclaration = 1,
        AfterMethodDeclarationName = 1 << 1,
        BetweenEmptyMethodDeclarationParentheses = 1 << 2,
        WithinMethodDeclarationParenthesis = 1 << 3,
        AfterMethodCallName = 1 << 4,
        BetweenEmptyMethodCallParentheses = 1 << 5,
        WithinMethodCallParentheses = 1 << 6,
        AfterControlFlowStatementKeyword = 1 << 7,
        WithinExpressionParentheses = 1 << 8,
        WithinCastParentheses = 1 << 9,
        BeforeSemicolonsInForStatement = 1 << 10,
        AfterSemicolonsInForStatement = 1 << 11,
        WithinOtherParentheses = 1 << 12,
        AfterCast = 1 << 13,
        BeforeOpenSquareBracket = 1 << 14,
        BetweenEmptySquareBrackets = 1 << 15,
        WithinSquareBrackets = 1 << 16,
        AfterColonInBaseTypeDeclaration = 1 << 17,
        BeforeColonInBaseTypeDeclaration = 1 << 18,
        AfterComma = 1 << 19,
        BeforeComma = 1 << 20,
        AfterDot = 1 << 21,
        BeforeDot = 1 << 22,
    }

    [Flags]
    internal enum NewLinePlacement
    {
        BeforeMembersInObjectInitializers = 1,
        BeforeMembersInAnonymousTypes = 1 << 1,
        BeforeElse = 1 << 2,
        BeforeCatch = 1 << 3,
        BeforeFinally = 1 << 4,
        BeforeOpenBraceInTypes = 1 << 5,
        BeforeOpenBraceInAnonymousTypes = 1 << 6,
        BeforeOpenBraceInObjectCollectionArrayInitializers = 1 << 7,
        BeforeOpenBraceInProperties = 1 << 8,
        BeforeOpenBraceInMethods = 1 << 9,
        BeforeOpenBraceInAccessors = 1 << 10,
        BeforeOpenBraceInAnonymousMethods = 1 << 11,
        BeforeOpenBraceInLambdaExpressionBody = 1 << 12,
        BeforeOpenBraceInControlBlocks = 1 << 13,
        BetweenQueryExpressionClauses = 1 << 14
    }

    [Flags]
    internal enum IndentationPlacement
    {
        Braces = 1,
        BlockContents = 1 << 1,
        SwitchCaseContents = 1 << 2,
        SwitchCaseContentsWhenBlock = 1 << 3,
        SwitchSection = 1 << 4
    }

    [DataContract]
    internal sealed class CSharpSyntaxFormattingOptions : SyntaxFormattingOptions, IEquatable<CSharpSyntaxFormattingOptions>
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

        public override SyntaxFormattingOptions With(LineFormattingOptions lineFormatting)
            => new CSharpSyntaxFormattingOptions()
            {
                Common = Common with { LineFormatting = lineFormatting },
                Spacing = Spacing,
                SpacingAroundBinaryOperator = SpacingAroundBinaryOperator,
                NewLines = NewLines,
                LabelPositioning = LabelPositioning,
                Indentation = Indentation,
                WrappingKeepStatementsOnSingleLine = WrappingKeepStatementsOnSingleLine,
                WrappingPreserveSingleLine = WrappingPreserveSingleLine,
                NamespaceDeclarations = NamespaceDeclarations,
                PreferTopLevelStatements = PreferTopLevelStatements
            };

        public override bool Equals(object? obj)
            => Equals(obj as CSharpSyntaxFormattingOptions);

        public bool Equals(CSharpSyntaxFormattingOptions? other)
            => other is not null &&
               Common.Equals(other.Common) &&
               SpacingAroundBinaryOperator == other.SpacingAroundBinaryOperator &&
               NewLines == other.NewLines &&
               LabelPositioning == other.LabelPositioning &&
               Indentation == other.Indentation &&
               WrappingKeepStatementsOnSingleLine == other.WrappingKeepStatementsOnSingleLine &&
               WrappingPreserveSingleLine == other.WrappingPreserveSingleLine &&
               NamespaceDeclarations.Equals(other.NamespaceDeclarations) &&
               PreferTopLevelStatements.Equals(other.PreferTopLevelStatements);

        public override int GetHashCode()
            => Hash.Combine(Common,
               Hash.Combine((int)SpacingAroundBinaryOperator,
               Hash.Combine((int)NewLines,
               Hash.Combine((int)LabelPositioning,
               Hash.Combine((int)Indentation,
               Hash.Combine(WrappingKeepStatementsOnSingleLine,
               Hash.Combine(WrappingPreserveSingleLine,
               Hash.Combine(NamespaceDeclarations,
               Hash.Combine(PreferTopLevelStatements, 0)))))))));
    }

    internal static class CSharpSyntaxFormattingOptionsProviders
    {
        public static CSharpSyntaxFormattingOptions GetCSharpSyntaxFormattingOptions(this AnalyzerConfigOptions options, CSharpSyntaxFormattingOptions? fallbackOptions)
        {
            fallbackOptions ??= CSharpSyntaxFormattingOptions.Default;

            return new()
            {
                Common = options.GetCommonSyntaxFormattingOptions(fallbackOptions.Common),
                Spacing =
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, fallbackOptions.Spacing.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration)) ? SpacePlacement.IgnoreAroundVariableDeclaration : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterMethodDeclarationName)) ? SpacePlacement.AfterMethodDeclarationName : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses)) ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis)) ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterMethodCallName, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterMethodCallName)) ? SpacePlacement.AfterMethodCallName : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses)) ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinMethodCallParentheses)) ? SpacePlacement.WithinMethodCallParentheses : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword)) ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceWithinExpressionParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinExpressionParentheses)) ? SpacePlacement.WithinExpressionParentheses : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceWithinCastParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinCastParentheses)) ? SpacePlacement.WithinCastParentheses : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement)) ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterSemicolonsInForStatement)) ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceWithinOtherParentheses, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinOtherParentheses)) ? SpacePlacement.WithinOtherParentheses : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterCast, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterCast)) ? SpacePlacement.AfterCast : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeOpenSquareBracket)) ? SpacePlacement.BeforeOpenSquareBracket : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, fallbackOptions.Spacing.HasFlag(SpacePlacement.BetweenEmptySquareBrackets)) ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets, fallbackOptions.Spacing.HasFlag(SpacePlacement.WithinSquareBrackets)) ? SpacePlacement.WithinSquareBrackets : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration)) ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration)) ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterComma, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterComma)) ? SpacePlacement.AfterComma : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBeforeComma, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeComma)) ? SpacePlacement.BeforeComma : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceAfterDot, fallbackOptions.Spacing.HasFlag(SpacePlacement.AfterDot)) ? SpacePlacement.AfterDot : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.SpaceBeforeDot, fallbackOptions.Spacing.HasFlag(SpacePlacement.BeforeDot)) ? SpacePlacement.BeforeDot : 0),
                SpacingAroundBinaryOperator = options.GetEditorConfigOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator, fallbackOptions.SpacingAroundBinaryOperator),
                NewLines =
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers)) ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes)) ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLineForElse, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeElse)) ? NewLinePlacement.BeforeElse : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLineForCatch, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeCatch)) ? NewLinePlacement.BeforeCatch : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLineForFinally, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeFinally)) ? NewLinePlacement.BeforeFinally : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInTypes, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes)) ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes)) ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers)) ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInProperties, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties)) ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInMethods, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods)) ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors)) ? NewLinePlacement.BeforeOpenBraceInAccessors : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods)) ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody)) ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks)) ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.NewLineForClausesInQuery, fallbackOptions.NewLines.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses)) ? NewLinePlacement.BetweenQueryExpressionClauses : 0),
                LabelPositioning = options.GetEditorConfigOption(CSharpFormattingOptions2.LabelPositioning, fallbackOptions.LabelPositioning),
                Indentation =
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.IndentBraces, fallbackOptions.Indentation.HasFlag(IndentationPlacement.Braces)) ? IndentationPlacement.Braces : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.IndentBlock, fallbackOptions.Indentation.HasFlag(IndentationPlacement.BlockContents)) ? IndentationPlacement.BlockContents : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.IndentSwitchCaseSection, fallbackOptions.Indentation.HasFlag(IndentationPlacement.SwitchCaseContents)) ? IndentationPlacement.SwitchCaseContents : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock, fallbackOptions.Indentation.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock)) ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
                    (options.GetEditorConfigOption(CSharpFormattingOptions2.IndentSwitchSection, fallbackOptions.Indentation.HasFlag(IndentationPlacement.SwitchSection)) ? IndentationPlacement.SwitchSection : 0),
                WrappingKeepStatementsOnSingleLine = options.GetEditorConfigOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, fallbackOptions.WrappingKeepStatementsOnSingleLine),
                WrappingPreserveSingleLine = options.GetEditorConfigOption(CSharpFormattingOptions2.WrappingPreserveSingleLine, fallbackOptions.WrappingPreserveSingleLine),
                NamespaceDeclarations = options.GetEditorConfigOption(CSharpCodeStyleOptions.NamespaceDeclarations, fallbackOptions.NamespaceDeclarations),
                PreferTopLevelStatements = options.GetEditorConfigOption(CSharpCodeStyleOptions.PreferTopLevelStatements, fallbackOptions.PreferTopLevelStatements)
            };
        }
    }
}

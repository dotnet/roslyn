// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal static class RazorPredefinedCodeRefactoringProviderNames
    {
        public static string AddAwait => PredefinedCodeRefactoringProviderNames.AddAwait;
        public static string AddConstructorParametersFromMembers => PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers;
        public static string AddDebuggerDisplay => PredefinedCodeRefactoringProviderNames.AddDebuggerDisplay;
        public static string AddFileBanner => PredefinedCodeRefactoringProviderNames.AddFileBanner;
        public static string AddMissingImports => PredefinedCodeRefactoringProviderNames.AddMissingImports;
        public static string AddParameterCheck => PredefinedCodeRefactoringProviderNames.AddParameterCheck;
        public static string ChangeSignature => PredefinedCodeRefactoringProviderNames.ChangeSignature;
        public static string ConvertAnonymousTypeToClass => PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass;
        public static string ConvertAnonymousTypeToTuple => PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToTuple;
        public static string ConvertAutoPropertyToFullProperty => PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty;
        public static string ConvertBetweenRegularAndVerbatimInterpolatedString => PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString;
        public static string ConvertBetweenRegularAndVerbatimString => PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString;
        public static string ConvertConcatenationToInterpolatedString => PredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString;
        public static string ConvertDirectCastToTryCast => PredefinedCodeRefactoringProviderNames.ConvertDirectCastToTryCast;
        public static string ConvertForEachToFor => PredefinedCodeRefactoringProviderNames.ConvertForEachToFor;
        public static string ConvertForEachToLinqQuery => PredefinedCodeRefactoringProviderNames.ConvertForEachToLinqQuery;
        public static string ConvertForToForEach => PredefinedCodeRefactoringProviderNames.ConvertForToForEach;
        public static string ConvertIfToSwitch => PredefinedCodeRefactoringProviderNames.ConvertIfToSwitch;
        public static string ConvertLinqQueryToForEach => PredefinedCodeRefactoringProviderNames.ConvertLinqQueryToForEach;
        public static string ConvertLocalFunctionToMethod => PredefinedCodeRefactoringProviderNames.ConvertLocalFunctionToMethod;
        public static string ConvertNamespace => PredefinedCodeRefactoringProviderNames.ConvertNamespace;
        public static string ConvertNumericLiteral => PredefinedCodeRefactoringProviderNames.ConvertNumericLiteral;
        public static string ConvertPlaceholderToInterpolatedString => PredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString;
        public static string ConvertPrimaryToRegularConstructor => PredefinedCodeRefactoringProviderNames.ConvertPrimaryToRegularConstructor;
        public static string ConvertToInterpolatedString => PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString;
        public static string ConvertToProgramMain => PredefinedCodeRefactoringProviderNames.ConvertToProgramMain;
        public static string ConvertToRawString => PredefinedCodeRefactoringProviderNames.ConvertToRawString;
        public static string ConvertToRecord => PredefinedCodeRefactoringProviderNames.ConvertToRecord;
        public static string ConvertToTopLevelStatements => PredefinedCodeRefactoringProviderNames.ConvertToTopLevelStatements;
        public static string ConvertTryCastToDirectCast => PredefinedCodeRefactoringProviderNames.ConvertTryCastToDirectCast;
        public static string ConvertTupleToStruct => PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct;
        public static string EnableNullable => PredefinedCodeRefactoringProviderNames.EnableNullable;
        public static string EncapsulateField => PredefinedCodeRefactoringProviderNames.EncapsulateField;
        public static string ExtractClass => PredefinedCodeRefactoringProviderNames.ExtractClass;
        public static string ExtractInterface => PredefinedCodeRefactoringProviderNames.ExtractInterface;
        public static string ExtractMethod => PredefinedCodeRefactoringProviderNames.ExtractMethod;
        public static string GenerateComparisonOperators => PredefinedCodeRefactoringProviderNames.GenerateComparisonOperators;
        public static string GenerateConstructorFromMembers => PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers;
        public static string GenerateDefaultConstructors => PredefinedCodeRefactoringProviderNames.GenerateDefaultConstructors;
        public static string GenerateEqualsAndGetHashCodeFromMembers => PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers;
        public static string GenerateOverrides => PredefinedCodeRefactoringProviderNames.GenerateOverrides;
        public static string ImplementInterfaceExplicitly => PredefinedCodeRefactoringProviderNames.ImplementInterfaceExplicitly;
        public static string ImplementInterfaceImplicitly => PredefinedCodeRefactoringProviderNames.ImplementInterfaceImplicitly;
        public static string InitializeMemberFromParameter => PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter;
        public static string InitializeMemberFromPrimaryConstructorParameter => PredefinedCodeRefactoringProviderNames.InitializeMemberFromPrimaryConstructorParameter;
        public static string InlineMethod => PredefinedCodeRefactoringProviderNames.InlineMethod;
        public static string InlineTemporary => PredefinedCodeRefactoringProviderNames.InlineTemporary;
        public static string IntroduceLocalForExpression => PredefinedCodeRefactoringProviderNames.IntroduceLocalForExpression;
        public static string IntroduceParameter => PredefinedCodeRefactoringProviderNames.IntroduceParameter;
        public static string IntroduceUsingStatement => PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement;
        public static string IntroduceVariable => PredefinedCodeRefactoringProviderNames.IntroduceVariable;
        public static string InvertConditional => PredefinedCodeRefactoringProviderNames.InvertConditional;
        public static string InvertIf => PredefinedCodeRefactoringProviderNames.InvertIf;
        public static string InvertLogical => PredefinedCodeRefactoringProviderNames.InvertLogical;
        public static string InvertMultiLineIf => PredefinedCodeRefactoringProviderNames.InvertMultiLineIf;
        public static string MakeLocalFunctionStatic => PredefinedCodeRefactoringProviderNames.MakeLocalFunctionStatic;
        public static string MergeConsecutiveIfStatements => PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements;
        public static string MergeNestedIfStatements => PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements;
        public static string MoveDeclarationNearReference => PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference;
        public static string MoveStaticMembers => PredefinedCodeRefactoringProviderNames.MoveStaticMembers;
        public static string MoveToNamespace => PredefinedCodeRefactoringProviderNames.MoveToNamespace;
        public static string MoveTypeToFile => PredefinedCodeRefactoringProviderNames.MoveTypeToFile;
        public static string NameTupleElement => PredefinedCodeRefactoringProviderNames.NameTupleElement;
        public static string PullMemberUp => PredefinedCodeRefactoringProviderNames.PullMemberUp;
        public static string RenameTracking => PredefinedCodeRefactoringProviderNames.RenameTracking;
        public static string ReplaceConditionalWithStatements => PredefinedCodeRefactoringProviderNames.ReplaceConditionalWithStatements;
        public static string ReplaceDocCommentTextWithTag => PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag;
        public static string ReplaceMethodWithProperty => PredefinedCodeRefactoringProviderNames.ReplaceMethodWithProperty;
        public static string ReplacePropertyWithMethods => PredefinedCodeRefactoringProviderNames.ReplacePropertyWithMethods;
        public static string ReverseForStatement => PredefinedCodeRefactoringProviderNames.ReverseForStatement;
        public static string SplitIntoConsecutiveIfStatements => PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements;
        public static string SplitIntoNestedIfStatements => PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements;
        public static string SyncNamespace => PredefinedCodeRefactoringProviderNames.SyncNamespace;
        public static string UseExplicitType => PredefinedCodeRefactoringProviderNames.UseExplicitType;
        public static string UseExpressionBody => PredefinedCodeRefactoringProviderNames.UseExpressionBody;
        public static string UseExpressionBodyForLambda => PredefinedCodeRefactoringProviderNames.UseExpressionBodyForLambda;
        public static string UseImplicitType => PredefinedCodeRefactoringProviderNames.UseImplicitType;
        public static string UseNamedArguments => PredefinedCodeRefactoringProviderNames.UseNamedArguments;
        public static string UseRecursivePatterns => PredefinedCodeRefactoringProviderNames.UseRecursivePatterns;
        public static string Wrapping => PredefinedCodeRefactoringProviderNames.Wrapping;
    }
}

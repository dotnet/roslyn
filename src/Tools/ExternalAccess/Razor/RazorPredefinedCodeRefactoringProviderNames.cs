// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal static class RazorPredefinedCodeRefactoringProviderNames
    {
        internal static string ApplyPrefix(string name) => $"{CodeRefactoringService.ProviderTagPrefix}{name}";

        public static string AddAwait => ApplyPrefix(PredefinedCodeRefactoringProviderNames.AddAwait);
        public static string AddConstructorParametersFromMembers => ApplyPrefix(PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers);
        public static string AddFileBanner => ApplyPrefix(PredefinedCodeRefactoringProviderNames.AddFileBanner);
        public static string AddMissingImports => ApplyPrefix(PredefinedCodeRefactoringProviderNames.AddMissingImports);
        public static string ChangeSignature => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ChangeSignature);
        public static string ConvertAnonymousTypeToClass => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass);
        public static string ConvertDirectCastToTryCast => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertDirectCastToTryCast);
        public static string ConvertTryCastToDirectCast => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertTryCastToDirectCast);
        public static string ConvertToInterpolatedString => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString);
        public static string ConvertTupleToStruct => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct);
        public static string EncapsulateField => ApplyPrefix(PredefinedCodeRefactoringProviderNames.EncapsulateField);
        public static string ExtractClass => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ExtractClass);
        public static string ExtractInterface => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ExtractInterface);
        public static string ExtractMethod => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ExtractMethod);
        public static string GenerateConstructorFromMembers => ApplyPrefix(PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers);
        public static string GenerateDefaultConstructors => ApplyPrefix(PredefinedCodeRefactoringProviderNames.GenerateDefaultConstructors);
        public static string GenerateEqualsAndGetHashCodeFromMembers => ApplyPrefix(PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers);
        public static string GenerateOverrides => ApplyPrefix(PredefinedCodeRefactoringProviderNames.GenerateOverrides);
        public static string InlineTemporary => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InlineTemporary);
        public static string IntroduceUsingStatement => ApplyPrefix(PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement);
        public static string IntroduceVariable => ApplyPrefix(PredefinedCodeRefactoringProviderNames.IntroduceVariable);
        public static string InvertConditional => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InvertConditional);
        public static string InvertIf => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InvertIf);
        public static string InvertLogical => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InvertLogical);
        public static string MergeConsecutiveIfStatements => ApplyPrefix(PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements);
        public static string MergeNestedIfStatements => ApplyPrefix(PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements);
        public static string MoveDeclarationNearReference => ApplyPrefix(PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference);
        public static string MoveToNamespace => ApplyPrefix(PredefinedCodeRefactoringProviderNames.MoveToNamespace);
        public static string MoveTypeToFile => ApplyPrefix(PredefinedCodeRefactoringProviderNames.MoveTypeToFile);
        public static string PullMemberUp => ApplyPrefix(PredefinedCodeRefactoringProviderNames.PullMemberUp);
        public static string InlineMethod => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InlineMethod);
        public static string ReplaceDocCommentTextWithTag => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag);
        public static string SplitIntoConsecutiveIfStatements => ApplyPrefix(PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements);
        public static string SplitIntoNestedIfStatements => ApplyPrefix(PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements);
        public static string SyncNamespace => ApplyPrefix(PredefinedCodeRefactoringProviderNames.SyncNamespace);
        public static string UseExplicitType => ApplyPrefix(PredefinedCodeRefactoringProviderNames.UseExplicitType);
        public static string UseExpressionBody => ApplyPrefix(PredefinedCodeRefactoringProviderNames.UseExpressionBody);
        public static string UseImplicitType => ApplyPrefix(PredefinedCodeRefactoringProviderNames.UseImplicitType);
        public static string Wrapping => ApplyPrefix(PredefinedCodeRefactoringProviderNames.Wrapping);
        public static string MakeLocalFunctionStatic => ApplyPrefix(PredefinedCodeRefactoringProviderNames.MakeLocalFunctionStatic);
        public static string GenerateComparisonOperators => ApplyPrefix(PredefinedCodeRefactoringProviderNames.GenerateComparisonOperators);
        public static string ReplacePropertyWithMethods => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ReplacePropertyWithMethods);
        public static string ReplaceMethodWithProperty => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ReplaceMethodWithProperty);
        public static string AddDebuggerDisplay => ApplyPrefix(PredefinedCodeRefactoringProviderNames.AddDebuggerDisplay);
        public static string ConvertAutoPropertyToFullProperty => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty);
        public static string ReverseForStatement => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ReverseForStatement);
        public static string ConvertLocalFunctionToMethod => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertLocalFunctionToMethod);
        public static string ConvertForEachToFor => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertForEachToFor);
        public static string ConvertLinqQueryToForEach => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertLinqQueryToForEach);
        public static string ConvertForEachToLinqQuery => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertForEachToLinqQuery);
        public static string ConvertNumericLiteral => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertNumericLiteral);
        public static string IntroduceLocalForExpression => ApplyPrefix(PredefinedCodeRefactoringProviderNames.IntroduceLocalForExpression);
        public static string AddParameterCheck => ApplyPrefix(PredefinedCodeRefactoringProviderNames.AddParameterCheck);
        public static string InitializeMemberFromParameter => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter);
        public static string NameTupleElement => ApplyPrefix(PredefinedCodeRefactoringProviderNames.NameTupleElement);
        public static string UseNamedArguments => ApplyPrefix(PredefinedCodeRefactoringProviderNames.UseNamedArguments);
        public static string ConvertForToForEach => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertForToForEach);
        public static string ConvertIfToSwitch => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertIfToSwitch);
        public static string ConvertBetweenRegularAndVerbatimString => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString);
        public static string ConvertBetweenRegularAndVerbatimInterpolatedString => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString);
        public static string RenameTracking => ApplyPrefix(PredefinedCodeRefactoringProviderNames.RenameTracking);
        public static string UseExpressionBodyForLambda => ApplyPrefix(PredefinedCodeRefactoringProviderNames.UseExpressionBodyForLambda);
        public static string ImplementInterfaceExplicitly => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ImplementInterfaceExplicitly);
        public static string ImplementInterfaceImplicitly => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ImplementInterfaceImplicitly);
        public static string ConvertPlaceholderToInterpolatedString => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString);
        public static string ConvertConcatenationToInterpolatedString => ApplyPrefix(PredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString);
        public static string InvertMultiLineIf => ApplyPrefix(PredefinedCodeRefactoringProviderNames.InvertMultiLineIf);
    }
}

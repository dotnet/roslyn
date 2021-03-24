// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal static class RazorPredefinedCodeRefactoringProviderNames
    {
        public const string AddAwait = PredefinedCodeRefactoringProviderNames.AddAwait;
        public const string AddConstructorParametersFromMembers = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers;
        public const string AddFileBanner = PredefinedCodeRefactoringProviderNames.AddFileBanner;
        public const string AddMissingImports = PredefinedCodeRefactoringProviderNames.AddMissingImports;
        public const string ChangeSignature = PredefinedCodeRefactoringProviderNames.ChangeSignature;
        public const string ConvertAnonymousTypeToClass = PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass;
        public const string ConvertDirectCastToTryCast = PredefinedCodeRefactoringProviderNames.ConvertDirectCastToTryCast;
        public const string ConvertTryCastToDirectCast = PredefinedCodeRefactoringProviderNames.ConvertTryCastToDirectCast;
        public const string ConvertToInterpolatedString = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString;
        public const string ConvertTupleToStruct = PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct;
        public const string EncapsulateField = PredefinedCodeRefactoringProviderNames.EncapsulateField;
        public const string ExtractClass = PredefinedCodeRefactoringProviderNames.ExtractClass;
        public const string ExtractInterface = PredefinedCodeRefactoringProviderNames.ExtractInterface;
        public const string ExtractMethod = PredefinedCodeRefactoringProviderNames.ExtractMethod;
        public const string GenerateConstructorFromMembers = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers;
        public const string GenerateDefaultConstructors = PredefinedCodeRefactoringProviderNames.GenerateDefaultConstructors;
        public const string GenerateEqualsAndGetHashCodeFromMembers = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers;
        public const string GenerateOverrides = PredefinedCodeRefactoringProviderNames.GenerateOverrides;
        public const string InlineTemporary = PredefinedCodeRefactoringProviderNames.InlineTemporary;
        public const string IntroduceUsingStatement = PredefinedCodeRefactoringProviderNames.IntroduceUsingStatement;
        public const string IntroduceVariable = PredefinedCodeRefactoringProviderNames.IntroduceVariable;
        public const string InvertConditional = PredefinedCodeRefactoringProviderNames.InvertConditional;
        public const string InvertIf = PredefinedCodeRefactoringProviderNames.InvertIf;
        public const string InvertLogical = PredefinedCodeRefactoringProviderNames.InvertLogical;
        public const string MergeConsecutiveIfStatements = PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements;
        public const string MergeNestedIfStatements = PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements;
        public const string MoveDeclarationNearReference = PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference;
        public const string MoveToNamespace = PredefinedCodeRefactoringProviderNames.MoveToNamespace;
        public const string MoveTypeToFile = PredefinedCodeRefactoringProviderNames.MoveTypeToFile;
        public const string PullMemberUp = PredefinedCodeRefactoringProviderNames.PullMemberUp;
        public const string InlineMethod = PredefinedCodeRefactoringProviderNames.InlineMethod;
        public const string ReplaceDocCommentTextWithTag = PredefinedCodeRefactoringProviderNames.ReplaceDocCommentTextWithTag;
        public const string SplitIntoConsecutiveIfStatements = PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements;
        public const string SplitIntoNestedIfStatements = PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements;
        public const string SyncNamespace = PredefinedCodeRefactoringProviderNames.SyncNamespace;
        public const string UseExplicitType = PredefinedCodeRefactoringProviderNames.UseExplicitType;
        public const string UseExpressionBody = PredefinedCodeRefactoringProviderNames.UseExpressionBody;
        public const string UseImplicitType = PredefinedCodeRefactoringProviderNames.UseImplicitType;
        public const string Wrapping = PredefinedCodeRefactoringProviderNames.Wrapping;
        public const string MakeLocalFunctionStatic = PredefinedCodeRefactoringProviderNames.MakeLocalFunctionStatic;
        public const string GenerateComparisonOperators = PredefinedCodeRefactoringProviderNames.GenerateComparisonOperators;
        public const string ReplacePropertyWithMethods = PredefinedCodeRefactoringProviderNames.ReplacePropertyWithMethods;
        public const string ReplaceMethodWithProperty = PredefinedCodeRefactoringProviderNames.ReplaceMethodWithProperty;
        public const string AddDebuggerDisplay = PredefinedCodeRefactoringProviderNames.AddDebuggerDisplay;
        public const string ConvertAutoPropertyToFullProperty = PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty;
        public const string ReverseForStatement = PredefinedCodeRefactoringProviderNames.ReverseForStatement;
        public const string ConvertLocalFunctionToMethod = PredefinedCodeRefactoringProviderNames.ConvertLocalFunctionToMethod;
        public const string ConvertForEachToFor = PredefinedCodeRefactoringProviderNames.ConvertForEachToFor;
        public const string ConvertLinqQueryToForEach = PredefinedCodeRefactoringProviderNames.ConvertLinqQueryToForEach;
        public const string ConvertForEachToLinqQuery = PredefinedCodeRefactoringProviderNames.ConvertForEachToLinqQuery;
        public const string ConvertNumericLiteral = PredefinedCodeRefactoringProviderNames.ConvertNumericLiteral;
        public const string IntroduceLocalForExpression = PredefinedCodeRefactoringProviderNames.IntroduceLocalForExpression;
        public const string AddParameterCheck = PredefinedCodeRefactoringProviderNames.AddParameterCheck;
        public const string InitializeMemberFromParameter = PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter;
        public const string NameTupleElement = PredefinedCodeRefactoringProviderNames.NameTupleElement;
        public const string UseNamedArguments = PredefinedCodeRefactoringProviderNames.UseNamedArguments;
        public const string ConvertForToForEach = PredefinedCodeRefactoringProviderNames.ConvertForToForEach;
        public const string ConvertIfToSwitch = PredefinedCodeRefactoringProviderNames.ConvertIfToSwitch;
        public const string ConvertBetweenRegularAndVerbatimString = PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimString;
        public const string ConvertBetweenRegularAndVerbatimInterpolatedString = PredefinedCodeRefactoringProviderNames.ConvertBetweenRegularAndVerbatimInterpolatedString;
        public const string RenameTracking = PredefinedCodeRefactoringProviderNames.RenameTracking;
        public const string UseExpressionBodyForLambda = PredefinedCodeRefactoringProviderNames.UseExpressionBodyForLambda;
        public const string ImplementInterfaceExplicitly = PredefinedCodeRefactoringProviderNames.ImplementInterfaceExplicitly;
        public const string ImplementInterfaceImplicitly = PredefinedCodeRefactoringProviderNames.ImplementInterfaceImplicitly;
        public const string ConvertPlaceholderToInterpolatedString = PredefinedCodeRefactoringProviderNames.ConvertPlaceholderToInterpolatedString;
        public const string ConvertConcatenationToInterpolatedString = PredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString;
        public const string InvertMultiLineIf = PredefinedCodeRefactoringProviderNames.InvertMultiLineIf;
    }
}

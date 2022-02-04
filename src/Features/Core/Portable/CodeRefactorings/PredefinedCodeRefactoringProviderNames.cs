// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal static class PredefinedCodeRefactoringProviderNames
    {
        public const string AddAwait = "Add Await Code Action Provider";
        public const string AddConstructorParametersFromMembers = "Add Parameters From Members Code Action Provider";
        public const string AddDebuggerDisplay = nameof(AddDebuggerDisplay);
        public const string AddFileBanner = "Add Banner To File Code Action Provider";
        public const string AddMissingImports = "Add Missing Imports On Paste Code Action Provider";
        public const string AddParameterCheck = nameof(AddParameterCheck);
        public const string ChangeSignature = "Change Signature Code Action Provider";
        public const string ConvertAnonymousTypeToClass = "Convert Anonymous Type to Class Code Action Provider";
        public const string ConvertAnonymousTypeToTuple = "Convert Anonymous Type to Tuple Code Action Provider";
        public const string ConvertAutoPropertyToFullProperty = nameof(ConvertAutoPropertyToFullProperty);
        public const string ConvertBetweenRegularAndVerbatimInterpolatedString = nameof(ConvertBetweenRegularAndVerbatimInterpolatedString);
        public const string ConvertBetweenRegularAndVerbatimString = nameof(ConvertBetweenRegularAndVerbatimString);
        public const string ConvertConcatenationToInterpolatedString = nameof(ConvertConcatenationToInterpolatedString);
        public const string ConvertDirectCastToTryCast = "Convert Direct Cast to Try Cast";
        public const string ConvertForEachToFor = nameof(ConvertForEachToFor);
        public const string ConvertForEachToLinqQuery = nameof(ConvertForEachToLinqQuery);
        public const string ConvertForToForEach = nameof(ConvertForToForEach);
        public const string ConvertIfToSwitch = nameof(ConvertIfToSwitch);
        public const string ConvertLinqQueryToForEach = nameof(ConvertLinqQueryToForEach);
        public const string ConvertLocalFunctionToMethod = nameof(ConvertLocalFunctionToMethod);
        public const string ConvertNamespace = "Convert Namespace";
        public const string ConvertNumericLiteral = nameof(ConvertNumericLiteral);
        public const string ConvertPlaceholderToInterpolatedString = nameof(ConvertPlaceholderToInterpolatedString);
        public const string ConvertToInterpolatedString = "Convert To Interpolated String Code Action Provider";
        public const string ConvertToRawString = nameof(ConvertToRawString);
        public const string ConvertTryCastToDirectCast = "Convert Try Cast to Direct Cast";
        public const string ConvertTupleToStruct = "Convert Tuple to Struct Code Action Provider";
        public const string EnableNullable = "Enable Nullable Reference Types";
        public const string EncapsulateField = "Encapsulate Field";
        public const string ExtractClass = "Extract Class Code Action Provider";
        public const string ExtractInterface = "Extract Interface Code Action Provider";
        public const string ExtractMethod = "Extract Method Code Action Provider";
        public const string GenerateComparisonOperators = nameof(GenerateComparisonOperators);
        public const string GenerateConstructorFromMembers = "Generate Constructor From Members Code Action Provider";
        public const string GenerateDefaultConstructors = "Generate Default Constructors Code Action Provider";
        public const string GenerateEqualsAndGetHashCodeFromMembers = "Generate Equals and GetHashCode Code Action Provider";
        public const string GenerateOverrides = "Generate Overrides Code Action Provider";
        public const string ImplementInterfaceExplicitly = nameof(ImplementInterfaceExplicitly);
        public const string ImplementInterfaceImplicitly = nameof(ImplementInterfaceImplicitly);
        public const string InitializeMemberFromParameter = nameof(InitializeMemberFromParameter);
        public const string InlineMethod = "Inline Method Code Action Provider";
        public const string InlineTemporary = "Inline Temporary Code Action Provider";
        public const string IntroduceLocalForExpression = nameof(IntroduceLocalForExpression);
        public const string IntroduceParameter = nameof(IntroduceParameter);
        public const string IntroduceUsingStatement = "Introduce Using Statement Code Action Provider";
        public const string IntroduceVariable = "Introduce Variable Code Action Provider";
        public const string InvertConditional = "Invert Conditional Code Action Provider";
        public const string InvertIf = "Invert If Code Action Provider";
        public const string InvertLogical = "Invert Logical Code Action Provider";
        public const string InvertMultiLineIf = nameof(InvertMultiLineIf);
        public const string MakeLocalFunctionStatic = nameof(MakeLocalFunctionStatic);
        public const string MergeConsecutiveIfStatements = "Merge Consecutive If Statements Code Action Provider";
        public const string MergeNestedIfStatements = "Merge Nested If Statements Code Action Provider";
        public const string MoveDeclarationNearReference = "Move Declaration Near Reference Code Action Provider";
        public const string MoveStaticMembers = "Move Static Members to Another Type Code Action Provider";
        public const string MoveToNamespace = "Move To Namespace Code Action Provider";
        public const string MoveTypeToFile = "Move Type To File Code Action Provider";
        public const string NameTupleElement = nameof(NameTupleElement);
        public const string PullMemberUp = "Pull Member Up Code Action Provider";
        public const string RenameTracking = nameof(RenameTracking);
        public const string ReplaceDocCommentTextWithTag = "Replace Documentation Comment Text With Tag Code Action Provider";
        public const string ReplaceMethodWithProperty = nameof(ReplaceMethodWithProperty);
        public const string ReplacePropertyWithMethods = nameof(ReplacePropertyWithMethods);
        public const string ReverseForStatement = nameof(ReverseForStatement);
        public const string SplitIntoConsecutiveIfStatements = "Split Into Consecutive If Statements Code Action Provider";
        public const string SplitIntoNestedIfStatements = "Split Into Nested If Statements Code Action Provider";
        public const string SyncNamespace = "Sync Namespace and Folder Name Code Action Provider";
        public const string UseExplicitType = "Use Explicit Type Code Action Provider";
        public const string UseExpressionBody = "Use Expression Body Code Action Provider";
        public const string UseExpressionBodyForLambda = nameof(UseExpressionBodyForLambda);
        public const string UseImplicitType = "Use Implicit Type Code Action Provider";
        public const string UseNamedArguments = nameof(UseNamedArguments);
        public const string UseRecursivePatterns = nameof(UseRecursivePatterns);
        public const string Wrapping = "Wrapping Code Action Provider";
    }
}

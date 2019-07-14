// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class PredefinedCodeFixProviderNames
    {
        public const string AddDocCommentNodes = nameof(AddDocCommentNodes);
        public const string AddAwait = nameof(AddAwait);
        public const string AddAsync = nameof(AddAsync);
        public const string AddParameter = nameof(AddParameter);
        public const string AddParenthesesAroundConditionalExpressionInInterpolatedString = nameof(AddParenthesesAroundConditionalExpressionInInterpolatedString);
        public const string AliasAmbiguousType = nameof(AliasAmbiguousType);
        public const string ApplyNamingStyle = nameof(ApplyNamingStyle);
        public const string AddBraces = nameof(AddBraces);
        public const string ChangeReturnType = nameof(ChangeReturnType);
        public const string ChangeToYield = nameof(ChangeToYield);
        public const string ConfigureCodeStyleOption = nameof(ConfigureCodeStyleOption);
        public const string ConfigureSeverity = nameof(ConfigureSeverity);
        public const string ConvertToAsync = nameof(ConvertToAsync);
        public const string ConvertToIterator = nameof(ConvertToIterator);
        public const string CorrectNextControlVariable = nameof(CorrectNextControlVariable);
        public const string RemoveDocCommentNode = nameof(RemoveDocCommentNode);
        public const string AddMissingReference = nameof(AddMissingReference);
        public const string AddImport = nameof(AddImport);
        public const string FullyQualify = nameof(FullyQualify);
        public const string FixFormatting = nameof(FixFormatting);
        public const string FixIncorrectFunctionReturnType = nameof(FixIncorrectFunctionReturnType);
        public const string FixIncorrectExitContinue = nameof(FixIncorrectExitContinue);
        public const string FixReturnType = nameof(FixReturnType);
        public const string GenerateConstructor = nameof(GenerateConstructor);
        public const string GenerateEndConstruct = nameof(GenerateEndConstruct);
        public const string GenerateEnumMember = nameof(GenerateEnumMember);
        public const string GenerateEvent = nameof(GenerateEvent);
        public const string GenerateVariable = nameof(GenerateVariable);
        public const string GenerateMethod = nameof(GenerateMethod);
        public const string GenerateConversion = nameof(GenerateConversion);
        public const string GenerateDeconstructMethod = nameof(GenerateDeconstructMethod);
        public const string GenerateType = nameof(GenerateType);
        public const string ImplementAbstractClass = nameof(ImplementAbstractClass);
        public const string ImplementInterface = nameof(ImplementInterface);
        public const string InsertMissingCast = nameof(InsertMissingCast);
        public const string MakeFieldReadonly = nameof(MakeFieldReadonly);
        public const string MakeStatementAsynchronous = nameof(MakeStatementAsynchronous);
        public const string MakeMethodSynchronous = nameof(MakeMethodSynchronous);
        public const string MoveMisplacedUsingDirectives = nameof(MoveMisplacedUsingDirectives);
        public const string MoveToTopOfFile = nameof(MoveToTopOfFile);
        public const string PopulateSwitch = nameof(PopulateSwitch);
        public const string QualifyMemberAccess = nameof(QualifyMemberAccess);
        public const string ReplaceDefaultLiteral = nameof(ReplaceDefaultLiteral);
        public const string RemoveUnnecessaryCast = nameof(RemoveUnnecessaryCast);
        public const string DeclareAsNullable = nameof(DeclareAsNullable);
        public const string RemoveUnnecessaryImports = nameof(RemoveUnnecessaryImports);
        public const string RemoveUnreachableCode = nameof(RemoveUnreachableCode);
        public const string RemoveUnusedValues = nameof(RemoveUnusedValues);
        public const string RemoveUnusedLocalFunction = nameof(RemoveUnusedLocalFunction);
        public const string RemoveUnusedMembers = nameof(RemoveUnusedMembers);
        public const string RemoveUnusedVariable = nameof(RemoveUnusedVariable);
        public const string RenameTracking = nameof(RenameTracking);
        public const string SimplifyNames = nameof(SimplifyNames);
        public const string SimplifyThisOrMe = nameof(SimplifyThisOrMe);
        public const string SpellCheck = nameof(SpellCheck);
        public const string Suppression = nameof(Suppression);
        public const string AddOverloads = nameof(AddOverloads);
        public const string AddNew = nameof(AddNew);
        public const string UnsealClass = nameof(UnsealClass);
        public const string UseImplicitType = nameof(UseImplicitType);
        public const string UseExplicitType = nameof(UseExplicitType);
        public const string UseExplicitTypeForConst = nameof(UseExplicitTypeForConst);
        public const string UseCollectionInitializer = nameof(UseCollectionInitializer);
        public const string UseObjectInitializer = nameof(UseObjectInitializer);
        public const string UseThrowExpression = nameof(UseThrowExpression);
        public const string PreferFrameworkType = nameof(PreferFrameworkType);
        public const string MakeStructFieldsWritable = nameof(MakeStructFieldsWritable);
    }
}

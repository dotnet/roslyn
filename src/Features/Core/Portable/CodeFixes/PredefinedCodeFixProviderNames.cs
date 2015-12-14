// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class PredefinedCodeFixProviderNames
    {
        public const string AddAwait = "Add Await For Expression";
        public const string AddAsync = "Add Async To Member";
        public const string ChangeReturnType = "Change Return Type";
        public const string ChangeToYield = "Change To Yield";
        public const string ConvertToAsync = "Convert To Async";
        public const string ConvertToIterator = "Convert To Iterator";
        public const string CorrectNextControlVariable = "Correct Next Control Variable";
        public const string AddMissingReference = "Add Missing Reference";
        public const string AddUsingOrImport = "Add Using or Import";
        public const string FullyQualify = "Fully Qualify";
        public const string FixIncorrectFunctionReturnType = "Fix Incorrect Function Return Type";
        public const string FixIncorrectExitContinue = "Fix Incorrect Exit Continue";
        public const string GenerateConstructor = "Generate Constructor";
        public const string GenerateEndConstruct = "Generate End Construct";
        public const string GenerateEnumMember = "Generate Enum Member";
        public const string GenerateEvent = "Generate Event";
        public const string GenerateVariable = "Generate Variable";
        public const string GenerateMethod = "Generate Method";
        public const string GenerateConversion = "Generate Conversion";
        public const string GenerateType = "Generate Type";
        public const string ImplementAbstractClass = "Implement Abstract Class";
        public const string ImplementInterface = "Implement Interface";
        public const string InsertMissingCast = nameof(InsertMissingCast);
        public const string MoveToTopOfFile = "Move To Top Of File";
        public const string RemoveUnnecessaryCast = "Remove Unnecessary Casts";
        public const string RemoveUnnecessaryImports = "Remove Unnecessary Usings or Imports";
        public const string RenameTracking = "Rename Tracking";
        public const string SimplifyNames = "Simplify Names";
        public const string SpellCheck = "Spell Check";
        public const string Suppression = nameof(Suppression);
        public const string AddOverloads = "Add Overloads to member";
        public const string AddNew = "Add new keyword to member";
        public const string UseImplicitTyping = "Use var";
    }
}

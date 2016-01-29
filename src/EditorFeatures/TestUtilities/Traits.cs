// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Test.Utilities
{
    public static class Traits
    {
        public const string Editor = "Editor";
        public static class Editors
        {
            public const string KeyProcessors = "KeyProcessors";
            public const string KeyProcessorProviders = "KeyProcessorProviders";
            public const string Preview = "Preview";
        }

        public const string Feature = "Feature";
        public static class Features
        {
            public const string Adornments = "Adornments";
            public const string AsyncLazy = nameof(AsyncLazy);
            public const string AutomaticEndConstructCorrection = "AutomaticEndConstructCorrection";
            public const string AutomaticCompletion = "AutomaticCompletion";
            public const string BlockCommentCompletion = "BlockCommentCompletion";
            public const string BraceHighlighting = "BraceHighlighting";
            public const string BraceMatching = "BraceMatching";
            public const string CallHierarchy = "CallHierarchy";
            public const string CaseCorrection = "CaseCorrection";
            public const string ChangeSignature = "ChangeSignature";
            public const string Classification = "Classification";
            public const string ClassView = "ClassView";
            public const string CodeActionsAddConstructorParameters = "CodeActions.AddConstructorParameters";
            public const string CodeActionsAddAsync = "CodeActions.AddAsync";
            public const string CodeActionsAddAwait = "CodeActions.AddAwait";
            public const string CodeActionsAddImport = "CodeActions.AddImport";
            public const string CodeActionsAddMissingReference = "CodeActions.AddMissingReference";
            public const string CodeActionsAddUsing = "CodeActions.AddUsing";
            public const string CodeActionsChangeToAsync = "CodeActions.ChangeToAsync";
            public const string CodeActionsChangeToIEnumerable = "CodeActions.ChangeToIEnumerable";
            public const string CodeActionsChangeToYield = "CodeActions.ChangeToYield";
            public const string CodeActionsConvertToIterator = "CodeActions.CodeActionsConvertToIterator";
            public const string CodeActionsCorrectExitContinue = "CodeActions.CorrectExitContinue";
            public const string CodeActionsCorrectFunctionReturnType = "CodeActions.CorrectFunctionReturnType";
            public const string CodeActionsCorrectNextControlVariable = "CodeActions.CorrectNextControlVariable";
            public const string CodeActionsGenerateConstructor = "CodeActions.GenerateConstructor";
            public const string CodeActionsGenerateDefaultConstructors = "CodeActions.GenerateDefaultConstructors";
            public const string CodeActionsGenerateEndConstruct = "CodeActions.GenerateEndConstruct";
            public const string CodeActionsGenerateEnumMember = "CodeActions.GenerateEnumMember";
            public const string CodeActionsGenerateEvent = "CodeActions.GenerateEvent";
            public const string CodeActionsGenerateEqualsAndGetHashCode = "CodeActions.GenerateEqualsAndGetHashCode";
            public const string CodeActionsGenerateVariable = "CodeActions.GenerateVariable";
            public const string CodeActionsGenerateMethod = "CodeActions.GenerateMethod";
            public const string CodeActionsGenerateType = "CodeActions.GenerateType";
            public const string CodeActionsExtractMethod = "CodeActions.ExtractMethod";
            public const string CodeActionsFixAllOccurrences = "CodeActions.FixAllOccurrences";
            public const string CodeActionsFullyQualify = "CodeActions.FullyQualify";
            public const string CodeActionsImplementAbstractClass = "CodeActions.ImplementAbstractClass";
            public const string CodeActionsImplementInterface = "CodeActions.ImplementInterface";
            public const string CodeActionsInlineTemporary = "CodeActions.InlineTemporary";
            public const string CodeActionsInsertBraces = "CodeActions.InsertBraces";
            public const string CodeActionsInsertMissingCast = "CodeActions.InsertMissingCast";
            public const string CodeActionsInsertMissingTokens = "CodeActions.InsertMissingTokens";
            public const string CodeActionsIntroduceVariable = "CodeActions.IntroduceVariable";
            public const string CodeActionsInvertIf = "CodeActions.InvertIf";
            public const string CodeActionsInvokeDelegateWithConditionalAccess = "CodeActions.InvokeDelegateWithConditionalAccess";
            public const string CodeActionsLambdaSimplifier = "CodeActions.LambdaSimplifier";
            public const string CodeActionsMakeMethodSynchronous = "CodeActions.MakeMethodSynchronous";
            public const string CodeActionsMoveDeclarationNearReference = "CodeActions.MoveDeclarationNearReference";
            public const string CodeActionsMoveToTopOfFile = "CodeActions.MoveToTopOfFile";
            public const string CodeActionsReplaceMethodWithProperty = "CodeActions.ReplaceMethodWithProperty";
            public const string CodeActionsRemoveByVal = "CodeActions.RemoveByVal";
            public const string CodeActionsRemoveUnnecessaryCast = "CodeActions.RemoveUnnecessaryCast";
            public const string CodeActionsRemoveUnnecessaryImports = "CodeActions.RemoveUnnecessaryImports";
            public const string CodeActionsSimplifyTypeNames = "CodeActions.SimplifyTypeNames";
            public const string CodeActionsSpellcheck = "CodeActions.Spellcheck";
            public const string CodeActionsSuppression = "CodeActions.Suppression";
            public const string CodeActionsUseAutoProperty = "CodeActions.UseAutoProperty";
            public const string CodeGeneration = "CodeGeneration";
            public const string CodeGenerationSortDeclarations = "CodeGeneration.SortDeclarations";
            public const string CodeModel = "CodeModel";
            public const string CodeModelEvents = "CodeModel.Events";
            public const string CodeModelMethodXml = "CodeModel.MethodXml";
            public const string CommentSelection = "CommentSelection";
            public const string Completion = "Completion";
            public const string DebuggingBreakpoints = "Debugging.Breakpoints";
            public const string DebuggingDataTips = "Debugging.DataTips";
            public const string DebuggingIntelliSense = "Debugging.IntelliSense";
            public const string DebuggingLocationName = "Debugging.LocationName";
            public const string DebuggingNameResolver = "Debugging.NameResolver";
            public const string DebuggingProximityExpressions = "Debugging.ProximityExpressions";
            public const string Diagnostics = "Diagnostics";
            public const string DocCommentFormatting = "DocCommentFormatting";
            public const string DocumentationComments = "DocumentationComments";
            public const string EncapsulateField = "EncapsulateField";
            public const string EndConstructGeneration = "EndConstructGeneration";
            public const string ErrorSquiggles = "ErrorSquiggles";
            public const string EventHookup = "EventHookup";
            public const string Expansion = "Expansion";
            public const string ExtractInterface = "Refactoring.ExtractInterface";
            public const string ExtractMethod = "Refactoring.ExtractMethod";
            public const string FindReferences = "FindReferences";
            public const string F1Help = "F1Help";
            public const string Formatting = "Formatting";
            public const string GoToDefinition = "GoToDefinition";
            public const string GoToImplementation = "GoToImplementation";
            public const string GoToAdjacentMember = "GoToAdjacentMember";
            public const string Interactive = "Interactive";
            public const string InteractiveHost = "InteractiveHost";
            public const string KeywordHighlighting = "KeywordHighlighting";
            public const string KeywordRecommending = "KeywordRecommending";
            public const string LineCommit = "LineCommit";
            public const string LineSeparators = "LineSeparators";
            public const string MetadataAsSource = "MetadataAsSource";
            public const string NavigateTo = "NavigateTo";
            public const string NavigationBar = "NavigationBar";
            public const string ObjectBrowser = "ObjectBrowser";
            public const string Options = "Options";
            public const string Organizing = "Organizing";
            public const string Outlining = "Outlining";
            public const string Peek = "Peek";
            public const string Progression = "Progression";
            public const string ProjectSystemShims = "ProjectSystemShims";
            public const string QuickInfo = "QuickInfo";
            public const string ReferenceHighlighting = "ReferenceHighlighting";
            public const string Rename = "Rename";
            public const string RenameTracking = "RenameTracking";
            public const string RQName = "RQName";
            public const string SignatureHelp = "SignatureHelp";
            public const string Simplification = "Simplification";
            public const string SmartIndent = "SmartIndent";
            public const string SmartTokenFormatting = "SmartTokenFormatting";
            public const string Snippets = "Snippets";
            public const string TextStructureNavigator = "TextStructureNavigator";
            public const string TodoComments = "TodoComments";
            public const string TypeInferenceService = "TypeInferenceService";
            public const string Venus = "Venus";
            public const string VsLanguageBlock = "VsLanguageBlock";
            public const string VsNavInfo = "VsNavInfo";
            public const string XmlTagCompletion = "XmlTagCompletion";
            public const string CodeActionsAddOverload = "CodeActions.AddOverloads";
            public const string CodeActionsAddNew = "CodeActions.AddNew";
        }
    }
}

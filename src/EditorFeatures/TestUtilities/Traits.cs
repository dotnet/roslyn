// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Test.Utilities
{
    public static class Traits
    {
        public const string Editor = nameof(Editor);
        public static class Editors
        {
            public const string KeyProcessors = nameof(KeyProcessors);
            public const string KeyProcessorProviders = nameof(KeyProcessorProviders);
            public const string Preview = nameof(Preview);
        }

        public const string Feature = nameof(Feature);
        public static class Features
        {
            public const string Adornments = nameof(Adornments);
            public const string AsyncLazy = nameof(AsyncLazy);
            public const string AutomaticEndConstructCorrection = nameof(AutomaticEndConstructCorrection);
            public const string AutomaticCompletion = nameof(AutomaticCompletion);
            public const string BlockCommentEditing = nameof(BlockCommentEditing);
            public const string BraceHighlighting = nameof(BraceHighlighting);
            public const string BraceMatching = nameof(BraceMatching);
            public const string CallHierarchy = nameof(CallHierarchy);
            public const string CaseCorrection = nameof(CaseCorrection);
            public const string ChangeSignature = nameof(ChangeSignature);
            public const string Classification = nameof(Classification);
            public const string ClassView = nameof(ClassView);
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
            public const string CodeActionsUseImplicitTyping = "CodeActions.UseImplicitTyping";
            public const string CodeActionsUseExplicitTyping = "CodeActions.UseExplicitTyping";
            public const string CodeGeneration = nameof(CodeGeneration);
            public const string CodeGenerationSortDeclarations = "CodeGeneration.SortDeclarations";
            public const string CodeModel = nameof(CodeModel);
            public const string CodeModelEvents = "CodeModel.Events";
            public const string CodeModelMethodXml = "CodeModel.MethodXml";
            public const string CommentSelection = nameof(CommentSelection);
            public const string Completion = nameof(Completion);
            public const string DebuggingBreakpoints = "Debugging.Breakpoints";
            public const string DebuggingDataTips = "Debugging.DataTips";
            public const string DebuggingIntelliSense = "Debugging.IntelliSense";
            public const string DebuggingLocationName = "Debugging.LocationName";
            public const string DebuggingNameResolver = "Debugging.NameResolver";
            public const string DebuggingProximityExpressions = "Debugging.ProximityExpressions";
            public const string Diagnostics = nameof(Diagnostics);
            public const string DocCommentFormatting = nameof(DocCommentFormatting);
            public const string DocumentationComments = nameof(DocumentationComments);
            public const string EncapsulateField = nameof(EncapsulateField);
            public const string EndConstructGeneration = nameof(EndConstructGeneration);
            public const string ErrorSquiggles = nameof(ErrorSquiggles);
            public const string EventHookup = nameof(EventHookup);
            public const string Expansion = nameof(Expansion);
            public const string ExtractInterface = "Refactoring.ExtractInterface";
            public const string ExtractMethod = "Refactoring.ExtractMethod";
            public const string FindReferences = nameof(FindReferences);
            public const string F1Help = nameof(F1Help);
            public const string Formatting = nameof(Formatting);
            public const string GoToDefinition = nameof(GoToDefinition);
            public const string GoToImplementation = nameof(GoToImplementation);
            public const string GoToAdjacentMember = nameof(GoToAdjacentMember);
            public const string Interactive = nameof(Interactive);
            public const string InteractiveHost = nameof(InteractiveHost);
            public const string KeywordHighlighting = nameof(KeywordHighlighting);
            public const string KeywordRecommending = nameof(KeywordRecommending);
            public const string LineCommit = nameof(LineCommit);
            public const string LineSeparators = nameof(LineSeparators);
            public const string MetadataAsSource = nameof(MetadataAsSource);
            public const string NavigateTo = nameof(NavigateTo);
            public const string NavigationBar = nameof(NavigationBar);
            public const string ObjectBrowser = nameof(ObjectBrowser);
            public const string Options = nameof(Options);
            public const string Organizing = nameof(Organizing);
            public const string Outlining = nameof(Outlining);
            public const string Peek = nameof(Peek);
            public const string Progression = nameof(Progression);
            public const string ProjectSystemShims = nameof(ProjectSystemShims);
            public const string QuickInfo = nameof(QuickInfo);
            public const string ReferenceHighlighting = nameof(ReferenceHighlighting);
            public const string Rename = nameof(Rename);
            public const string RenameTracking = nameof(RenameTracking);
            public const string RQName = nameof(RQName);
            public const string SignatureHelp = nameof(SignatureHelp);
            public const string Simplification = nameof(Simplification);
            public const string SmartIndent = nameof(SmartIndent);
            public const string SmartTokenFormatting = nameof(SmartTokenFormatting);
            public const string Snippets = nameof(Snippets);
            public const string TextStructureNavigator = nameof(TextStructureNavigator);
            public const string TodoComments = nameof(TodoComments);
            public const string TypeInferenceService = nameof(TypeInferenceService);
            public const string Venus = nameof(Venus);
            public const string VsLanguageBlock = nameof(VsLanguageBlock);
            public const string VsNavInfo = nameof(VsNavInfo);
            public const string XmlTagCompletion = nameof(XmlTagCompletion);
            public const string CodeActionsAddOverload = "CodeActions.AddOverloads";
            public const string CodeActionsAddNew = "CodeActions.AddNew";
        }
    }
}

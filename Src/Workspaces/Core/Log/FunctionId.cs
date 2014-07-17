// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Enum to uniquely identify each function location.
    /// </summary>
    internal enum FunctionId
    {
        [PerfGoal(InteractionClass.Instant)]
        AdornmentManager_OnLayoutChanged = 1,

        [PerfGoal(InteractionClass.Instant)]
        AdornmentManager_UpdateInvalidSpans,

        AsyncFix,

        AsynchronousTagger_UIUpdateTask,
        Tagging_TagSource_RecomputeTags,
        Tagging_TagSource_ProcessNewTags,
        AsynchronousViewTagger_NotifyEditorOfChangedTags,

        BraceHighlighting_ProduceTags,

        CaseCorrection_AbstractCaseCorrectionService_CaseCorrect,
        CaseCorrection_AbstractCaseCorrectionService_CaseCorrect_ReplaceTokens,
        CaseCorrection_AbstractCaseCorrectionService_CaseCorrect_AddReplacements,

        CodeAction_AddRefactoring,
        CodeAction_AddIssues_Node,
        CodeAction_AddIssues_Token,

        CodeCleanup_CleanupAsync,
        CodeCleanup_Cleanup,
        CodeCleanup_IterateAllCodeCleanupProviders,
        CodeCleanup_IterateOneCodeCleanup,

        CommandHandler_GetCommandState,
        CommandHandler_ExecuteHandlers,

        Document_GetSemanticModel,
        Document_GetSyntaxTree,
        Document_GetTextChanges,

        DocumentState_FullyParseSyntaxTree,
        DocumentState_IncrementallyParseSyntaxTree,

        EndConstruct_DoStatement,
        EndConstruct_XmlCData,
        EndConstruct_XmlComment,
        EndConstruct_XmlElement,
        EndConstruct_XmlEmbeddedExpression,
        EndConstruct_XmlProcessingInstruction,

        Squiggles_SquiggleTagProducer_ProduceTags,

        EditorTestApp_RefreshTask,
        EditorTestApp_UpdateDiagnostics,

        FindReference_Rename,
        FindReference_ChangeSignature,
        FindReference_Start,
        FindReference_DetermineAllSymbolsAsync,
        FindReference_CreateProjectMapAsync,
        FindReference_CreateDocumentMapAsync,
        FindReference_ProcessAsync,
        FindReference_ProcessProjectAsync,
        FindReference_ProcessDocumentAsync,

        Formatting_TokenStreamConstruction,
        Formatting_ContextInitialization,
        Formatting_Format,
        Formatting_ApplyResultToBuffer,
        Formatting_IterateNodes,
        Formatting_CollectIndentBlock,
        Formatting_CollectSuppressOperation,
        Formatting_CollectAlignOperation,
        Formatting_CollectAnchorOperation,
        Formatting_CollectTokenOperation,
        Formatting_BuildContext,
        Formatting_ApplySpaceAndLine,
        Formatting_ApplyAnchorOperation,
        Formatting_ApplyAlignOperation,
        Formatting_AggregateCreateTextChanges,
        Formatting_AggregateCreateFormattedRoot,
        Formatting_CreateTextChanges,
        Formatting_CreateFormattedRoot,

        Highlighting_HighlighterTagProducer_ProduceTags,

        Completion_AbstractKeywordCompletionProvider_GetItemsWorker,
        Intellisense_AsyncCompletionSet,

        CodeActions_IssueProducer_AddNewItemsWorker,
        CodeActions_RefactoringProducer_AddNewItemsWorker,

        LineSeparators_TagProducer_ProduceTags,

        NavigateTo_Search,

        Rename_InlineSession,
        Rename_FindLinkedSpans,
        Rename_GetSymbolRenameInfo,
        Rename_OnTextBufferChanged,
        Rename_ApplyReplacementText,
        Rename_CommitCore,
        Rename_CommitCoreWithPreview,
        Rename_GetAsynchronousLocationsSource,
        Rename_AllRenameLocations,
        Rename_StartSearchingForSpansInAllOpenDocuments,
        Rename_StartSearchingForSpansInOpenDocument,
        Rename_CreateOpenTextBufferManagerForAllOpenDocs,
        Rename_CreateOpenTextBufferManagerForAllOpenDocument,
        Rename_ReportSpan,
        Rename_GetNoChangeConflictResolution,

        SmartIndentation_Start,
        SmartIndentation_OpenCurly,
        SmartIndentation_CloseCurly,

        TPLTask_TaskScheduled,
        TPLTask_TaskStarted,
        TPLTask_TaskCompleted,

        Utilities_NonReentrantLock_BlockingWait,

        VisualStudioWaitIndicator_Wait,

        PersistenceService_ReadAsync,
        PersistenceService_WriteAsync,

        Project_GetCompilation,

        Rename_Tracking_Buffer_Changed,

        Host_TemporaryStorageServiceFactory_ReadText,
        Host_TemporaryStorageServiceFactory_WriteText,
        Host_TemporaryStorageServiceFactory_ReadStream,
        Host_TemporaryStorageServiceFactory_WriteStream,

        Winform_Designer_Generate_XML,

        Workspace_ApplyChanges,
        Workspace_TryGetDocument,
        Workspace_TryGetDocumentFromInProgressSolution,

        Classification_TagProducer_ProduceTags,

        CodeHierarchy_SearchForField,
        CodeHierarchy_FindMethodOrPropertyOrEventImplements,
        CodeHierarchy_FindMethodOrPropertyOrEventImplementedBy,
        CodeHierarchy_FindMethodOrPropertyOrEventOverrides,
        CodeHierarchy_FindMethodOrPropertyOrEventOverriddenBy,
        CodeHierarchy_FindMethodOrPropertyOrEventCallers,
        CodeHierarchy_SearchForMethodOrPropertyOrEvent,
        CodeHierarchy_SearchForNamedType,
        CodeHierarchy_SearchForNamedTypeParts,
        CodeHierarchy_SearchForNamedTypeImplements,
        CodeHierarchy_SearchForNamedTypeImplementedBy,
        CodeHierarchy_SearchForNamedTypeInherits,
        CodeHierarchy_SearchForNamedTypeInheritedBy,

        CSharp_Completion_SymbolCompletionProvider_GetItemsWorker,
        VisualBasic_Completion_SymbolCompletionProvider_GetItemsWorker,
        AddImport_AbstractAddImportService_AddImport,
        FullyQualify_AbstractFullyQualifyService_FullyQualify,
        GenerateFromMembers_AddConstructorParameters_AbstractAddConstructorParametersService_AddConstructorParameters,
        GenerateFromMembers_GenerateConstructor_AbstractGenerateConstructorService_GenerateConstructor,
        GenerateFromMembers_GenerateEqualsAndGetHashCode_AbstractGenerateEqualsAndGetHashCodeService_GenerateEqualsAndGetHashCode,
        GenerateMember_GenerateConstructor_AbstractGenerateConstructorService_GenerateConstructor,
        GenerateMember_GenerateDefaultConstructors_AbstractGenerateDefaultConstructorsService_GenerateDefaultConstructors,
        GenerateMember_GenerateEnumMember_AbstractGenerateEnumMemberService_GenerateEnumMember,
        GenerateMember_GenerateMethod_AbstractGenerateMethodService_GenerateMethod,
        GenerateMember_GenerateVariable_AbstractGenerateVariableService_GenerateVariable,
        ImplementAbstractClass_AbstractImplementAbstractClassService_ImplementAbstractClass,
        ImplementInterface_AbstractImplementInterfaceService_GetCodeIssue,
        ImplementInterface_AbstractImplementInterfaceService_ImplementInterface,
        IntroduceVariable_AbstractIntroduceVariableService_IntroduceVariable,

        CSharp_RemoveUnnecessaryImports_CSharpRemoveUnnecessaryImportsService_RemoveUnnecessaryImports,
        VisualBasic_RemoveUnnecessaryImports_VisualBasicRemoveUnnecessaryImportsService_RemoveUnnecessaryImports,
        GenerateType_AbstractGenerateTypeService_GenerateType,

        Debugging_AbstractLanguageDebugInfoService_GetDataTipSpanAndText,
        Debugging_VsLanguageDebugInfo_ValidateBreakpointLocation,
        Debugging_VsLanguageDebugInfo_GetProximityExpressions,
        Debugging_VsLanguageDebugInfo_ResolveName,
        Debugging_VsLanguageDebugInfo_GetNameOfLocation,
        Debugging_VsLanguageDebugInfo_GetDataTipText,

        Completion_ModelComputer_DoInBackground,
        Completion_ModelComputation_FilterModelInBackground,
        Completion_ModelComputation_WaitForModel,

        SignatureHelp_ModelComputation_ComputeModelInBackground,
        SignatureHelp_ModelComputation_UpdateModelInBackground,

        QuickInfo_ModelComputation_ComputeModelInBackground,

        DocumentProvider_OnBeforeSave,

        Outlining_TagProducer_ProduceTags,

        ReferenceHighlighting_TagProducer_ProduceTags,

        TextStructureNavigator_GetExtentOfWord,
        TextStructureNavigator_GetSpanOfEnclosing,
        TextStructureNavigator_GetSpanOfFirstChild,
        TextStructureNavigator_GetSpanOfNextSibling,
        TextStructureNavigator_GetSpanOfPreviousSibling,

        CSharp_NavigationBarItemService_GetMembersInTypes,
        CSharp_NavigationBarItemService_GetTypesInFile,

        NavigationBar_UpdateDropDownsSynchronously_WaitForModel,
        NavigationBar_UpdateDropDownsSynchronously_WaitForSelectedItemInfo,
        Host_BackgroundCompiler_BuildCompilationsAsync,

        Event_Hookup_Determine_If_Event_Hookup,
        Event_Hookup_Generate_Handler,
        Event_Hookup_Type_Char,

        // a value to use in unit tests that won't interfere with reporting
        // for our other scenarios.
        TestEvent_NotUsed,

        SmartTags_SmartTagInitializeFixes,
        SmartTags_ApplyQuickFix,

        Cache_Created,
        Cache_AddOrAccess,
        Cache_Remove,
        Cache_Evict,
        Cache_EvictAll,
        Cache_ItemRank,

        SaveEventsSink_OnBeforeSave,
        CSharp_Completion_SnippetCompletionProvider_GetItemsWorker,

        CompilationTracker_BuildCompilationAsync,
        TaskList_Refresh,

        WorkCoordinator_DocumentWorker_Enqueue,
        WorkCoordinator_ProcessProjectAsync,
        WorkCoordinator_ProcessDocumentAsync,
        WorkCoordinator_SemanticChange_Enqueue,
        WorkCoordinator_SemanticChange_EnqueueFromMember,
        WorkCoordinator_SemanticChange_EnqueueFromType,
        WorkCoordinator_SemanticChange_FullProjects,
        WorkCoordinator_Project_Enqueue,

        Text_GetChangeRanges,

        ForegroundNotificationService_Processed,
        ForegroundNotificationService_NotifyOnForeground,

        LineCommit_CommitRegion,
        NavigationBar_ComputeModelAsync,

        AsyncWorkItemQueue_LastItem,
        AsyncWorkItemQueue_FirstItem,

        Snippet_OnBeforeInsertion,
        Snippet_OnAfterInsertion,

        Classification_TagComputer_GetTags,
        SmartTags_RefreshSession,

        PersistenceService_ReadAsyncFailed,
        PersistenceService_WriteAsyncFailed,
        PersistenceService_Initialization,

        SkeletonAssembly_GetMetadataOnlyImage,
        SkeletonAssembly_EmitMetadataOnlyImage,
        Simplifier_ReduceAsync,
        Simplifier_ExpandNode,
        Simplifier_ExpandToken,
        SmartTags_Preview,
        Recoverable_RecoverRootAsync,
        Recoverable_RecoverRoot,
        Recoverable_RecoverTextAsync,
        Recoverable_RecoverText,

        TaskList_VSTaskItemBase_NavigateTo,
        NavigationService_VSDocumentNavigationService_NavigateTo,

        Diagnostics_SyntaxDiagnostic,
        Diagnostics_SemanticDiagnostic,
        Diagnostics_ProjectDiagnostic,
        Diagnostics_DocumentReset,
        Diagnostics_DocumentOpen,
        Diagnostics_RemoveDocument,
        Diagnostics_RemoveProject,
    }
}
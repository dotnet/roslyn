﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Enum to uniquely identify each function location.
    /// </summary>
    internal enum FunctionId
    {
        // a value to use in unit tests that won't interfere with reporting
        // for our other scenarios.
        TestEvent_NotUsed = 1,

        WorkCoordinator_DocumentWorker_Enqueue,
        WorkCoordinator_ProcessProjectAsync,
        WorkCoordinator_ProcessDocumentAsync,
        WorkCoordinator_SemanticChange_Enqueue,
        WorkCoordinator_SemanticChange_EnqueueFromMember,
        WorkCoordinator_SemanticChange_EnqueueFromType,
        WorkCoordinator_SemanticChange_FullProjects,
        WorkCoordinator_Project_Enqueue,
        WorkCoordinator_AsyncWorkItemQueue_LastItem,
        WorkCoordinator_AsyncWorkItemQueue_FirstItem,

        Diagnostics_SyntaxDiagnostic,
        Diagnostics_SemanticDiagnostic,
        Diagnostics_ProjectDiagnostic,
        Diagnostics_DocumentReset,
        Diagnostics_DocumentOpen,
        Diagnostics_RemoveDocument,
        Diagnostics_RemoveProject,
        Diagnostics_DocumentClose,

        // add new values after this
        Run_Environment,
        Run_Environment_Options,

        Tagger_AdornmentManager_OnLayoutChanged,
        Tagger_AdornmentManager_UpdateInvalidSpans,
        Tagger_BatchChangeNotifier_NotifyEditorNow,
        Tagger_BatchChangeNotifier_NotifyEditor,
        Tagger_TagSource_RecomputeTags,
        Tagger_TagSource_ProcessNewTags,
        Tagger_SyntacticClassification_TagComputer_GetTags,
        Tagger_SemanticClassification_TagProducer_ProduceTags,
        Tagger_BraceHighlighting_TagProducer_ProduceTags,
        Tagger_LineSeparator_TagProducer_ProduceTags,
        Tagger_Outlining_TagProducer_ProduceTags,
        Tagger_Highlighter_TagProducer_ProduceTags,
        Tagger_ReferenceHighlighting_TagProducer_ProduceTags,

        CaseCorrection_CaseCorrect,
        CaseCorrection_ReplaceTokens,
        CaseCorrection_AddReplacements,

        CodeCleanup_CleanupAsync,
        CodeCleanup_Cleanup,
        CodeCleanup_IterateAllCodeCleanupProviders,
        CodeCleanup_IterateOneCodeCleanup,

        CommandHandler_GetCommandState,
        CommandHandler_ExecuteHandlers,
        CommandHandler_FormatCommand,
        CommandHandler_CompleteStatement,
        CommandHandler_ToggleBlockComment,
        CommandHandler_ToggleLineComment,

        Workspace_SourceText_GetChangeRanges,
        Workspace_Recoverable_RecoverRootAsync,
        Workspace_Recoverable_RecoverRoot,
        Workspace_Recoverable_RecoverTextAsync,
        Workspace_Recoverable_RecoverText,
        Workspace_SkeletonAssembly_GetMetadataOnlyImage,
        Workspace_SkeletonAssembly_EmitMetadataOnlyImage,
        Workspace_Document_State_FullyParseSyntaxTree,
        Workspace_Document_State_IncrementallyParseSyntaxTree,
        Workspace_Document_GetSemanticModel,
        Workspace_Document_GetSyntaxTree,
        Workspace_Document_GetTextChanges,
        Workspace_Project_GetCompilation,
        Workspace_Project_CompilationTracker_BuildCompilationAsync,
        Workspace_ApplyChanges,
        Workspace_TryGetDocument,
        Workspace_TryGetDocumentFromInProgressSolution,
        Workspace_Solution_LinkedFileDiffMergingSession,
        Workspace_Solution_LinkedFileDiffMergingSession_LinkedFileGroup,
        Workspace_Solution_Info,

        EndConstruct_DoStatement,
        EndConstruct_XmlCData,
        EndConstruct_XmlComment,
        EndConstruct_XmlElement,
        EndConstruct_XmlEmbeddedExpression,
        EndConstruct_XmlProcessingInstruction,

        FindReference_Rename,
        FindReference_ChangeSignature,
        FindReference,
        FindReference_DetermineAllSymbolsAsync,
        FindReference_CreateProjectMapAsync,
        FindReference_CreateDocumentMapAsync,
        FindReference_ProcessAsync,
        FindReference_ProcessProjectAsync,
        FindReference_ProcessDocumentAsync,

        LineCommit_CommitRegion,

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
        Formatting_Partitions,

        SmartIndentation_Start,
        SmartIndentation_OpenCurly,
        SmartIndentation_CloseCurly,

        Rename_InlineSession,
        Rename_InlineSession_Session,
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
        Rename_Tracking_BufferChanged,

        TPLTask_TaskScheduled,
        TPLTask_TaskStarted,
        TPLTask_TaskCompleted,

        Get_QuickInfo_Async,

        Completion_ModelComputer_DoInBackground,
        Completion_ModelComputation_FilterModelInBackground,
        Completion_ModelComputation_WaitForModel,
        Completion_SymbolCompletionProvider_GetItemsWorker,
        Completion_KeywordCompletionProvider_GetItemsWorker,
        Completion_SnippetCompletionProvider_GetItemsWorker_CSharp,
        Completion_TypeImportCompletionProvider_GetCompletionItemsAsync,
        Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync,

        SignatureHelp_ModelComputation_ComputeModelInBackground,
        SignatureHelp_ModelComputation_UpdateModelInBackground,

        Refactoring_CodeRefactoringService_GetRefactoringsAsync,
        Refactoring_AddImport,
        Refactoring_FullyQualify,
        Refactoring_GenerateFromMembers_AddConstructorParametersFromMembers,
        Refactoring_GenerateFromMembers_GenerateConstructorFromMembers,
        Refactoring_GenerateFromMembers_GenerateEqualsAndGetHashCode,
        Refactoring_GenerateMember_GenerateConstructor,
        Refactoring_GenerateMember_GenerateDefaultConstructors,
        Refactoring_GenerateMember_GenerateEnumMember,
        Refactoring_GenerateMember_GenerateMethod,
        Refactoring_GenerateMember_GenerateVariable,
        Refactoring_ImplementAbstractClass,
        Refactoring_ImplementInterface,
        Refactoring_IntroduceVariable,
        Refactoring_GenerateType,
        Refactoring_RemoveUnnecessaryImports_CSharp,
        Refactoring_RemoveUnnecessaryImports_VisualBasic,

        Snippet_OnBeforeInsertion,
        Snippet_OnAfterInsertion,

        Misc_NonReentrantLock_BlockingWait,
        Misc_VisualStudioWaitIndicator_Wait,
        Misc_SaveEventsSink_OnBeforeSave,

        TaskList_Refresh,
        TaskList_NavigateTo,

        WinformDesigner_GenerateXML,

        NavigateTo_Search,

        NavigationService_VSDocumentNavigationService_NavigateTo,

        NavigationBar_ComputeModelAsync,
        NavigationBar_ItemService_GetMembersInTypes_CSharp,
        NavigationBar_ItemService_GetTypesInFile_CSharp,
        NavigationBar_UpdateDropDownsSynchronously_WaitForModel,
        NavigationBar_UpdateDropDownsSynchronously_WaitForSelectedItemInfo,

        EventHookup_Determine_If_Event_Hookup,
        EventHookup_Generate_Handler,
        EventHookup_Type_Char,

        Cache_Created,
        Cache_AddOrAccess,
        Cache_Remove,
        Cache_Evict,
        Cache_EvictAll,
        Cache_ItemRank,

        TextStructureNavigator_GetExtentOfWord,
        TextStructureNavigator_GetSpanOfEnclosing,
        TextStructureNavigator_GetSpanOfFirstChild,
        TextStructureNavigator_GetSpanOfNextSibling,
        TextStructureNavigator_GetSpanOfPreviousSibling,

        Debugging_LanguageDebugInfoService_GetDataTipSpanAndText,
        Debugging_VsLanguageDebugInfo_ValidateBreakpointLocation,
        Debugging_VsLanguageDebugInfo_GetProximityExpressions,
        Debugging_VsLanguageDebugInfo_ResolveName,
        Debugging_VsLanguageDebugInfo_GetNameOfLocation,
        Debugging_VsLanguageDebugInfo_GetDataTipText,
        Debugging_EncSession,
        Debugging_EncSession_EditSession,
        Debugging_EncSession_EditSession_EmitDeltaErrorId,
        Debugging_EncSession_EditSession_RudeEdit,

        Simplifier_ReduceAsync,
        Simplifier_ExpandNode,
        Simplifier_ExpandToken,

        ForegroundNotificationService_Processed,
        ForegroundNotificationService_NotifyOnForeground,

        BackgroundCompiler_BuildCompilationsAsync,

        PersistenceService_ReadAsync,
        PersistenceService_WriteAsync,
        PersistenceService_ReadAsyncFailed,
        PersistenceService_WriteAsyncFailed,
        PersistenceService_Initialization,

        TemporaryStorageServiceFactory_ReadText,
        TemporaryStorageServiceFactory_WriteText,
        TemporaryStorageServiceFactory_ReadStream,
        TemporaryStorageServiceFactory_WriteStream,

        PullMembersUpWarning_ChangeTargetToAbstract,
        PullMembersUpWarning_ChangeOriginToPublic,
        PullMembersUpWarning_ChangeOriginToNonStatic,
        PullMembersUpWarning_UserProceedToFinish,
        PullMembersUpWarning_UserGoBack,

        // currently no-one uses these
        SmartTags_RefreshSession,
        SmartTags_SmartTagInitializeFixes,
        SmartTags_ApplyQuickFix,

        EditorTestApp_RefreshTask,
        EditorTestApp_UpdateDiagnostics,

        IncrementalAnalyzerProcessor_Analyzers,
        IncrementalAnalyzerProcessor_Analyzer,
        IncrementalAnalyzerProcessor_ActiveFileAnalyzers,
        IncrementalAnalyzerProcessor_ActiveFileAnalyzer,
        IncrementalAnalyzerProcessor_Shutdown,

        WorkCoordinatorRegistrationService_Register,
        WorkCoordinatorRegistrationService_Unregister,
        WorkCoordinatorRegistrationService_Reanalyze,

        WorkCoordinator_SolutionCrawlerOption,
        WorkCoordinator_PersistentStorageAdded,
        WorkCoordinator_PersistentStorageRemoved,
        WorkCoordinator_Shutdown,

        DiagnosticAnalyzerService_Analyzers,
        DiagnosticAnalyzerDriver_AnalyzerCrash,
        DiagnosticAnalyzerDriver_AnalyzerTypeCount,
        PersistedSemanticVersion_Info,
        StorageDatabase_Exceptions,
        WorkCoordinator_ShutdownTimeout,
        Diagnostics_HyperLink,

        CodeFixes_FixAllOccurrencesSession,
        CodeFixes_FixAllOccurrencesContext,
        CodeFixes_FixAllOccurrencesComputation,
        CodeFixes_FixAllOccurrencesComputation_Document_Diagnostics,
        CodeFixes_FixAllOccurrencesComputation_Project_Diagnostics,
        CodeFixes_FixAllOccurrencesComputation_Document_Fixes,
        CodeFixes_FixAllOccurrencesComputation_Project_Fixes,
        CodeFixes_FixAllOccurrencesComputation_Document_Merge,
        CodeFixes_FixAllOccurrencesComputation_Project_Merge,
        CodeFixes_FixAllOccurrencesPreviewChanges,
        CodeFixes_ApplyChanges,

        SolutionExplorer_AnalyzerItemSource_GetItems,
        SolutionExplorer_DiagnosticItemSource_GetItems,
        WorkCoordinator_ActiveFileEnqueue,
        SymbolFinder_FindDeclarationsAsync,
        SymbolFinder_Project_AddDeclarationsAsync,
        SymbolFinder_Assembly_AddDeclarationsAsync,
        SymbolFinder_Solution_Name_FindSourceDeclarationsAsync,
        SymbolFinder_Project_Name_FindSourceDeclarationsAsync,
        SymbolFinder_Solution_Predicate_FindSourceDeclarationsAsync,
        SymbolFinder_Project_Predicate_FindSourceDeclarationsAsync,
        Tagger_Diagnostics_RecomputeTags,
        Tagger_Diagnostics_Updated,
        SuggestedActions_HasSuggestedActionsAsync,
        SuggestedActions_GetSuggestedActions,
        AnalyzerDependencyCheckingService_LogConflict,
        AnalyzerDependencyCheckingService_LogMissingDependency,
        VirtualMemory_MemoryLow,
        Extension_Exception,

        WorkCoordinator_WaitForHigherPriorityOperationsAsync,

        CSharp_Interactive_Window,
        VisualBasic_Interactive_Window,

        NonFatalWatson,
        GlobalOperationRegistration,
        CommandHandler_FindAllReference,

        CodefixInfobar_Enable,
        CodefixInfobar_EnableAndIgnoreFutureErrors,
        CodefixInfobar_LeaveDisabled,
        CodefixInfobar_ErrorIgnored,

        Refactoring_NamingStyle,

        // Caches
        SymbolTreeInfo_ExceptionInCacheRead,
        SpellChecker_ExceptionInCacheRead,
        BKTree_ExceptionInCacheRead,
        IntellisenseBuild_Failed,

        FileTextLoader_FileLengthThresholdExceeded,

        // Generic performance measurement action IDs
        MeasurePerformance_StartAction,
        MeasurePerformance_StopAction,

        Serializer_CreateChecksum,
        Serializer_Serialize,
        Serializer_Deserialize,

        CodeAnalysisService_CalculateDiagnosticsAsync,
        CodeAnalysisService_SerializeDiagnosticResultAsync,
        CodeAnalysisService_GetReferenceCountAsync,
        CodeAnalysisService_FindReferenceLocationsAsync,
        CodeAnalysisService_FindReferenceMethodsAsync,
        CodeAnalysisService_GetFullyQualifiedName,
        CodeAnalysisService_GetTodoCommentsAsync,
        CodeAnalysisService_GetDesignerAttributesAsync,

        ServiceHubRemoteHostClient_CreateAsync,
        PinnedRemotableDataScope_GetRemotableData,

        RemoteHost_Connect,
        RemoteHost_Disconnect,

        RemoteHostClientService_AddGlobalAssetsAsync,
        RemoteHostClientService_RemoveGlobalAssets,
        RemoteHostClientService_Enabled,
        RemoteHostClientService_Restarted,

        RemoteHostService_SynchronizePrimaryWorkspaceAsync,
        RemoteHostService_SynchronizeGlobalAssetsAsync,

        AssetStorage_CleanAssets,
        AssetStorage_TryGetAsset,

        AssetService_GetAssetAsync,
        AssetService_SynchronizeAssetsAsync,
        AssetService_SynchronizeSolutionAssetsAsync,
        AssetService_SynchronizeProjectAssetsAsync,

        CodeLens_GetReferenceCountAsync,
        CodeLens_FindReferenceLocationsAsync,
        CodeLens_FindReferenceMethodsAsync,
        CodeLens_GetFullyQualifiedName,

        SolutionState_ComputeChecksumsAsync,
        ProjectState_ComputeChecksumsAsync,
        DocumentState_ComputeChecksumsAsync,

        SolutionSynchronizationService_GetRemotableData,
        SolutionSynchronizationServiceFactory_CreatePinnedRemotableDataScopeAsync,

        SolutionChecksumUpdater_SynchronizePrimaryWorkspace,

        JsonRpcSession_RequestAssetAsync,

        SolutionService_GetSolutionAsync,
        SolutionService_UpdatePrimaryWorkspaceAsync,

        SnapshotService_RequestAssetAsync,

        CompilationService_GetCompilationAsync,
        SolutionCreator_AssetDifferences,
        Extension_InfoBar,
        FxCopAnalyzersInstall,
        AssetStorage_ForceGC,
        RemoteHost_Bitness,
        Intellisense_Completion,
        MetadataOnlyImage_EmitFailure,
        LiveTableDataSource_OnDiagnosticsUpdated,
        Experiment_KeybindingsReset,
        Diagnostics_GeneratePerformaceReport,
        Diagnostics_BadAnalyzer,
        CodeAnalysisService_ReportAnalyzerPerformance,
        PerformanceTrackerService_AddSnapshot,
        AbstractProject_SetIntelliSenseBuild,
        AbstractProject_Created,
        AbstractProject_PushedToWorkspace,
        ExternalErrorDiagnosticUpdateSource_AddError,
        DiagnosticIncrementalAnalyzer_SynchronizeWithBuildAsync,
        Completion_ExecuteCommand_TypeChar,
        RemoteHostService_SynchronizeTextAsync,

        SymbolFinder_Solution_Pattern_FindSourceDeclarationsAsync,
        SymbolFinder_Project_Pattern_FindSourceDeclarationsAsync,
        Intellisense_Completion_Commit,

        CodeCleanupInfobar_BarDisplayed,
        CodeCleanupInfobar_ConfigureNow,
        CodeCleanupInfobar_NeverShowCodeCleanupInfoBarAgain,

        FormatDocument,
        CodeCleanup_ApplyCodeFixesAsync,
        CodeCleanup_RemoveUnusedImports,
        CodeCleanup_SortImports,
        CodeCleanup_Format,
        CodeCleanupABTest_AssignedToOnByDefault,
        CodeCleanupABTest_AssignedToOffByDefault,
        Workspace_Events,

        Refactoring_ExtractMethod_UnknownMatrixItem,

        SyntaxTreeIndex_Precalculate,
        SyntaxTreeIndex_Precalculate_Create,
        SymbolTreeInfo_Create,
        SymbolTreeInfo_TryLoadOrCreate,
        CommandHandler_GoToImplementation,
        GraphQuery_ImplementedBy,
        GraphQuery_Implements,
        GraphQuery_IsCalledBy,
        GraphQuery_IsUsedBy,
        GraphQuery_Overrides,

        Intellisense_AsyncCompletion_Data,
        Intellisense_CompletionProviders_Data,
        SnapshotService_IsExperimentEnabledAsync,
        PartialLoad_FullyLoaded,
        Liveshare_UnknownCodeAction,
        Liveshare_LexicalClassifications,
        Liveshare_SyntacticClassifications,
        Liveshare_SyntacticTagger,

        CommandHandler_GoToBase,

        DiagnosticAnalyzerService_GetDiagnosticsForSpanAsync,
        CodeFixes_GetCodeFixesAsync,
    }
}

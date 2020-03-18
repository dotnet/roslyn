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

        WorkCoordinator_DocumentWorker_Enqueue = 2,
        WorkCoordinator_ProcessProjectAsync = 3,
        WorkCoordinator_ProcessDocumentAsync = 4,
        WorkCoordinator_SemanticChange_Enqueue = 5,
        WorkCoordinator_SemanticChange_EnqueueFromMember = 6,
        WorkCoordinator_SemanticChange_EnqueueFromType = 7,
        WorkCoordinator_SemanticChange_FullProjects = 8,
        WorkCoordinator_Project_Enqueue = 9,
        WorkCoordinator_AsyncWorkItemQueue_LastItem = 10,
        WorkCoordinator_AsyncWorkItemQueue_FirstItem = 11,

        Diagnostics_SyntaxDiagnostic = 12,
        Diagnostics_SemanticDiagnostic = 13,
        Diagnostics_ProjectDiagnostic = 14,
        Diagnostics_DocumentReset = 15,
        Diagnostics_DocumentOpen = 16,
        Diagnostics_RemoveDocument = 17,
        Diagnostics_RemoveProject = 18,
        Diagnostics_DocumentClose = 19,

        // add new values after this
        Run_Environment = 20,
        Run_Environment_Options = 21,

        Tagger_AdornmentManager_OnLayoutChanged = 22,
        Tagger_AdornmentManager_UpdateInvalidSpans = 23,
        Tagger_BatchChangeNotifier_NotifyEditorNow = 24,
        Tagger_BatchChangeNotifier_NotifyEditor = 25,
        Tagger_TagSource_RecomputeTags = 26,
        Tagger_TagSource_ProcessNewTags = 27,
        Tagger_SyntacticClassification_TagComputer_GetTags = 28,
        Tagger_SemanticClassification_TagProducer_ProduceTags = 29,
        Tagger_BraceHighlighting_TagProducer_ProduceTags = 30,
        Tagger_LineSeparator_TagProducer_ProduceTags = 31,
        Tagger_Outlining_TagProducer_ProduceTags = 32,
        Tagger_Highlighter_TagProducer_ProduceTags = 33,
        Tagger_ReferenceHighlighting_TagProducer_ProduceTags = 34,

        CaseCorrection_CaseCorrect = 35,
        CaseCorrection_ReplaceTokens = 36,
        CaseCorrection_AddReplacements = 37,

        CodeCleanup_CleanupAsync = 38,
        CodeCleanup_Cleanup = 39,
        CodeCleanup_IterateAllCodeCleanupProviders = 40,
        CodeCleanup_IterateOneCodeCleanup = 41,

        CommandHandler_GetCommandState = 42,
        CommandHandler_ExecuteHandlers = 43,
        CommandHandler_FormatCommand = 44,
        CommandHandler_CompleteStatement = 45,
        CommandHandler_ToggleBlockComment = 46,
        CommandHandler_ToggleLineComment = 47,

        Workspace_SourceText_GetChangeRanges = 48,
        Workspace_Recoverable_RecoverRootAsync = 49,
        Workspace_Recoverable_RecoverRoot = 50,
        Workspace_Recoverable_RecoverTextAsync = 51,
        Workspace_Recoverable_RecoverText = 52,
        Workspace_SkeletonAssembly_GetMetadataOnlyImage = 53,
        Workspace_SkeletonAssembly_EmitMetadataOnlyImage = 54,
        Workspace_Document_State_FullyParseSyntaxTree = 55,
        Workspace_Document_State_IncrementallyParseSyntaxTree = 56,
        Workspace_Document_GetSemanticModel = 57,
        Workspace_Document_GetSyntaxTree = 58,
        Workspace_Document_GetTextChanges = 59,
        Workspace_Project_GetCompilation = 60,
        Workspace_Project_CompilationTracker_BuildCompilationAsync = 61,
        Workspace_ApplyChanges = 62,
        Workspace_TryGetDocument = 63,
        Workspace_TryGetDocumentFromInProgressSolution = 64,
        Workspace_Solution_LinkedFileDiffMergingSession = 65,
        Workspace_Solution_LinkedFileDiffMergingSession_LinkedFileGroup = 66,
        Workspace_Solution_Info = 67,

        EndConstruct_DoStatement = 68,
        EndConstruct_XmlCData = 69,
        EndConstruct_XmlComment = 70,
        EndConstruct_XmlElement = 71,
        EndConstruct_XmlEmbeddedExpression = 72,
        EndConstruct_XmlProcessingInstruction = 73,

        FindReference_Rename = 74,
        FindReference_ChangeSignature = 75,
        FindReference = 76,
        FindReference_DetermineAllSymbolsAsync = 77,
        FindReference_CreateProjectMapAsync = 78,
        FindReference_CreateDocumentMapAsync = 79,
        FindReference_ProcessAsync = 80,
        FindReference_ProcessProjectAsync = 81,
        FindReference_ProcessDocumentAsync = 82,

        LineCommit_CommitRegion = 83,

        Formatting_TokenStreamConstruction = 84,
        Formatting_ContextInitialization = 85,
        Formatting_Format = 86,
        Formatting_ApplyResultToBuffer = 87,
        Formatting_IterateNodes = 88,
        Formatting_CollectIndentBlock = 89,
        Formatting_CollectSuppressOperation = 90,
        Formatting_CollectAlignOperation = 91,
        Formatting_CollectAnchorOperation = 92,
        Formatting_CollectTokenOperation = 93,
        Formatting_BuildContext = 94,
        Formatting_ApplySpaceAndLine = 95,
        Formatting_ApplyAnchorOperation = 96,
        Formatting_ApplyAlignOperation = 97,
        Formatting_AggregateCreateTextChanges = 98,
        Formatting_AggregateCreateFormattedRoot = 99,
        Formatting_CreateTextChanges = 100,
        Formatting_CreateFormattedRoot = 101,
        Formatting_Partitions = 102,

        SmartIndentation_Start = 103,
        SmartIndentation_OpenCurly = 104,
        SmartIndentation_CloseCurly = 105,

        Rename_InlineSession = 106,
        Rename_InlineSession_Session = 107,
        Rename_FindLinkedSpans = 108,
        Rename_GetSymbolRenameInfo = 109,
        Rename_OnTextBufferChanged = 110,
        Rename_ApplyReplacementText = 111,
        Rename_CommitCore = 112,
        Rename_CommitCoreWithPreview = 113,
        Rename_GetAsynchronousLocationsSource = 114,
        Rename_AllRenameLocations = 115,
        Rename_StartSearchingForSpansInAllOpenDocuments = 116,
        Rename_StartSearchingForSpansInOpenDocument = 117,
        Rename_CreateOpenTextBufferManagerForAllOpenDocs = 118,
        Rename_CreateOpenTextBufferManagerForAllOpenDocument = 119,
        Rename_ReportSpan = 120,
        Rename_GetNoChangeConflictResolution = 121,
        Rename_Tracking_BufferChanged = 122,

        TPLTask_TaskScheduled = 123,
        TPLTask_TaskStarted = 124,
        TPLTask_TaskCompleted = 125,

        Get_QuickInfo_Async = 126,

        Completion_ModelComputer_DoInBackground = 127,
        Completion_ModelComputation_FilterModelInBackground = 128,
        Completion_ModelComputation_WaitForModel = 129,
        Completion_SymbolCompletionProvider_GetItemsWorker = 130,
        Completion_KeywordCompletionProvider_GetItemsWorker = 131,
        Completion_SnippetCompletionProvider_GetItemsWorker_CSharp = 132,
        Completion_TypeImportCompletionProvider_GetCompletionItemsAsync = 133,
        Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync = 134,

        SignatureHelp_ModelComputation_ComputeModelInBackground = 135,
        SignatureHelp_ModelComputation_UpdateModelInBackground = 136,

        Refactoring_CodeRefactoringService_GetRefactoringsAsync = 137,
        Refactoring_AddImport = 138,
        Refactoring_FullyQualify = 139,
        Refactoring_GenerateFromMembers_AddConstructorParametersFromMembers = 140,
        Refactoring_GenerateFromMembers_GenerateConstructorFromMembers = 141,
        Refactoring_GenerateFromMembers_GenerateEqualsAndGetHashCode = 142,
        Refactoring_GenerateMember_GenerateConstructor = 143,
        Refactoring_GenerateMember_GenerateDefaultConstructors = 144,
        Refactoring_GenerateMember_GenerateEnumMember = 145,
        Refactoring_GenerateMember_GenerateMethod = 146,
        Refactoring_GenerateMember_GenerateVariable = 147,
        Refactoring_ImplementAbstractClass = 148,
        Refactoring_ImplementInterface = 149,
        Refactoring_IntroduceVariable = 150,
        Refactoring_GenerateType = 151,
        Refactoring_RemoveUnnecessaryImports_CSharp = 152,
        Refactoring_RemoveUnnecessaryImports_VisualBasic = 153,

        Snippet_OnBeforeInsertion = 154,
        Snippet_OnAfterInsertion = 155,

        Misc_NonReentrantLock_BlockingWait = 156,
        Misc_VisualStudioWaitIndicator_Wait = 157,
        Misc_SaveEventsSink_OnBeforeSave = 158,

        TaskList_Refresh = 159,
        TaskList_NavigateTo = 160,

        WinformDesigner_GenerateXML = 161,

        NavigateTo_Search = 162,

        NavigationService_VSDocumentNavigationService_NavigateTo = 163,

        NavigationBar_ComputeModelAsync = 164,
        NavigationBar_ItemService_GetMembersInTypes_CSharp = 165,
        NavigationBar_ItemService_GetTypesInFile_CSharp = 166,
        NavigationBar_UpdateDropDownsSynchronously_WaitForModel = 167,
        NavigationBar_UpdateDropDownsSynchronously_WaitForSelectedItemInfo = 168,

        EventHookup_Determine_If_Event_Hookup = 169,
        EventHookup_Generate_Handler = 170,
        EventHookup_Type_Char = 171,

        Cache_Created = 172,
        Cache_AddOrAccess = 173,
        Cache_Remove = 174,
        Cache_Evict = 175,
        Cache_EvictAll = 176,
        Cache_ItemRank = 177,

        TextStructureNavigator_GetExtentOfWord = 178,
        TextStructureNavigator_GetSpanOfEnclosing = 179,
        TextStructureNavigator_GetSpanOfFirstChild = 180,
        TextStructureNavigator_GetSpanOfNextSibling = 181,
        TextStructureNavigator_GetSpanOfPreviousSibling = 182,

        Debugging_LanguageDebugInfoService_GetDataTipSpanAndText = 183,
        Debugging_VsLanguageDebugInfo_ValidateBreakpointLocation = 184,
        Debugging_VsLanguageDebugInfo_GetProximityExpressions = 185,
        Debugging_VsLanguageDebugInfo_ResolveName = 186,
        Debugging_VsLanguageDebugInfo_GetNameOfLocation = 187,
        Debugging_VsLanguageDebugInfo_GetDataTipText = 188,
        Debugging_EncSession = 189,
        Debugging_EncSession_EditSession = 190,
        Debugging_EncSession_EditSession_EmitDeltaErrorId = 191,
        Debugging_EncSession_EditSession_RudeEdit = 192,

        Simplifier_ReduceAsync = 193,
        Simplifier_ExpandNode = 194,
        Simplifier_ExpandToken = 195,

        ForegroundNotificationService_Processed = 196,
        ForegroundNotificationService_NotifyOnForeground = 197,

        BackgroundCompiler_BuildCompilationsAsync = 198,

        PersistenceService_ReadAsync = 199,
        PersistenceService_WriteAsync = 200,
        PersistenceService_ReadAsyncFailed = 201,
        PersistenceService_WriteAsyncFailed = 202,
        PersistenceService_Initialization = 203,

        TemporaryStorageServiceFactory_ReadText = 204,
        TemporaryStorageServiceFactory_WriteText = 205,
        TemporaryStorageServiceFactory_ReadStream = 206,
        TemporaryStorageServiceFactory_WriteStream = 207,

        PullMembersUpWarning_ChangeTargetToAbstract = 208,
        PullMembersUpWarning_ChangeOriginToPublic = 209,
        PullMembersUpWarning_ChangeOriginToNonStatic = 210,
        PullMembersUpWarning_UserProceedToFinish = 211,
        PullMembersUpWarning_UserGoBack = 212,

        // currently no-one uses these
        SmartTags_RefreshSession = 213,
        SmartTags_SmartTagInitializeFixes = 214,
        SmartTags_ApplyQuickFix = 215,

        EditorTestApp_RefreshTask = 216,
        EditorTestApp_UpdateDiagnostics = 217,

        IncrementalAnalyzerProcessor_Analyzers = 218,
        IncrementalAnalyzerProcessor_Analyzer = 219,
        IncrementalAnalyzerProcessor_ActiveFileAnalyzers = 220,
        IncrementalAnalyzerProcessor_ActiveFileAnalyzer = 221,
        IncrementalAnalyzerProcessor_Shutdown = 222,

        WorkCoordinatorRegistrationService_Register = 223,
        WorkCoordinatorRegistrationService_Unregister = 224,
        WorkCoordinatorRegistrationService_Reanalyze = 225,

        WorkCoordinator_SolutionCrawlerOption = 226,
        WorkCoordinator_PersistentStorageAdded = 227,
        WorkCoordinator_PersistentStorageRemoved = 228,
        WorkCoordinator_Shutdown = 229,

        DiagnosticAnalyzerService_Analyzers = 230,
        DiagnosticAnalyzerDriver_AnalyzerCrash = 231,
        DiagnosticAnalyzerDriver_AnalyzerTypeCount = 232,
        PersistedSemanticVersion_Info = 233,
        StorageDatabase_Exceptions = 234,
        WorkCoordinator_ShutdownTimeout = 235,
        Diagnostics_HyperLink = 236,

        CodeFixes_FixAllOccurrencesSession = 237,
        CodeFixes_FixAllOccurrencesContext = 238,
        CodeFixes_FixAllOccurrencesComputation = 239,
        CodeFixes_FixAllOccurrencesComputation_Document_Diagnostics = 240,
        CodeFixes_FixAllOccurrencesComputation_Project_Diagnostics = 241,
        CodeFixes_FixAllOccurrencesComputation_Document_Fixes = 242,
        CodeFixes_FixAllOccurrencesComputation_Project_Fixes = 243,
        CodeFixes_FixAllOccurrencesComputation_Document_Merge = 244,
        CodeFixes_FixAllOccurrencesComputation_Project_Merge = 245,
        CodeFixes_FixAllOccurrencesPreviewChanges = 246,
        CodeFixes_ApplyChanges = 247,

        SolutionExplorer_AnalyzerItemSource_GetItems = 248,
        SolutionExplorer_DiagnosticItemSource_GetItems = 249,
        WorkCoordinator_ActiveFileEnqueue = 250,
        SymbolFinder_FindDeclarationsAsync = 251,
        SymbolFinder_Project_AddDeclarationsAsync = 252,
        SymbolFinder_Assembly_AddDeclarationsAsync = 253,
        SymbolFinder_Solution_Name_FindSourceDeclarationsAsync = 254,
        SymbolFinder_Project_Name_FindSourceDeclarationsAsync = 255,
        SymbolFinder_Solution_Predicate_FindSourceDeclarationsAsync = 256,
        SymbolFinder_Project_Predicate_FindSourceDeclarationsAsync = 257,
        Tagger_Diagnostics_RecomputeTags = 258,
        Tagger_Diagnostics_Updated = 259,
        SuggestedActions_HasSuggestedActionsAsync = 260,
        SuggestedActions_GetSuggestedActions = 261,
        AnalyzerDependencyCheckingService_LogConflict = 262,
        AnalyzerDependencyCheckingService_LogMissingDependency = 263,
        VirtualMemory_MemoryLow = 264,
        Extension_Exception = 265,

        WorkCoordinator_WaitForHigherPriorityOperationsAsync = 266,

        CSharp_Interactive_Window = 267,
        VisualBasic_Interactive_Window = 268,

        NonFatalWatson = 269,
        GlobalOperationRegistration = 270,
        CommandHandler_FindAllReference = 271,

        CodefixInfobar_Enable = 272,
        CodefixInfobar_EnableAndIgnoreFutureErrors = 273,
        CodefixInfobar_LeaveDisabled = 274,
        CodefixInfobar_ErrorIgnored = 275,

        Refactoring_NamingStyle = 276,

        // Caches
        SymbolTreeInfo_ExceptionInCacheRead = 277,
        SpellChecker_ExceptionInCacheRead = 278,
        BKTree_ExceptionInCacheRead = 279,
        IntellisenseBuild_Failed = 280,

        FileTextLoader_FileLengthThresholdExceeded = 281,

        // Generic performance measurement action IDs
        MeasurePerformance_StartAction = 282,
        MeasurePerformance_StopAction = 283,

        Serializer_CreateChecksum = 284,
        Serializer_Serialize = 285,
        Serializer_Deserialize = 286,

        CodeAnalysisService_CalculateDiagnosticsAsync = 287,
        CodeAnalysisService_SerializeDiagnosticResultAsync = 288,
        CodeAnalysisService_GetReferenceCountAsync = 289,
        CodeAnalysisService_FindReferenceLocationsAsync = 290,
        CodeAnalysisService_FindReferenceMethodsAsync = 291,
        CodeAnalysisService_GetFullyQualifiedName = 292,
        CodeAnalysisService_GetTodoCommentsAsync = 293,
        CodeAnalysisService_GetDesignerAttributesAsync = 294,

        ServiceHubRemoteHostClient_CreateAsync = 295,
        PinnedRemotableDataScope_GetRemotableData = 296,

        RemoteHost_Connect = 297,
        RemoteHost_Disconnect = 298,

        RemoteHostClientService_AddGlobalAssetsAsync = 299,
        RemoteHostClientService_RemoveGlobalAssets = 300,
        RemoteHostClientService_Enabled = 301,
        RemoteHostClientService_Restarted = 302,

        RemoteHostService_SynchronizePrimaryWorkspaceAsync = 303,
        RemoteHostService_SynchronizeGlobalAssetsAsync = 304,

        AssetStorage_CleanAssets = 305,
        AssetStorage_TryGetAsset = 306,

        AssetService_GetAssetAsync = 307,
        AssetService_SynchronizeAssetsAsync = 308,
        AssetService_SynchronizeSolutionAssetsAsync = 309,
        AssetService_SynchronizeProjectAssetsAsync = 310,

        CodeLens_GetReferenceCountAsync = 311,
        CodeLens_FindReferenceLocationsAsync = 312,
        CodeLens_FindReferenceMethodsAsync = 313,
        CodeLens_GetFullyQualifiedName = 314,

        SolutionState_ComputeChecksumsAsync = 315,
        ProjectState_ComputeChecksumsAsync = 316,
        DocumentState_ComputeChecksumsAsync = 317,

        SolutionSynchronizationService_GetRemotableData = 318,
        SolutionSynchronizationServiceFactory_CreatePinnedRemotableDataScopeAsync = 319,

        SolutionChecksumUpdater_SynchronizePrimaryWorkspace = 320,

        JsonRpcSession_RequestAssetAsync = 321,

        SolutionService_GetSolutionAsync = 322,
        SolutionService_UpdatePrimaryWorkspaceAsync = 323,

        SnapshotService_RequestAssetAsync = 324,

        // obsolete: CompilationService_GetCompilationAsync = 325,
        SolutionCreator_AssetDifferences = 326,
        Extension_InfoBar = 327,
        FxCopAnalyzersInstall = 328,
        AssetStorage_ForceGC = 329,
        RemoteHost_Bitness = 330,
        Intellisense_Completion = 331,
        MetadataOnlyImage_EmitFailure = 332,
        LiveTableDataSource_OnDiagnosticsUpdated = 333,
        Experiment_KeybindingsReset = 334,
        Diagnostics_GeneratePerformaceReport = 335,
        Diagnostics_BadAnalyzer = 336,
        CodeAnalysisService_ReportAnalyzerPerformance = 337,
        PerformanceTrackerService_AddSnapshot = 338,
        AbstractProject_SetIntelliSenseBuild = 339,
        AbstractProject_Created = 340,
        AbstractProject_PushedToWorkspace = 341,
        ExternalErrorDiagnosticUpdateSource_AddError = 342,
        DiagnosticIncrementalAnalyzer_SynchronizeWithBuildAsync = 343,
        Completion_ExecuteCommand_TypeChar = 344,
        RemoteHostService_SynchronizeTextAsync = 345,

        SymbolFinder_Solution_Pattern_FindSourceDeclarationsAsync = 346,
        SymbolFinder_Project_Pattern_FindSourceDeclarationsAsync = 347,
        Intellisense_Completion_Commit = 348,

        CodeCleanupInfobar_BarDisplayed = 349,
        CodeCleanupInfobar_ConfigureNow = 350,
        CodeCleanupInfobar_NeverShowCodeCleanupInfoBarAgain = 351,

        FormatDocument = 352,
        CodeCleanup_ApplyCodeFixesAsync = 353,
        CodeCleanup_RemoveUnusedImports = 354,
        CodeCleanup_SortImports = 355,
        CodeCleanup_Format = 356,
        CodeCleanupABTest_AssignedToOnByDefault = 357,
        CodeCleanupABTest_AssignedToOffByDefault = 358,
        Workspace_Events = 359,

        Refactoring_ExtractMethod_UnknownMatrixItem = 360,

        SyntaxTreeIndex_Precalculate = 361,
        SyntaxTreeIndex_Precalculate_Create = 362,
        SymbolTreeInfo_Create = 363,
        SymbolTreeInfo_TryLoadOrCreate = 364,
        CommandHandler_GoToImplementation = 365,
        GraphQuery_ImplementedBy = 366,
        GraphQuery_Implements = 367,
        GraphQuery_IsCalledBy = 368,
        GraphQuery_IsUsedBy = 369,
        GraphQuery_Overrides = 370,

        Intellisense_AsyncCompletion_Data = 371,
        Intellisense_CompletionProviders_Data = 372,
        SnapshotService_IsExperimentEnabledAsync = 373,
        PartialLoad_FullyLoaded = 374,
        Liveshare_UnknownCodeAction = 375,
        Liveshare_LexicalClassifications = 376,
        Liveshare_SyntacticClassifications = 377,
        Liveshare_SyntacticTagger = 378,

        CommandHandler_GoToBase = 379,

        DiagnosticAnalyzerService_GetDiagnosticsForSpanAsync = 380,
        CodeFixes_GetCodeFixesAsync = 381,

        LanguageServer_ActivateFailed = 382,
        LanguageServer_OnLoadedFailed = 383,

        CodeFixes_AddExplicitCast = 384,
    }
}

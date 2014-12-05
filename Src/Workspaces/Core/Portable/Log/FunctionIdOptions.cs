// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FunctionIdOptions
    {
        private const string FeatureName = "Performance/FunctionId";

        private static readonly ConcurrentDictionary<FunctionId, Option<bool>> options =
            new ConcurrentDictionary<FunctionId, Option<bool>>();

        private static readonly Func<FunctionId, Option<bool>> optionGetter =
            id => new Option<bool>(FeatureName, Enum.GetName(typeof(FunctionId), id), defaultValue: GetDefaultValue(id));

        public static Option<bool> GetOption(FunctionId id)
        {
            return options.GetOrAdd(id, optionGetter);
        }

        private static bool GetDefaultValue(FunctionId id)
        {
            switch (id)
            {
                case FunctionId.TestEvent_NotUsed:
                    return false;

                case FunctionId.Tagger_AdornmentManager_OnLayoutChanged:
                case FunctionId.Tagger_AdornmentManager_UpdateInvalidSpans:
                case FunctionId.Tagger_BatchChangeNotifier_NotifyEditorNow:
                case FunctionId.Tagger_BatchChangeNotifier_NotifyEditor:
                case FunctionId.Tagger_TagSource_ProcessNewTags:
                case FunctionId.Tagger_SyntacticClassification_TagComputer_GetTags:
                case FunctionId.Tagger_SemanticClassification_TagProducer_ProduceTags:
                case FunctionId.Tagger_BraceHighlighting_TagProducer_ProduceTags:
                case FunctionId.Tagger_LineSeparator_TagProducer_ProduceTags:
                case FunctionId.Tagger_Outlining_TagProducer_ProduceTags:
                case FunctionId.Tagger_Highlighter_TagProducer_ProduceTags:
                case FunctionId.Tagger_ReferenceHighlighting_TagProducer_ProduceTags:
                    return false;

                case FunctionId.Tagger_Diagnostics_RecomputeTags:
                case FunctionId.Tagger_Diagnostics_Updated:
                    return false;

                case FunctionId.Workspace_SourceText_GetChangeRanges:
                case FunctionId.Workspace_Recoverable_RecoverRootAsync:
                case FunctionId.Workspace_Recoverable_RecoverRoot:
                case FunctionId.Workspace_Recoverable_RecoverTextAsync:
                case FunctionId.Workspace_Recoverable_RecoverText:
                    return false;

                case FunctionId.Misc_NonReentrantLock_BlockingWait:
                    return false;

                case FunctionId.Cache_Created:
                case FunctionId.Cache_AddOrAccess:
                case FunctionId.Cache_Remove:
                case FunctionId.Cache_Evict:
                case FunctionId.Cache_EvictAll:
                case FunctionId.Cache_ItemRank:
                    return false;

                case FunctionId.Simplifier_ReduceAsync:
                case FunctionId.Simplifier_ExpandNode:
                case FunctionId.Simplifier_ExpandToken:
                    return false;

                case FunctionId.TemporaryStorageServiceFactory_ReadText:
                case FunctionId.TemporaryStorageServiceFactory_WriteText:
                case FunctionId.TemporaryStorageServiceFactory_ReadStream:
                case FunctionId.TemporaryStorageServiceFactory_WriteStream:
                    return false;

                case FunctionId.WorkCoordinator_DocumentWorker_Enqueue:
                case FunctionId.WorkCoordinator_ProcessProjectAsync:
                case FunctionId.WorkCoordinator_ProcessDocumentAsync:
                case FunctionId.WorkCoordinator_SemanticChange_Enqueue:
                case FunctionId.WorkCoordinator_SemanticChange_EnqueueFromMember:
                case FunctionId.WorkCoordinator_SemanticChange_EnqueueFromType:
                case FunctionId.WorkCoordinator_SemanticChange_FullProjects:
                case FunctionId.WorkCoordinator_Project_Enqueue:
                case FunctionId.WorkCoordinator_ActivieFileEnqueue:
                    return false;

                case FunctionId.Diagnostics_SyntaxDiagnostic:
                case FunctionId.Diagnostics_SemanticDiagnostic:
                case FunctionId.Diagnostics_ProjectDiagnostic:
                case FunctionId.Diagnostics_DocumentReset:
                case FunctionId.Diagnostics_DocumentOpen:
                case FunctionId.Diagnostics_RemoveDocument:
                case FunctionId.Diagnostics_RemoveProject:
                    return false;

                case FunctionId.SuggestedActions_HasSuggestedActionsAsync:
                case FunctionId.SuggestedActions_GetSuggestedActions:
                    return false;

                default:
                    return true;
            }
        }
    }
}
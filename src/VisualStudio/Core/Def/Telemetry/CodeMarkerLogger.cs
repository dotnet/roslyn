// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.Internal.Performance;
using CodeMarkerId = System.Int32;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal sealed class CodeMarkerLogger : ILogger
    {
        public static readonly CodeMarkerLogger Instance = new CodeMarkerLogger();

        private static readonly Dictionary<FunctionId, List<Tuple<CodeMarkerId, CodeMarkerId>>> s_blockMap
            = new Dictionary<FunctionId, List<Tuple<CodeMarkerId, CodeMarkerId>>>()
            {
                { FunctionId.NavigateTo_Search, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpNavigateToStartSearch, CodeMarkerEvent.perfVSCSharpNavigateToEndSearch),
                        Tuple.Create(CodeMarkerEvent.perfVBNavigateToStartSearch, CodeMarkerEvent.perfVBNavigateToEndSearch),
                    }
                },
                { FunctionId.Rename_InlineSession, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpRenameStart, CodeMarkerEvent.perfVSCSharpRenameEnd)
                    }
                },
                { FunctionId.Rename_FindLinkedSpans, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpRenameFindDefinitionStart, CodeMarkerEvent.perfVSCSharpRenameFindDefinitionEnd)
                    }
                },
                { FunctionId.WinformDesigner_GenerateXML, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpGetXmlStart, CodeMarkerEvent.perfVSCSharpGetXmlEnd)
                    }
                },
                { FunctionId.BackgroundCompiler_BuildCompilationsAsync, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpBatchedRequestsAdded, CodeMarkerEvent.perfVSCSharpBatchedRequestsCompleted),
                        Tuple.Create(CodeMarkerEvent.perfVBCompilerBackgroundThreadStart, CodeMarkerEvent.perfVBCompilerBackgroundThreadStop),
                    }
                },
                { FunctionId.FindReference, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpFindAllReferencesStart, CodeMarkerEvent.perfVSCSharpFindAllReferencesEnd)
                    }
                },
                { FunctionId.SmartTags_SmartTagInitializeFixes, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVBSmartTagInitializeFixesBegin, CodeMarkerEvent.perfVBSmartTagInitializeFixesEnd)
                    }
                },
                { FunctionId.SmartTags_ApplyQuickFix, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVBApplyQuickFixBegin, CodeMarkerEvent.perfVBApplyQuickFixEnd),
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpGenerateTypeNoUIStart, CodeMarkerEvent.perfVSCSharpGenerateTypeNoUIEnd)
                    }
                },
                { FunctionId.LineCommit_CommitRegion, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVBCompilerPrettyListBegin, CodeMarkerEvent.perfVBCompilerPrettyListEnd)
                    }
                },
                { FunctionId.Tagger_Outlining_TagProducer_ProduceTags, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVBCompilerStartOutliningBegin, CodeMarkerEvent.perfVBCompilerStartOutliningEnd)
                    }
                },
                { FunctionId.Tagger_LineSeparator_TagProducer_ProduceTags, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVBCompilerUpdateLineSeparatorsBegin, CodeMarkerEvent.perfVBCompilerUpdateLineSeparatorsEnd)
                    }
                },
                { FunctionId.NavigationBar_ComputeModelAsync, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVBCompilerDropDownLoadBegin, CodeMarkerEvent.perfVBCompilerDropDownLoadEnd)
                    }
                },
                { FunctionId.Completion_ModelComputer_DoInBackground, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpCompletionListStart, CodeMarkerEvent.perfVSCSharpCompletionListEnd),
                        Tuple.Create(CodeMarkerEvent.perfVBCompilerIntellisenseBegin, CodeMarkerEvent.perfVBCompilerIntellisenseEnd)
                    }
                },
                { FunctionId.SignatureHelp_ModelComputation_UpdateModelInBackground, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpParamHelpStart, CodeMarkerEvent.perfVSCSharpParamHelpEnd)
                    }
                },
                { FunctionId.Formatting_Format, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpFormatStart, CodeMarkerEvent.perfVSCSharpFormatEnd)
                    }
                },
                { FunctionId.Formatting_ApplyResultToBuffer, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpCommitStart, CodeMarkerEvent.perfVSCSharpCommitEnd)
                    }
                },
                { FunctionId.SmartTags_RefreshSession, new List<Tuple<CodeMarkerId, CodeMarkerId>>()
                    {
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpIdleSynchronizeInstantaneousSmartTagsStart, CodeMarkerEvent.perfVSCSharpIdleSynchronizeInstantaneousSmartTagsEnd),
                        Tuple.Create(CodeMarkerEvent.perfVSCSharpIdleSynchronizeDelaySmartTagsStart, CodeMarkerEvent.perfVSCSharpIdleSynchronizeDelaySmartTagsEnd)
                    }
                }
            };

        private static readonly Dictionary<FunctionId, List<CodeMarkerId>> s_map
            = new Dictionary<FunctionId, List<CodeMarkerId>>()
            {
                { FunctionId.Rename_InlineSession, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBRenameSymbolEnd } },
                { FunctionId.BackgroundCompiler_BuildCompilationsAsync, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBCompilerReachedBoundState, CodeMarkerEvent.perfVBCompilerReachedCompiledState } },
                { FunctionId.Completion_ModelComputer_DoInBackground, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBIntelliXMLIndexingEnd } },
                { FunctionId.WorkCoordinator_AsyncWorkItemQueue_FirstItem, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBCompilerRegisterDesignViewAttributeBegin, CodeMarkerEvent.perfVBCompilerCommitBegin } },
                { FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBCompilerRegisterDesignViewAttributeEnd, CodeMarkerEvent.perfVBCompilerCommitEnd } },
                { FunctionId.Snippet_OnAfterInsertion, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBInsertSnippetEnd } }
            };

        private static readonly Func<CodeMarkerId, CodeMarkerId> s_getter = i => i;
        private static Func<Tuple<CodeMarkerId, CodeMarkerId>, CodeMarkerId> s_startGetter => t => t.Item1;
        private static Func<Tuple<CodeMarkerId, CodeMarkerId>, CodeMarkerId> s_endGetter => t => t.Item2;

        private CodeMarkerLogger()
        {
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return Microsoft.Internal.Performance.CodeMarkers.Instance.IsEnabled && CanHandle(functionId);
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            FireCodeMarkers(s_map, functionId, s_getter);
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
            FireCodeMarkers(s_blockMap, functionId, s_startGetter);
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            FireCodeMarkers(s_map, functionId, s_getter);
            FireCodeMarkers(s_blockMap, functionId, s_endGetter);
        }

        private static bool CanHandle(FunctionId functionId)
        {
            return s_map.ContainsKey(functionId) || s_blockMap.ContainsKey(functionId);
        }

        private static void FireCodeMarkers<T>(Dictionary<FunctionId, List<T>> map, FunctionId functionId, Func<T, int> getter)
        {
            if (!map.TryGetValue(functionId, out var items))
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var marker = getter(items[i]);
                Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(marker);
            }
        }
    }
}

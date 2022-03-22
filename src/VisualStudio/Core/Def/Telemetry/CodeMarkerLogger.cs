// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            };

        private static readonly Dictionary<FunctionId, List<CodeMarkerId>> s_map
            = new Dictionary<FunctionId, List<CodeMarkerId>>()
            {
                { FunctionId.Rename_InlineSession, new List<CodeMarkerId>() { CodeMarkerEvent.perfVBRenameSymbolEnd } },
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
            => Microsoft.Internal.Performance.CodeMarkers.Instance.IsEnabled && CanHandle(functionId);

        public void Log(FunctionId functionId, LogMessage logMessage)
            => FireCodeMarkers(s_map, functionId, s_getter);

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            => FireCodeMarkers(s_blockMap, functionId, s_startGetter);

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            FireCodeMarkers(s_map, functionId, s_getter);
            FireCodeMarkers(s_blockMap, functionId, s_endGetter);
        }

        private static bool CanHandle(FunctionId functionId)
            => s_map.ContainsKey(functionId) || s_blockMap.ContainsKey(functionId);

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

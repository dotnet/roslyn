// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal static class AsyncCompletionLogger
    {
        private static readonly LogAggregator s_logAggregator = new LogAggregator();

        internal enum ActionInfo
        {
            // For type import completion
            SessionWithTypeImportCompletionEnabled,
            CommitWithTypeImportCompletionEnabled,
            CommitTypeImportCompletionItem,
        }

        internal static void LogSessionWithTypeImportCompletionEnabled() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.SessionWithTypeImportCompletionEnabled);

        internal static void LogCommitWithTypeImportCompletionEnabled() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitWithTypeImportCompletionEnabled);

        internal static void LogCommitTypeImportCompletionItem() =>
            s_logAggregator.IncreaseCount((int)ActionInfo.CommitTypeImportCompletionItem);

        internal static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Intellisense_AsyncCompletion_Data, KeyValueLogMessage.Create(m =>
            {
                foreach (var kv in s_logAggregator)
                {
                    var mergeInfo = ((ActionInfo)kv.Key).ToString("f");
                    m[mergeInfo] = kv.Value.GetCount();
                }
            }));
        }
    }
}

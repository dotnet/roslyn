// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

internal static class OnTheFlyDocsLogger
{
    private static readonly CountLogAggregator<ActionInfo> s_countLogAggregator = new();

    private enum ActionInfo
    {
        ShowedOnTheFlyDocsLink,
        OnTheFlyDocsResultsRequested,
        OnTheFlyDocsResultsRequestedWithDocComments,
    }

    internal static void LogShowedOnTheFlyDocsLink()
        => s_countLogAggregator.IncreaseCount(ActionInfo.ShowedOnTheFlyDocsLink);

    internal static void LogOnTheFlyDocsResultsRequested()
        => s_countLogAggregator.IncreaseCount(ActionInfo.OnTheFlyDocsResultsRequested);

    internal static void LogOnTheFlyDocsResultsRequestedWithDocComments()
        => s_countLogAggregator.IncreaseCount(ActionInfo.OnTheFlyDocsResultsRequestedWithDocComments);

    public static void ReportTelemetry()
    {
        Logger.Log(FunctionId.InheritanceMargin_GetInheritanceMemberItems, KeyValueLogMessage.Create(m =>
        {
            foreach (var kv in s_countLogAggregator)
            {
                m[kv.Key.ToString()] = kv.Value.GetCount();
            }
        }));
    }
}

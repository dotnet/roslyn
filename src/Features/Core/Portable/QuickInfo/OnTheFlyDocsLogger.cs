// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal static class OnTheFlyDocsLogger
{
    private static readonly CountLogAggregator<ActionInfo> s_countLogAggregator = new();

    private enum ActionInfo
    {
        HoveredSourceSymbol,
        HoveredMetadataSymbol,
        ShowedOnTheFlyDocsLink,
        ShowedOnTheFlyDocsLinkWithDocComments,
        OnTheFlyDocsResultsRequested,
        OnTheFlyDocsResultsRequestedWithDocComments,
    }

    internal static void LogHoveredSourceSymbol()
        => s_countLogAggregator.IncreaseCount(ActionInfo.HoveredSourceSymbol);

    internal static void LogHoveredMetadataSymbol()
        => s_countLogAggregator.IncreaseCount(ActionInfo.HoveredMetadataSymbol);

    internal static void LogShowedOnTheFlyDocsLink()
        => s_countLogAggregator.IncreaseCount(ActionInfo.ShowedOnTheFlyDocsLink);

    internal static void LogShowedOnTheFlyDocsLinkWithDocComments()
        => s_countLogAggregator.IncreaseCount(ActionInfo.ShowedOnTheFlyDocsLinkWithDocComments);

    internal static void LogOnTheFlyDocsResultsRequested()
        => s_countLogAggregator.IncreaseCount(ActionInfo.OnTheFlyDocsResultsRequested);

    internal static void LogOnTheFlyDocsResultsRequestedWithDocComments()
        => s_countLogAggregator.IncreaseCount(ActionInfo.OnTheFlyDocsResultsRequestedWithDocComments);

    public static void ReportTelemetry()
    {
        Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Get_Counts, KeyValueLogMessage.Create(static m =>
        {
            foreach (var kv in s_countLogAggregator)
            {
                m[kv.Key.ToString()] = kv.Value.GetCount();
            }
        }));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.InheritanceMargin
{
    internal static class InheritanceMarginLogger
    {
        // 1 sec per bucket, and if it takes more than 1 min, then this log is considered as time-out in the last bucket.
        private static readonly HistogramLogAggregator s_histogramLogAggregator = new(1000, 60000);

        private enum ActionInfo
        {
            GetInheritanceMarginMembers,
        }

        public static void LogGenerateBackgroundInheritanceInfo(TimeSpan elapsedTime)
            => s_histogramLogAggregator.IncreaseCount(
                ActionInfo.GetInheritanceMarginMembers, Convert.ToDecimal(elapsedTime.TotalMilliseconds));

        public static void LogInheritanceTargetsMenuOpen()
            => Logger.Log(FunctionId.InheritanceMargin_TargetsMenuOpen, KeyValueLogMessage.Create(LogType.UserAction));

        public static void LogNavigateToTarget()
            => Logger.Log(FunctionId.InheritanceMargin_NavigateToTarget, KeyValueLogMessage.Create(LogType.UserAction));

        public static void ReportTelemetry()
        {
            Logger.Log(FunctionId.InheritanceMargin_GetInheritanceMemberItems,
                KeyValueLogMessage.Create(
                m =>
                {
                    var histogramLogAggragator = s_histogramLogAggregator.GetValue(ActionInfo.GetInheritanceMarginMembers);
                    if (histogramLogAggragator != null)
                    {
                        m[$"{ActionInfo.GetInheritanceMarginMembers}.BucketSize"] = histogramLogAggragator.BucketSize;
                        m[$"{ActionInfo.GetInheritanceMarginMembers}.BucketCount"] = histogramLogAggragator.BucketCount;
                        m[$"{ActionInfo.GetInheritanceMarginMembers}.Bucket"] = histogramLogAggragator.GetBucketsAsString();
                    }
                }));
        }
    }
}

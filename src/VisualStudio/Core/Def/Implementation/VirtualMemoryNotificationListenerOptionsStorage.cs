// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

internal static class VirtualMemoryNotificationListenerOptionsStorage
{
    public static readonly Option2<bool> BackgroundAnalysisMemoryMonitor = new("dotnet_enable_full_solution_analysis_memory_monitor", defaultValue: true);
}

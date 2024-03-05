// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal static class UnitTestingSolutionCrawlerTimeSpan
{
    public static readonly TimeSpan AllFilesWorkerBackOff = TimeSpan.FromMilliseconds(1500);
    public static readonly TimeSpan EntireProjectWorkerBackOff = TimeSpan.FromMilliseconds(5000);
    public static readonly TimeSpan SemanticChangeBackOff = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan ProjectPropagationBackOff = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan PreviewBackOff = TimeSpan.FromMilliseconds(500);
}

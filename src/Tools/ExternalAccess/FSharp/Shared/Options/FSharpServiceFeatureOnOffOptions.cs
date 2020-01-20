﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Shared.Options
{
    internal static class FSharpServiceFeatureOnOffOptions
    {
        /// <summary>
        /// this option is solely for performance. don't confused by option name. 
        /// this option doesn't mean we will show all diagnostics that belong to opened files when turned off,
        /// rather it means we will only show diagnostics that are cheap to calculate for small scope such as opened files.
        /// </summary>
        [Obsolete("Currently used by F# - should move to the new option SolutionCrawlerOptions.BackgroundAnalysisScopeOption")]
        public static PerLanguageOption<bool?> ClosedFileDiagnostic => SolutionCrawlerOptions.ClosedFileDiagnostic;
    }
}

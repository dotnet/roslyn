// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : ServiceHubServiceBase
    {
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;

        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            // TODO: currently we only use the cache for information that doesn't involve references or packages.
            // Once we move all analysis OOP we will create the full cache.
            _analyzerInfoCache = new DiagnosticAnalyzerInfoCache(ImmutableArray<AnalyzerReference>.Empty);

            StartService();
        }
    }
}

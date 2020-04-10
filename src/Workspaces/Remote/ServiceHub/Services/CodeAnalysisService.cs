// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : ServiceBase
    {
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;

        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _analyzerInfoCache = new DiagnosticAnalyzerInfoCache();

            StartService();
        }
    }
}

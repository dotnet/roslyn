// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService
    {
        public Task SymbolSearch_UpdateContinuouslyAsync(
            string sourceName, string localSettingsDirectory, byte[] solutionChecksum)
        {
            var logService = new LogService(this);
            return RoslynServices.SymbolSearchService.UpdateContinuouslyAsync(
                logService, sourceName, localSettingsDirectory);
        }

        private class LogService : ISymbolSearchLogService
        {
            private readonly CodeAnalysisService _service;

            public LogService(CodeAnalysisService service)
            {
                _service = service;
            }

            public Task LogExceptionAsync(string exception, string text)
                => _service.Rpc.InvokeAsync(nameof(LogExceptionAsync), exception, text);

            public Task LogInfoAsync(string text)
                => _service.Rpc.InvokeAsync(nameof(LogInfoAsync), text);
        }
    }
}
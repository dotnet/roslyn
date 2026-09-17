// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using MSBuildWorkspaceTester.Services;

namespace MSBuildWorkspaceTester.Logging
{
    internal class OutputLoggerProvider : ILoggerProvider
    {
        private readonly OutputService _outputService;

        public OutputLoggerProvider(OutputService outputService)
        {
            _outputService = outputService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new OutputLogger(_outputService);
        }

        public void Dispose() { }
    }
}

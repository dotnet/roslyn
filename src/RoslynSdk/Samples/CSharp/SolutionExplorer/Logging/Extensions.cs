// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using MSBuildWorkspaceTester.Services;

namespace MSBuildWorkspaceTester.Logging
{
    internal static class Extensions
    {
        public static ILoggerFactory AddOutput(this ILoggerFactory loggerFactory, OutputService outputService)
        {
            loggerFactory.AddProvider(new OutputLoggerProvider(outputService));
            return loggerFactory;
        }
    }
}

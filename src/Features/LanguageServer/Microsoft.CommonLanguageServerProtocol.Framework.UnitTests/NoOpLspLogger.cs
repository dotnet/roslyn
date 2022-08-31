// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests
{
    public class NoOpLspLogger : ILspLogger
    {
        public static NoOpLspLogger Instance = new NoOpLspLogger();

        public Task LogErrorAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogExceptionAsync(Exception exception, string? message = null, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogInformationAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogStartContextAsync(string context, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogEndContextAsync(string context, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }
    }
}

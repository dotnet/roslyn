// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework.UnitTests
{
    public class NoOpLspLogger : ILspLogger
    {
        public static NoOpLspLogger Instance = new NoOpLspLogger();

        public Task LogErrorAsync(string message)
        {
            return Task.CompletedTask;
        }

        public Task LogExceptionAsync(Exception exception)
        {
            throw exception;
        }

        public Task LogInformationAsync(string message)
        {
            return Task.CompletedTask;
        }

        public Task LogStartContextAsync(string context)
        {
            return Task.CompletedTask;
        }

        public Task LogEndContextAsync(string context)
        {
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message)
        {
            return Task.CompletedTask;
        }
    }
}

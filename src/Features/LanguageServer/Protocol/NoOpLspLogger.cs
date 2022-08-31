// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class NoOpLspLogger : ILspServiceLogger
    {
        public static readonly ILspServiceLogger Instance = new NoOpLspLogger();

        private NoOpLspLogger() { }

        public Task LogExceptionAsync(Exception exception, string? message = null, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogInformationAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogStartContextAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogEndContextAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }
    }
}

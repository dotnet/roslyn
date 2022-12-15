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

        public void LogException(Exception exception, string? message = null, params object[] @params)
        {
        }

        public void LogInformation(string message, params object[] @params)
        {
        }

        public void LogWarning(string message, params object[] @params)
        {
        }

        public void LogError(string message, params object[] @params)
        {
        }

        public void LogStartContext(string message, params object[] @params)
        {
        }

        public void LogEndContext(string message, params object[] @params)
        {
        }
    }
}

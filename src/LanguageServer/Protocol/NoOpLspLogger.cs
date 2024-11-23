// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class NoOpLspLogger : AbstractLspLogger, ILspService
    {
        public static readonly NoOpLspLogger Instance = new NoOpLspLogger();

        private NoOpLspLogger() { }

        public override void LogDebug(string message, params object[] @params)
        {
        }

        public override void LogException(Exception exception, string? message = null, params object[] @params)
        {
        }

        public override void LogInformation(string message, params object[] @params)
        {
        }

        public override void LogWarning(string message, params object[] @params)
        {
        }

        public override void LogError(string message, params object[] @params)
        {
        }

        public override void LogStartContext(string message, params object[] @params)
        {
        }

        public override void LogEndContext(string message, params object[] @params)
        {
        }
    }
}

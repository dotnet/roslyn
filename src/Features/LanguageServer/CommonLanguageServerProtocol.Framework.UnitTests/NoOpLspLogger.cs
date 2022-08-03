// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommonLanguageServerProtocol.Framework.UnitTests
{
    public class NoOpLspLogger : ILspLogger
    {
        public static NoOpLspLogger Instance = new NoOpLspLogger();

        public void TraceError(string message)
        {
        }

        public void TraceException(Exception exception)
        {
            throw exception;
        }

        public void TraceInformation(string message)
        {
        }

        public void TraceStart(string message)
        {
        }

        public void TraceStop(string message)
        {
        }

        public void TraceWarning(string message)
        {
        }
    }
}

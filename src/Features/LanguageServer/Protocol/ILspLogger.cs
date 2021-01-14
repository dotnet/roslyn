// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface ILspLogger : IDisposable
    {
        void TraceInformation(string message);
    }

    internal class NoOpLspLogger : ILspLogger
    {
        public static readonly ILspLogger Instance = new NoOpLspLogger();

        private NoOpLspLogger()
        {
        }

        public void Dispose()
        {
        }

        public void TraceInformation(string message)
        {
        }
    }
}

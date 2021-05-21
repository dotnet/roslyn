// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RequestShutdownEventArgs : EventArgs
    {
        public string Message { get; }

        public RequestShutdownEventArgs(string message)
        {
            this.Message = message;
        }
    }
}

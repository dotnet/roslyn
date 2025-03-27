// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorAsynchronousOperationListenerWrapper
    {
        private readonly IAsynchronousOperationListener _implementation;

        public RazorAsynchronousOperationListenerWrapper(IAsynchronousOperationListener implementation)
        {
            _implementation = implementation;
        }

        public IDisposable BeginAsyncOperation(string name, object? tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => _implementation.BeginAsyncOperation(name, tag, filePath, lineNumber);
    }
}

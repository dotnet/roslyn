// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Shared.TestHooks;

internal interface IAsynchronousOperationListener : IExpeditableDelaySource
{
    IAsyncToken BeginAsyncOperation(string name, object? tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
}

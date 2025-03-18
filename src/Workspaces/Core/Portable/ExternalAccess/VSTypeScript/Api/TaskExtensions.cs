// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal static class TaskExtensions
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    public static Task CompletesAsyncOperation(this Task task, VSTypeScriptAsyncToken asyncToken)
#pragma warning restore VSTHRD200
        => task.CompletesAsyncOperation(asyncToken.UnderlyingObject);
}

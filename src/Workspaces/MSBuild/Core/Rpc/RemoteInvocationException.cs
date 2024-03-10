// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.MSBuild.Rpc;

internal sealed class RemoteInvocationException : Exception
{
    public RemoteInvocationException(string? message) : base(message)
    {
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

internal sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    public string Message { get; }
    public RequiresUnreferencedCodeAttribute(string message)
    {
        Message = message;
    }
}
#endif
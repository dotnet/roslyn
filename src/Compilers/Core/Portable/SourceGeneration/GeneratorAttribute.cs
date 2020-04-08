// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered a source generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratorAttribute : Attribute
    {
        // https://github.com/dotnet/roslyn/issues/: we don't know if we'll keep this, but for now it lets us re-use the analyzer discovery mechanism
    }
}

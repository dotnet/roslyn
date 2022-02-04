// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ResourceException : Exception
    {
        internal ResourceException(string? name, Exception? inner = null)
            : base(name, inner)
        {
        }
    }
}

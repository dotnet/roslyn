// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Flags]
    internal enum BoundMethodGroupFlags
    {
        None = 0,
        SearchExtensions = 1,

        /// <summary>
        /// Set if the group has a receiver but one was not specified in syntax.
        /// </summary>
        HasImplicitReceiver = 2,
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Flags]
    internal enum BoundMethodGroupFlags
    {
        None = 0,
        SearchExtensionMethods = 1,

        /// <summary>
        /// Set if the group has a receiver but none was not specified in syntax.
        /// </summary>
        HasImplicitReceiver = 2,
    }
}

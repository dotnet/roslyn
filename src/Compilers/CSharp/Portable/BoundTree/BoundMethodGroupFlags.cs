// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

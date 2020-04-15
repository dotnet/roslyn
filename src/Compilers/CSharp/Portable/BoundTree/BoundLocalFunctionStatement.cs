﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundLocalFunctionStatement
    {
        public BoundBlock? Body { get => BlockBody ?? ExpressionBody; }
    }
}

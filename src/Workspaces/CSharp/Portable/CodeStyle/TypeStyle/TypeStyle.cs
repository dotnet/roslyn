﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle
{
    [Flags]
    internal enum UseVarPreference
    {
        None = 0,
        ForBuiltInTypes = 1 << 0,
        WhenTypeIsApparent = 1 << 1,
        Elsewhere = 1 << 2,
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
